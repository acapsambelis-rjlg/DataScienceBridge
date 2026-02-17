# Data Science Workbench

## Overview
The Data Science Workbench is a Windows Forms application developed with Mono (.NET Framework) that offers an integrated Python editor tailored for data science tasks. Its primary purpose is to provide a robust, embeddable environment for data analysis and scripting. The core functionality is encapsulated within a reusable `DataScienceControl` UserControl, allowing for easy integration into larger projects. The current application serves as a comprehensive demonstration of its capabilities, featuring dummy data and a fully functional UI.

## User Preferences
- The DataScienceControl is designed for eventual integration into a larger project
- Current form and data are dummy/demo content

## System Architecture
The application leverages Mono (C# / .NET Framework 4.7.2 compatible) for cross-platform execution on Linux via VNC. Python integration is achieved through subprocess-based execution, facilitating data exchange between .NET and Python environments by streaming data in-memory directly to Python variables. The UI is built using Windows Forms, with the `DataScienceControl` acting as a self-contained, reusable component.

**Key Features:**
- **Integrated Python Editor:** Features include syntax highlighting, line numbers, real-time syntax checking, code snippets, context-aware autocomplete (for dataset columns, class members, DataFrame methods), bracket matching, current line highlighting, word/symbol occurrence highlighting, auto-indentation, block indent/unindent, line duplication/movement, code folding indicators, bookmarks, zoom functionality, and a cursor position indicator. Error squiggles provide visual feedback for syntax errors and undefined names, with detailed tooltips on hover.
- **Data Reference Tab:** A TreeView displays all loaded datasets, their columns, data types, registered Python classes, and context variables. A detail panel provides additional information and example Python code snippets. It includes search and filter capabilities.
- **Package Manager Tab:** Allows users to install and uninstall pip packages within the isolated Python environment. Displays a flat alphabetical list of all installed packages with search/filter capability. Pip commands override Nix's global `PIP_USER=yes` setting via `PIP_USER=0` environment variable.
- **In-memory Data Bridge:** All .NET data is streamed directly into the Python environment as pre-loaded variables, eliminating file I/O for data transfer. Supports recursive flattening of nested `[PythonVisible]` classes into prefixed DataFrame columns (e.g., `Address_City`, `Address_State`). Bitmap/Image properties are automatically serialized as base64 PNG and decoded into PIL Image objects in Python.
- **Virtual Environment Management:** Automatically creates and manages an isolated Python virtual environment in `python/venv/` on first run, pre-installing essential packages like pandas, numpy, and matplotlib. It falls back to the system Python if venv creation fails and includes a mechanism to reset the environment.
- **Plot Viewer:** Supports `plt.show()` from matplotlib, capturing generated plots and displaying them in an interactive viewer with navigation and saving capabilities.
- **Python Class Registration and Context Hub:** Provides APIs (`RegisterPythonClass`, `SetContext`) for the host application to inject Python class definitions and named variables (strings, numbers, booleans, lists, dictionaries) into the Python execution environment. These registered elements are visible in the Data Reference tab and are recognized by the autocomplete and symbol analyzer.
- **Public API (`DataScienceControl`):** Offers methods for loading custom data (`LoadData`), registering in-memory data sources, managing registered Python classes and context variables, executing scripts (`RunScript`), and controlling the editor's content and output. It also provides eventing for status updates and menu strip creation for shortcut key routing.

**UI/UX Decisions:**
- The application adopts a clear, functional UI with distinct tabs for the Python Editor, Data Reference, and Package Manager.
- Output panels are designed with a light mode (white background, dark text) for readability.
- Find & Replace functionality is integrated as an inline panel within the Python editor.

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
- **RegisterPythonClass(className, pythonCode, description, example, notes)** - Register with custom reference info (description, example code, notes shown in Data Reference tab)
- **UnregisterPythonClass(className)** - Remove a registered Python class
- **SetContext(key, value)** - Send a variable to the Python environment (overloads: string, double, int, bool, string[], double[], Dict<string,string>, Dict<string,double>)
- **RemoveContext(key)** - Remove a context variable
- **ClearContext()** - Remove all context variables
- **ResetPythonEnvironment()** - Delete and recreate the virtual environment (python/venv folder)
- **RunScript()** - Execute the current Python script
- **ScriptText** - Get/set the Python editor text
- **OutputText** - Get the output panel text
- **ClearOutput()** - Clear the output panel
- **AppendOutput(text, color)** - Add text to the output panel
- **CreateMenuStrip()** - Returns the control's built-in MenuStrip (File/Edit/Run/Help menus are embedded in the Python Editor tab); host form can assign to MainMenuStrip for shortcut key routing
- **HandleKeyDown(keyCode)** - Forward key events (e.g., F5 to run)
- **StatusChanged event** - Subscribe to status bar updates

## Build & Run

### Visual Studio (Windows)
Open `DataScienceWorkbench.sln` in Visual Studio. Targets .NET Framework 4.7.2.

### Mono (Linux/Replit)
```bash
bash build.sh    # Compile with Mono (mcs)
bash run.sh      # Run the application
```

## External Dependencies
- **System:** Mono runtime, libgdiplus, X11 libraries, gtk2, cairo, pango, fontconfig
- **Python Libraries (Pre-installed in venv):** pandas, numpy, matplotlib, pillow (PIL)

## Recent Changes
- 2026-02-17: Added Bitmap/Image data type support across the full pipeline. Customer model now has a Logo property (32x32 PNG). Bitmaps are serialized as base64 PNG strings (`__IMG__:` prefix) in CSV, automatically decoded to PIL Image objects in Python via preamble code. Data Reference detail panel shows image-specific example code (plt.imshow, numpy conversion). New "Display Images" code snippet shows grid display and pixel analysis. GetPythonTypeName maps Bitmap/Image to "image" type. PIL added to known modules in symbol analyzer.
- 2026-02-16: Data Reference tab now displays nested [PythonVisible] classes hierarchically instead of flat. Sub-class properties are grouped under collapsible nodes (e.g., Address (Address) > Street → Address_Street : string). Clicking a sub-class node shows a detail panel with all its flattened columns, prefix info, and example Python code. Search/filter works across column names, types, descriptions, and nested property names. Sub-class node tags store full path prefix (e.g., "Address_Geo_") for correct deep nesting support.
- 2026-02-16: Renamed `[UserVisible]` attribute to `[PythonVisible]` across both codebases; `UserVisibleHelper` → `PythonVisibleHelper`, `UserVisibleAttribute` → `PythonVisibleAttribute`. File renamed to `PythonVisibleAttribute.cs`. Fixed merge conflict in VS search filter (was calling `GetVisibleProperties` with `fp` variable names).
- 2026-02-16: Implemented recursive flattening of nested [PythonVisible] classes. The [PythonVisible] attribute now supports class-level decoration (AttributeTargets.Class) to signal that a class's properties should be explored and flattened. New FlattenedProperty class and GetFlattenedProperties() method walk the object graph recursively (up to depth 4), producing prefixed column names (e.g., Address_Street, Address_City). Updated CSV serialization, Data Reference tree, field detail panel, autocomplete, and search filter to all use flattened properties. Added [PythonVisible] to Address class and its properties as demo.
- 2026-02-16: Added comprehensive in-app help (Help > Keyboard Shortcuts, Editor Features) and tooltips on key UI elements (buttons, search boxes, editor). Created standalone UserGuide.md for end-user distribution.
- 2026-02-16: Reverted package list to flat alphabetical display (removed grouped categories and owner-drawn rendering). Removed `ListPackagesGrouped()`, `BasePackageNames`, and `PackageListItem` class. Package list now uses standard ListBox with plain strings from `pip list`. Search/filter functionality retained. Fixed pip commands failing inside venv due to Nix's global `PIP_USER=yes` by setting `PIP_USER=0` environment variable override.
- 2026-02-13: Moved File/Edit/Run/Help menus from external CreateMenuStrip() into the Python Editor tab's own embedded MenuStrip (editorMenuBar). CreateMenuStrip() now returns this embedded MenuStrip for host form MainMenuStrip assignment (keyboard shortcut routing). Removed toolbar buttons (Run, Check Syntax, Clear Output) in favor of menu items. Insert Snippet remains as a top-level menu item in the editor bar.