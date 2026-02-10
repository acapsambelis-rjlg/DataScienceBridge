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
  PythonBridge.cs                   - Python bridge + syntax checker
  PythonSyntaxHighlighter.cs        - Syntax highlighting + line number panel
src/
  (same .cs files including .Designer.cs) - Used by Mono build on Linux/Replit
build.sh                 - Mono compiler script (for Linux/Replit)
run.sh                   - Launch script (Xvfb + x11vnc + mono, for Linux/Replit)
```
Note: Source files exist in both `src/` (for Mono/Replit) and `DataScienceWorkbench/` (for Visual Studio).
Keep them in sync when making changes.

## DataScienceControl Public API
- **LoadData(...)** - Replace dummy data with custom datasets
- **RunScript()** - Execute the current Python script
- **ScriptText** - Get/set the Python editor text
- **OutputText** - Get the output panel text
- **ClearOutput()** - Clear the output panel
- **AppendOutput(text, color)** - Add text to the output panel
- **CreateMenuStrip()** - Get a MenuStrip with File/Edit/Run/Help menus for the host form
- **HandleKeyDown(keyCode)** - Forward key events (e.g., F5 to run)
- **StatusChanged event** - Subscribe to status bar updates

## Key Features
- **7 dummy datasets**: Products (200), Customers (150), Orders (500), Employees (100), Sensor Readings (1000), Stock Prices (365 days x 10 symbols), Web Events (2000)
- **Integrated Python editor** with syntax highlighting, line numbers, syntax checking, and code snippets
- **Data Browser** tab with DataGridView for browsing all datasets
- **Package Manager** tab for installing/uninstalling pip packages (lazy-loaded on first tab visit)
- **CSV data bridge**: All .NET data exported as CSV for Python pandas consumption

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
- 2026-02-10: Added Python syntax highlighting (keywords, strings, comments, numbers, decorators, builtins), line numbers, and syntax error checking
- 2026-02-10: Added .Designer.cs and .resx files for both DataScienceControl and MainForm (Visual Studio designer support)
- 2026-02-10: Added Visual Studio solution and project files targeting .NET Framework 4.7.2
- 2026-02-10: Extracted all functionality from MainForm into DataScienceControl UserControl with public API for reuse
- 2026-02-10: Initial project creation with full data model, Python editor, data browser, and package manager
