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
- `src/`: Mono project using standard WinForms controls (TreeView, TreeNode, etc.) for Linux/Replit execution.
- `DataScienceWorkbench/PythonWorkbench/`: Visual Studio project — UI controls and editor integration (DataScienceControl, PlotViewerForm, RunConfigurationDialog, PythonBridge, SyntaxEditorHelper, CompletionProvider, etc.). Uses Telerik UI for WinForms for non-editor UI (RadTreeView/RadTreeNode, RadPanel, etc.) and `CodeTextBox` for the editor.
- `DataScienceWorkbench/PythonWorkbenchData/`: Data layer — models, streaming queue, attribute definitions, run configurations (DataModels, DataQueue, PythonVisibleAttribute, RunConfiguration).
- `extern/SyntaxEditorControl/`: Git submodule providing the `CodeTextBox` editor control, shared by both projects.

**IMPORTANT: DataScienceControl.cs differs between projects.** The `src/` version uses standard WinForms TreeView/TreeNode types, while `DataScienceWorkbench/PythonWorkbench/` uses Telerik RadTreeView/RadTreeNode. Do NOT blindly copy DataScienceControl.cs between projects.

**Editor Control — CodeTextBox (migration COMPLETE):**
`CodeTextBox` provides syntax highlighting, code folding, completion/autocomplete via `ICompletionProvider`, diagnostics (error squiggles) via `IDiagnosticProvider`/`SetDiagnostics()`, find/replace, bracket matching, auto-indent, tab indent/dedent, undo/redo, line numbers, zoom, and multi-cursor editing. Domain-specific completions are provided by `DataSciencePythonCompletionProvider` (implements `ICompletionProvider`). Extension methods in `SyntaxEditorHelper.cs` bridge CodeTextBox's API with legacy call patterns.

