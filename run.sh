#!/bin/bash

export MONO_PATH=/nix/store
export LD_LIBRARY_PATH="$(dirname $(find /nix/store -name 'libgdiplus.so' 2>/dev/null | head -1)):$LD_LIBRARY_PATH"

if [ ! -f DataScienceWorkbench.exe ]; then
    echo "Building..."
    bash build.sh
fi

echo "Starting Data Science Workbench..."
mono DataScienceWorkbench.exe
