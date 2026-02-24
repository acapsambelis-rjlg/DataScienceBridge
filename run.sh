#!/bin/bash

GDIPLUS_DIR="/nix/store/mq1lzh05pkdbdgksad4y6s57g62vsskf-libgdiplus-6.1/lib"
export LD_LIBRARY_PATH="${GDIPLUS_DIR}:${LD_LIBRARY_PATH}"
export MONO_PATH="lib:${MONO_PATH}"
export MONO_REGISTRY_PATH="$HOME/.mono/registry"
mkdir -p "$MONO_REGISTRY_PATH"

if [ ! -f DataScienceWorkbench.exe ]; then
    echo "Building..."
    bash build.sh
fi

cleanup() {
    kill $MONO_PID $XVFB_PID 2>/dev/null
    wait $MONO_PID $XVFB_PID 2>/dev/null
}
trap cleanup EXIT INT TERM

pkill -9 -f "mono.*DataScienceWorkbench" 2>/dev/null || true
pkill -9 -f "x11vnc" 2>/dev/null || true
pkill -9 -f "Xvfb :0" 2>/dev/null || true
sleep 0.5
rm -f /tmp/.X11-unix/X0 /tmp/.X0-lock 2>/dev/null || true

echo "Starting Xvfb..."
Xvfb :0 -screen 0 1280x800x24 -ac +extension GLX +render -noreset &
XVFB_PID=$!
sleep 1

if ! kill -0 $XVFB_PID 2>/dev/null; then
    echo "ERROR: Xvfb failed to start"
    exit 1
fi

export DISPLAY=:0

echo "Starting x11vnc on port 5900..."
x11vnc -display :0 -forever -nopw -rfbport 5900 -shared -bg -o /tmp/x11vnc.log
sleep 0.5

echo "Starting Data Science Workbench..."
mono DataScienceWorkbench.exe &
MONO_PID=$!

wait $MONO_PID