**Key Editor Integration Classes:**
- `DataSciencePythonCompletionProvider`: Context-aware Python completions (keywords, builtins, DataFrame methods, dataset columns, registered classes, dynamic symbols, library members)
- `TooltipProviderBase` (from SyntaxEditor): Reusable base class implementing `ITooltipProvider` with dictionary-backed tooltip registration, qualified-name lookup (`obj.member`), and management methods (`RegisterTooltip`, `RemoveTooltip`, `ClearTooltips`, `HasTooltip`). Subclass and override `GetTooltip` for custom lookup logic.
- `DataSciencePythonTooltipProvider`: Extends `TooltipProviderBase` with Python-specific logic — auto-extracts function signatures and docstrings from embedded helper `.py` resources and module introspection data. Registered on `CodeTextBox.TooltipProvider`.
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
- **Dynamic Module Introspection:** Uses Python's `inspect` module to discover public functions, classes, and submodules of installed packages at runtime, providing version-accurate completions and hover tooltips with real function signatures and docstrings. The introspection script (`introspect_modules.py`) is embedded as a resource alongside the helper `.py` files; at runtime it is extracted to a temp file, executed, and cleaned up automatically.
- **Dockable Tool Panels:** Files, Output, Data Reference, and Package Manager panels are dockable, repositionable, and hideable via WeifenLuo DockPanel Suite.
- **Data Reference Panel:** Displays loaded datasets, columns, data types (including nullable indicators and enum value listings), registered Python classes, and context variables. `GetPythonTypeName` handles `Nullable<T>` (appends "(nullable)") and enums (lists all values). `ShowFieldDetail` renders dedicated sections for nullable fields and enum values with context-appropriate Python example code.
- **Package Manager Panel:** Facilitates pip package installation and uninstallation within the isolated Python virtual environment.
- **Interactive Script Execution:** Scripts run asynchronously with real-time streaming output, interactive input prompts, and cancellation (Ctrl+C).
- **Run Configurations:** PyCharm-style run configurations allow specifying target scripts, command-line arguments, and input files, persisted in `python/run_configurations.ini`.
- **In-memory Data Bridge:** Streams .NET data to Python via a virtual `DotNetData` module, supporting both fixed (CSV lines to DataFrame) and streaming datasets (lazy row-by-row transfer). Bitmap/Image properties are serialized as base64 PNG with `__IMG__:` prefix and decoded to PIL Images on the Python side. Null bitmaps serialize as empty strings and decode to Python `None`. Both `Bitmap` and base `Image` types are handled (non-Bitmap Images are converted via `new Bitmap(img)`). Corrupt base64 payloads decode gracefully to `None`. `PythonVisibleHelper.PrepareItem` and `ReleaseItem` static delegates (`Action<object>`) allow the host app to perform per-row preparation (e.g., lazy image loading) and cleanup around serialization. Image column metadata is sent in the `__DATASET__` protocol header (`__DATASET__||name||count||imgcol1,imgcol2`) so Python decodes image columns by type metadata rather than value sniffing; falls back to sniffing for backward compatibility.
- **Python Helper Functions:** Helper functions are stored as `.py` files in `Helpers/` directories (`DataScienceWorkbench/PythonWorkbench/Helpers/` for VS, `src/Helpers/` for Mono), compiled as Embedded Resources into the DLL/EXE. At bootstrap, `AppendHelperFunctions()` reads embedded `.py` resources, injects their code into the script, and attaches exported functions to the `DotNetData` module. Each `.py` file declares its exports via a `# exports: func1, func2` header comment. `PythonRunner.BuiltInHelperNames` auto-discovers export names from the embedded resources for autocomplete. Available helpers: `display_images` (image grid), `display_image` (single image), `compare_images` (side-by-side), `image_stats` (RGB statistics). To add new helpers: create a `.py` file with a `# exports:` header, add it as an EmbeddedResource in the `.csproj` (VS) or `-resource:` flag (Mono build).
- **Toolbar-Only UI:** The menu bar (`editorMenuBar`) has been removed. All actions are on the `editorToolBar` (ToolStrip): Config dropdown, Run, Stop, Check, Save, Undo, Redo, Find, Clear Output, Data Ref, Reset Layout, and Snippets dropdown. `CreateMenuStrip()` returns null (kept for API compatibility). `SetupToolbarActions()` wires the Clear Output, Data Ref, and Reset Layout buttons.
- **Code Snippets (Embedded Resources):** Snippet items are loaded dynamically into the `insertSnippetBtn` (`ToolStripDropDownButton` on the toolbar) from `.py` files in `Snippets/` directories (`DataScienceWorkbench/PythonWorkbench/Snippets/` for VS, `src/Snippets/` for Mono), compiled as Embedded Resources. Each `.py` file uses a `# snippet: Menu Label` header to define the menu item text, and an optional `# separator: before` header to insert a menu separator before the item. `SetupSnippetMenu()` discovers all embedded resources whose name contains "Snippets" and ends with ".py", sorts them alphabetically (filenames are numbered `01_`–`13_` for ordering), and builds the dropdown dynamically. To add a new snippet: create a `.py` file with a `# snippet:` header, add it as an EmbeddedResource in the `.csproj` (VS) or the `src/Snippets/` directory (Mono build auto-discovers via glob in `build.sh`).
- **Virtual Environment Management:** Automatically creates and manages an isolated Python virtual environment (`python/venv/`) with pre-installed essential packages.
- **Plot Viewer:** Captures and displays `matplotlib.pyplot` plots interactively.
- **Python Class Registration and Context Hub:** Provides APIs for injecting Python class definitions and named variables into the execution environment.
- **Public API (`DataScienceControl`):** Offers methods for data loading, registration, context setting, script execution, and environment reset, along with properties for editor content and output, and status update events.
- **Custom Keyboard Shortcuts:** Includes shortcuts for line manipulation and navigation.

## External Dependencies
- **System:** Mono runtime, libgdiplus, X11 libraries, gtk2, cairo, pango, fontconfig
- **Python Libraries (Pre-installed in venv):** pandas, numpy, matplotlib, pillow (PIL), scikit-learn