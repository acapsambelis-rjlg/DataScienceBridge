# Data Science Workbench

## Overview
The Data Science Workbench is a Windows Forms application (compatible with Mono and .NET Framework 4.7.2) designed to provide an integrated Python editor and environment for data science tasks. Its primary purpose is to enhance data analysis workflows and support complex data science projects through an embeddable and reusable `DataScienceControl` UserControl. The project aims to offer a robust platform for data analysis and scripting.

## User Preferences
- The DataScienceControl is designed for integration into a larger Telerik-based application (RJLG IntelliSEM)
- Current form and data are dummy/demo content
- Namespaces are intentionally decoupled from file structure to match the host application's namespace hierarchy

## System Architecture
The application uses Mono (C# / .NET Framework 4.7.2 compatible) with Python integration achieved via subprocess-based execution, streaming data in-memory directly to Python variables. The UI is built using Windows Forms, with `DataScienceControl` serving as a self-contained, reusable component.

**Dual-Project Structure:**
- `src/`: Mono project using standard WinForms controls for Linux/Replit execution.
- `DataScienceWorkbench/PythonWorkbench/`: Visual Studio project using Telerik UI for WinForms for non-editor UI (RadMenu, RadTreeView, RadPanel, etc.) and `CodeTextBox` for the editor.
- `extern/SyntaxEditorControl/`: Git submodule providing the `CodeTextBox` editor control, shared by both projects.

**Editor Control — CodeTextBox (migration COMPLETE):**
`CodeTextBox` provides syntax highlighting, code folding, completion/autocomplete via `ICompletionProvider`, diagnostics (error squiggles) via `IDiagnosticProvider`/`SetDiagnostics()`, find/replace, bracket matching, auto-indent, tab indent/dedent, undo/redo, line numbers, zoom, and multi-cursor editing. Domain-specific completions are provided by `DataSciencePythonCompletionProvider` (implements `ICompletionProvider`). Extension methods in `SyntaxEditorHelper.cs` bridge CodeTextBox's API with legacy call patterns.

**Key Editor Integration Classes:**
- `DataSciencePythonCompletionProvider`: Context-aware Python completions (keywords, builtins, DataFrame methods, dataset columns, registered classes, dynamic symbols, library members)
- `SyntaxEditorHelper`: Extension methods on `CodeTextBox` for text access, caret/selection manipulation, clipboard, undo/redo
- `PythonSymbolAnalyzer`: Static analysis producing `Diagnostic` objects for the editor
- `IndentFoldingProvider` (from SyntaxEditor): Python indentation-based code folding

**Namespace Structure:**
Namespaces are decoupled from file paths to align with the host application's hierarchy:
- `RJLG.IntelliSEM.UI.Controls.PythonDataScience`
- `RJLG.IntelliSEM.Data.PythonDataScience`
- `DataScienceWorkbench` (for demo app only)

**Core Features:**
- **Multi-File Editor:** Supports multiple Python files with a TreeView-based file explorer, persistent state, and file operations (new, rename, delete).
- **Integrated Python Editor:** Offers syntax highlighting, real-time syntax checking, code snippets, context-aware autocomplete (dataset columns, class members, dynamic package members), bracket matching, code folding, bookmarks, find/replace, and error squiggles. Dynamic symbol loading and introspection from the Python environment enhance autocomplete.
- **Dynamic Module Introspection:** Uses Python's `inspect` module to discover public functions, classes, and submodules of installed packages at runtime, providing version-accurate completions.
- **Dockable Tool Panels:** Files, Output, Data Reference, and Package Manager panels are dockable, repositionable, and hideable via WeifenLuo DockPanel Suite.
- **Data Reference Panel:** Displays loaded datasets, columns, data types, registered Python classes, and context variables.
- **Package Manager Panel:** Facilitates pip package installation and uninstallation within the isolated Python virtual environment.
- **Interactive Script Execution:** Scripts run asynchronously with real-time streaming output, interactive input prompts, and cancellation (Ctrl+C).
- **Run Configurations:** PyCharm-style run configurations allow specifying target scripts, command-line arguments, and input files, persisted in `python/run_configurations.ini`.
- **In-memory Data Bridge:** Streams .NET data to Python via a virtual `DotNetData` module, supporting both fixed (CSV lines to DataFrame) and streaming datasets (lazy row-by-row transfer).
- **Virtual Environment Management:** Automatically creates and manages an isolated Python virtual environment (`python/venv/`) with pre-installed essential packages.
- **Plot Viewer:** Captures and displays `matplotlib.pyplot` plots interactively.
- **Python Class Registration and Context Hub:** Provides APIs for injecting Python class definitions and named variables into the execution environment.
- **Public API (`DataScienceControl`):** Offers methods for data loading, registration, context setting, script execution, and environment reset, along with properties for editor content and output, and status update events.
- **Custom Keyboard Shortcuts:** Includes shortcuts for line manipulation, bookmarking, and navigation.

## External Dependencies
- **System:** Mono runtime, libgdiplus, X11 libraries, gtk2, cairo, pango, fontconfig
- **Python Libraries (Pre-installed in venv):** pandas, numpy, matplotlib, pillow (PIL), scikit-learn