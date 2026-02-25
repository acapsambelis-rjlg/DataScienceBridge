# Data Science Workbench

## Overview
The Data Science Workbench is a Windows Forms application developed with Mono (.NET Framework 4.7.2 compatible) that provides an integrated Python editor for data science tasks. It offers a robust, embeddable environment for data analysis and scripting, encapsulated within a reusable `DataScienceControl` UserControl. The application serves as a comprehensive demonstration of these capabilities, featuring dummy data and a fully functional UI, with a vision to enhance data analysis workflows and support complex data science projects.

## User Preferences
- The DataScienceControl is designed for integration into a larger Telerik-based application (RJLG IntelliSEM)
- Current form and data are dummy/demo content
- Namespaces are intentionally decoupled from file structure to match the host application's namespace hierarchy

## System Architecture
The application uses Mono (C# / .NET Framework 4.7.2 compatible) for cross-platform execution on Linux. Python integration occurs via subprocess-based execution, streaming data in-memory directly to Python variables. The UI is built with Windows Forms, and `DataScienceControl` is a self-contained, reusable component.

**Dual-Project Structure:**
- `src/` — Mono project, standard WinForms controls for Linux/Replit execution
- `DataScienceWorkbench/PythonWorkbench/` — Visual Studio project, Telerik UI for WinForms controls (non-editor UI only)
- `extern/SyntaxEditorControl/` — Git submodule containing the CodeTextBox editor control (shared by both projects)

**Editor Control — CodeTextBox (from extern/SyntaxEditorControl):**
Both projects now use `CodeEditor.CodeTextBox` as the Python editor, replacing the previous SquiggleRichTextBox (Mono) and RadSyntaxEditor (VS) implementations. CodeTextBox provides built-in:
- Syntax highlighting via `SyntaxRuleset` (using `SyntaxRuleset.CreatePythonRuleset()`)
- Code folding via `IndentFoldingProvider`
- Completion/autocomplete via `ICompletionProvider` interface (implemented by `DataSciencePythonCompletionProvider`)
- Diagnostics (error squiggles) via `SetDiagnostics()`/`ClearDiagnostics()`
- Find/Replace (Ctrl+F / Ctrl+H)
- Bracket matching and auto-close
- Auto-indent on Enter
- Tab indent/dedent (with selection support)
- Built-in undo/redo
- Line numbers
- Ctrl+scroll zoom
- Ctrl+Shift+D duplicate line, Ctrl+Shift+L delete line

**Telerik Migration Status (VS project only):**
- **Phase 1 COMPLETE:** Basic layout/container controls swapped — TabControl→RadPageView, SplitContainer→RadSplitContainer+SplitPanel, Panel→RadPanel, Button→RadButton, Label→RadLabel, MenuStrip→RadMenu, ToolStripMenuItem→RadMenuItem, ContextMenuStrip→RadContextMenu, GroupBox→RadGroupBox, TextBox→RadTextBox, ComboBox→RadDropDownList
- **Phase 2 PARTIAL:** TreeView→RadTreeView COMPLETE (both fileTreeView and refTreeView migrated with NodeFormatting-based styling, ValueValidating for inline rename, RadTreeViewEventArgs for click/double-click events, SelectedNodeChanged for selection). ListBox→RadListControl PENDING.
- **Phase 3 COMPLETE — CodeTextBox Migration:** Replaced RadSyntaxEditor with CodeTextBox from `extern/SyntaxEditorControl`. Eliminated PythonTagger.cs, DiagnosticTagger.cs, AutoCompletePopup.cs, PythonSyntaxHighlighter.cs, ErrorSquiggleOverlay.cs, LineNumberPanel.cs. Custom undo/redo stack removed. SyntaxEditorHelper.cs rewritten as CodeTextBox extension methods. DataSciencePythonCompletionProvider.cs created implementing `ICompletionProvider`.

**Namespace Structure (decoupled from file paths — do not change):**
- `RJLG.IntelliSEM.UI.Controls.PythonDataScience` — DataScienceControl, DataSciencePythonCompletionProvider, SyntaxEditorHelper, PlotViewerForm, PythonBridge, PythonSymbolAnalyzer
- `RJLG.IntelliSEM.Data.PythonDataScience` — DataModels (PythonVisibleAttribute, SampleDataItem, etc.), JsonHelper
- `DataScienceWorkbench` — MainForm, Program (demo app only, not part of reusable control)

Files can be copied directly between `src/` and `DataScienceWorkbench/PythonWorkbench/` for non-Telerik files without namespace adjustments. The VS Designer.cs uses fully-qualified Telerik types for non-editor controls. The `CreateMenuStrip()` public API returns `RadMenu` in the VS project and `MenuStrip` in the Mono project.

**Docking Layout (WeifenLuo DockPanel Suite v3.1.0):**
- `lib/WeifenLuo.WinFormsUI.Docking.dll` + `lib/WeifenLuo.WinFormsUI.Docking.ThemeVS2015.dll` — WeifenLuo DockPanel Suite with VS2015 Light theme
- `src/DockPanelContent.cs` — `ToolDockContent` (draggable tool panels, HideOnClose), `DocumentDockContent` (fixed editor panel), and `DockIcons` (programmatic 16×16 ICO generation for tab icons using BMP XOR/AND format for Mono compatibility)
- Mono project uses DockPanel as root container with `ShowDocumentIcon = true`. The Python editor is a fixed Document-area panel. Files, Output, Data Reference, and Package Manager are all draggable/dockable ToolDockContent panels that can be repositioned, floated, tabbed together, or hidden. All tabs are pinned (docked, not auto-hidden) by default and display relevant icons (folder for Files, terminal for Output, graph for Data Reference, box for Package Manager, code brackets for Editor).
- View menu provides show/hide toggles for all tool panels plus a Reset Layout option.
- `MONO_PATH=lib` in run.sh for runtime assembly resolution. `MONO_REGISTRY_PATH` set to writable path for WeifenLuo's PatchController registry access on Linux.

**Build Process (Mono/Replit):**
- `build.sh` — Two-stage build: first compiles SyntaxEditor.dll from `extern/SyntaxEditorControl/SyntaxEditor/*.cs`, then compiles the main application referencing both SyntaxEditor.dll and WeifenLuo DLLs from `lib/`
- `run.sh` — Sets MONO_PATH and MONO_REGISTRY_PATH, starts Xvfb, x11vnc (port 5900), and runs DataScienceWorkbench.exe via Mono

**Key Features:**
- **Multi-File Editor:** Supports multiple Python files with a TreeView-based file explorer panel showing the full directory hierarchy of `python/scripts/`. Features persistent storage, per-file state preservation (undo/redo, bookmarks, cursor/scroll position), and cross-file imports. Includes right-click context menu for file operations: New File, New Folder, Rename (inline label editing with validation), Delete (with confirmation), and folder/subfolder creation. Files in subdirectories are fully supported.
- **Integrated Python Editor:** Features syntax highlighting, line numbers, real-time syntax checking, code snippets, context-aware autocomplete (for dataset columns, class members, DataFrame methods), bracket matching, code folding, bookmarks, find/replace, and error squiggles with tooltips. PythonSymbolAnalyzer uses dynamic symbol loading — known modules, member names, and magic names are instance fields initialized with sensible defaults, with public API for Add/Remove/Set/LoadModuleSymbols. `LoadSymbolsFromVenv(path)` auto-discovers installed packages from the venv's site-packages directory. Symbols are refreshed automatically when the venv initializes and after each successful pip install.
- **Dockable Tool Panels:** Files, Output, Data Reference, and Package Manager panels are all dockable via WeifenLuo DockPanel Suite. Panels can be dragged, floated, tabbed together, docked to any edge, or hidden/restored via the View menu.
- **Data Reference Panel:** Displays loaded datasets, columns, data types, registered Python classes, and context variables in a TreeView, with a detail panel for information and example snippets. Includes search and filter.
- **Package Manager Panel:** Allows installation and uninstallation of pip packages within an isolated Python environment, displaying a flat alphabetical list with search/filter.
- **In-memory Data Bridge:** Streams .NET data to Python via a virtual `DotNetData` module, eliminating file I/O. Supports recursive flattening of nested `[PythonVisible]` classes into prefixed DataFrame columns and serializes Bitmap/Image properties to PIL Image objects in Python.
- **Virtual Environment Management:** Automatically creates and manages an isolated Python virtual environment (`python/venv/`) on first run, pre-installing essential packages like pandas, numpy, and matplotlib. Includes fallback to system Python and a reset mechanism.
- **Plot Viewer:** Captures and displays `matplotlib.pyplot` plots in an interactive viewer with navigation and saving.
- **Python Class Registration and Context Hub:** Provides APIs (`RegisterPythonClass`, `SetContext`) for the host application to inject Python class definitions and named variables (strings, numbers, booleans, lists, dictionaries) into the Python execution environment. These are visible in the Data Reference tab and recognized by autocomplete.
- **Public API (`DataScienceControl`):** Offers methods for `LoadData`, `RegisterInMemoryData`, `RegisterPythonClass`, `SetContext`, `RunScript`, and `ResetPythonEnvironment`, along with properties for editor content and output, and events for status updates.
- **Custom Keyboard Shortcuts (DataScienceControl):** Ctrl+D (duplicate line), Alt+Up/Down (move line), Ctrl+B (toggle bookmark), F2/Shift+F2 (bookmark navigation). CodeTextBox handles Ctrl+F (find), Ctrl+H (replace), Ctrl+Shift+D (duplicate), Ctrl+Shift+L (delete line), Ctrl+scroll (zoom).

## External Dependencies
- **System:** Mono runtime, libgdiplus, X11 libraries, gtk2, cairo, pango, fontconfig
- **Python Libraries (Pre-installed in venv):** pandas, numpy, matplotlib, pillow (PIL), scikit-learn
