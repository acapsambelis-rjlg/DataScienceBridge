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
src/
  DataModels.cs          - Data classes (Customer, Product, Order, Employee, SensorReading, StockPrice, WebEvent) and DataGenerator
  JsonHelper.cs          - Lightweight JSON serializer (no external dependencies)
  PythonBridge.cs        - Python script execution, pip package management, CSV/JSON export
  DataScienceControl.cs  - Reusable UserControl with Python editor, data browser, output panel, package manager
  MainForm.cs            - Thin host form that contains the DataScienceControl
  Program.cs             - Entry point
build.sh                 - Mono compiler script
run.sh                   - Launch script (Xvfb + x11vnc + mono)
```

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
- **Integrated Python editor** with code snippets (histograms, scatter plots, correlations, time series)
- **Data Browser** tab with DataGridView for browsing all datasets
- **Package Manager** tab for installing/uninstalling pip packages (lazy-loaded on first tab visit)
- **CSV data bridge**: All .NET data exported as CSV for Python pandas consumption

## Build & Run
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
- 2026-02-10: Extracted all functionality from MainForm into DataScienceControl UserControl with public API for reuse
- 2026-02-10: Initial project creation with full data model, Python editor, data browser, and package manager
