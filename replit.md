# Data Science Workbench

## Overview
A Windows Forms application built with Mono (.NET Framework) that provides an integrated Python editor for data science activities. The core functionality lives in a reusable `DataScienceControl` UserControl, making it easy to embed into larger projects. The current form and dummy data serve as a demonstration.

## Architecture
- **Runtime**: Mono (C# / .NET Framework 4.7.2 compatible) on Linux via VNC
- **Python Integration**: subprocess-based execution bridging .NET data to Python via CSV exports
- **UI Framework**: Windows Forms (System.Windows.Forms via Mono)
- **Reusable Control**: `DataScienceControl` is a self-contained UserControl that can be dropped into any Form

## Project Structure
```
DataScienceWorkbench.sln            - Visual Studio solution file
DataScienceWorkbench/
  DataScienceWorkbench.csproj       - VS project file
  Properties/
    AssemblyInfo.cs                 - Assembly metadata
  DataModels.cs                     - Data classes and DataGenerator
  DataScienceControl.cs             - Reusable UserControl (partial, logic + events)
  DataScienceControl.Designer.cs    - Designer-generated UI layout
  DataScienceControl.resx           - Designer resource file
  JsonHelper.cs                     - JSON serializer
  MainForm.cs                       - Host form (partial, logic)
  MainForm.Designer.cs              - Designer-generated form layout
  MainForm.resx                     - Designer resource file
  Program.cs                        - Entry point
  AutoCompletePopup.cs              - Autocomplete popup for Python keywords/methods
  ErrorSquiggleOverlay.cs           - SquiggleRichTextBox: error squiggles, line highlight, bracket matching
  LineNumberPanel.cs                - Line number gutter control
  PythonBridge.cs                   - Python bridge + syntax checker
  PythonSymbolAnalyzer.cs           - Undefined name detection (symbol cache)
  PlotViewerForm.cs                 - Plot image viewer window (prev/next/save)
  PythonSyntaxHighlighter.cs        - Syntax highlighting engine
src/
  (same .cs files including .Designer.cs) - Used by Mono build on Linux/Replit
build.sh                 - Mono compiler script (for Linux/Replit)
run.sh                   - Launch script (Xvfb + x11vnc + mono, for Linux/Replit)
```
Note: Source files exist in both `src/` (for Mono/Replit) and `DataScienceWorkbench/` (for Visual Studio).
Keep them in sync when making changes.

## DataScienceControl Public API
- **LoadData(customers, employees)** - Replace demo data with custom datasets
- **RegisterInMemoryData(name, values, columnName)** - Register IEnumerable for in-memory piping to Python (no file I/O)
- **RegisterInMemoryData<T>(name, dataProvider)** - Register a Func<List<T>> for in-memory piping (uses reflection, respects [UserVisible] attribute)
- **RegisterInMemoryData(name, dataTableProvider)** - Register a Func<DataTable> for in-memory piping
- **UnregisterInMemoryData(name)** - Remove a registered in-memory data source
- **RegisterPythonClass(className, pythonCode)** - Register a Python class definition injected before every script run
- **UnregisterPythonClass(className)** - Remove a registered Python class
- **SetContext(key, value)** - Send a variable to the Python environment (overloads: string, double, int, bool, string[], double[], Dict<string,string>, Dict<string,double>)
- **RemoveContext(key)** - Remove a context variable
- **ClearContext()** - Remove all context variables
- **RunScript()** - Execute the current Python script
- **ScriptText** - Get/set the Python editor text
- **OutputText** - Get the output panel text
- **ClearOutput()** - Clear the output panel
- **AppendOutput(text, color)** - Add text to the output panel
- **CreateMenuStrip()** - Get a MenuStrip with File/Edit/Run/Help menus for the host form
- **HandleKeyDown(keyCode)** - Forward key events (e.g., F5 to run)
- **StatusChanged event** - Subscribe to status bar updates

## Key Features
- **2 demo datasets**: Customers (150), Employees (100) — demo data for development; host app replaces via LoadData()
- **Integrated Python editor** with syntax highlighting, line numbers, syntax checking, code snippets, context-aware autocomplete (dataset columns, class members, DataFrame methods), bracket matching, current line highlight, auto-indentation, block indent/unindent, line duplicate/move, code folding indicators, and bookmarks
- **Data Reference** tab with TreeView showing all datasets, columns, types, registered classes, and context variables with detail panel and example Python code
- **Package Manager** tab for installing/uninstalling pip packages (lazy-loaded on first tab visit)
- **In-memory data bridge**: All .NET data streamed to Python via stdin as pre-loaded variables (e.g., `customers.CreditLimit.mean()`)

## Build & Run

### Visual Studio (Windows)
Open `DataScienceWorkbench.sln` in Visual Studio. Targets .NET Framework 4.7.2.

### Mono (Linux/Replit)
```bash
bash build.sh    # Compile with Mono (mcs)
bash run.sh      # Run the application
```

## Dependencies
- System: mono, libgdiplus, X11 libs, gtk2, cairo, pango, fontconfig
- Python: pandas, numpy, matplotlib (pre-installed)

## User Preferences
- The DataScienceControl is designed for eventual integration into a larger project
- Current form and data are dummy/demo content

## Recent Changes
- 2026-02-13: Added Ctrl+F shortcut to open Find & Replace dialog from the Python editor. Added search/filter box to Data Reference tab: filters datasets by name, column name/type, and [UserVisible] description text; also filters registered classes and context variables. Uses placeholder text pattern.
- 2026-02-13: Added field-level detail view in Data Reference tab: clicking a field/column node shows the property's [Description] attribute text, type info, parent dataset, and type-appropriate example Python code. Uses System.ComponentModel.DescriptionAttribute on Customer and Employee properties. Falls back to "No description available" for properties without descriptions. Field node tags use string arrays to avoid underscore-in-name parsing issues.
- 2026-02-13: Updated output section to light mode: white background, dark text, adjusted all AppendOutput colors for readability on light backgrounds.
- 2026-02-13: Added [UserVisible] attribute for property-level visibility control: properties marked with [UserVisible] are the only ones serialized to Python, shown in Data Reference, and offered in autocomplete. If no properties are marked, all eligible properties are included (backward compatible). GetColumnsForDataset and GetClassNameForTag now use reflection instead of hardcoded switch statements. AutoCompletePopup dataset columns and PythonSymbolAnalyzer dataset names are now dynamic.
- 2026-02-13: Cleanup sweep: removed unused dataset fields (products, orders, sensorReadings, stockPrices, webEvents), Custom Data Sources UI section, Custom Data Access snippet, demo MainForm registrations (measurements, MathHelper, context vars), dead cases in GetColumnsForDataset/GetExampleCode/GetClassNameForTag/GetRecordCountForTag, unused dataset entries in AutoCompletePopup. Simplified LoadData API to (customers, employees). Made dataGen local to InitializeData.
- 2026-02-13: Replaced Data Browser tab with Data Reference tab: removed DataGridView/dataset combo (Data Browser), promoted Data Reference content (tree view + detail panel) to tab index 1, removed redundant separate Data Reference tab. App now has 3 tabs: Python Editor, Data Reference, Package Manager. Package Manager lazy-load index updated from 3 to 2.
- 2026-02-13: Added plt.show() support: monkey-patches matplotlib's show() to save figures and display them in a PlotViewerForm window with navigation (prev/next) and Save As functionality; all snippets updated to use plt.show() instead of plt.savefig(); PythonResult now includes PlotPaths list
- 2026-02-13: Added Python Class Registration (RegisterPythonClass/UnregisterPythonClass) and Context Hub (SetContext/RemoveContext/ClearContext) systems: host app can register Python classes and send named variables (str, float, int, bool, list, dict) into the Python environment; both appear in Data Reference tree with detail panels; names recognized by autocomplete and symbol analyzer; includes input validation and Python string escaping; demo in MainForm with MathHelper class and sample context vars
- 2026-02-13: Added context-aware dot-completion autocomplete: dataset variables show column names + DataFrame methods, self. shows class methods/attributes, ClassName. shows class members, instance variables infer type from constructor calls. Enhanced symbol analyzer: dataset names (products, customers, etc.) and self/cls no longer produce false squiggles; added global/nonlocal/lambda support and except line skipping
- 2026-02-13: Added error tooltips on squiggle hover: hovering over red (syntax error) or yellow (undefined name) squiggles shows a dark-themed tooltip with the error message; tooltips auto-hide when mouse leaves the error region or errors are cleared
- 2026-02-13: Switched from CSV file bridge to fully in-memory data bridge: all datasets are now top-level Python variables (e.g., `products.Cost.mean()` instead of `dotnet.measurements.values.mean()`). Removed ExportCustomData, ExportAllData, CSV export code, DataExporter class. Updated all snippets, help text, Data Reference tab, and default script.
- 2026-02-13: Added Tab/Shift+Tab block indent/unindent, Ctrl+D line duplicate, Alt+Up/Down line move, code folding indicators in gutter, bookmarks (Ctrl+B toggle, F2/Shift+F2 navigate, click gutter), widened line number panel to 60px
- 2026-02-13: Added Data Reference tab with TreeView showing all datasets, columns, types (including computed properties), and detail panel with example Python code
- 2026-02-10: Added Python symbol analyzer for undefined name detection (yellow squiggly lines), tracks definitions (assignments, def, class, for, import, with, except) and flags undefined references
- 2026-02-10: Added autocomplete popup (Python keywords, builtins, pandas/numpy/matplotlib methods), bracket auto-closing with matching highlight, current line highlighting, and auto-indentation after colon
- 2026-02-10: Added custom undo/redo system (survives syntax highlighting), full Edit menu with keyboard shortcuts, Find & Replace dialog
- 2026-02-10: Added Python syntax highlighting (keywords, strings, comments, numbers, decorators, builtins), line numbers, and syntax error checking
- 2026-02-10: Added .Designer.cs and .resx files for both DataScienceControl and MainForm (Visual Studio designer support)
- 2026-02-10: Added Visual Studio solution and project files targeting .NET Framework 4.7.2
- 2026-02-10: Extracted all functionality from MainForm into DataScienceControl UserControl with public API for reuse
- 2026-02-10: Initial project creation with full data model, Python editor, data browser, and package manager
