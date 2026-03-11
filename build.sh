#!/bin/bash
set -e

echo "Building SyntaxEditor library..."

REFS="-r:System.Windows.Forms.dll -r:System.Drawing.dll -r:System.dll -r:System.Core.dll"

mcs $REFS \
    -target:library \
    -out:SyntaxEditor.dll \
    -langversion:7 \
    -recurse:extern/SyntaxEditorControl/SyntaxEditor/*.cs

echo "Building Data Science Workbench..."

REFS="-r:System.Windows.Forms.dll -r:System.Drawing.dll -r:System.dll -r:System.Data.dll -r:System.Core.dll -r:System.Xml.dll -r:SyntaxEditor.dll -r:lib/WeifenLuo.WinFormsUI.Docking.dll -r:lib/WeifenLuo.WinFormsUI.Docking.ThemeVS2015.dll"

HELPERS=""
for f in src/Helpers/*.py; do
    [ -f "$f" ] && HELPERS="$HELPERS -resource:$f"
done

mcs $REFS \
    -target:winexe \
    -out:DataScienceWorkbench.exe \
    -langversion:7 \
    $HELPERS \
    src/DataModels.cs \
    src/DataQueue.cs \
    src/RunConfiguration.cs \
    src/JsonHelper.cs \
    src/PythonBridge.cs \
    src/PythonSymbolAnalyzer.cs \
    src/SyntaxEditorHelper.cs \
    src/DataSciencePythonCompletionProvider.cs \
    src/DataSciencePythonTooltipProvider.cs \
    src/DockPanelContent.cs \
    src/DataScienceControl.Designer.cs \
    src/RunConfigurationDialog.cs \
    src/DataScienceControl.cs \
    src/PlotViewerForm.Designer.cs \
    src/PlotViewerForm.cs \
    src/MainForm.Designer.cs \
    src/MainForm.cs \
    src/Program.cs

echo "Build successful! Output: DataScienceWorkbench.exe"
