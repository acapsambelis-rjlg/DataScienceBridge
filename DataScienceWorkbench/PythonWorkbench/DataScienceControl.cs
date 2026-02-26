using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using CodeEditor;
using Telerik.WinControls;
using Telerik.WinControls.UI;
using WeifenLuo.WinFormsUI.Docking;
using RJLG.IntelliSEM.Data.PythonDataScience;

namespace RJLG.IntelliSEM.UI.Controls.PythonDataScience
{
    public partial class DataScienceControl : UserControl
    {
        private PythonRunner pythonRunner;
        private List<Customer> customers;
        private List<Employee> employees;

        private ToolDockContent filesDockContent;
        private ToolDockContent outputDockContent;
        private ToolDockContent referenceDockContent;
        private ToolDockContent packagesDockContent;

        private bool packagesLoaded;
        private bool packagesLoading;
        private List<string> allPackageItems = new List<string>();
        private System.Windows.Forms.Timer highlightTimer;
        private bool suppressHighlight;
        private bool textDirty;

        private DataSciencePythonCompletionProvider completionProvider;
        private List<Diagnostic> symbolDiagnostics = new List<Diagnostic>();
        private List<Diagnostic> syntaxDiagnostics = new List<Diagnostic>();
        private PythonSymbolAnalyzer symbolAnalyzer = new PythonSymbolAnalyzer();
        private Dictionary<string, Func<string>> inMemoryDataSources = new Dictionary<string, Func<string>>();
        private Dictionary<string, Type> inMemoryDataTypes = new Dictionary<string, Type>();
        private Dictionary<string, PythonClassInfo> registeredPythonClasses = new Dictionary<string, PythonClassInfo>();
        private Dictionary<string, ContextVariable> contextVariables = new Dictionary<string, ContextVariable>();
        private RadContextMenu fileContextMenu;
        private Dictionary<RadTreeNode, Tuple<Font, Color>> nodeStyles = new Dictionary<RadTreeNode, Tuple<Font, Color>>();
        private float editorFontSize = 10f;
        private const float MinFontSize = 6f;
        private const float MaxFontSize = 28f;
        private const float DefaultFontSize = 10f;
        private Font editorFont;
        private CodeTextBox pythonEditor => activeFile?.Editor;

        private readonly object _pendingUILock = new object();
        private List<Action> _pendingUIActions = new List<Action>();

        private class FileTab
        {
            public string FilePath;
            public string FileName;
            public string Content;
            public int CursorPosition;
            public int ScrollPosition;
            public bool IsModified;
            public CodeTextBox Editor;
            public FileDockContent DockContent;
        }

        private List<FileTab> openFiles = new List<FileTab>();
        private FileTab activeFile;
        private string scriptsDir;
        private int untitledCounter = 0;

        public event EventHandler<string> StatusChanged;

        private void RunOnUIThread(Action action)
        {
            if (!IsHandleCreated)
            {
                lock (_pendingUILock)
                {
                    if (_pendingUIActions != null)
                    {
                        _pendingUIActions.Add(action);
                        return;
                    }
                }
            }
            if (InvokeRequired)
                BeginInvoke(action);
            else
                action();
        }

        private static Font ResolveMonoFont(float size)
        {
            string[] preferred = { "Consolas", "DejaVu Sans Mono", "Liberation Mono", "Courier New" };
            foreach (var name in preferred)
            {
                using (var test = new Font(name, size))
                {
                    if (test.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        return new Font(name, size);
                }
            }
            return new Font(System.Drawing.FontFamily.GenericMonospace, size);
        }

        private static Font ResolveUIFont(float size, FontStyle style = FontStyle.Regular)
        {
            string[] preferred = { "Segoe UI", "DejaVu Sans", "Liberation Sans", "Arial" };
            foreach (var name in preferred)
            {
                using (var test = new Font(name, size, style))
                {
                    if (test.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        return new Font(name, size, style);
                }
            }
            return new Font(System.Drawing.FontFamily.GenericSansSerif, size, style);
        }

        private void SetNodeStyle(RadTreeNode node, Font font, Color foreColor)
        {
            nodeStyles[node] = Tuple.Create(font, foreColor);
        }

        private void SetNodeForeColor(RadTreeNode node, Color foreColor)
        {
            Font existingFont = null;
            Tuple<Font, Color> existing;
            if (nodeStyles.TryGetValue(node, out existing))
                existingFont = existing.Item1;
            nodeStyles[node] = Tuple.Create(existingFont, foreColor);
        }

        private void SetNodeFont(RadTreeNode node, Font font)
        {
            Color existingColor = Color.Empty;
            Tuple<Font, Color> existing;
            if (nodeStyles.TryGetValue(node, out existing))
                existingColor = existing.Item2;
            nodeStyles[node] = Tuple.Create(font, existingColor);
        }

        private void OnTreeNodeFormatting(object sender, TreeNodeFormattingEventArgs e)
        {
            Tuple<Font, Color> style;
            if (nodeStyles.TryGetValue(e.Node, out style))
            {
                if (style.Item1 != null)
                    e.NodeElement.ContentElement.Font = style.Item1;
                else
                    e.NodeElement.ContentElement.ResetValue(LightVisualElement.FontProperty, ValueResetFlags.Local);

                if (style.Item2 != Color.Empty)
                    e.NodeElement.ContentElement.ForeColor = style.Item2;
                else
                    e.NodeElement.ContentElement.ResetValue(LightVisualElement.ForeColorProperty, ValueResetFlags.Local);
            }
            else
            {
                e.NodeElement.ContentElement.ResetValue(LightVisualElement.FontProperty, ValueResetFlags.Local);
                e.NodeElement.ContentElement.ResetValue(LightVisualElement.ForeColorProperty, ValueResetFlags.Local);
            }
        }

        private void ResolveRuntimeFonts()
        {
            editorFont = ResolveMonoFont(10f);
            var monoFont9 = ResolveMonoFont(9f);
            var uiFontBold = ResolveUIFont(9f, FontStyle.Bold);

            outputBox.Font = monoFont9;
            packageListBox.Font = monoFont9;

            outputLabel.Font = uiFontBold;
            pkgListLabel.Font = uiFontBold;
            fileTreeView.Font = ResolveUIFont(9f);
            fileListLabel.Font = uiFontBold;
        }

        private Image folderIcon;
        private Image folderOpenIcon;
        private Image pythonFileIcon;

        private void CreateFileTreeIcons()
        {
            folderIcon = DrawFolderIcon(false);
            folderOpenIcon = DrawFolderIcon(true);
            pythonFileIcon = DrawPythonFileIcon();
        }

        private static Bitmap DrawFolderIcon(bool open)
        {
            var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var body = Color.FromArgb(220, 180, 60);
                var outline = Color.FromArgb(180, 140, 30);
                using (var brush = new SolidBrush(body))
                using (var pen = new Pen(outline, 1f))
                {
                    if (open)
                    {
                        g.FillRectangle(brush, 1, 3, 14, 10);
                        g.DrawRectangle(pen, 1, 3, 13, 9);
                        g.FillRectangle(brush, 1, 1, 6, 3);
                        g.DrawRectangle(pen, 1, 1, 5, 2);
                    }
                    else
                    {
                        g.FillRectangle(brush, 1, 3, 13, 10);
                        g.DrawRectangle(pen, 1, 3, 12, 9);
                        g.FillRectangle(brush, 1, 1, 6, 3);
                        g.DrawRectangle(pen, 1, 1, 5, 2);
                    }
                }
            }
            return bmp;
        }

        private static Bitmap DrawPythonFileIcon()
        {
            var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var pageBrush = new SolidBrush(Color.White))
                using (var pagePen = new Pen(Color.FromArgb(150, 150, 150), 1f))
                {
                    g.FillRectangle(pageBrush, 2, 1, 11, 14);
                    g.DrawRectangle(pagePen, 2, 1, 10, 13);
                    var fold = new Point[] { new Point(9, 1), new Point(13, 5), new Point(9, 5) };
                    g.FillPolygon(pageBrush, fold);
                    g.DrawPolygon(pagePen, fold);
                }
                using (var pyFont = new Font("Arial", 7f, FontStyle.Bold))
                using (var pyBrush = new SolidBrush(Color.FromArgb(55, 118, 171)))
                {
                    g.DrawString("Py", pyFont, pyBrush, 2, 5);
                }
            }
            return bmp;
        }

        public DataScienceControl()
        {
            InitializeComponent();
            InitializeDocking();
            CreateFileTreeIcons();
            ResolveRuntimeFonts();
            InitializeData();
            SetupEditorMenuBar();
            SetupSnippetMenu();
            SetupSyntaxHighlighting();
            RegisterAllDatasetsInMemory();
            PopulateReferenceTree();
            SetupRefSearch();
            SetupPkgSearch();
            SetupTooltips();
            InitializeFileSystem();
            SetupFileListEvents();
            fileTreeView.NodeFormatting += OnTreeNodeFormatting;
            refTreeView.NodeFormatting += OnTreeNodeFormatting;

            this.HandleCreated += (s, e) =>
            {
                filesDockContent.Show(dockPanel, DockState.DockLeftAutoHide);
                outputDockContent.Show(dockPanel, DockState.DockBottomAutoHide);
                packagesDockContent.Show(dockPanel, DockState.DockRightAutoHide);

                List<Action> pending;
                lock (_pendingUILock)
                {
                    pending = _pendingUIActions;
                    _pendingUIActions = null;
                }
                if (pending != null)
                {
                    foreach (var act in pending)
                        BeginInvoke(act);
                }

                var initialActive = activeFile;
                foreach (var tab in openFiles)
                    CreateEditorForTab(tab);

                activeFile = initialActive;
                if (activeFile?.DockContent != null)
                    activeFile.DockContent.Activate();

                BeginInvoke((Action)(() =>
                {
                    ApplySyntaxHighlighting();
                    if (activeFile != null) activeFile.IsModified = false;
                    RefreshFileList();
                    if (activeFile?.Editor != null)
                    {
                        activeFile.Editor.Refresh();
                    }
                }));
            };
        }

        private void InitializeDocking()
        {
            dockPanel.ShowDocumentIcon = true;
            dockPanel.DockLeftPortion = 0.18;
            dockPanel.DockBottomPortion = 0.25;
            dockPanel.DockRightPortion = 0.35;

            filesDockContent = new ToolDockContent();
            filesDockContent.Text = "Files";
            filesDockContent.Icon = DockIcons.CreateFilesIcon();
            filesDockContent.Controls.Add(fileListPanel);

            outputDockContent = new ToolDockContent();
            outputDockContent.Text = "Output";
            outputDockContent.Icon = DockIcons.CreateOutputIcon();
            outputDockContent.Controls.Add(outputPanel);

            referenceDockContent = new ToolDockContent();
            referenceDockContent.Text = "Data Reference";
            referenceDockContent.Icon = DockIcons.CreateReferenceIcon();
            referenceDockContent.CloseButtonVisible = true;
            referenceDockContent.Controls.Add(refPanel);

            packagesDockContent = new ToolDockContent();
            packagesDockContent.Text = "Package Manager";
            packagesDockContent.Icon = DockIcons.CreatePackageIcon();
            packagesDockContent.Controls.Add(pkgPanel);

            dockPanel.ActiveDocumentChanged += (s, e) =>
            {
                var content = dockPanel.ActiveDocument as FileDockContent;
                if (content == null) return;
                var tab = openFiles.Find(f => f.DockContent == content);
                if (tab == null || tab == activeFile) return;
                activeFile = tab;
                if (pythonEditor != null)
                {
                    UpdateCursorPositionStatus();
                    BeginInvoke((Action)(() => { if (pythonEditor != null && !IsDisposed) pythonEditor.Refresh(); }));
                }
                RefreshFileList();
                RaiseStatus("Editing: " + tab.FileName);
            };
            packagesDockContent.DockStateChanged += (s, e) =>
            {
                if (packagesDockContent.DockState != DockState.Hidden &&
                    packagesDockContent.DockState != DockState.Unknown &&
                    !packagesLoaded && !packagesLoading)
                {
                    LoadPackagesAsync();
                }
            };
        }

        private void ShowDockPanel(ToolDockContent panel)
        {
            if (panel.DockState == DockState.Hidden)
                panel.Show(dockPanel);
            else if (panel.DockState == DockState.DockBottomAutoHide)
                panel.DockState = DockState.DockBottom;
            else if (panel.DockState == DockState.DockLeftAutoHide)
                panel.DockState = DockState.DockLeft;
            else if (panel.DockState == DockState.DockRightAutoHide)
                panel.DockState = DockState.DockRight;
            else if (panel.DockState == DockState.DockTopAutoHide)
                panel.DockState = DockState.DockTop;
            panel.Activate();
        }

        private void FloatPanel(ToolDockContent panel)
        {
            panel.Show(dockPanel, DockState.Float);
            panel.Activate();
        }

        private void ResetDockLayout()
        {
            filesDockContent.Show(dockPanel, DockState.DockLeftAutoHide);
            outputDockContent.Show(dockPanel, DockState.DockBottomAutoHide);
            referenceDockContent.DockPanel = null;
            packagesDockContent.Show(dockPanel, DockState.DockRightAutoHide);

            foreach (var tab in openFiles)
                if (tab.DockContent != null && !tab.DockContent.IsDisposed)
                    tab.DockContent.Show(dockPanel, DockState.Document);

            if (activeFile?.DockContent != null)
                activeFile.DockContent.Activate();
        }

        private void SetupSyntaxHighlighting()
        {
            completionProvider = new DataSciencePythonCompletionProvider();
            UpdateDynamicSymbols();

            highlightTimer = new System.Windows.Forms.Timer();
            highlightTimer.Interval = 500;
            highlightTimer.Tick += (s, e) =>
            {
                highlightTimer.Stop();
                if (!textDirty) return;
                textDirty = false;
                RunSymbolAnalysis();
                RunLiveSyntaxCheck();
            };
        }

        private void CreateEditorForTab(FileTab tab)
        {
            var editor = new CodeTextBox();
            editor.Dock = DockStyle.Fill;
            if (editorFont != null) editor.EditorFont = editorFont;
            editor.Ruleset = SyntaxRuleset.CreatePythonRuleset();
            editor.FoldingProvider = new IndentFoldingProvider();
            editor.CompletionProvider = completionProvider;

            tab.Editor = editor;

            var content = new FileDockContent();
            content.Text = tab.IsModified ? "\u2022 " + tab.FileName : tab.FileName;
            content.Icon = DockIcons.CreateEditorIcon();
            content.Controls.Add(editor);
            content.CloseRequested += (s, e) => OnCloseFileTab(tab);
            tab.DockContent = content;

            SetupEditorEvents(tab, editor);

            content.Show(dockPanel, DockState.Document);

            suppressHighlight = true;
            editor.SetText(tab.Content ?? "");
            editor.SetCaretIndex(Math.Min(tab.CursorPosition, (tab.Content ?? "").Length));
            editor.ClearSelection();
            suppressHighlight = false;
            editor.Refresh();
        }

