#!/bin/bash
set -e

echo "Building Data Science Workbench..."

REFS="-r:System.Windows.Forms.dll -r:System.Drawing.dll -r:System.dll -r:System.Data.dll -r:System.Core.dll -r:System.Xml.dll"

mcs $REFS \
    -target:winexe \
    -out:DataScienceWorkbench.exe \
    -langversion:7 \
    src/DataModels.cs \
    src/JsonHelper.cs \
    src/PythonBridge.cs \
    src/MainForm.cs \
    src/Program.cs

echo "Build successful! Output: DataScienceWorkbench.exe"
