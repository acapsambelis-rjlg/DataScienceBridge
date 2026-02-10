# Data Science Workbench

## Overview
A Windows Forms application built with Mono (.NET Framework) that provides an integrated Python editor for data science activities. Users can browse complex dummy datasets, write and execute Python scripts against the data, and manage Python packages.

## Architecture
- **Runtime**: Mono (C# / .NET Framework 4.7.2 compatible) on Linux via VNC
- **Python Integration**: subprocess-based execution bridging .NET data to Python via CSV exports
- **UI Framework**: Windows Forms (System.Windows.Forms via Mono)

## Project Structure
```
src/
  DataModels.cs      - Data classes (Customer, Product, Order, Employee, SensorReading, StockPrice, WebEvent) and DataGenerator
  JsonHelper.cs      - Lightweight JSON serializer (no external dependencies)
  PythonBridge.cs    - Python script execution, pip package management, CSV/JSON export
  MainForm.cs        - Main UI with Python editor, data browser, output panel, package manager
  Program.cs         - Entry point
build.sh             - Mono compiler script
run.sh               - Launch script (sets LD_LIBRARY_PATH for libgdiplus)
```

## Key Features
- **7 dummy datasets**: Products (200), Customers (150), Orders (500), Employees (100), Sensor Readings (1000), Stock Prices (365 days x 10 symbols), Web Events (2000)
- **Integrated Python editor** with code snippets (histograms, scatter plots, correlations, time series)
- **Data Browser** tab with DataGridView for browsing all datasets
- **Package Manager** tab for installing/uninstalling pip packages
- **CSV data bridge**: All .NET data exported as CSV for Python pandas consumption

## Build & Run
```bash
bash build.sh    # Compile with Mono (mcs)
bash run.sh      # Run the application
```

## Dependencies
- System: mono, libgdiplus, X11 libs, gtk2, cairo, pango, fontconfig
- Python: pandas, numpy, matplotlib (pre-installed)

## Recent Changes
- 2026-02-10: Initial project creation with full data model, Python editor, data browser, and package manager