        private void SetupEditorEvents(FileTab tab, CodeTextBox editor)
        {
            editor.TextChanged += (s, e) =>
            {
                if (!suppressHighlight)
                {
                    textDirty = true;
                    highlightTimer.Stop();
                    highlightTimer.Start();

                    if (tab != null && !tab.IsModified)
                    {
                        tab.IsModified = true;
                        if (tab.DockContent != null)
                            tab.DockContent.Text = "\u2022 " + tab.FileName;
                        if (tab.FilePath != null)
                        {
                            var node = FindNodeByPath(fileTreeView.Nodes, tab.FilePath);
                            if (node != null)
                                node.Text = "\u2022 " + tab.FileName;
                        }
                    }
                }
            };

            editor.KeyUp += (s, e) =>
            {
                if (!suppressHighlight && tab == activeFile)
                    UpdateCursorPositionStatus();
            };

            editor.MouseClick += (s, e) =>
            {
                if (!suppressHighlight && tab == activeFile)
                    UpdateCursorPositionStatus();
            };

            editor.KeyDown += (s, e) =>
            {
                if (tab != activeFile) return;
                if (e.Control && e.KeyCode == Keys.D && !e.Shift)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    DuplicateLine();
                }
                else if (e.Alt && (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down))
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    MoveLine(e.KeyCode == Keys.Up);
                }
            };
        }

        private void DuplicateLine()
        {
            if (pythonEditor == null) return;
            suppressHighlight = true;
            pythonEditor.DuplicateLine();
            suppressHighlight = false;

            textDirty = true;
            highlightTimer.Stop();
            highlightTimer.Start();
        }

        private void MoveLine(bool up)
        {
            if (pythonEditor == null) return;
            int pos = pythonEditor.GetCaretIndex();
            int lineIndex = pythonEditor.GetLineFromCharIndex(pos);
            var lines = new List<string>(pythonEditor.GetLines());

            if (up && lineIndex <= 0) return;
            if (!up && lineIndex >= lines.Count - 1) return;

            int swapWith = up ? lineIndex - 1 : lineIndex + 1;
            string temp = lines[lineIndex];
            lines[lineIndex] = lines[swapWith];
            lines[swapWith] = temp;

            int colOffset = pos - pythonEditor.GetFirstCharIndexFromLine(lineIndex);
            string destLine = lines[swapWith];

            suppressHighlight = true;
            pythonEditor.SetText(string.Join("\n", lines));

            int newLineStart = pythonEditor.GetFirstCharIndexFromLine(swapWith);
            int clampedCol = Math.Min(colOffset, destLine.Length);
            pythonEditor.SetCaretIndex(Math.Min(newLineStart + clampedCol, pythonEditor.GetText().Length));
            pythonEditor.ClearSelection();
            suppressHighlight = false;

            textDirty = true;
            highlightTimer.Stop();
            highlightTimer.Start();
        }

        private void UpdateCursorPositionStatus()
        {
            if (pythonEditor == null) return;
            int pos = pythonEditor.GetCaretIndex();
            int line = pythonEditor.GetLineFromCharIndex(pos) + 1;
            int firstChar = pythonEditor.GetFirstCharIndexFromLine(line - 1);
            int col = pos - firstChar + 1;
            int zoomPct = (int)Math.Round(editorFontSize / DefaultFontSize * 100);
            if (zoomPct == 100)
                RaiseStatus("Ln " + line + ", Col " + col);
            else
                RaiseStatus("Ln " + line + ", Col " + col + "  |  Zoom: " + zoomPct + "%");
        }

        private void ApplySyntaxHighlighting()
        {
            RunSymbolAnalysis();
        }

        private void RunSymbolAnalysis()
        {
            if (pythonEditor == null) return;
            try
            {
                string code = pythonEditor.GetText();
                if (string.IsNullOrWhiteSpace(code))
                {
                    symbolDiagnostics.Clear();
                    MergeDiagnostics();
                    return;
                }
                var errors = symbolAnalyzer.Analyze(code);
                symbolDiagnostics.Clear();
                if (errors != null)
                {
                    foreach (var err in errors)
                    {
                        string text = pythonEditor.GetText();
                        int lineIndex = 0;
                        int charCount = 0;
                        string[] allLines = text.Split('\n');
                        for (int i = 0; i < allLines.Length; i++)
                        {
                            if (charCount + allLines[i].Length >= err.StartIndex)
                            {
                                lineIndex = i;
                                break;
                            }
                            charCount += allLines[i].Length + 1;
                        }
                        int col = err.StartIndex - charCount;
                        symbolDiagnostics.Add(new Diagnostic(
                            lineIndex, col, err.Length,
                            err.Message ?? "Symbol issue",
                            DiagnosticSeverity.Warning));
                    }
                }
                MergeDiagnostics();
            }
            catch
            {
                symbolDiagnostics.Clear();
                MergeDiagnostics();
            }
        }

        private void RunLiveSyntaxCheck()
        {
            if (pythonEditor == null) return;
            if (venvInitializing || !pythonRunner.PythonAvailable) return;

            string script = pythonEditor.GetText();
            if (string.IsNullOrWhiteSpace(script))
            {
                syntaxDiagnostics.Clear();
                MergeDiagnostics();
                RaiseStatus("Ready");
                return;
            }

            try
            {
                var result = pythonRunner.CheckSyntax(script);

                if (result.Success)
                {
                    syntaxDiagnostics.Clear();
                    MergeDiagnostics();
                    RaiseStatus("Ready");
                }
                else
                {
                    int errorLine = ParseErrorLine(result.Error);
                    if (errorLine > 0)
                    {
                        var errorMsg = result.Error.Trim();
                        var firstLine = errorMsg.Split('\n')[0];
                        syntaxDiagnostics.Clear();
                        syntaxDiagnostics.Add(new Diagnostic(
                            errorLine - 1, 0, 0,
                            firstLine,
                            DiagnosticSeverity.Error));
                        MergeDiagnostics();
                        RaiseStatus("Line " + errorLine + ": " + firstLine);
                    }
                }
            }
            catch { }
        }

        private void MergeDiagnostics()
        {
            if (pythonEditor == null) return;
            var all = new List<Diagnostic>();
            all.AddRange(symbolDiagnostics);
            all.AddRange(syntaxDiagnostics);
            if (all.Count > 0)
                pythonEditor.SetDiagnostics(all);
            else
                pythonEditor.ClearDiagnostics();
        }

        public void RegisterInMemoryData<T>(string name, Func<List<T>> dataProvider) where T : class
        {
            inMemoryDataTypes[name] = typeof(T);
            inMemoryDataSources[name] = () =>
            {
                var data = dataProvider();
                var flatProps = PythonVisibleHelper.GetFlattenedProperties(typeof(T));
                var sb = new System.Text.StringBuilder();

                var headerParts = new List<string>();
                foreach (var fp in flatProps)
                    headerParts.Add(fp.ColumnName);
                sb.AppendLine(string.Join(",", headerParts));

                foreach (var item in data)
                {
                    var vals = new List<string>();
                    foreach (var fp in flatProps)
                    {
                        var val = fp.GetValue(item);
                        string s;
                        if (PythonVisibleHelper.IsImageType(fp.LeafType) && val is System.Drawing.Bitmap bmp)
                            s = PythonVisibleHelper.BitmapToBase64(bmp);
                        else
                            s = val != null ? val.ToString() : "";
                        if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
                            s = "\"" + s.Replace("\"", "\"\"") + "\"";
                        vals.Add(s);
                    }
                    sb.AppendLine(string.Join(",", vals));
                }
                return sb.ToString();
            };
            UpdateDynamicSymbols();
            PopulateReferenceTree();
        }

        private string BuildPreamble()
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("import sys as _sys, os as _os, tempfile as _tempfile, atexit as _atexit");
            if (scriptsDir != null)
            {
                sb.AppendLine("if r'" + scriptsDir.Replace("'", "\\'") + "' not in _sys.path: _sys.path.insert(0, r'" + scriptsDir.Replace("'", "\\'") + "')");
                sb.AppendLine("__name__ = '__main__'");
            }
            sb.AppendLine("_plot_counter = [0]");
            sb.AppendLine("_plot_dir = _tempfile.mkdtemp(prefix='dsw_plots_')");
            sb.AppendLine("def _patched_show(*args, **kwargs):");
            sb.AppendLine("    import matplotlib.pyplot as _plt");
            sb.AppendLine("    figs = [_plt.figure(i) for i in _plt.get_fignums()]");
            sb.AppendLine("    for fig in figs:");
            sb.AppendLine("        _plot_counter[0] += 1");
            sb.AppendLine("        path = _os.path.join(_plot_dir, f'plot_{_plot_counter[0]}.png')");
            sb.AppendLine("        fig.savefig(path, dpi=150, bbox_inches='tight')");
            sb.AppendLine("        print(f'__PLOT__:{path}')");
            sb.AppendLine("    _plt.close('all')");
            sb.AppendLine("try:");
            sb.AppendLine("    import matplotlib.pyplot as _plt");
            sb.AppendLine("    _plt.show = _patched_show");
            sb.AppendLine("except ImportError:");
            sb.AppendLine("    pass");
            sb.AppendLine();

            foreach (var kvp in registeredPythonClasses)
            {
                sb.AppendLine(kvp.Value.PythonCode);
                sb.AppendLine();
            }

            foreach (var kvp in contextVariables)
            {
                sb.AppendLine(kvp.Key + " = " + kvp.Value.PythonLiteral);
            }

            if (sb.Length > 0)
                sb.AppendLine();

            return sb.ToString();
        }

        private static readonly Dictionary<string, string> CommonImportAliases = new Dictionary<string, string>
        {
            { "pd", "pandas" },
            { "np", "numpy" },
            { "plt", "matplotlib.pyplot" },
            { "sns", "seaborn" },
            { "tf", "tensorflow" },
            { "sk", "sklearn" },
            { "sp", "scipy" },
        };

        private void IntrospectInstalledModules()
        {
            if (pythonRunner == null || !pythonRunner.PythonAvailable) return;

            var modulesToIntrospect = new List<string>();
            foreach (var mod in symbolAnalyzer.KnownModules)
            {
                if (CommonImportAliases.ContainsKey(mod)) continue;
                modulesToIntrospect.Add(mod);
            }
            foreach (var alias in CommonImportAliases)
            {
                modulesToIntrospect.Add(alias.Value);
            }

            var distinct = new HashSet<string>(modulesToIntrospect);

            pythonRunner.IntrospectModulesAsync(distinct, moduleData =>
            {
                RunOnUIThread(() =>
                {
                    if (completionProvider != null)
                    {
                        completionProvider.SetImportAliases(CommonImportAliases);
                        completionProvider.SetModuleCompletions(moduleData);
                    }

                    foreach (var kvp in moduleData)
                    {
                        var symbols = new List<string>(kvp.Value.Functions);
                        symbols.AddRange(kvp.Value.Constants);
                        symbols.AddRange(kvp.Value.Classes.Keys);
                        symbolAnalyzer.LoadModuleSymbols(kvp.Key, symbols);
                    }
                });
            });
        }

        private void UpdateDynamicSymbols()
        {
            var names = new List<string>();
            names.AddRange(registeredPythonClasses.Keys);
            names.AddRange(contextVariables.Keys);
            symbolAnalyzer.SetDynamicKnownSymbols(names);

            var colMap = new Dictionary<string, List<string>>();
            foreach (var kvp in inMemoryDataTypes)
            {
                var flatProps = PythonVisibleHelper.GetFlattenedProperties(kvp.Value);
                var colNames = new List<string>();
                foreach (var fp in flatProps)
                    colNames.Add(fp.ColumnName);
                colMap[kvp.Key] = colNames;
            }
            symbolAnalyzer.SetDatasetColumns(colMap);

            if (completionProvider != null)
            {
                var allNames = new List<string>(names);
                allNames.AddRange(inMemoryDataTypes.Keys);
                completionProvider.SetDynamicSymbols(allNames);
                completionProvider.SetDataSources(colMap);
                completionProvider.SetRegisteredClasses(registeredPythonClasses);
                completionProvider.SetContextVariables(contextVariables);
            }
        }

        private Dictionary<string, string> SerializeInMemoryData()
        {
            var result = new Dictionary<string, string>();
            foreach (var kvp in inMemoryDataSources)
            {
                try
                {
                    result[kvp.Key] = kvp.Value();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Warning: Failed to serialize in-memory data '" + kvp.Key + "': " + ex.Message);
                }
            }
            return result;
        }

        public void ResetPythonEnvironment()
        {
            AppendOutput("Resetting Python environment...\n", Color.FromArgb(0, 100, 180));
            Application.DoEvents();

            pythonRunner.SetupProgress += OnPythonSetupProgress;
            var result = pythonRunner.ResetEnvironment();
            pythonRunner.SetupProgress -= OnPythonSetupProgress;

            if (result.Success)
            {
                AppendOutput(result.Output, Color.FromArgb(0, 128, 0));
                RaiseStatus("Ready (" + pythonRunner.PythonVersion + ", venv)");
            }
            else
            {
                AppendOutput("Reset failed: " + result.Error + "\n", Color.FromArgb(200, 0, 0));
                RaiseStatus("Environment reset failed");
            }
        }

        private void OnPythonSetupProgress(string message)
        {
            RunOnUIThread(() =>
            {
                AppendOutput(message + "\n", Color.FromArgb(100, 100, 100));
                RaiseStatus(message);
            });
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ScriptText
        {
            get { return pythonEditor != null ? pythonEditor.GetText() : activeFile?.Content ?? ""; }
            set
            {
                if (pythonEditor != null)
                    pythonEditor.SetText(value);
                else if (activeFile != null)
                    activeFile.Content = value;
            }
        }

        public void ClearOutput()
        {
            RunOnUIThread(() => outputBox.Clear());
        }

        private void SetupEditorMenuBar()
        {
            var fileMenu = new RadMenuItem("File");

            var newFileItem = new RadMenuItem("New File");
            newFileItem.Click += OnNewFile;
            newFileItem.HintText = "Ctrl+N";
            newFileItem.Shortcuts.Add(new RadShortcut(Keys.Control, Keys.N));
            fileMenu.Items.Add(newFileItem);

            var openFileItem = new RadMenuItem("Open File...");
            openFileItem.Click += OnOpenFile;
            fileMenu.Items.Add(openFileItem);

            var saveItem = new RadMenuItem("Save");
            saveItem.Click += OnSaveFile;
            saveItem.HintText = "Ctrl+S";
            saveItem.Shortcuts.Add(new RadShortcut(Keys.Control, Keys.S));
            fileMenu.Items.Add(saveItem);

            var saveAsItem = new RadMenuItem("Save As...");
            saveAsItem.Click += OnSaveFileAs;
            fileMenu.Items.Add(saveAsItem);
            fileMenu.Items.Add(new RadMenuSeparatorItem());

            var closeItem = new RadMenuItem("Close File");
            closeItem.Click += OnCloseFile;
            closeItem.HintText = "Ctrl+W";
            closeItem.Shortcuts.Add(new RadShortcut(Keys.Control, Keys.W));
            fileMenu.Items.Add(closeItem);

            var editMenu = new RadMenuItem("&Edit");

            var undoItem = new RadMenuItem("Undo");
            undoItem.Click += (s, e) => { if (pythonEditor != null && pythonEditor.ContainsFocus) pythonEditor.PerformUndo(); };
            undoItem.HintText = "Ctrl+Z";
            editMenu.Items.Add(undoItem);

            var redoItem = new RadMenuItem("Redo");
            redoItem.Click += (s, e) => { if (pythonEditor != null && pythonEditor.ContainsFocus) pythonEditor.PerformRedo(); };
            redoItem.HintText = "Ctrl+Y";
            editMenu.Items.Add(redoItem);

            editMenu.Items.Add(new RadMenuSeparatorItem());

            var cutItem = new RadMenuItem("Cut");
            cutItem.Click += (s, e) => { if (pythonEditor != null && pythonEditor.ContainsFocus) pythonEditor.PerformCut(); };
            cutItem.HintText = "Ctrl+X";
            editMenu.Items.Add(cutItem);

            var copyItem = new RadMenuItem("Copy");
            copyItem.Click += (s, e) => { if (pythonEditor != null && pythonEditor.ContainsFocus) pythonEditor.PerformCopy(); };
            copyItem.HintText = "Ctrl+C";
            editMenu.Items.Add(copyItem);

            var pasteItem = new RadMenuItem("Paste");
            pasteItem.Click += (s, e) => { if (pythonEditor != null && pythonEditor.ContainsFocus) pythonEditor.PerformPaste(); };
            pasteItem.HintText = "Ctrl+V";
            editMenu.Items.Add(pasteItem);

            var deleteItem = new RadMenuItem("Delete");
            deleteItem.Click += (s, e) => { if (pythonEditor != null && pythonEditor.ContainsFocus && pythonEditor.SelectionLength > 0) pythonEditor.DeleteSelectionText(); };
            deleteItem.HintText = "Del";
            editMenu.Items.Add(deleteItem);

            editMenu.Items.Add(new RadMenuSeparatorItem());

            var selectAllItem = new RadMenuItem("Select All");
            selectAllItem.Click += (s, e) => { if (pythonEditor != null && pythonEditor.ContainsFocus) pythonEditor.PerformSelectAll(); };
            selectAllItem.HintText = "Ctrl+A";
            editMenu.Items.Add(selectAllItem);

            editMenu.Items.Add(new RadMenuSeparatorItem());

            var findItem = new RadMenuItem("Find && Replace...");
            findItem.Click += (s, e) => pythonEditor?.ShowReplace();
            findItem.HintText = "Ctrl+H";
            editMenu.Items.Add(findItem);

            editMenu.Items.Add(new RadMenuSeparatorItem());

            var dupLineItem = new RadMenuItem("Duplicate Line");
            dupLineItem.Click += (s, e) => { if (pythonEditor != null && pythonEditor.ContainsFocus) DuplicateLine(); };
            dupLineItem.HintText = "Ctrl+D";
            editMenu.Items.Add(dupLineItem);

            var moveUpItem = new RadMenuItem("Move Line Up");
            moveUpItem.Click += (s, e) => { if (pythonEditor != null && pythonEditor.ContainsFocus) MoveLine(true); };
            moveUpItem.HintText = "Alt+Up";
            editMenu.Items.Add(moveUpItem);

            var moveDownItem = new RadMenuItem("Move Line Down");
            moveDownItem.Click += (s, e) => { if (pythonEditor != null && pythonEditor.ContainsFocus) MoveLine(false); };
            moveDownItem.HintText = "Alt+Down";
            editMenu.Items.Add(moveDownItem);

            editMenu.Items.Add(new RadMenuSeparatorItem());

            var clearOutputItem = new RadMenuItem("Clear Output");
            clearOutputItem.Click += (s, e) => ClearOutput();
            editMenu.Items.Add(clearOutputItem);

            editMenu.DropDownOpening += (s, e) =>
            {
                bool editorFocused = pythonEditor != null && pythonEditor.ContainsFocus;
                undoItem.Enabled = editorFocused;
                redoItem.Enabled = editorFocused;
                bool hasSelection = editorFocused && pythonEditor.SelectionLength > 0;
                cutItem.Enabled = hasSelection;
                copyItem.Enabled = hasSelection;
                deleteItem.Enabled = hasSelection;
            };

            var viewMenu = new RadMenuItem("&View");
            var showFilesItem = new RadMenuItem("Files Panel");
            showFilesItem.Click += (s, e) => ShowDockPanel(filesDockContent);
            viewMenu.Items.Add(showFilesItem);
            var showOutputItem = new RadMenuItem("Output Panel");
            showOutputItem.Click += (s, e) => ShowDockPanel(outputDockContent);
            viewMenu.Items.Add(showOutputItem);
            var showRefItem = new RadMenuItem("Data Reference");
            showRefItem.Click += (s, e) => FloatPanel(referenceDockContent);
            viewMenu.Items.Add(showRefItem);
            var showPkgItem = new RadMenuItem("Package Manager");
            showPkgItem.Click += (s, e) => ShowDockPanel(packagesDockContent);
            viewMenu.Items.Add(showPkgItem);
            viewMenu.Items.Add(new RadMenuSeparatorItem());
            var resetLayoutItem = new RadMenuItem("Reset Layout");
            resetLayoutItem.Click += (s, e) => ResetDockLayout();
            viewMenu.Items.Add(resetLayoutItem);

            var helpMenu = new RadMenuItem("Help");
            var quickStartItem = new RadMenuItem("Quick Start Guide");
            quickStartItem.Click += OnShowHelp;
            helpMenu.Items.Add(quickStartItem);
            var resetEnvItem = new RadMenuItem("Reset Python Environment");
            resetEnvItem.Click += (s, e) =>
            {
                var confirm = MessageBox.Show(
                    "This will delete the virtual environment and recreate it.\nAll installed packages will need to be reinstalled.\n\nContinue?",
                    "Reset Python Environment", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm == DialogResult.Yes)
                    ResetPythonEnvironment();
            };
            helpMenu.Items.Add(resetEnvItem);
            var shortcutsItem = new RadMenuItem("Keyboard Shortcuts");
            shortcutsItem.Click += OnShowKeyboardShortcuts;
            helpMenu.Items.Add(shortcutsItem);
            var editorFeaturesItem = new RadMenuItem("Editor Features");
            editorFeaturesItem.Click += OnShowEditorFeatures;
            helpMenu.Items.Add(editorFeaturesItem);
            helpMenu.Items.Add(new RadMenuSeparatorItem());
            var aboutItem = new RadMenuItem("About");
            aboutItem.Click += (s, e) => MessageBox.Show(
                "Data Science Workbench v1.0\n\n" +
                "A .NET Windows Forms control with\n" +
                "integrated Python scripting for data analysis.\n\n" +
                "Built with Mono + Python 3",
                "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
            helpMenu.Items.Add(aboutItem);

            editorMenuBar.Items.Insert(0, fileMenu);
            editorMenuBar.Items.Insert(1, editMenu);
            editorMenuBar.Items.Insert(2, viewMenu);
            editorMenuBar.Items.Add(helpMenu);
        }

        public RadMenu CreateMenuStrip()
        {
            return editorMenuBar;
        }

        public bool HandleKeyDown(Keys keyCode)
        {
            if (keyCode == Keys.F5)
            {
                OnRunScript(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        private volatile bool venvInitializing;

        private void InitializeData()
        {
            var dataGen = new DataGenerator(42);
            var products = dataGen.GenerateProducts(200);
            customers = dataGen.GenerateCustomers(150, products);
            employees = dataGen.GenerateEmployees(100);

            pythonRunner = new PythonRunner();

            if (!pythonRunner.PythonAvailable)
            {
                AppendOutput("WARNING: " + pythonRunner.PythonError + "\n", Color.FromArgb(200, 0, 0));
                AppendOutput("The Python editor is available for writing code, but scripts cannot be executed,\n", Color.FromArgb(140, 100, 0));
                AppendOutput("syntax checking is disabled, and the Package Manager will not function.\n\n", Color.FromArgb(140, 100, 0));
                RaiseStatus("Python not found");
            }
            else
            {
                venvInitializing = true;
                RaiseStatus("Setting up Python environment...");
                pythonRunner.SetupProgress += OnPythonSetupProgress;

                var venvThread = new Thread(() =>
                {
                    pythonRunner.EnsureVenv();

                    RunOnUIThread(() =>
                    {
                        pythonRunner.SetupProgress -= OnPythonSetupProgress;
                        venvInitializing = false;

                        if (pythonRunner.VenvReady)
                        {
                            symbolAnalyzer.LoadSymbolsFromVenv(pythonRunner.VenvPath);
                            IntrospectInstalledModules();
                            RaiseStatus("Ready (" + pythonRunner.PythonVersion + ", venv)");
                        }
                        else
                        {
                            AppendOutput("Virtual environment setup failed: " + pythonRunner.VenvError + "\n", Color.FromArgb(200, 120, 0));
                            AppendOutput("Using system Python instead.\n\n", Color.FromArgb(140, 100, 0));
                            IntrospectInstalledModules();
                            RaiseStatus("Ready (" + pythonRunner.PythonVersion + ", system)");
                        }

                        LoadPackagesAsync();
                    });
                });
                venvThread.IsBackground = true;
                venvThread.Start();
            }
        }

        private void SetupSnippetMenu()
        {
            var item = new RadMenuItem("List Datasets");
            item.Click += (s, e) => InsertSnippet(GetLoadDataSnippet());
            insertSnippetBtn.Items.Add(item);

            item = new RadMenuItem("Basic Statistics");
            item.Click += (s, e) => InsertSnippet(GetStatsSnippet());
            insertSnippetBtn.Items.Add(item);

            item = new RadMenuItem("Plot Histogram");
            item.Click += (s, e) => InsertSnippet(GetHistogramSnippet());
            insertSnippetBtn.Items.Add(item);

            item = new RadMenuItem("Scatter Plot");
            item.Click += (s, e) => InsertSnippet(GetScatterSnippet());
            insertSnippetBtn.Items.Add(item);

            item = new RadMenuItem("Group By Analysis");
            item.Click += (s, e) => InsertSnippet(GetGroupBySnippet());
            insertSnippetBtn.Items.Add(item);

            item = new RadMenuItem("Correlation Matrix");
            item.Click += (s, e) => InsertSnippet(GetCorrelationSnippet());
            insertSnippetBtn.Items.Add(item);

            item = new RadMenuItem("Time Series Plot");
            item.Click += (s, e) => InsertSnippet(GetTimeSeriesSnippet());
            insertSnippetBtn.Items.Add(item);

            item = new RadMenuItem("Display Images");
            item.Click += (s, e) => InsertSnippet(GetImageDisplaySnippet());
            insertSnippetBtn.Items.Add(item);

            var mlSeparator = new RadMenuSeparatorItem();
            insertSnippetBtn.Items.Add(mlSeparator);

            item = new RadMenuItem("ML: Salary Prediction (Linear Regression)");
            item.Click += (s, e) => InsertSnippet(GetLinearRegressionSnippet());
            insertSnippetBtn.Items.Add(item);

            item = new RadMenuItem("ML: Department Classifier (Random Forest)");
            item.Click += (s, e) => InsertSnippet(GetClassificationSnippet());
            insertSnippetBtn.Items.Add(item);

            item = new RadMenuItem("ML: Employee Clustering (K-Means)");
            item.Click += (s, e) => InsertSnippet(GetClusteringSnippet());
            insertSnippetBtn.Items.Add(item);

            item = new RadMenuItem("ML: Customer Segmentation (PCA)");
            item.Click += (s, e) => InsertSnippet(GetPCASnippet());
            insertSnippetBtn.Items.Add(item);
        }

        private void RegisterAllDatasetsInMemory()
        {
            RegisterInMemoryData<Customer>("customers", () => customers);
            RegisterInMemoryData<Employee>("employees", () => employees);
        }

        private void SetupRefSearch()
        {
            refSearchBox.GotFocus += (s, e) =>
            {
                if (refSearchBox.Text == "Search...")
                {
                    refSearchBox.Text = "";
                    refSearchBox.ForeColor = Color.Black;
                }
            };
            refSearchBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(refSearchBox.Text))
                {
                    refSearchBox.Text = "Search...";
                    refSearchBox.ForeColor = Color.Gray;
                }
            };
            refSearchBox.TextChanged += (s, e) =>
            {
                string filter = refSearchBox.Text;
                if (filter == "Search...") filter = "";
                FilterReferenceTree(filter.Trim());
            };
        }

        private void FilterReferenceTree(string filter)
        {
            if (string.IsNullOrEmpty(filter))
            {
                PopulateReferenceTree();
                return;
            }

            refTreeView.BeginUpdate();
            refTreeView.Nodes.Clear();
            nodeStyles.Clear();

            foreach (var kvp in inMemoryDataTypes)
            {
                string name = kvp.Key;
                Type type = kvp.Value;
                int count = GetRecordCountForTag(name);

                bool datasetMatches = name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
                var matchedColNames = new HashSet<string>(StringComparer.Ordinal);

                var flatProps = PythonVisibleHelper.GetFlattenedProperties(type);
                foreach (var fp in flatProps)
                {
                    string typeName = PythonVisibleHelper.GetPythonTypeName(fp.LeafType);
                    bool colMatch = fp.ColumnName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                        || typeName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!colMatch)
                    {
                        var fpAttr = fp.GetAttribute();
                        if (fpAttr != null)
                        {
                            string desc = fpAttr.Description;
                            if (!string.IsNullOrEmpty(desc) && desc.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                                colMatch = true;
                        }
                    }
                    if (!colMatch)
                    {
                        foreach (var pi in fp.PropertyPath)
                        {
                            if (pi.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                            { colMatch = true; break; }
                        }
                    }
                    if (colMatch)
                        matchedColNames.Add(fp.ColumnName);
                }

                if (datasetMatches || matchedColNames.Count > 0)
                {
                    var node = new RadTreeNode(name + "  (" + count + ")");
                    node.Tag = name;
                    refTreeView.Nodes.Add(node);
                    SetNodeFont(node, new Font(refTreeView.Font, FontStyle.Bold));

                    AddHierarchicalColumns(node, name, type, datasetMatches ? null : matchedColNames);
                    node.ExpandAll();
                }
            }

            if (registeredPythonClasses.Count > 0)
            {
                var matchingClasses = new List<string>();
                bool headerMatches = "Registered Classes".IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
                foreach (var name in registeredPythonClasses.Keys)
                {
                    var info = registeredPythonClasses[name];
                    if (headerMatches || name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (info.Description != null && info.Description.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (info.Notes != null && info.Notes.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        info.PythonCode.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                        matchingClasses.Add(name);
                }
                if (matchingClasses.Count > 0)
                {
                    var classNode = new RadTreeNode("Registered Classes  (" + matchingClasses.Count + ")");
                    classNode.Tag = "regclasses";
                    refTreeView.Nodes.Add(classNode);
                    SetNodeFont(classNode, new Font(refTreeView.Font, FontStyle.Bold));
                    foreach (var name in matchingClasses)
                    {
                        var child = new RadTreeNode(name);
                        child.Tag = "regclass_" + name;
                        classNode.Nodes.Add(child);
                        SetNodeForeColor(child, Color.FromArgb(0, 100, 160));
                    }
                    classNode.Expand();
                }
            }

            if (contextVariables.Count > 0)
            {
                var matchingCtx = new List<KeyValuePair<string, ContextVariable>>();
                bool headerMatches = "Context Hub".IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
                foreach (var kvp in contextVariables)
                {
                    if (headerMatches || kvp.Key.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        kvp.Value.TypeDescription.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                        matchingCtx.Add(kvp);
                }
                if (matchingCtx.Count > 0)
                {
                    var ctxNode = new RadTreeNode("Context Hub  (" + matchingCtx.Count + ")");
                    ctxNode.Tag = "contexthub";
                    refTreeView.Nodes.Add(ctxNode);
                    SetNodeFont(ctxNode, new Font(refTreeView.Font, FontStyle.Bold));
                    foreach (var kvp in matchingCtx)
                    {
                        var child = new RadTreeNode(kvp.Key + "  :  " + kvp.Value.TypeDescription);
                        child.Tag = "ctx_" + kvp.Key;
                        ctxNode.Nodes.Add(child);
                        SetNodeForeColor(child, Color.FromArgb(128, 0, 128));
                    }
                    ctxNode.Expand();
                }
            }

            refTreeView.EndUpdate();
        }

        private void AddHierarchicalColumns(RadTreeNode parentNode, string datasetName, Type type, HashSet<string> filterColumns)
        {
            var flatProps = PythonVisibleHelper.GetFlattenedProperties(type);
            var groupChildren = new Dictionary<string, List<FlattenedProperty>>(StringComparer.Ordinal);
            var directProps = new List<FlattenedProperty>();

            foreach (var fp in flatProps)
            {
                if (filterColumns != null && !filterColumns.Contains(fp.ColumnName)) continue;

                if (fp.PropertyPath.Length <= 1)
                {
                    directProps.Add(fp);
                }
                else
                {
                    string groupName = fp.PropertyPath[0].Name;
                    if (!groupChildren.ContainsKey(groupName))
                        groupChildren[groupName] = new List<FlattenedProperty>();
                    groupChildren[groupName].Add(fp);
                }
            }

            foreach (var fp in directProps)
            {
                string typeName = PythonVisibleHelper.GetPythonTypeName(fp.LeafType);
                if (fp.IsComputed) typeName += " (computed)";
                var child = new RadTreeNode(fp.ColumnName + "  :  " + typeName);
                child.Tag = new string[] { "field", datasetName, fp.ColumnName };
                parentNode.Nodes.Add(child);
                SetNodeForeColor(child, Color.FromArgb(80, 80, 80));
            }

            foreach (var kvp in groupChildren)
            {
                string groupName = kvp.Key;
                var groupProps = kvp.Value;
                string groupTypeName = groupProps[0].PropertyPath[0].PropertyType.Name;
                string groupPrefix = groupName + "_";
                var groupNode = new RadTreeNode(groupName + "  (" + groupTypeName + ")");
                groupNode.Tag = new string[] { "subclass", datasetName, groupPrefix };
                parentNode.Nodes.Add(groupNode);
                SetNodeStyle(groupNode, new Font(refTreeView.Font, FontStyle.Italic), Color.FromArgb(0, 100, 130));

                AddSubclassChildren(groupNode, datasetName, groupProps, 1, filterColumns, groupPrefix);
            }
        }

        private void AddSubclassChildren(RadTreeNode parentNode, string datasetName, List<FlattenedProperty> groupProps, int depth, HashSet<string> filterColumns, string pathPrefix)
        {
            var subGroups = new Dictionary<string, List<FlattenedProperty>>(StringComparer.Ordinal);
            var leafProps = new List<FlattenedProperty>();

            foreach (var fp in groupProps)
            {
                if (fp.PropertyPath.Length > depth + 1)
                {
                    string subGroupName = fp.PropertyPath[depth].Name;
                    if (!subGroups.ContainsKey(subGroupName))
                        subGroups[subGroupName] = new List<FlattenedProperty>();
                    subGroups[subGroupName].Add(fp);
                }
                else
                {
                    leafProps.Add(fp);
                }
            }

            foreach (var fp in leafProps)
            {
                string leafName = fp.PropertyPath[fp.PropertyPath.Length - 1].Name;
                string typeName = PythonVisibleHelper.GetPythonTypeName(fp.LeafType);
                if (fp.IsComputed) typeName += " (computed)";
                var child = new RadTreeNode(leafName + "  \u2192  " + fp.ColumnName + "  :  " + typeName);
                child.Tag = new string[] { "field", datasetName, fp.ColumnName };
                parentNode.Nodes.Add(child);
                SetNodeForeColor(child, Color.FromArgb(80, 80, 80));
            }

            foreach (var kvp in subGroups)
            {
                string subGroupName = kvp.Key;
                var subGroupProps = kvp.Value;
                string subGroupTypeName = subGroupProps[0].PropertyPath[depth].PropertyType.Name;
                string subPrefix = pathPrefix + subGroupName + "_";
                var subGroupNode = new RadTreeNode(subGroupName + "  (" + subGroupTypeName + ")");
                subGroupNode.Tag = new string[] { "subclass", datasetName, subPrefix };
                parentNode.Nodes.Add(subGroupNode);
                SetNodeStyle(subGroupNode, new Font(refTreeView.Font, FontStyle.Italic), Color.FromArgb(0, 100, 130));

                AddSubclassChildren(subGroupNode, datasetName, subGroupProps, depth + 1, filterColumns, subPrefix);
            }
        }

        private void PopulateReferenceTree()
        {
            nodeStyles.Clear();
            refTreeView.Nodes.Clear();

            foreach (var kvp in inMemoryDataTypes)
            {
                string name = kvp.Key;
                Type type = kvp.Value;
                int count = GetRecordCountForTag(name);
                var node = new RadTreeNode(name + "  (" + count + ")");
                node.Tag = name;
                refTreeView.Nodes.Add(node);
                SetNodeFont(node, new Font(refTreeView.Font, FontStyle.Bold));

                AddHierarchicalColumns(node, name, type, null);
                node.Expand();
            }

            if (registeredPythonClasses.Count > 0)
            {
                var classNode = new RadTreeNode("Registered Classes  (" + registeredPythonClasses.Count + ")");
                classNode.Tag = "regclasses";
                refTreeView.Nodes.Add(classNode);
                SetNodeFont(classNode, new Font(refTreeView.Font, FontStyle.Bold));
                foreach (var name in registeredPythonClasses.Keys)
                {
                    var child = new RadTreeNode(name);
                    child.Tag = "regclass_" + name;
                    classNode.Nodes.Add(child);
                    SetNodeForeColor(child, Color.FromArgb(0, 100, 160));
                }
                classNode.Expand();
            }

            if (contextVariables.Count > 0)
            {
                var ctxNode = new RadTreeNode("Context Hub  (" + contextVariables.Count + ")");
                ctxNode.Tag = "contexthub";
                refTreeView.Nodes.Add(ctxNode);
                SetNodeFont(ctxNode, new Font(refTreeView.Font, FontStyle.Bold));
                foreach (var kvp in contextVariables)
                {
                    var child = new RadTreeNode(kvp.Key + "  :  " + kvp.Value.TypeDescription);
                    child.Tag = "ctx_" + kvp.Key;
                    ctxNode.Nodes.Add(child);
                    SetNodeForeColor(child, Color.FromArgb(128, 0, 128));
                }
                ctxNode.Expand();
            }

            if (refTreeView.Nodes.Count > 0)
                refTreeView.SelectedNode = refTreeView.Nodes[0];
        }

        private List<Tuple<string, string>> GetColumnsForDataset(string tag)
        {
            var cols = new List<Tuple<string, string>>();
            Type type;
            if (!inMemoryDataTypes.TryGetValue(tag, out type))
                return cols;

            var flatProps = PythonVisibleHelper.GetFlattenedProperties(type);
            foreach (var fp in flatProps)
            {
                string typeName = PythonVisibleHelper.GetPythonTypeName(fp.LeafType);
                if (fp.IsComputed)
                    typeName += " (computed)";
                cols.Add(Tuple.Create(fp.ColumnName, typeName));
            }
            return cols;
        }

        private void OnRefTreeSelect(object sender, EventArgs e)
        {
            var selectedNode = refTreeView.SelectedNode;
            if (selectedNode == null || selectedNode.Tag == null) return;

            var tagArr = selectedNode.Tag as string[];
            if (tagArr != null && tagArr.Length == 3 && tagArr[0] == "field")
            {
                ShowFieldDetail(tagArr[1], tagArr[2]);
                return;
            }
            if (tagArr != null && tagArr.Length == 3 && tagArr[0] == "subclass")
            {
                ShowSubclassDetail(tagArr[1], tagArr[2], selectedNode);
                return;
            }

            string tag = selectedNode.Tag.ToString();

            if (tag == "regclasses" || tag.StartsWith("regclass_"))
            {
                ShowRegisteredClassDetail(tag);
                return;
            }

            if (tag == "contexthub" || tag.StartsWith("ctx_"))
            {
                ShowContextDetail(tag);
                return;
            }

            ShowDatasetDetail(tag);
        }

        private void ShowFieldDetail(string datasetName, string fieldName)
        {
            Type type;
            if (!inMemoryDataTypes.TryGetValue(datasetName, out type)) return;

            var flatProps = PythonVisibleHelper.GetFlattenedProperties(type);
            FlattenedProperty fp = null;
            foreach (var f in flatProps)
            {
                if (f.ColumnName == fieldName) { fp = f; break; }
            }
            if (fp == null) return;

            string typeName = PythonVisibleHelper.GetPythonTypeName(fp.LeafType);

            refDetailBox.Clear();

            AppendRefText(fieldName, Color.FromArgb(0, 0, 180), true, 12);
            AppendRefText("  :  " + typeName + (fp.IsComputed ? " (computed)" : "") + "\n\n", Color.FromArgb(100, 100, 100), false, 12);

            AppendRefText("Description\n", Color.FromArgb(0, 100, 0), true, 10);
            AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
            var attr = fp.GetAttribute();
            string desc = attr != null ? attr.Description : null;
            if (!string.IsNullOrEmpty(desc))
            {
                AppendRefText(desc + "\n\n", Color.FromArgb(60, 60, 60), false, 10);
            }
            else if (fp.PropertyPath.Length > 1)
            {
                string leafName = fp.PropertyPath[fp.PropertyPath.Length - 1].Name;
                var pathParts = new List<string>();
                for (int i = 0; i < fp.PropertyPath.Length - 1; i++)
                    pathParts.Add(fp.PropertyPath[i].Name + " (" + fp.PropertyPath[i].PropertyType.Name + ")");
                string pathDesc = string.Join(" > ", pathParts.ToArray());
                AppendRefText(leafName + " property from nested class path: " + pathDesc + ".\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("Flattened as column: " + fp.ColumnName + "\n\n", Color.FromArgb(100, 100, 100), false, 10);
            }
            else
            {
                AppendRefText("No description available.\n\n", Color.FromArgb(150, 150, 150), false, 10);
            }

            AppendRefText("Dataset\n", Color.FromArgb(0, 100, 0), true, 10);
            AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
            string className = GetClassNameForTag(datasetName);
            if (fp.PropertyPath.Length > 1)
            {
                var dotPath = new List<string>();
                foreach (var pi in fp.PropertyPath)
                    dotPath.Add(pi.Name);
                AppendRefText("  " + className + "." + string.Join(".", dotPath.ToArray()) + "  (variable: " + datasetName + ")\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("  Column name: " + fieldName + "\n\n", Color.FromArgb(100, 100, 100), false, 10);
            }
            else
            {
                AppendRefText("  " + className + "." + fieldName + "  (variable: " + datasetName + ")\n\n", Color.FromArgb(60, 60, 60), false, 10);
            }

            AppendRefText("Example Python Code\n", Color.FromArgb(0, 100, 0), true, 10);
            AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
            AppendRefText("from DotNetData import " + datasetName + "\n\n", Color.FromArgb(60, 60, 60), false, 10);

            string customExample = attr != null ? attr.Example : null;

            if (!string.IsNullOrEmpty(customExample))
            {
                string resolved = customExample.Replace("{dataset}", datasetName).Replace("{field}", fieldName);
                foreach (string line in resolved.Split('\n'))
                {
                    string trimmed = line.TrimStart();
                    if (trimmed.StartsWith("#"))
                        AppendRefText(line + "\n", Color.FromArgb(0, 128, 0), false, 10);
                    else
                        AppendRefText(line + "\n", Color.FromArgb(60, 60, 60), false, 10);
                }
            }
            else if (typeName == "string")
            {
                AppendRefText("# Value counts\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText(datasetName + "." + fieldName + ".value_counts()\n\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("# Unique values\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText(datasetName + "." + fieldName + ".unique()\n", Color.FromArgb(60, 60, 60), false, 10);
            }
            else if (typeName == "bool")
            {
                AppendRefText("# Count true values\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText(datasetName + "." + fieldName + ".sum()\n\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("# Filter to true rows\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText(datasetName + "[" + datasetName + "." + fieldName + "]\n", Color.FromArgb(60, 60, 60), false, 10);
            }
            else if (typeName == "datetime")
            {
                AppendRefText("# Date range\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText(datasetName + "." + fieldName + ".min(), " + datasetName + "." + fieldName + ".max()\n\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("# Extract year\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText(datasetName + "." + fieldName + ".dt.year\n", Color.FromArgb(60, 60, 60), false, 10);
            }
            else if (typeName == "image")
            {
                AppendRefText("# Access a single image via row indexing\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText("img = " + datasetName + "[0]." + fieldName + "\n\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("# Display it\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText("import matplotlib.pyplot as plt\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("import numpy as np\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("plt.imshow(np.array(img))\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("plt.axis('off')\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("plt.show()\n\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("# Convert to numpy array for calculations\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText("arr = np.array(" + datasetName + "[0]." + fieldName + ")\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("print(f'Shape: {arr.shape}, dtype: {arr.dtype}')\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("print(f'Mean pixel: {arr.mean():.1f}')\n", Color.FromArgb(60, 60, 60), false, 10);
            }
            else
            {
                AppendRefText("# Basic statistics\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText(datasetName + "." + fieldName + ".describe()\n\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("# Mean value\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText(datasetName + "." + fieldName + ".mean()\n", Color.FromArgb(60, 60, 60), false, 10);
            }

            refDetailBox.SelectionStart = 0;
            refDetailBox.ScrollToCaret();
        }

        private void ShowSubclassDetail(string datasetName, string prefix, RadTreeNode node)
        {
            Type type;
            if (!inMemoryDataTypes.TryGetValue(datasetName, out type)) return;

            var flatProps = PythonVisibleHelper.GetFlattenedProperties(type);
            var subProps = new List<FlattenedProperty>();
            foreach (var fp in flatProps)
            {
                if (fp.ColumnName.StartsWith(prefix, StringComparison.Ordinal))
                    subProps.Add(fp);
            }

            string displayName = prefix.TrimEnd('_');
            if (displayName.Contains("_"))
                displayName = displayName.Substring(displayName.LastIndexOf('_') + 1);
            else
                displayName = prefix.TrimEnd('_');

            string subclassTypeName = "";
            foreach (var fp in subProps)
            {
                int segCount = prefix.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries).Length;
                if (fp.PropertyPath.Length >= segCount)
                {
                    subclassTypeName = fp.PropertyPath[segCount - 1].PropertyType.Name;
                    break;
                }
            }

            refDetailBox.Clear();

            AppendRefText(displayName, Color.FromArgb(0, 100, 130), true, 12);
            AppendRefText("  (" + subclassTypeName + ")\n\n", Color.FromArgb(100, 100, 100), false, 12);

            AppendRefText("Nested class from ", Color.FromArgb(60, 60, 60), false, 10);
            AppendRefText(datasetName, Color.FromArgb(0, 0, 180), true, 10);
            AppendRefText("\n", Color.Black, false, 10);
            AppendRefText("Columns are flattened with prefix: ", Color.FromArgb(60, 60, 60), false, 10);
            AppendRefText(prefix + "\n\n", Color.FromArgb(128, 0, 0), false, 10);

            AppendRefText("Columns (" + subProps.Count + ")\n", Color.FromArgb(0, 100, 0), true, 10);
            AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);

            int maxPropLen = 0;
            foreach (var fp in subProps)
            {
                string propName = fp.PropertyPath[fp.PropertyPath.Length - 1].Name;
                if (propName.Length > maxPropLen) maxPropLen = propName.Length;
            }

            foreach (var fp in subProps)
            {
                string propName = fp.PropertyPath[fp.PropertyPath.Length - 1].Name;
                string typeName = PythonVisibleHelper.GetPythonTypeName(fp.LeafType);
                if (fp.IsComputed) typeName += " (computed)";
                AppendRefText("  " + propName.PadRight(maxPropLen + 2), Color.FromArgb(0, 0, 0), false, 10);
                AppendRefText("\u2192  " + fp.ColumnName + "  :  ", Color.FromArgb(120, 120, 120), false, 10);
                AppendRefText(typeName + "\n", Color.FromArgb(100, 100, 100), false, 10);
            }

            AppendRefText("\n", Color.Black, false, 10);
            AppendRefText("Example Python Code\n", Color.FromArgb(0, 100, 0), true, 10);
            AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
            AppendRefText("from DotNetData import " + datasetName + "\n\n", Color.FromArgb(60, 60, 60), false, 10);

            AppendRefText("# Select all " + displayName + " columns\n", Color.FromArgb(0, 128, 0), false, 10);
            AppendRefText(datasetName + "[[c for c in " + datasetName + ".columns if c.startswith('" + prefix + "')]]\n\n", Color.FromArgb(60, 60, 60), false, 10);

            if (subProps.Count > 0)
            {
                AppendRefText("# Access a specific column\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText(datasetName + "." + subProps[0].ColumnName + "\n", Color.FromArgb(60, 60, 60), false, 10);
            }

            refDetailBox.SelectionStart = 0;
            refDetailBox.ScrollToCaret();
        }

        private void ShowDatasetDetail(string tag)
        {
            refDetailBox.Clear();
            var columns = GetColumnsForDataset(tag);
            string className = GetClassNameForTag(tag);
            int count = GetRecordCountForTag(tag);

            AppendRefText(className + " Dataset", Color.FromArgb(0, 0, 180), true, 12);
            AppendRefText("\n\n", Color.Black, false, 10);

            AppendRefText("Variable:  ", Color.FromArgb(100, 100, 100), false, 10);
            AppendRefText(tag + "\n", Color.FromArgb(0, 0, 0), false, 10);

            AppendRefText("Records:   ", Color.FromArgb(100, 100, 100), false, 10);
            AppendRefText(count.ToString("N0") + "\n", Color.FromArgb(0, 0, 0), false, 10);

            AppendRefText("Import:    ", Color.FromArgb(100, 100, 100), false, 10);
            AppendRefText("from DotNetData import " + tag + "\n", Color.FromArgb(0, 0, 0), false, 10);

            AppendRefText("Access:    ", Color.FromArgb(100, 100, 100), false, 10);
            AppendRefText(tag + ".column_name  (e.g. " + tag + ".head())\n\n", Color.FromArgb(0, 0, 0), false, 10);

            AppendRefText("Columns (" + columns.Count + ")\n", Color.FromArgb(0, 100, 0), true, 10);
            AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);

            int maxNameLen = 0;
            foreach (var c in columns)
                if (c.Item1.Length > maxNameLen) maxNameLen = c.Item1.Length;

            foreach (var col in columns)
            {
                string name = col.Item1.PadRight(maxNameLen + 2);
                bool isComputed = col.Item2.Contains("computed");
                Color nameColor = isComputed ? Color.FromArgb(128, 0, 128) : Color.FromArgb(0, 0, 0);
                AppendRefText("  " + name, nameColor, false, 10);
                AppendRefText(col.Item2 + "\n", Color.FromArgb(100, 100, 100), false, 10);
            }

            AppendRefText("\n", Color.Black, false, 10);
            AppendRefText("Example Python Code\n", Color.FromArgb(0, 100, 0), true, 10);
            AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
            string example = GetExampleCode(tag);
            AppendRefText(example + "\n", Color.FromArgb(60, 60, 60), false, 10);

            refDetailBox.SelectionStart = 0;
            refDetailBox.ScrollToCaret();
        }

        private void ShowRegisteredClassDetail(string tag)
        {
            refDetailBox.Clear();

            if (tag == "regclasses")
            {
                AppendRefText("Registered Python Classes\n\n", Color.FromArgb(0, 0, 180), true, 12);
                AppendRefText("Classes registered from the host application via\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("RegisterPythonClass(). Available in every script run.\n\n", Color.FromArgb(60, 60, 60), false, 10);

                foreach (var kvp in registeredPythonClasses)
                {
                    AppendRefText("  " + kvp.Key, Color.FromArgb(0, 100, 160), false, 10);
                    if (!string.IsNullOrEmpty(kvp.Value.Description))
                        AppendRefText("  \u2014 " + kvp.Value.Description, Color.FromArgb(130, 130, 130), false, 10);
                    AppendRefText("\n", Color.Black, false, 10);
                }

                AppendRefText("\n", Color.Black, false, 10);
                AppendRefText("Usage\n", Color.FromArgb(0, 100, 0), true, 10);
                AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
                AppendRefText("# Classes are injected before your script runs.\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText("# Use them directly:\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText("obj = ClassName()\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("result = ClassName.static_method()\n", Color.FromArgb(60, 60, 60), false, 10);
            }
            else
            {
                string name = tag.Substring("regclass_".Length);
                AppendRefText(name + "  (Registered Class)\n\n", Color.FromArgb(0, 0, 180), true, 12);

                if (registeredPythonClasses.ContainsKey(name))
                {
                    var info = registeredPythonClasses[name];

                    AppendRefText("Description\n", Color.FromArgb(0, 100, 0), true, 10);
                    AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
                    if (!string.IsNullOrEmpty(info.Description))
                        AppendRefText(info.Description + "\n\n", Color.FromArgb(60, 60, 60), false, 10);
                    else
                        AppendRefText("Injected by the host application before script execution.\n\n", Color.FromArgb(150, 150, 150), false, 10);

                    if (!string.IsNullOrEmpty(info.Example))
                    {
                        AppendRefText("Example\n", Color.FromArgb(0, 100, 0), true, 10);
                        AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
                        foreach (string line in info.Example.Split('\n'))
                        {
                            string trimmed = line.TrimStart();
                            if (trimmed.StartsWith("#"))
                                AppendRefText(line + "\n", Color.FromArgb(0, 128, 0), false, 10);
                            else
                                AppendRefText(line + "\n", Color.FromArgb(60, 60, 60), false, 10);
                        }
                        AppendRefText("\n", Color.Black, false, 10);
                    }

                    if (!string.IsNullOrEmpty(info.Notes))
                    {
                        AppendRefText("Notes\n", Color.FromArgb(0, 100, 0), true, 10);
                        AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
                        AppendRefText(info.Notes + "\n\n", Color.FromArgb(100, 100, 100), false, 10);
                    }

                    AppendRefText("Source Code\n", Color.FromArgb(0, 100, 0), true, 10);
                    AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
                    AppendRefText(info.PythonCode + "\n", Color.FromArgb(60, 60, 60), false, 10);
                }
            }

            refDetailBox.SelectionStart = 0;
            refDetailBox.ScrollToCaret();
        }

        private void ShowContextDetail(string tag)
        {
            refDetailBox.Clear();

            if (tag == "contexthub")
            {
                AppendRefText("Context Hub\n\n", Color.FromArgb(0, 0, 180), true, 12);
                AppendRefText("Variables sent from the host application via SetContext().\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("Available as top-level Python variables in every script.\n\n", Color.FromArgb(60, 60, 60), false, 10);

                int maxLen = 0;
                foreach (var kvp in contextVariables)
                    if (kvp.Key.Length > maxLen) maxLen = kvp.Key.Length;

                foreach (var kvp in contextVariables)
                {
                    AppendRefText("  " + kvp.Key.PadRight(maxLen + 2), Color.FromArgb(128, 0, 128), false, 10);
                    AppendRefText(kvp.Value.TypeDescription + "  =  ", Color.FromArgb(100, 100, 100), false, 10);
                    string preview = kvp.Value.PythonLiteral;
                    if (preview.Length > 60) preview = preview.Substring(0, 57) + "...";
                    AppendRefText(preview + "\n", Color.FromArgb(60, 60, 60), false, 10);
                }

                AppendRefText("\n", Color.Black, false, 10);
                AppendRefText("Usage\n", Color.FromArgb(0, 100, 0), true, 10);
                AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
                AppendRefText("# Variables are injected before your script runs.\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText("# Use them directly by name:\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText("print(variable_name)\n", Color.FromArgb(60, 60, 60), false, 10);
            }
            else
            {
                string key = tag.Substring("ctx_".Length);
                if (contextVariables.ContainsKey(key))
                {
                    var cv = contextVariables[key];
                    AppendRefText(key + "  (Context Variable)\n\n", Color.FromArgb(0, 0, 180), true, 12);

                    AppendRefText("Type:   ", Color.FromArgb(100, 100, 100), false, 10);
                    AppendRefText(cv.TypeDescription + "\n", Color.FromArgb(0, 0, 0), false, 10);

                    AppendRefText("Value:  ", Color.FromArgb(100, 100, 100), false, 10);
                    AppendRefText(cv.PythonLiteral + "\n\n", Color.FromArgb(0, 0, 0), false, 10);

                    AppendRefText("Sent from the host application via SetContext().\n", Color.FromArgb(60, 60, 60), false, 10);
                    AppendRefText("Available as a top-level Python variable.\n\n", Color.FromArgb(60, 60, 60), false, 10);

                    AppendRefText("Example\n", Color.FromArgb(0, 100, 0), true, 10);
                    AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
                    AppendRefText("print(" + key + ")\n", Color.FromArgb(60, 60, 60), false, 10);
                    if (cv.TypeDescription == "list")
                        AppendRefText("for item in " + key + ":\n    print(item)\n", Color.FromArgb(60, 60, 60), false, 10);
                    else if (cv.TypeDescription == "dict")
                        AppendRefText("for k, v in " + key + ".items():\n    print(k, v)\n", Color.FromArgb(60, 60, 60), false, 10);
                }
            }

            refDetailBox.SelectionStart = 0;
            refDetailBox.ScrollToCaret();
        }

        private void AppendRefText(string text, Color color, bool bold, float size)
        {
            int start = refDetailBox.TextLength;
            refDetailBox.AppendText(text);
            refDetailBox.Select(start, text.Length);
            Font f = bold ? new Font("Consolas", size, FontStyle.Bold) : new Font("Consolas", size);
            refDetailBox.SelectionFont = f;
            refDetailBox.SelectionColor = color;
            refDetailBox.SelectionLength = 0;
        }

        private string GetClassNameForTag(string tag)
        {
            Type type;
            if (inMemoryDataTypes.TryGetValue(tag, out type))
                return type.Name;
            return tag;
        }

        private int GetRecordCountForTag(string tag)
        {
            if (tag == "customers") return customers.Count;
            if (tag == "employees") return employees.Count;
            return 0;
        }

        private string GetExampleCode(string tag)
        {
            switch (tag)
            {
                case "customers":
                    return "from DotNetData import customers\n\n# Customer count by tier\nprint(customers.Tier.value_counts())\n\n# Average credit limit by tier\nprint(customers.df.groupby('Tier')['CreditLimit'].mean())";
                case "employees":
                    return "from DotNetData import employees\n\n# Average salary by department\nprint(employees.df.groupby('Department')['Salary'].mean().sort_values(ascending=False))\n\n# Remote vs office distribution\nprint(employees.IsRemote.value_counts())";
                default:
                    return "from DotNetData import " + tag + "\n\nprint(" + tag + ".head())\nprint(" + tag + ".describe())";
            }
        }

        

        private void OnRunScript(object sender, EventArgs e)
        {
            if (pythonEditor == null) return;
            string script = pythonEditor.GetText();
            if (string.IsNullOrWhiteSpace(script))
            {
                AppendOutput("No script to run.\n", Color.FromArgb(180, 140, 0));
                return;
            }

            if (venvInitializing)
            {
                AppendOutput("Python environment is still being set up. Please wait...\n", Color.FromArgb(180, 140, 0));
                RaiseStatus("Environment setup in progress...");
                return;
            }

            if (!pythonRunner.PythonAvailable)
            {
                AppendOutput("Cannot run script: " + pythonRunner.PythonError + "\n", Color.FromArgb(200, 0, 0));
                RaiseStatus("Python not available");
                return;
            }

            SaveAllFilesToDisk();
            ShowDockPanel(outputDockContent);

            string runFileName = activeFile != null ? activeFile.FileName : "untitled";
            RaiseStatus("Running " + runFileName + "...");
            AppendOutput("--- Running [" + runFileName + "] as __main__ at " + DateTime.Now.ToString("HH:mm:ss") + " ---\n", Color.FromArgb(0, 100, 180));

            Application.DoEvents();

            Dictionary<string, string> memData = null;
            if (inMemoryDataSources.Count > 0)
                memData = SerializeInMemoryData();

            string preamble = BuildPreamble();
            var result = pythonRunner.Execute(script, memData, preamble);

            if (!string.IsNullOrEmpty(result.Output))
                AppendOutput(result.Output, Color.FromArgb(0, 0, 0));

            if (!string.IsNullOrEmpty(result.Error))
            {
                if (result.Success)
                    AppendOutput(result.Error, Color.FromArgb(140, 120, 0));
                else
                    AppendOutput("ERROR:\n" + result.Error, Color.FromArgb(200, 0, 0));
            }

            if (result.PlotPaths != null && result.PlotPaths.Count > 0)
            {
                AppendOutput("Generated " + result.PlotPaths.Count + " plot(s).\n", Color.FromArgb(0, 128, 0));
                var viewer = new PlotViewerForm(result.PlotPaths);
                viewer.Show();
            }

            AppendOutput("--- Finished (exit code: " + result.ExitCode + ") ---\n\n", Color.FromArgb(0, 100, 180));
            RaiseStatus(result.Success ? "Script completed successfully." : "Script failed with errors.");
        }

        private void OnToolbarUndo(object sender, EventArgs e)
        {
            if (pythonEditor != null) pythonEditor.PerformUndo();
        }

        private void OnToolbarRedo(object sender, EventArgs e)
        {
            if (pythonEditor != null) pythonEditor.PerformRedo();
        }

        private void OnToolbarFind(object sender, EventArgs e)
        {
            if (pythonEditor != null) pythonEditor.ShowFind();
        }

        private int ParseErrorLine(string error)
        {
            var match = System.Text.RegularExpressions.Regex.Match(error, @"[Ll]ine (\d+)");
            if (match.Success)
            {
                int line;
                if (int.TryParse(match.Groups[1].Value, out line))
                    return line;
            }
            return -1;
        }

        private static string FindScriptsDirectory()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string candidate = Path.Combine(baseDir, "python", "scripts");
            if (Directory.Exists(candidate))
                return candidate;

            string dir = baseDir;
            for (int i = 0; i < 5; i++)
            {
                string parent = Directory.GetParent(dir)?.FullName;
                if (parent == null) break;
                dir = parent;
                candidate = Path.Combine(dir, "python", "scripts");
                if (Directory.Exists(candidate))
                    return candidate;
            }

            candidate = Path.Combine(baseDir, "python", "scripts");
            Directory.CreateDirectory(candidate);
            return candidate;
        }

        private void InitializeFileSystem()
        {
            scriptsDir = FindScriptsDirectory();
            if (!Directory.Exists(scriptsDir))
                Directory.CreateDirectory(scriptsDir);

            symbolAnalyzer.ScriptsDirectory = scriptsDir;
            symbolAnalyzer.SetFileContentResolver(moduleName =>
            {
                string fileName = moduleName + ".py";
                var tab = openFiles.Find(f =>
                    f.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
                if (tab != null)
                {
                    if (tab.Editor != null)
                        return tab.Editor.GetText();
                    return tab.Content;
                }
                return null;
            });

            var existingFiles = Directory.GetFiles(scriptsDir, "*.py", SearchOption.AllDirectories);

            if (existingFiles.Length == 0)
            {
                var mainTab = new FileTab
                {
                    FilePath = Path.Combine(scriptsDir, "main.py"),
                    FileName = "main.py",
                    Content = GetDefaultScript(),
                    CursorPosition = 0,
                    IsModified = false
                };
                openFiles.Add(mainTab);
                File.WriteAllText(mainTab.FilePath, mainTab.Content);
            }
            else
            {
                var rootFiles = Directory.GetFiles(scriptsDir, "*.py");
                foreach (var fp in rootFiles.OrderBy(f => f))
                {
                    var tab = new FileTab
                    {
                        FilePath = fp,
                        FileName = Path.GetFileName(fp),
                        Content = File.ReadAllText(fp),
                        CursorPosition = 0,
                        IsModified = false
                    };
                    openFiles.Add(tab);
                }

                if (openFiles.Count == 0)
                {
                    var firstFile = existingFiles.OrderBy(f => f).First();
                    var tab = new FileTab
                    {
                        FilePath = firstFile,
                        FileName = Path.GetFileName(firstFile),
                        Content = File.ReadAllText(firstFile),
                        CursorPosition = 0,
                        IsModified = false
                    };
                    openFiles.Add(tab);
                }
            }

            activeFile = openFiles[0];
            RefreshFileList();
        }

        private void SetupFileListEvents()
        {
            fileTreeView.NodeMouseClick += OnFileTreeNodeClick;
            fileTreeView.NodeMouseDoubleClick += OnFileTreeNodeDoubleClick;
            fileTreeView.ValueValidating += OnFileTreeValueValidating;

            fileContextMenu = new RadContextMenu();
            fileTreeView.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    var node = fileTreeView.GetNodeAt(e.Location);
                    if (node != null)
                        fileTreeView.SelectedNode = node;
                    BuildFileContextMenu(node);
                    fileContextMenu.Show(fileTreeView.PointToScreen(e.Location));
                }
            };
        }

        private void BeginNodeRename(RadTreeNode node)
        {
            if (node == null) return;
            fileTreeView.AllowEdit = true;
            fileTreeView.SelectedNode = node;
            fileTreeView.BeginEdit();
        }

        private void BuildFileContextMenu(RadTreeNode node)
        {
            fileContextMenu.Items.Clear();

            var newFileCtx = new RadMenuItem("New File");
            newFileCtx.Click += OnNewFile;
            fileContextMenu.Items.Add(newFileCtx);
            var newFolderCtx = new RadMenuItem("New Folder");
            newFolderCtx.Click += OnNewFolder;
            fileContextMenu.Items.Add(newFolderCtx);

            if (node != null)
            {
                string path = (string)node.Tag;
                bool isFolder = Directory.Exists(path);

                fileContextMenu.Items.Add(new RadMenuSeparatorItem());

                if (!isFolder)
                {
                    var openItem = new RadMenuItem("Open");
                    openItem.Click += (s, e) => OpenFileFromTree(node);
                    fileContextMenu.Items.Add(openItem);
                }

                var renameItem = new RadMenuItem("Rename");
                renameItem.Click += (s, e) => { BeginNodeRename(node); };
                fileContextMenu.Items.Add(renameItem);

                var deleteItem = new RadMenuItem("Delete");
                deleteItem.Click += (s, e) => OnDeleteFileOrFolder(node);
                fileContextMenu.Items.Add(deleteItem);
            }
        }

        private void OnFileTreeNodeClick(object sender, RadTreeViewEventArgs e)
        {
            OpenFileFromTree(e.Node);
        }

        private void OnFileTreeNodeDoubleClick(object sender, RadTreeViewEventArgs e)
        {
            OpenFileFromTree(e.Node);
        }

        private void OpenFileFromTree(RadTreeNode node)
        {
            if (node == null) return;
            string tag = node.Tag as string;
            if (tag == null) return;

            if (tag.StartsWith("UNSAVED:"))
            {
                string unsavedName = tag.Substring(8);
                var ft = openFiles.Find(f => f.FilePath == null && f.FileName == unsavedName);
                if (ft != null && ft != activeFile)
                    SwitchToFile(ft);
                return;
            }

            if (Directory.Exists(tag)) return;

            var existing = openFiles.Find(f => f.FilePath != null &&
                f.FilePath.Equals(tag, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                if (existing != activeFile)
                    SwitchToFile(existing);
                return;
            }

            var newTab = new FileTab
            {
                FilePath = tag,
                FileName = Path.GetFileName(tag),
                Content = File.ReadAllText(tag),
                CursorPosition = 0,
                IsModified = false
            };
            openFiles.Add(newTab);
            SwitchToFile(newTab);
            RaiseStatus("Opened: " + newTab.FileName);
        }

        private void OnNewFolder(object sender, EventArgs e)
        {
            string parentDir = scriptsDir;
            var selectedNode = fileTreeView.SelectedNode;
            if (selectedNode != null)
            {
                string tag = selectedNode.Tag as string;
                if (tag != null && Directory.Exists(tag))
                    parentDir = tag;
                else if (tag != null && File.Exists(tag))
                    parentDir = Path.GetDirectoryName(tag);
            }

            string baseName = "new_folder";
            string folderPath = Path.Combine(parentDir, baseName);
            int counter = 1;
            while (Directory.Exists(folderPath))
            {
                folderPath = Path.Combine(parentDir, baseName + counter);
                counter++;
            }

            Directory.CreateDirectory(folderPath);
            RefreshFileList();

            var node = FindNodeByPath(fileTreeView.Nodes, folderPath);
            if (node != null)
                BeginNodeRename(node);

            RaiseStatus("Created folder: " + Path.GetFileName(folderPath));
        }

        private void OnDeleteFileOrFolder(RadTreeNode node)
        {
            if (node == null) return;
            string path = node.Tag as string;
            if (path == null || path.StartsWith("UNSAVED:")) return;

            bool isFolder = Directory.Exists(path);
            string itemName = Path.GetFileName(path);
            string message = isFolder
                ? "Delete folder '" + itemName + "' and all its contents?\n\nThis cannot be undone."
                : "Delete file '" + itemName + "'?\n\nThis cannot be undone.";

            var result = MessageBox.Show(message, "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            if (isFolder)
            {
                var toClose = openFiles.FindAll(f => f.FilePath != null &&
                    f.FilePath.StartsWith(path + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
                foreach (var ft in toClose)
                {
                    openFiles.Remove(ft);
                    CloseDockContent(ft);
                }

                Directory.Delete(path, true);
            }
            else
            {
                var openTab = openFiles.Find(f => f.FilePath != null &&
                    f.FilePath.Equals(path, StringComparison.OrdinalIgnoreCase));
                if (openTab != null)
                {
                    openFiles.Remove(openTab);
                    CloseDockContent(openTab);
                }

                File.Delete(path);
            }

            if (openFiles.Count == 0)
            {
                var mainTab = new FileTab
                {
                    FilePath = Path.Combine(scriptsDir, "main.py"),
                    FileName = "main.py",
                    Content = GetDefaultScript(),
                    CursorPosition = 0,
                    IsModified = false
                };
                openFiles.Add(mainTab);
                File.WriteAllText(mainTab.FilePath, mainTab.Content);
            }

            if (activeFile == null || !openFiles.Contains(activeFile))
            {
                activeFile = null;
                SwitchToFile(openFiles[0]);
            }

            RefreshFileList();
            RaiseStatus("Deleted: " + itemName);
        }

        private void OnFileTreeValueValidating(object sender, TreeNodeValidatingEventArgs e)
        {
            fileTreeView.AllowEdit = false;

            string newLabel = e.NewValue as string;
            if (string.IsNullOrEmpty(newLabel) || string.IsNullOrEmpty(newLabel.Trim()))
            {
                e.Cancel = true;
                return;
            }

            string newName = newLabel.Trim();

            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in newName)
            {
                if (Array.IndexOf(invalidChars, c) >= 0)
                {
                    e.Cancel = true;
                    MessageBox.Show("The name contains invalid characters.", "Invalid Name",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            string oldPath = e.Node.Tag as string;
            if (oldPath == null || oldPath.StartsWith("UNSAVED:"))
            {
                e.Cancel = true;
                return;
            }

            bool isFolder = Directory.Exists(oldPath);
            string parentDir = Path.GetDirectoryName(oldPath);

            if (!isFolder && !newName.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
                newName += ".py";

            string newPath = Path.Combine(parentDir, newName);

            if (newPath.Equals(oldPath, StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true;
                return;
            }

            if ((isFolder && Directory.Exists(newPath)) || (!isFolder && File.Exists(newPath)))
            {
                e.Cancel = true;
                MessageBox.Show("An item with this name already exists.", "Name Conflict",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (isFolder)
                {
                    Directory.Move(oldPath, newPath);

                    foreach (var ft in openFiles)
                    {
                        if (ft.FilePath != null && ft.FilePath.StartsWith(oldPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                        {
                            ft.FilePath = newPath + ft.FilePath.Substring(oldPath.Length);
                            ft.FileName = Path.GetFileName(ft.FilePath);
                            if (ft.DockContent != null)
                                ft.DockContent.Text = ft.IsModified ? "\u2022 " + ft.FileName : ft.FileName;
                        }
                    }
                }
                else
                {
                    File.Move(oldPath, newPath);

                    var openTab = openFiles.Find(f => f.FilePath != null &&
                        f.FilePath.Equals(oldPath, StringComparison.OrdinalIgnoreCase));
                    if (openTab != null)
                    {
                        openTab.FilePath = newPath;
                        openTab.FileName = newName;
                        if (openTab.DockContent != null)
                            openTab.DockContent.Text = openTab.IsModified ? "\u2022 " + newName : newName;
                    }
                }

                e.Cancel = true;
                RefreshFileList();
                RaiseStatus("Renamed to: " + newName);
            }
            catch (Exception ex)
            {
                e.Cancel = true;
                MessageBox.Show("Failed to rename: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshFileList()
        {
            fileTreeView.BeginUpdate();
            fileTreeView.Nodes.Clear();
            nodeStyles.Clear();
            PopulateTreeNode(fileTreeView.Nodes, scriptsDir);
            fileTreeView.ExpandAll();

            if (activeFile != null && activeFile.FilePath != null)
            {
                var node = FindNodeByPath(fileTreeView.Nodes, activeFile.FilePath);
                if (node != null)
                    fileTreeView.SelectedNode = node;
            }
            else if (activeFile != null && activeFile.FilePath == null)
            {
                var node = FindNodeByPath(fileTreeView.Nodes, "UNSAVED:" + activeFile.FileName);
                if (node != null)
                    fileTreeView.SelectedNode = node;
            }

            fileTreeView.EndUpdate();
        }

        private void PopulateTreeNode(RadTreeNodeCollection parentNodes, string dirPath)
        {
            foreach (var subDir in Directory.GetDirectories(dirPath).OrderBy(d => d))
            {
                string dirName = Path.GetFileName(subDir);
                var dirNode = new RadTreeNode(dirName);
                dirNode.Tag = subDir;
                dirNode.Image = folderIcon;
                parentNodes.Add(dirNode);
                SetNodeForeColor(dirNode, Color.FromArgb(80, 80, 80));
                PopulateTreeNode(dirNode.Nodes, subDir);
            }

            foreach (var filePath in Directory.GetFiles(dirPath, "*.py").OrderBy(f => f))
            {
                string fileName = Path.GetFileName(filePath);
                var fileTab = openFiles.Find(f => f.FilePath != null &&
                    f.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));

                string displayName = fileName;
                bool isActive = (fileTab != null && fileTab == activeFile);

                if (isActive)
                    displayName = "\u25b8 " + fileName;
                else if (fileTab != null && fileTab.IsModified)
                    displayName = "\u2022 " + fileName;

                var fileNode = new RadTreeNode(displayName);
                fileNode.Tag = filePath;
                fileNode.Image = pythonFileIcon;

                Color nodeColor;
                if (isActive)
                    nodeColor = Color.FromArgb(0, 90, 180);
                else if (fileTab != null)
                    nodeColor = Color.FromArgb(30, 30, 30);
                else
                    nodeColor = Color.FromArgb(100, 100, 100);

                parentNodes.Add(fileNode);
                SetNodeForeColor(fileNode, nodeColor);
            }

            foreach (var ft in openFiles)
            {
                if (ft.FilePath == null)
                {
                    bool isActive = (ft == activeFile);
                    string displayName = isActive ? "\u25b8 " + ft.FileName
                        : ft.IsModified ? "\u2022 " + ft.FileName
                        : ft.FileName;
                    var node = new RadTreeNode(displayName);
                    node.Tag = "UNSAVED:" + ft.FileName;
                    node.Image = pythonFileIcon;
                    fileTreeView.Nodes.Add(node);
                    SetNodeForeColor(node, isActive ? Color.FromArgb(0, 90, 180) : Color.FromArgb(128, 128, 128));
                }
            }
        }

        private RadTreeNode FindNodeByPath(RadTreeNodeCollection nodes, string path)
        {
            foreach (RadTreeNode node in nodes)
            {
                string tag = node.Tag as string;
                if (tag != null && tag.Equals(path, StringComparison.OrdinalIgnoreCase))
                    return node;
                var found = FindNodeByPath(node.Nodes, path);
                if (found != null) return found;
            }
            return null;
        }

        private void SaveCurrentFileState()
        {
            if (activeFile == null || activeFile.Editor == null) return;
            activeFile.CursorPosition = activeFile.Editor.GetCaretIndex();
        }

        private void SwitchToFile(FileTab tab)
        {
            if (tab == null) return;
            if (tab == activeFile)
            {
                tab.DockContent?.Activate();
                return;
            }

            SaveCurrentFileState();

            if (tab.DockContent == null || tab.DockContent.IsDisposed)
                CreateEditorForTab(tab);

            activeFile = tab;

            tab.DockContent.Activate();

            if (pythonEditor != null) UpdateCursorPositionStatus();
            RefreshFileList();
            RaiseStatus("Editing: " + tab.FileName);
        }

        private void OnNewFile(object sender, EventArgs e)
        {
            string targetDir = scriptsDir;
            var selectedNode = fileTreeView.SelectedNode;
            if (selectedNode != null)
            {
                string tag = selectedNode.Tag as string;
                if (tag != null && Directory.Exists(tag))
                    targetDir = tag;
                else if (tag != null && File.Exists(tag))
                    targetDir = Path.GetDirectoryName(tag);
            }

            untitledCounter++;
            string name = "untitled" + untitledCounter + ".py";
            string filePath = Path.Combine(targetDir, name);

            File.WriteAllText(filePath, "");

            var tab = new FileTab
            {
                FilePath = filePath,
                FileName = name,
                Content = "",
                CursorPosition = 0,
                IsModified = false
            };
            openFiles.Add(tab);
            SwitchToFile(tab);
            RefreshFileList();

            var node = FindNodeByPath(fileTreeView.Nodes, filePath);
            if (node != null)
                BeginNodeRename(node);

            RaiseStatus("New file: " + name);
        }

        private void OnOpenFile(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog
            {
                Filter = "Python files (*.py)|*.py|All files (*.*)|*.*",
                InitialDirectory = scriptsDir
            })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    var existing = openFiles.Find(f => f.FilePath != null &&
                        f.FilePath.Equals(dlg.FileName, StringComparison.OrdinalIgnoreCase));
                    if (existing != null)
                    {
                        SwitchToFile(existing);
                        return;
                    }

                    string fileName = Path.GetFileName(dlg.FileName);
                    string targetPath = Path.Combine(scriptsDir, fileName);
                    if (!dlg.FileName.Equals(targetPath, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Copy(dlg.FileName, targetPath, true);
                    }

                    var tab = new FileTab
                    {
                        FilePath = targetPath,
                        FileName = fileName,
                        Content = File.ReadAllText(targetPath),
                        CursorPosition = 0,
                        IsModified = false
                    };
                    openFiles.Add(tab);
                    SwitchToFile(tab);
                    RaiseStatus("Opened: " + fileName);
                }
            }
        }

        private void OnSaveFile(object sender, EventArgs e)
        {
            if (activeFile == null) return;

            if (activeFile.FilePath == null)
            {
                OnSaveFileAs(sender, e);
                return;
            }

            string content = activeFile.Editor != null ? activeFile.Editor.GetText() : activeFile.Content ?? "";
            File.WriteAllText(activeFile.FilePath, content);
            activeFile.Content = content;
            activeFile.IsModified = false;
            if (activeFile.DockContent != null)
                activeFile.DockContent.Text = activeFile.FileName;
            RefreshFileList();
            RaiseStatus("Saved: " + activeFile.FileName);
        }

        private void OnSaveFileAs(object sender, EventArgs e)
        {
            if (activeFile == null) return;

            using (var dlg = new SaveFileDialog
            {
                Filter = "Python files (*.py)|*.py|All files (*.*)|*.*",
                DefaultExt = "py",
                InitialDirectory = scriptsDir,
                FileName = activeFile.FileName
            })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    string targetPath = dlg.FileName;
                    string fileName = Path.GetFileName(targetPath);

                    if (!targetPath.StartsWith(scriptsDir, StringComparison.OrdinalIgnoreCase))
                    {
                        targetPath = Path.Combine(scriptsDir, fileName);
                    }

                    string parentDir = Path.GetDirectoryName(targetPath);
                    if (!Directory.Exists(parentDir))
                        Directory.CreateDirectory(parentDir);

                    string saveContent = activeFile.Editor != null ? activeFile.Editor.GetText() : activeFile.Content ?? "";
                    File.WriteAllText(targetPath, saveContent);
                    activeFile.Content = saveContent;
                    activeFile.FilePath = targetPath;
                    activeFile.FileName = fileName;
                    activeFile.IsModified = false;
                    if (activeFile.DockContent != null)
                        activeFile.DockContent.Text = fileName;
                    RefreshFileList();
                    RaiseStatus("Saved: " + fileName);
                }
            }
        }

        private void OnCloseFile(object sender, EventArgs e)
        {
            if (activeFile != null)
                OnCloseFileTab(activeFile);
        }

        private void CloseDockContent(FileTab tab)
        {
            if (tab?.DockContent == null) return;
            tab.DockContent.AllowClose = true;
            tab.DockContent.Close();
            tab.DockContent = null;
            tab.Editor = null;
        }

        private void OnCloseFileTab(FileTab tab)
        {
            if (tab == null) return;
            if (openFiles.Count <= 1)
            {
                RaiseStatus("Cannot close the last file.");
                return;
            }

            if (tab.IsModified)
            {
                var result = MessageBox.Show(
                    "Save changes to " + tab.FileName + "?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Cancel) return;
                if (result == DialogResult.Yes)
                {
                    string content = tab.Editor != null ? tab.Editor.GetText() : tab.Content ?? "";
                    if (tab.FilePath != null)
                        File.WriteAllText(tab.FilePath, content);
                }
            }

            int idx = openFiles.IndexOf(tab);
            openFiles.Remove(tab);

            tab.DockContent.AllowClose = true;
            tab.DockContent.Close();
            tab.DockContent = null;
            tab.Editor = null;

            int newIdx = Math.Min(idx, openFiles.Count - 1);
            var next = openFiles[newIdx];
            if (tab == activeFile)
            {
                activeFile = null;
                SwitchToFile(next);
            }
            RefreshFileList();
        }

        private void SaveAllFilesToDisk()
        {
            foreach (var ft in openFiles)
            {
                string content = ft.Editor != null ? ft.Editor.GetText() : ft.Content ?? "";
                ft.Content = content;

                if (ft.FilePath == null)
                    ft.FilePath = Path.Combine(scriptsDir, ft.FileName);

                File.WriteAllText(ft.FilePath, content);
                ft.IsModified = false;
                if (ft.DockContent != null)
                    ft.DockContent.Text = ft.FileName;
            }
            RefreshFileList();
        }

        private bool packageOperationInProgress = false;

        private void SetPackageControlsEnabled(bool enabled)
        {
            installBtn.Enabled = enabled;
            uninstallBtn.Enabled = enabled;
            packageNameBox.Enabled = enabled;
        }

        private void OnInstallPackage(object sender, EventArgs e)
        {
            string pkg = packageNameBox.Text.Trim();
            if (string.IsNullOrEmpty(pkg)) return;

            if (venvInitializing)
            {
                AppendOutput("Python environment is still being set up. Please wait...\n", Color.FromArgb(180, 140, 0));
                return;
            }

            if (packageOperationInProgress)
            {
                AppendOutput("A package operation is already in progress. Please wait...\n", Color.FromArgb(180, 140, 0));
                return;
            }

            if (!pythonRunner.PythonAvailable)
            {
                AppendOutput("Cannot install packages: " + pythonRunner.PythonError + "\n", Color.FromArgb(200, 0, 0));
                RaiseStatus("Python not available");
                return;
            }

            packageOperationInProgress = true;
            SetPackageControlsEnabled(false);
            ShowDockPanel(outputDockContent);

            AppendOutput("Installing " + pkg + "...\n", Color.FromArgb(100, 100, 100));
            RaiseStatus("Installing " + pkg + "...");

            pythonRunner.InstallPackageAsync(pkg,
                line => RunOnUIThread(() =>
                {
                    AppendOutput("  " + line + "\n", Color.FromArgb(100, 100, 100));
                }),
                result => RunOnUIThread(() =>
                {
                    packageOperationInProgress = false;
                    SetPackageControlsEnabled(true);

                    if (result.Success)
                    {
                        AppendOutput("Successfully installed: " + pkg + "\n", Color.FromArgb(0, 128, 0));
                        RaiseStatus(pkg + " installed successfully.");
                        if (pythonRunner.VenvReady)
                            symbolAnalyzer.LoadSymbolsFromVenv(pythonRunner.VenvPath);
                        IntrospectInstalledModules();
                    }
                    else
                    {
                        AppendOutput("Failed to install " + pkg + ":\n" + result.Error + "\n", Color.FromArgb(200, 0, 0));
                        RaiseStatus("Installation failed for " + pkg);
                    }

                    OnRefreshPackages(null, null);
                })
            );
        }

        private static readonly HashSet<string> ProtectedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "pandas", "numpy", "matplotlib", "pillow", "PIL", "pip", "setuptools"
        };

        private void OnUninstallPackage(object sender, EventArgs e)
        {
            string pkg = packageNameBox.Text.Trim();
            if (string.IsNullOrEmpty(pkg)) return;

            if (ProtectedPackages.Contains(pkg))
            {
                AppendOutput("Cannot uninstall '" + pkg + "': it is a core dependency required by the data science environment.\n", Color.FromArgb(200, 0, 0));
                RaiseStatus("'" + pkg + "' is a protected package");
                return;
            }

            if (venvInitializing)
            {
                AppendOutput("Python environment is still being set up. Please wait...\n", Color.FromArgb(180, 140, 0));
                return;
            }

            if (packageOperationInProgress)
            {
                AppendOutput("A package operation is already in progress. Please wait...\n", Color.FromArgb(180, 140, 0));
                return;
            }

            var confirm = MessageBox.Show("Uninstall " + pkg + "?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            if (!pythonRunner.PythonAvailable)
            {
                AppendOutput("Cannot uninstall packages: " + pythonRunner.PythonError + "\n", Color.FromArgb(200, 0, 0));
                RaiseStatus("Python not available");
                return;
            }

            packageOperationInProgress = true;
            SetPackageControlsEnabled(false);

            AppendOutput("Uninstalling " + pkg + "...\n", Color.FromArgb(100, 100, 100));
            RaiseStatus("Uninstalling " + pkg + "...");

            pythonRunner.UninstallPackageAsync(pkg,
                line => RunOnUIThread(() =>
                {
                    AppendOutput("  " + line + "\n", Color.FromArgb(100, 100, 100));
                }),
                result => RunOnUIThread(() =>
                {
                    packageOperationInProgress = false;
                    SetPackageControlsEnabled(true);

                    if (result.Success)
                    {
                        AppendOutput("Successfully uninstalled: " + pkg + "\n", Color.FromArgb(0, 128, 0));
                        RaiseStatus(pkg + " uninstalled.");
                    }
                    else
                    {
                        AppendOutput("Failed to uninstall " + pkg + ":\n" + result.Error + "\n", Color.FromArgb(200, 0, 0));
                        RaiseStatus("Uninstall failed for " + pkg);
                    }

                    OnRefreshPackages(null, null);
                })
            );
        }

        private void OnQuickInstall(object sender, EventArgs e)
        {
            if (quickCombo.SelectedItem != null)
            {
                packageNameBox.Text = quickCombo.SelectedItem.Text;
                OnInstallPackage(sender, e);
            }
        }

        private void OnRefreshPackages(object sender, EventArgs e)
        {
            LoadPackagesAsync();
        }

        private void LoadPackagesAsync()
        {
            if (packagesLoading) return;
            packagesLoading = true;

            allPackageItems.Clear();
            packageListBox.Items.Clear();
            pkgSearchBox.Text = pkgSearchPlaceholder;
            pkgSearchBox.ForeColor = Color.Gray;
            pkgSearchPlaceholderActive = true;

            if (!pythonRunner.PythonAvailable)
            {
                packageListBox.Items.Add("(Python not available — cannot list packages)");
                packagesLoading = false;
                return;
            }

            packageListBox.Items.Add("Loading packages...");

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                var items = new List<string>();
                string error = null;

                try
                {
                    var result = pythonRunner.ListPackages();

                    if (result.Success && !string.IsNullOrEmpty(result.Output))
                    {
                        var lines = result.Output.Split('\n');
                        foreach (var line in lines)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            var trimmed = line.Trim();
                            if (trimmed.StartsWith("Package") && trimmed.Contains("Version")) continue;
                            if (trimmed.StartsWith("---")) continue;
                            var parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                                items.Add(parts[0] + "  " + parts[1]);
                            else if (parts.Length == 1)
                                items.Add(parts[0]);
                        }
                        items.Sort(StringComparer.OrdinalIgnoreCase);
                    }
                    else if (!result.Success)
                    {
                        error = result.Error;
                    }
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }

                try
                {
                    RunOnUIThread(() =>
                    {
                        allPackageItems.Clear();
                        allPackageItems.AddRange(items);
                        packageListBox.Items.Clear();

                        if (error != null)
                            packageListBox.Items.Add("(Failed to list packages: " + error + ")");
                        else
                            PopulatePackageList("");

                        packagesLoaded = true;
                        packagesLoading = false;
                    });
                }
                catch (InvalidOperationException)
                {
                    packagesLoading = false;
                }
            });
        }

        private bool pkgSearchPlaceholderActive = true;
        private readonly string pkgSearchPlaceholder = "Search packages...";

        private void SetupTooltips()
        {
            var tips = new ToolTip();
            tips.AutoPopDelay = 10000;
            tips.InitialDelay = 400;
            tips.ReshowDelay = 200;

            tips.SetToolTip(installBtn, "Install the named package from PyPI into the virtual environment");
            tips.SetToolTip(uninstallBtn, "Remove the selected package from the virtual environment");
            tips.SetToolTip(refreshBtn, "Reload the list of installed packages");
            tips.SetToolTip(packageNameBox, "Type a package name to install (e.g. scikit-learn, seaborn)");
            tips.SetToolTip(pkgSearchBox, "Filter the package list by name");
            tips.SetToolTip(refSearchBox, "Filter datasets, columns, classes, and variables by name");
            tips.SetToolTip(refTreeView, "Browse available datasets, columns, registered classes, and context variables");
            tips.SetToolTip(quickCombo, "Select a commonly used package to install");
            tips.SetToolTip(quickInstallBtn, "Install the selected quick-install package");
        }

        private void SetupPkgSearch()
        {
            pkgSearchBox.Text = pkgSearchPlaceholder;
            pkgSearchBox.ForeColor = Color.Gray;
            pkgSearchPlaceholderActive = true;

            pkgSearchBox.GotFocus += (s, ev) =>
            {
                if (pkgSearchPlaceholderActive)
                {
                    pkgSearchBox.Text = "";
                    pkgSearchBox.ForeColor = Color.Black;
                    pkgSearchPlaceholderActive = false;
                }
            };

            pkgSearchBox.LostFocus += (s, ev) =>
            {
                if (string.IsNullOrEmpty(pkgSearchBox.Text))
                {
                    pkgSearchBox.Text = pkgSearchPlaceholder;
                    pkgSearchBox.ForeColor = Color.Gray;
                    pkgSearchPlaceholderActive = true;
                }
            };
        }

        private void OnPkgSearchChanged(object sender, EventArgs e)
        {
            if (pkgSearchPlaceholderActive) return;
            PopulatePackageList(pkgSearchBox.Text.Trim());
        }

        private void PopulatePackageList(string filter)
        {
            packageListBox.Items.Clear();
            bool hasFilter = !string.IsNullOrEmpty(filter);

            foreach (var item in allPackageItems)
            {
                if (hasFilter)
                {
                    string name = item.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0];
                    if (name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                }
                packageListBox.Items.Add(item);
            }

            if (packageListBox.Items.Count == 0 && hasFilter)
                packageListBox.Items.Add("No packages match '" + filter + "'");
        }

        private void OnShowHelp(object sender, EventArgs e)
        {
            string help = @"=== Data Science Workbench - Quick Start ===

AVAILABLE DATASETS:
  customers        - 150 customers with demographics
  employees        - 100 employees with salary, dept

HOW TO USE:
  1. Import datasets: from DotNetData import customers, employees
  2. Write Python code in the editor
  3. Press F5 or click Run to execute
  4. Access columns directly: customers.CreditLimit.mean()
  5. Access rows by index: customers[0].FullName
  6. Slice datasets: first_five = customers[0:5]
  7. Use .df for full DataFrame: customers.df.describe()
  8. Install packages via Package Manager tab

EXAMPLE:
  from DotNetData import customers
  print(customers[0].FullName)
  print(customers.CreditLimit.mean())

TIPS:
  - Use 'Insert Snippet' for ready-made code
  - Matplotlib plots save as PNG files
  - All standard Python libraries available
  - Install any pip package via Package Manager
  - .NET host can register classes and context variables";

            MessageBox.Show(help, "Quick Start Guide", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnShowKeyboardShortcuts(object sender, EventArgs e)
        {
            string shortcuts = @"=== Keyboard Shortcuts ===

RUNNING CODE
  F5                    Run script
  Ctrl+Shift+S          Check syntax

FILE
  Ctrl+S                Save script to file
  Ctrl+O                Open script from file

EDITING
  Ctrl+Z                Undo
  Ctrl+Y                Redo
  Ctrl+X / C / V        Cut / Copy / Paste
  Ctrl+A                Select all
  Ctrl+H                Find & Replace
  Ctrl+D                Duplicate current line
  Alt+Up / Down         Move current line up/down
  Tab                   Indent selected lines
  Shift+Tab             Unindent selected lines
  Delete                Delete selection

EDITOR
  Ctrl+Mouse Wheel      Zoom in/out
  Escape                Close autocomplete / Find panel";

            MessageBox.Show(shortcuts, "Keyboard Shortcuts", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnShowEditorFeatures(object sender, EventArgs e)
        {
            string features = @"=== Editor Features ===

SYNTAX HIGHLIGHTING
  Python keywords, strings, numbers, comments,
  and built-in functions are color-coded automatically.

AUTOCOMPLETE
  Displays suggestions as you type, including:
  - Python keywords and built-in functions
  - Dataset names after 'from DotNetData import'
  - Dataset column names (e.g. customers.CreditLimit)
  - Row indexing (e.g. customers[0].FullName, customers[0:5])
  - DataFrame methods (after .df.)
  - Registered class members and context variables
  Press Tab or Enter to accept, Escape to dismiss.

ERROR DETECTION
  - Real-time syntax checking with red underlines
  - Undefined variable warnings with blue underlines
  - Hover over underlined text for error details

LINE NUMBERS
  Displayed in the gutter on the left side.

BRACKET MATCHING
  Matching parentheses, brackets, and braces
  are highlighted when the cursor is adjacent.

CODE SNIPPETS
  Use 'Insert Snippet' in the menu bar for
  ready-made code templates (statistics, plots, etc).

DATA REFERENCE TAB
  Browse all available datasets, their columns,
  data types, and registered classes. Use the search
  box to filter. Click an item to see details and
  example code in the panel below.

PACKAGE MANAGER TAB
  Install or uninstall Python packages. Type a
  package name and click Install. Use the search
  box to filter the installed package list.
  Click Refresh to update the list after changes.

PLOT VIEWER
  When your script calls plt.show(), plots are
  captured and displayed in a viewer window.
  Use Previous/Next to browse multiple plots,
  and Save to export as PNG.";

            MessageBox.Show(features, "Editor Features", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void AppendOutput(string text, Color color)
        {
            RunOnUIThread(() =>
            {
                outputBox.SelectionStart = outputBox.TextLength;
                outputBox.SelectionLength = 0;
                outputBox.SelectionColor = color;
                outputBox.AppendText(text);
                outputBox.ScrollToCaret();
            });
        }

        private void RaiseStatus(string msg)
        {
            StatusChanged?.Invoke(this, msg);
        }

        private void InsertSnippet(string code)
        {
            if (pythonEditor == null) return;
            int pos = pythonEditor.GetCaretIndex();
            suppressHighlight = true;
            pythonEditor.SetCaretIndex(pos);
            pythonEditor.ClearSelection();
            pythonEditor.InsertAtCaret(code);
            suppressHighlight = false;
            pythonEditor.SetCaretIndex(pos + code.Length);
            pythonEditor.Focus();
            ApplySyntaxHighlighting();
        }

        private string GetDefaultScript()
        {
            return @"from DotNetData import customers, employees

# Access columns directly: customers.CreditLimit.mean()
# Access rows by index: customers[0].FullName
# Slice datasets: customers[0:5]
# Use .df for full DataFrame: customers.df.describe()

print('=== Data Science Workbench ===')
print()

# Quick look at customers
print(f'Customers: {len(customers)} records')
print(f'First customer: {customers[0].FullName}')
print(f'Average credit limit: ${customers.CreditLimit.mean():.2f}')
print()
print('=== Customer Summary ===')
print(customers.df.describe())
";
        }

        private string GetLoadDataSnippet()
        {
            return @"
from DotNetData import customers, employees

for name, ds in [('customers', customers), ('employees', employees)]:
    print(f'  {name}: {len(ds)} rows, {len(ds.df.columns)} columns')
";
        }

        private string GetStatsSnippet()
        {
            return @"
from DotNetData import customers

print('=== Descriptive Statistics ===')
print(customers.df.describe())
print()
print('=== Data Types ===')
print(customers.df.dtypes)
print()
print('=== Missing Values ===')
print(customers.df.isnull().sum())
";
        }

        private string GetHistogramSnippet()
        {
            return @"
from DotNetData import employees
import matplotlib.pyplot as plt

fig, ax = plt.subplots(figsize=(10, 6))
ax.hist(employees.Salary, bins=30, edgecolor='black', alpha=0.7)
ax.set_xlabel('Salary ($)')
ax.set_ylabel('Count')
ax.set_title('Employee Salary Distribution')
plt.tight_layout()
plt.show()
";
        }

        private string GetScatterSnippet()
        {
            return @"
from DotNetData import employees
import matplotlib.pyplot as plt

fig, ax = plt.subplots(figsize=(10, 6))
ax.scatter(employees.Salary, employees.PerformanceScore,
           alpha=0.6, edgecolors='black', linewidth=0.5)
ax.set_xlabel('Salary ($)')
ax.set_ylabel('Performance Score')
ax.set_title('Salary vs Performance Score')
plt.tight_layout()
plt.show()
";
        }

        private string GetGroupBySnippet()
        {
            return @"
from DotNetData import employees

print('=== Average Salary by Department ===')
group = employees.df.groupby('Department').agg(
    Count=('Id', 'count'),
    Avg_Salary=('Salary', 'mean'),
    Avg_Performance=('PerformanceScore', 'mean')
).round(2)
print(group.sort_values('Avg_Salary', ascending=False))
";
        }

        private string GetCorrelationSnippet()
        {
            return @"
from DotNetData import employees
import matplotlib.pyplot as plt

numeric_cols = employees.df.select_dtypes(include='number')
corr = numeric_cols.corr()
print('=== Correlation Matrix ===')
print(corr.round(3))

fig, ax = plt.subplots(figsize=(10, 8))
im = ax.imshow(corr, cmap='coolwarm', vmin=-1, vmax=1)
ax.set_xticks(range(len(corr.columns)))
ax.set_yticks(range(len(corr.columns)))
ax.set_xticklabels(corr.columns, rotation=45, ha='right')
ax.set_yticklabels(corr.columns)
plt.colorbar(im)
ax.set_title('Employee Data Correlation Matrix')
plt.tight_layout()
plt.show()
";
        }

        private string GetTimeSeriesSnippet()
        {
            return @"
from DotNetData import customers
import pandas as pd
import matplotlib.pyplot as plt

df = customers.df.copy()
df['RegistrationDate'] = pd.to_datetime(df['RegistrationDate'])
monthly = df.set_index('RegistrationDate').resample('M').size()

fig, ax = plt.subplots(figsize=(12, 6))
ax.plot(monthly.index, monthly.values, linewidth=1.5, marker='o', markersize=3)
ax.set_xlabel('Date')
ax.set_ylabel('New Registrations')
ax.set_title('Customer Registrations Over Time')
plt.xticks(rotation=45)
plt.tight_layout()
plt.show()
";
        }

        private string GetImageDisplaySnippet()
        {
            return @"
from DotNetData import customers
import matplotlib.pyplot as plt
import numpy as np
from PIL import Image

# Find the first image column in a dataset
dataset = customers
img_col = None
for col in dataset.columns:
    val = dataset[col].dropna().iloc[0] if len(dataset[col].dropna()) > 0 else None
    if isinstance(val, Image.Image):
        img_col = col
        break

if img_col is None:
    print('No image columns found in dataset.')
elif len(dataset[img_col].dropna()) == 0:
    print('Image column found but contains no data.')
else:
    imgs = dataset[img_col].dropna().reset_index(drop=True)
    n = min(16, len(imgs))
    cols = 4
    rows = (n + cols - 1) // cols

    fig, axes = plt.subplots(rows, cols, figsize=(8, 2 * rows))
    if rows == 1:
        axes = [axes]
    for idx in range(rows * cols):
        ax = axes[idx // cols][idx % cols] if cols > 1 else axes[idx // cols]
        if idx < n:
            img = imgs.iloc[idx]
            ax.imshow(np.array(img))
            ax.set_title(f""Image {idx+1}"", fontsize=8)
        ax.axis('off')
    plt.suptitle(img_col + ' images')
    plt.tight_layout()
    plt.show()

    # Compute average color across all images
    r_avg, g_avg, b_avg = [], [], []
    for img in imgs:
        arr = np.array(img)
        r_avg.append(arr[:,:,0].mean())
        g_avg.append(arr[:,:,1].mean())
        b_avg.append(arr[:,:,2].mean())

    print(f'Average R: {np.mean(r_avg):.1f}')
    print(f'Average G: {np.mean(g_avg):.1f}')
    print(f'Average B: {np.mean(b_avg):.1f}')
";
        }

        private string GetLinearRegressionSnippet()
        {
            return @"
from DotNetData import employees
from sklearn.linear_model import LinearRegression
from sklearn.model_selection import train_test_split
from sklearn.metrics import mean_absolute_error, r2_score
import matplotlib.pyplot as plt
import pandas as pd
import numpy as np

# Salary is generated from: DeptBase + YearsEmployed*$2800 + TitleLevel*12% + noise
# This model should recover those relationships with high R²

df = employees.df.copy()

features = pd.get_dummies(df[['YearsEmployed', 'PerformanceScore', 'IsRemote', 'Department']], drop_first=True)
target = df['Salary']

X_train, X_test, y_train, y_test = train_test_split(features, target, test_size=0.25, random_state=42)

model = LinearRegression()
model.fit(X_train, y_train)
y_pred = model.predict(X_test)

print('=== Salary Prediction — Linear Regression ===')
print()
print('Known data correlations (built into the dataset):')
print('  Salary = DepartmentBase + YearsEmployed * ~$2,800 + TitleLevel * 12%')
print('  Engineering base ~$115K, Support base ~$60K')
print()
print(f'R² Score:           {r2_score(y_test, y_pred):.4f}')
print(f'Mean Absolute Error: ${mean_absolute_error(y_test, y_pred):,.2f}')
print()

coefs = pd.Series(model.coef_, index=features.columns).sort_values()
print('Recovered Coefficients (compare to known structure):')
tenure_coef = coefs.get('YearsEmployed', 0)
print(f'  YearsEmployed coefficient: ${tenure_coef:+,.0f}/yr  (expected ~$2,800/yr)')
print()
print('All feature coefficients:')
for name, val in coefs.items():
    print(f'  {name:30s} {val:+,.2f}')

# Show the actual correlations in the raw data
print()
print('Raw correlation matrix (key columns):')
corr_cols = ['Salary', 'YearsEmployed', 'PerformanceScore', 'IsRemote']
print(df[corr_cols].corr().round(3).to_string())

fig, axes = plt.subplots(1, 3, figsize=(18, 5))

axes[0].scatter(df['YearsEmployed'], df['Salary'], alpha=0.4, c='steelblue', edgecolors='black', linewidth=0.3)
z = np.polyfit(df['YearsEmployed'], df['Salary'], 1)
x_line = np.linspace(df['YearsEmployed'].min(), df['YearsEmployed'].max(), 100)
axes[0].plot(x_line, np.polyval(z, x_line), 'r-', linewidth=2, label=f'slope=${z[0]:,.0f}/yr')
axes[0].set_xlabel('Years Employed')
axes[0].set_ylabel('Salary ($)')
axes[0].set_title('Tenure → Salary (Known Correlation)')
axes[0].legend()

axes[1].scatter(y_test, y_pred, alpha=0.5, edgecolors='black', linewidth=0.5)
mn, mx = min(y_test.min(), y_pred.min()), max(y_test.max(), y_pred.max())
axes[1].plot([mn, mx], [mn, mx], 'r--', linewidth=1)
axes[1].set_xlabel('Actual Salary ($)')
axes[1].set_ylabel('Predicted Salary ($)')
axes[1].set_title(f'Actual vs Predicted (R²={r2_score(y_test, y_pred):.3f})')

top_coefs = coefs.abs().nlargest(10).index
coefs[top_coefs].sort_values().plot.barh(ax=axes[2], color=['#d9534f' if v < 0 else '#5cb85c' for v in coefs[top_coefs].sort_values()])
axes[2].set_xlabel('Coefficient Value')
axes[2].set_title('Top 10 Feature Coefficients')

plt.tight_layout()
plt.show()
";
        }

        private string GetClassificationSnippet()
        {
            return @"
from DotNetData import employees
from sklearn.ensemble import RandomForestClassifier
from sklearn.model_selection import train_test_split
from sklearn.metrics import classification_report, confusion_matrix
import matplotlib.pyplot as plt
import pandas as pd
import numpy as np

# Each department has distinct salary ranges, remote rates, and performance profiles:
#   Engineering: base $115K, 60% remote  |  Support: base $60K, 65% remote
#   Finance: base $95K, 25% remote       |  Sales: base $75K, 40% remote
# Salary is the strongest signal since each dept has a unique base salary.

df = employees.df.copy()

# Show the correlations the classifier will exploit
print('=== Department Classification — Random Forest ===')
print()
print('Department salary ranges (the key signal):')
dept_stats = df.groupby('Department')['Salary'].agg(['mean', 'std']).sort_values('mean', ascending=False)
for dept, row in dept_stats.iterrows():
    remote_pct = df[df['Department'] == dept]['IsRemote'].mean() * 100
    print(f'  {dept:15s}  avg ${row[""mean""]:>9,.0f} ± ${row[""std""]:>7,.0f}   remote: {remote_pct:.0f}%')
print()

feature_cols = ['Salary', 'PerformanceScore', 'YearsEmployed', 'IsRemote']
X = df[feature_cols].copy()
X['IsRemote'] = X['IsRemote'].astype(int)
y = df['Department']

X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.25, random_state=42, stratify=y)

model = RandomForestClassifier(n_estimators=100, random_state=42)
model.fit(X_train, y_train)
y_pred = model.predict(X_test)

print(f'Accuracy: {model.score(X_test, y_test):.2%}')
print()
print(classification_report(y_test, y_pred, zero_division=0))

importances = pd.Series(model.feature_importances_, index=feature_cols).sort_values()
print('Feature importance (Salary dominates because each dept has a unique base):')
for feat, imp in importances.sort_values(ascending=False).items():
    print(f'  {feat:20s} {imp:.3f}')

fig, axes = plt.subplots(1, 3, figsize=(18, 5))

importances.plot.barh(ax=axes[0], color='steelblue')
axes[0].set_xlabel('Importance')
axes[0].set_title('Feature Importance')

for dept in sorted(df['Department'].unique()):
    mask = df['Department'] == dept
    axes[1].scatter(df.loc[mask, 'Salary'], df.loc[mask, 'IsRemote'].astype(int) + np.random.uniform(-0.15, 0.15, mask.sum()),
                    alpha=0.5, label=dept, s=20)
axes[1].set_xlabel('Salary ($)')
axes[1].set_ylabel('Is Remote (jittered)')
axes[1].set_title('Dept Separation by Salary & Remote')
axes[1].legend(fontsize=6, ncol=2, loc='center right')

labels = sorted(y.unique())
cm = confusion_matrix(y_test, y_pred, labels=labels)
im = axes[2].imshow(cm, cmap='Blues')
axes[2].set_xticks(range(len(labels)))
axes[2].set_yticks(range(len(labels)))
axes[2].set_xticklabels(labels, rotation=45, ha='right', fontsize=7)
axes[2].set_yticklabels(labels, fontsize=7)
axes[2].set_xlabel('Predicted')
axes[2].set_ylabel('Actual')
axes[2].set_title('Confusion Matrix')
for i in range(len(labels)):
    for j in range(len(labels)):
        axes[2].text(j, i, str(cm[i, j]), ha='center', va='center',
                     color='white' if cm[i, j] > cm.max() / 2 else 'black', fontsize=7)

plt.tight_layout()
plt.show()
";
        }

        private string GetClusteringSnippet()
        {
            return @"
from DotNetData import employees
from sklearn.cluster import KMeans
from sklearn.preprocessing import StandardScaler
import matplotlib.pyplot as plt
import pandas as pd
import numpy as np

df = employees.df.copy()

cluster_features = ['Salary', 'PerformanceScore', 'YearsEmployed']
X = df[cluster_features].dropna()
scaler = StandardScaler()
X_scaled = scaler.fit_transform(X)

inertias = []
K_range = range(2, 9)
for k in K_range:
    km = KMeans(n_clusters=k, random_state=42, n_init=10)
    km.fit(X_scaled)
    inertias.append(km.inertia_)

n_clusters = 4
kmeans = KMeans(n_clusters=n_clusters, random_state=42, n_init=10)
df.loc[X.index, 'Cluster'] = kmeans.fit_predict(X_scaled)

print('=== Employee Clustering — K-Means ===')
print(f'Number of clusters: {n_clusters}')
print()
for c in range(n_clusters):
    subset = df[df['Cluster'] == c]
    print(f'Cluster {c} ({len(subset)} employees):')
    print(f'  Avg Salary:      ${subset[""Salary""].mean():,.0f}')
    print(f'  Avg Performance:  {subset[""PerformanceScore""].mean():.2f}')
    print(f'  Avg Tenure:       {subset[""YearsEmployed""].mean():.1f} years')
    print(f'  Top Departments:  {"", "".join(subset[""Department""].value_counts().head(3).index)}')
    print()

fig, axes = plt.subplots(1, 3, figsize=(18, 5))

axes[0].plot(list(K_range), inertias, 'bo-')
axes[0].axvline(x=n_clusters, color='r', linestyle='--', alpha=0.7)
axes[0].set_xlabel('Number of Clusters (k)')
axes[0].set_ylabel('Inertia')
axes[0].set_title('Elbow Method')

colors = plt.cm.Set2(np.linspace(0, 1, n_clusters))
for c in range(n_clusters):
    mask = df['Cluster'] == c
    axes[1].scatter(df.loc[mask, 'Salary'], df.loc[mask, 'PerformanceScore'],
                    c=[colors[c]], label=f'Cluster {c}', alpha=0.6, edgecolors='black', linewidth=0.3)
axes[1].set_xlabel('Salary ($)')
axes[1].set_ylabel('Performance Score')
axes[1].set_title('Clusters: Salary vs Performance')
axes[1].legend()

for c in range(n_clusters):
    mask = df['Cluster'] == c
    axes[2].scatter(df.loc[mask, 'YearsEmployed'], df.loc[mask, 'Salary'],
                    c=[colors[c]], label=f'Cluster {c}', alpha=0.6, edgecolors='black', linewidth=0.3)
axes[2].set_xlabel('Years Employed')
axes[2].set_ylabel('Salary ($)')
axes[2].set_title('Clusters: Tenure vs Salary')
axes[2].legend()

plt.tight_layout()
plt.show()
";
        }

        private string GetPCASnippet()
        {
            return @"
from DotNetData import customers
from sklearn.decomposition import PCA
from sklearn.preprocessing import StandardScaler, LabelEncoder
import matplotlib.pyplot as plt
import pandas as pd
import numpy as np

df = customers.df.copy()

tier_enc = LabelEncoder()
df['TierEncoded'] = tier_enc.fit_transform(df['Tier'])

feature_cols = ['Age', 'CreditLimit', 'TierEncoded', 'IsActive']
X = df[feature_cols].copy()
X['IsActive'] = X['IsActive'].astype(int)
X = X.dropna()

scaler = StandardScaler()
X_scaled = scaler.fit_transform(X)

pca = PCA()
X_pca = pca.fit_transform(X_scaled)

print('=== Customer Segmentation — PCA ===')
print()
print('Explained Variance Ratio:')
for i, var in enumerate(pca.explained_variance_ratio_):
    cumulative = sum(pca.explained_variance_ratio_[:i+1])
    bar = '#' * int(var * 50)
    print(f'  PC{i+1}: {var:.3f} (cumulative: {cumulative:.3f})  {bar}')
print()

print('Principal Component Loadings:')
loadings = pd.DataFrame(pca.components_.T, index=feature_cols,
                         columns=[f'PC{i+1}' for i in range(len(feature_cols))])
print(loadings.round(3))

fig, axes = plt.subplots(1, 3, figsize=(18, 5))

axes[0].bar(range(1, len(pca.explained_variance_ratio_) + 1),
            pca.explained_variance_ratio_, color='steelblue', alpha=0.8)
axes[0].plot(range(1, len(pca.explained_variance_ratio_) + 1),
             np.cumsum(pca.explained_variance_ratio_), 'ro-')
axes[0].set_xlabel('Principal Component')
axes[0].set_ylabel('Variance Explained')
axes[0].set_title('Scree Plot')
axes[0].set_xticks(range(1, len(feature_cols) + 1))

tiers = df.loc[X.index, 'Tier']
tier_names = sorted(tiers.unique())
colors = plt.cm.Set1(np.linspace(0, 1, len(tier_names)))
for i, tier in enumerate(tier_names):
    mask = tiers == tier
    axes[1].scatter(X_pca[mask, 0], X_pca[mask, 1],
                    c=[colors[i]], label=tier, alpha=0.5, edgecolors='black', linewidth=0.3)
axes[1].set_xlabel('PC1')
axes[1].set_ylabel('PC2')
axes[1].set_title('Customers in PCA Space (by Tier)')
axes[1].legend()

for i, feat in enumerate(feature_cols):
    axes[2].arrow(0, 0, pca.components_[0, i], pca.components_[1, i],
                  head_width=0.05, head_length=0.02, fc='steelblue', ec='steelblue')
    axes[2].text(pca.components_[0, i] * 1.15, pca.components_[1, i] * 1.15,
                 feat, fontsize=9, ha='center')
axes[2].set_xlabel('PC1')
axes[2].set_ylabel('PC2')
axes[2].set_title('Feature Loadings (Biplot)')
axes[2].axhline(y=0, color='gray', linestyle='--', linewidth=0.5)
axes[2].axvline(x=0, color='gray', linestyle='--', linewidth=0.5)

plt.tight_layout()
plt.show()
";
        }

    }

    public class ContextVariable
    {
        public string Name { get; set; }
        public string PythonLiteral { get; set; }
        public string TypeDescription { get; set; }
    }

    public class PythonClassInfo
    {
        public string PythonCode { get; set; }
        public string Description { get; set; }
        public string Example { get; set; }
        public string Notes { get; set; }
    }

}
