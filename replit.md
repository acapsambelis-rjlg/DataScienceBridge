# Data Science Workbench

## Overview
The Data Science Workbench is a Windows Forms application developed with Mono (.NET Framework) that provides an integrated Python editor for data science tasks. It offers a robust, embeddable environment for data analysis and scripting, encapsulated within a reusable `DataScienceControl` UserControl. The application serves as a comprehensive demonstration of these capabilities, featuring dummy data and a fully functional UI, with a vision to enhance data analysis workflows and support complex data science projects.

## User Preferences
- The DataScienceControl is designed for integration into a larger Telerik-based application (RJLG IntelliSEM)
- Current form and data are dummy/demo content
- Namespaces are intentionally decoupled from file structure to match the host application's namespace hierarchy

## System Architecture
The application uses Mono (C# / .NET Framework 4.7.2 compatible) for cross-platform execution on Linux. Python integration occurs via subprocess-based execution, streaming data in-memory directly to Python variables. The UI is built with Windows Forms, and `DataScienceControl` is a self-contained, reusable component.

**Dual-Project Structure:**
- `src/` — Mono project, stays with standard WinForms controls for Linux/Replit execution
- `DataScienceWorkbench/PythonWorkbench/` — Visual Studio project, migrating to Telerik UI for WinForms controls

**Telerik Migration Status (VS project only):**
- **Phase 1 COMPLETE:** Basic layout/container controls swapped — TabControl→RadPageView, SplitContainer→RadSplitContainer+SplitPanel, Panel→RadPanel, Button→RadButton, Label→RadLabel, MenuStrip→RadMenu, ToolStripMenuItem→RadMenuItem, ContextMenuStrip→RadContextMenu, GroupBox→RadGroupBox, TextBox→RadTextBox, ComboBox→RadDropDownList
- **Phase 2 PENDING:** TreeView→RadTreeView, ListBox→RadListControl
- **Phase 3 PENDING:** RichTextBox migration, SquiggleRichTextBox adaptation

**Namespace Structure (decoupled from file paths — do not change):**
- `RJLG.IntelliSEM.UI.Controls.PythonDataScience` — DataScienceControl, AutoCompletePopup, ErrorSquiggleOverlay (SquiggleRichTextBox), LineNumberPanel, PlotViewerForm, PythonBridge, PythonSyntaxHighlighter, PythonSymbolAnalyzer
- `RJLG.IntelliSEM.Data.PythonDataScience` — DataModels (PythonVisibleAttribute, SampleDataItem, etc.), JsonHelper
- `DataScienceWorkbench` — MainForm, Program (demo app only, not part of reusable control)

Files can be copied directly between `src/` and `DataScienceWorkbench/PythonWorkbench/` for non-Telerik files without namespace adjustments. The VS Designer.cs now uses fully-qualified Telerik types (e.g., `Telerik.WinControls.UI.RadPageView`). The `CreateMenuStrip()` public API now returns `RadMenu` in the VS project (was `MenuStrip`).

**Key Features:**
- **Multi-File Editor:** Supports multiple Python files with a TreeView-based file explorer panel showing the full directory hierarchy of `python/scripts/`. Features persistent storage, per-file state preservation (undo/redo, bookmarks, cursor/scroll position), and cross-file imports. Includes right-click context menu for file operations: New File, New Folder, Rename (inline label editing with validation), Delete (with confirmation), and folder/subfolder creation. Files in subdirectories are fully supported.
- **Integrated Python Editor:** Features syntax highlighting, line numbers, real-time syntax checking, code snippets, context-aware autocomplete (for dataset columns, class members, DataFrame methods), bracket matching, code folding, bookmarks, and error squiggles with tooltips.
- **Data Reference Tab:** Displays loaded datasets, columns, data types, registered Python classes, and context variables in a TreeView, with a detail panel for information and example snippets. Includes search and filter.
- **Package Manager Tab:** Allows installation and uninstallation of pip packages within an isolated Python environment, displaying a flat alphabetical list with search/filter.
- **In-memory Data Bridge:** Streams .NET data to Python via a virtual `DotNetData` module, eliminating file I/O. Supports recursive flattening of nested `[PythonVisible]` classes into prefixed DataFrame columns and serializes Bitmap/Image properties to PIL Image objects in Python.
- **Virtual Environment Management:** Automatically creates and manages an isolated Python virtual environment (`python/venv/`) on first run, pre-installing essential packages like pandas, numpy, and matplotlib. Includes fallback to system Python and a reset mechanism.
- **Plot Viewer:** Captures and displays `matplotlib.pyplot` plots in an interactive viewer with navigation and saving.
- **Python Class Registration and Context Hub:** Provides APIs (`RegisterPythonClass`, `SetContext`) for the host application to inject Python class definitions and named variables (strings, numbers, booleans, lists, dictionaries) into the Python execution environment. These are visible in the Data Reference tab and recognized by autocomplete.
- **Public API (`DataScienceControl`):** Offers methods for `LoadData`, `RegisterInMemoryData`, `RegisterPythonClass`, `SetContext`, `RunScript`, and `ResetPythonEnvironment`, along with properties for editor content and output, and events for status updates.
- **UI/UX Decisions:** Features a clear, functional tabbed UI for editor, data reference, and package manager. Output panels use a light mode, and find/replace is integrated inline within the editor.

## External Dependencies
- **System:** Mono runtime, libgdiplus, X11 libraries, gtk2, cairo, pango, fontconfig
- **Python Libraries (Pre-installed in venv):** pandas, numpy, matplotlib, pillow (PIL)