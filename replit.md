# Data Science Workbench

## Overview
The Data Science Workbench is a Windows Forms application designed to provide an integrated Python editor and environment for data science tasks. Its primary purpose is to enhance data analysis workflows and support complex data science projects through an embeddable and reusable `DataScienceControl` UserControl, offering a robust platform for data analysis and scripting.

## User Preferences
- The DataScienceControl is designed for integration into a larger Telerik-based application (RJLG IntelliSEM)
- Current form and data are dummy/demo content
- Namespaces are intentionally decoupled from file structure to match the host application's namespace hierarchy

## System Architecture
The application uses Mono (C# / .NET Framework 4.7.2 compatible) with Python integration achieved via subprocess-based execution, streaming data in-memory directly to Python variables. The UI is built using Windows Forms, with `DataScienceControl` serving as a self-contained, reusable component.

**Dual-Project Structure:**
- `src/`: Mono project using standard WinForms controls.
- `DataScienceWorkbench/PythonWorkbench/`: Visual Studio project leveraging Telerik UI for WinForms and `CodeTextBox` for the editor.
- `DataScienceWorkbench/PythonWorkbenchData/`: Data layer for models, streaming, and run configurations.
- `extern/SyntaxEditorControl/`: Git submodule for the `CodeTextBox` editor control.

**IMPORTANT:** `DataScienceControl.cs` differs between projects, using standard WinForms in `src/` and Telerik controls in `DataScienceWorkbench/PythonWorkbench/`. Direct copying is not advised.

**Editor Control:**
The `CodeTextBox` editor provides syntax highlighting, code folding, completion/autocomplete (`DataSciencePythonCompletionProvider`), diagnostics, find/replace, bracket matching, auto-indent, and line numbers. Dynamic module introspection enhances completions and tooltips. `TooltipProviderBase` and `DataSciencePythonTooltipProvider` handle Python-specific tooltips including function signatures and docstrings.

**Namespace Structure:**
Namespaces are `RJLG.IntelliSEM.UI.Controls.PythonDataScience`, `RJLG.IntelliSEM.Data.PythonDataScience`, and `DataScienceWorkbench` (for demo).

**Core Features:**
- **Multi-File Editor:** TreeView-based file explorer for managing multiple Python files.
- **Integrated Python Editor:** Features syntax highlighting, real-time syntax checking, snippets, context-aware autocomplete (dataset columns, class members, dynamic package members), code folding, and error squiggles.
- **Dynamic Module Introspection:** Uses Python's `inspect` module for runtime discovery of functions, classes, and submodules in installed packages, providing accurate completions and hover tooltips.
- **Dockable Tool Panels:** Files, Output, Data Reference, and Package Manager panels are dockable via WeifenLuo DockPanel Suite.
- **Data Reference Panel:** Displays loaded datasets, columns, data types, registered Python classes, context variables, and helper functions, with detailed views for complex types like nullable fields, enums, and dictionaries.
- **Package Manager Panel:** Facilitates pip package installation and uninstallation within an isolated Python virtual environment.
- **Interactive Script Execution:** Asynchronous script execution with real-time output, interactive input, and cancellation.
- **Run Configurations:** PyCharm-style run configurations for specifying scripts, arguments, and input files, persisted in `python/run_configurations.ini`.
- **In-memory Data Bridge:** Streams .NET data to Python via a virtual `DotNetData` module, supporting fixed and streaming datasets. Handles serialization of bitmaps, images, dictionaries, and nested objects (`__OBJ__:{...}` JSON prefix) into Python-accessible structures with dot-notation access.
- **Python Bootstrap & Helper Functions:** Embedded Python resources (`dotnet_data_bootstrap.py`, `dotnet_data_streaming.py`) provide core functionality. Helper functions are stored as embedded `.py` files, exported to the `DotNetData` module (e.g., `display_images`, `compare_images`).
- **Toolbar-Only UI:** All actions are accessible via the `editorToolBar`, with no traditional menu bar.
- **Code Snippets:** Dynamically loaded from embedded `.py` files, providing predefined code insertions via a dropdown menu.
- **Virtual Environment Management:** Automatic creation and management of an isolated Python virtual environment.
- **Plot Viewer:** Captures and displays `matplotlib.pyplot` plots interactively.
- **Python Class Registration and Context Hub:** APIs for injecting Python class definitions and named variables.
- **Public API (`DataScienceControl`):** Methods for data loading, registration, context setting, script execution, and environment reset.
- **Custom Keyboard Shortcuts:** For line manipulation and navigation.

## External Dependencies
- **System:** Mono runtime, libgdiplus, X11 libraries, gtk2, cairo, pango, fontconfig
- **Python Libraries (Pre-installed in venv):** pandas, numpy, matplotlib, pillow (PIL), scikit-learn