#!/bin/bash

GDIPLUS_DIR="/nix/store/mq1lzh05pkdbdgksad4y6s57g62vsskf-libgdiplus-6.1/lib"
export LD_LIBRARY_PATH="${GDIPLUS_DIR}:${LD_LIBRARY_PATH}"

if [ ! -f DataScienceWorkbench.exe ]; then
    echo "Building..."
    bash build.sh
fi

pkill -f "Xvfb" 2>/dev/null || true
pkill -f "x11vnc" 2>/dev/null || true
rm -f /tmp/.X11-unix/X* /tmp/.X*-lock 2>/dev/null || true
sleep 1

echo "Starting Xvfb on display :0..."
Xvfb :0 -screen 0 1280x800x24 &
sleep 1

echo "Starting x11vnc on port 5900..."
x11vnc -display :0 -forever -nopw -rfbport 5900 -shared -bg -o /tmp/x11vnc.log
sleep 1

export DISPLAY=:0

echo "Starting Data Science Workbench..."
exec mono DataScienceWorkbench.exe
