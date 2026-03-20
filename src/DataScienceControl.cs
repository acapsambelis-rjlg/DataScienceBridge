using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using CodeEditor;
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
        private DataSciencePythonTooltipProvider tooltipProvider;
        private List<Diagnostic> symbolDiagnostics = new List<Diagnostic>();
        private List<Diagnostic> syntaxDiagnostics = new List<Diagnostic>();
        private PythonSymbolAnalyzer symbolAnalyzer = new PythonSymbolAnalyzer();
        private Dictionary<string, Func<IInMemoryDataSource>> inMemoryDataSources = new Dictionary<string, Func<IInMemoryDataSource>>();
        private Dictionary<string, IStreamingDataSource> streamingDataSources = new Dictionary<string, IStreamingDataSource>();
        private Dictionary<string, Type> inMemoryDataTypes = new Dictionary<string, Type>();
        private Dictionary<string, PythonClassInfo> registeredPythonClasses = new Dictionary<string, PythonClassInfo>();
        private Dictionary<string, ContextVariable> contextVariables = new Dictionary<string, ContextVariable>();
        private ContextMenuStrip fileContextMenu;
        private float editorFontSize = 10f;
        private const float MinFontSize = 6f;
        private const float MaxFontSize = 28f;
        private const float DefaultFontSize = 10f;
        private Font editorFont;
        private CodeTextBox pythonEditor => activeFile?.Editor;

        private List<RunConfiguration> runConfigurations = new List<RunConfiguration>();
        private int selectedConfigIndex = -1;
        private string configFilePath;

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
        private int inputStartPosition = -1;

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

        private ImageList fileTreeImageList;

        private void CreateFileTreeIcons()
        {
            fileTreeImageList = new ImageList();
            fileTreeImageList.ImageSize = new Size(16, 16);
            fileTreeImageList.ColorDepth = ColorDepth.Depth32Bit;

            fileTreeImageList.Images.Add("folder", DrawFolderIcon(false));
            fileTreeImageList.Images.Add("folder_open", DrawFolderIcon(true));
            fileTreeImageList.Images.Add("python", DrawPythonFileIcon());

            fileTreeView.ImageList = fileTreeImageList;
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
            SetupSnippetMenu();
            SetupToolbarActions();
            SetupSyntaxHighlighting();
            RegisterAllDatasetsInMemory();
            PopulateReferenceTree();
            SetupRefSearch();
            SetupPkgSearch();
            SetupTooltips();
            InitializeFileSystem();
            SetupFileListEvents();
            outputBox.KeyDown += OnOutputBoxKeyDown;
            outputBox.KeyPress += OnOutputBoxKeyPress;

            this.HandleCreated += (s, e) =>
            {
                filesDockContent.Show(dockPanel, DockState.DockLeftAutoHide);
                outputDockContent.Show(dockPanel, DockState.DockBottomAutoHide);
                packagesDockContent.Show(dockPanel, DockState.DockRightAutoHide);

                var initialActive = activeFile;
                foreach (var tab in openFiles)
                    CreateEditorForTab(tab);

                activeFile = initialActive;
                if (activeFile?.DockContent != null)
                    activeFile.DockContent.Activate();

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

                BeginInvoke((Action)(() =>
                {
                    ApplySyntaxHighlighting();
                    if (activeFile != null) activeFile.IsModified = false;
                    RefreshFileList();
                    if (activeFile?.Editor != null)
                        activeFile.Editor.Refresh();
                }));
            };
        }

        private void InitializeDocking()
        {
            dockPanel.Theme = new WeifenLuo.WinFormsUI.Docking.VS2015LightTheme();
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

        private void SetupSyntaxHighlighting()
        {
            completionProvider = new DataSciencePythonCompletionProvider();
            completionProvider.SetHelperFunctions(PythonRunner.BuiltInHelperNames);
            tooltipProvider = new DataSciencePythonTooltipProvider();
            tooltipProvider.LoadFromEmbeddedResources();
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
            editor.TooltipProvider = tooltipProvider;

            tab.Editor = editor;

            var content = new FileDockContent();
            content.Text = tab.IsModified ? "\u2022 " + tab.FileName : tab.FileName;
            content.Icon = DockIcons.CreateEditorIcon();
            content.Controls.Add(editor);
            content.CloseRequested += (s, e) => OnCloseFileTab(tab);
            content.TabPageContextMenuStrip = CreateTabContextMenu(tab);
            tab.DockContent = content;

            SetupEditorEvents(tab, editor);

            content.Show(dockPanel, DockState.Document);
            SetupTabMiddleClick(content);

            suppressHighlight = true;
            editor.SetText(tab.Content ?? "");
            editor.SetCaretIndex(Math.Min(tab.CursorPosition, (tab.Content ?? "").Length));
            editor.ClearSelection();
            if (tab.CursorPosition == 0)
                editor.ScrollToTop();
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

        private void GoToLine(int lineIndex)
        {
            if (pythonEditor == null) return;
            if (lineIndex < 0 || lineIndex >= pythonEditor.GetLineCount()) return;
            int charIdx = pythonEditor.GetFirstCharIndexFromLine(lineIndex);
            pythonEditor.SetCaretIndex(charIdx);
            pythonEditor.ClearSelection();
            pythonEditor.ScrollToCaretPosition();
            RaiseStatus("Ln " + (lineIndex + 1));
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

        private void ZoomEditor(float delta)
        {
            float newSize;
            if (delta == 0f)
                newSize = DefaultFontSize;
            else
                newSize = editorFontSize + delta;

            if (newSize < MinFontSize) newSize = MinFontSize;
            if (newSize > MaxFontSize) newSize = MaxFontSize;
            if (Math.Abs(newSize - editorFontSize) < 0.01f) return;

            editorFontSize = newSize;
            var oldFont = editorFont;
            editorFont = ResolveMonoFont(editorFontSize);
            if (oldFont != null && oldFont != editorFont)
            {
                try { oldFont.Dispose(); } catch { }
            }

            foreach (var tab in openFiles)
                if (tab.Editor != null)
                    tab.Editor.EditorFont = editorFont;

            if (pythonEditor != null) UpdateCursorPositionStatus();
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

        public void LoadData(List<Customer> customers, List<Employee> employees)
        {
            this.customers = customers;
            this.employees = employees;

            RegisterAllDatasetsInMemory();
            PopulateReferenceTree();
        }

        public void RegisterInMemoryData(string name, System.Collections.IEnumerable values, string columnName = "value")
        {
            inMemoryDataSources[name] = () =>
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine(columnName);
                foreach (var item in values)
                {
                    string s = item != null ? item.ToString() : "";
                    if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
                        s = "\"" + s.Replace("\"", "\"\"") + "\"";
                    sb.AppendLine(s);
                }
                return new StringDataSource(sb.ToString());
            };
            PopulateReferenceTree();
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
                    if (PythonVisibleHelper.PrepareItem != null)
                        PythonVisibleHelper.PrepareItem(item);
                    try
                    {
                        var vals = new List<string>();
                        foreach (var fp in flatProps)
                        {
                            var val = fp.GetValue(item);
                            string s;
                            if (PythonVisibleHelper.IsImageType(fp.LeafType))
                            {
                                if (val is System.Drawing.Bitmap bmp)
                                    s = PythonVisibleHelper.BitmapToBase64(bmp);
                                else if (val is System.Drawing.Image img)
                                    using (var tmp = new System.Drawing.Bitmap(img))
                                        s = PythonVisibleHelper.BitmapToBase64(tmp);
                                else
                                    s = "";
                            }
                            else if (PythonVisibleHelper.IsDictionaryType(fp.LeafType))
                            {
                                s = PythonVisibleHelper.DictionaryToJson(val);
                            }
                            else if (fp.IsSubObject)
                            {
                                s = PythonVisibleHelper.SubObjectToJson(val);
                            }
                            else
                                s = val != null ? val.ToString() : "";
                            if (s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r"))
                                s = "\"" + s.Replace("\"", "\"\"") + "\"";
                            vals.Add(s);
                        }
                        sb.AppendLine(string.Join(",", vals));
                    }
                    finally
                    {
                        if (PythonVisibleHelper.ReleaseItem != null)
                            PythonVisibleHelper.ReleaseItem(item);
                    }
                }
                var imgCols = new List<string>();
                foreach (var fp in flatProps)
                {
                    if (PythonVisibleHelper.IsImageType(fp.LeafType))
                        imgCols.Add(fp.ColumnName);
                }
                return new StringDataSource(sb.ToString(), imgCols.ToArray());
            };
            UpdateDynamicSymbols();
            PopulateReferenceTree();
        }

        public void RegisterInMemoryData<T>(string name, DataQueue<T> queue) where T : class
        {
            streamingDataSources.Remove(name);
            inMemoryDataTypes[name] = typeof(T);
            inMemoryDataSources[name] = () => queue;
            UpdateDynamicSymbols();
            PopulateReferenceTree();
        }

        public void RegisterStreamingData<T>(string name, DataQueue<T> queue) where T : class
        {
            inMemoryDataSources.Remove(name);
            inMemoryDataTypes[name] = typeof(T);
            streamingDataSources[name] = queue;
            UpdateDynamicSymbols();
            PopulateReferenceTree();
        }

        public void RegisterInMemoryData(string name, Func<System.Data.DataTable> dataProvider)
        {
            inMemoryDataSources[name] = () =>
            {
                var table = dataProvider();
                var sb = new System.Text.StringBuilder();
                var headers = new List<string>();
                foreach (System.Data.DataColumn col in table.Columns)
                    headers.Add(col.ColumnName);
                sb.AppendLine(string.Join(",", headers));
                foreach (System.Data.DataRow row in table.Rows)
                {
                    var vals = new List<string>();
                    foreach (var item in row.ItemArray)
                    {
                        string s = item != null ? item.ToString() : "";
                        if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
                            s = "\"" + s.Replace("\"", "\"\"") + "\"";
                        vals.Add(s);
                    }
                    sb.AppendLine(string.Join(",", vals));
                }
                return new StringDataSource(sb.ToString());
            };
            PopulateReferenceTree();
        }

        public void UnregisterInMemoryData(string name)
        {
            inMemoryDataSources.Remove(name);
            streamingDataSources.Remove(name);
            PopulateReferenceTree();
        }

        public void RegisterPythonClass(string className, string pythonCode)
        {
            RegisterPythonClass(className, pythonCode, null, null, null);
        }

        public void RegisterPythonClass(string className, string pythonCode, string description, string example = null, string notes = null)
        {
            if (!IsValidPythonIdentifier(className))
                throw new ArgumentException("Invalid Python class name: " + className);
            registeredPythonClasses[className] = new PythonClassInfo
            {
                PythonCode = pythonCode,
                Description = description,
                Example = example,
                Notes = notes
            };
            UpdateDynamicSymbols();
            PopulateReferenceTree();
        }

        public void UnregisterPythonClass(string className)
        {
            registeredPythonClasses.Remove(className);
            UpdateDynamicSymbols();
            PopulateReferenceTree();
        }

        public void SetContext(string key, string value)
        {
            if (!IsValidPythonIdentifier(key))
                throw new ArgumentException("Invalid Python identifier for context key: " + key);
            contextVariables[key] = new ContextVariable { Name = key, PythonLiteral = EscapePythonString(value), TypeDescription = "str" };
            UpdateDynamicSymbols();
            PopulateReferenceTree();
        }

        public void SetContext(string key, double value)
        {
            if (!IsValidPythonIdentifier(key))
                throw new ArgumentException("Invalid Python identifier for context key: " + key);
            contextVariables[key] = new ContextVariable { Name = key, PythonLiteral = value.ToString("G"), TypeDescription = "float" };
            UpdateDynamicSymbols();
            PopulateReferenceTree();
        }

        public void SetContext(string key, int value)
        {
            if (!IsValidPythonIdentifier(key))
                throw new ArgumentException("Invalid Python identifier for context key: " + key);
            contextVariables[key] = new ContextVariable { Name = key, PythonLiteral = value.ToString(), TypeDescription = "int" };
            UpdateDynamicSymbols();
            PopulateReferenceTree();
        }

        public void SetContext(string key, bool value)
        {
            if (!IsValidPythonIdentifier(key))
                throw new ArgumentException("Invalid Python identifier for context key: " + key);
            contextVariables[key] = new ContextVariable { Name = key, PythonLiteral = value ? "True" : "False", TypeDescription = "bool" };
            UpdateDynamicSymbols();
            PopulateReferenceTree();
        }

        public void SetContext(string key, string[] values)
        {
            if (!IsValidPythonIdentifier(key))
                throw new ArgumentException("Invalid Python identifier for context key: " + key);
            var parts = new List<string>();
            foreach (var v in values)
                parts.Add(EscapePythonString(v));
            contextVariables[key] = new ContextVariable { Name = key, PythonLiteral = "[" + string.Join(", ", parts) + "]", TypeDescription = "list" };
            UpdateDynamicSymbols();
            PopulateReferenceTree();
        }

        public void SetContext(string key, double[] values)
        {
            if (!IsValidPythonIdentifier(key))
                throw new ArgumentException("Invalid Python identifier for context key: " + key);
            var parts = new List<string>();
            foreach (var v in values)
                parts.Add(v.ToString("G"));
            contextVariables[key] = new ContextVariable { Name = key, PythonLiteral = "[" + string.Join(", ", parts) + "]", TypeDescription = "list" };
            UpdateDynamicSymbols();
            PopulateReferenceTree();
        }

        public void SetContext(string key, Dictionary<string, string> values)
        {
            if (!IsValidPythonIdentifier(key))
                throw new ArgumentException("Invalid Python identifier for context key: " + key);
            var parts = new List<string>();
            foreach (var kvp in values)
                parts.Add(EscapePythonString(kvp.Key) + ": " + EscapePythonString(kvp.Value));
            contextVariables[key] = new ContextVariable { Name = key, PythonLiteral = "{" + string.Join(", ", parts) + "}", TypeDescription = "dict" };
            UpdateDynamicSymbols();
            PopulateReferenceTree();
        }

        public void SetContext(string key, Dictionary<string, double> values)
        {
            if (!IsValidPythonIdentifier(key))
                throw new ArgumentException("Invalid Python identifier for context key: " + key);
            var parts = new List<string>();
            foreach (var kvp in values)
                parts.Add(EscapePythonString(kvp.Key) + ": " + kvp.Value.ToString("G"));
            contextVariables[key] = new ContextVariable { Name = key, PythonLiteral = "{" + string.Join(", ", parts) + "}", TypeDescription = "dict" };
            UpdateDynamicSymbols();
            PopulateReferenceTree();
        }

        public void RemoveContext(string key)
        {
            contextVariables.Remove(key);
            UpdateDynamicSymbols();
            PopulateReferenceTree();
        }

        public void ClearContext()
        {
            contextVariables.Clear();
            UpdateDynamicSymbols();
            PopulateReferenceTree();
        }

        private static string EscapePythonString(string s)
        {
            if (s == null) return "None";
            var sb = new System.Text.StringBuilder(s.Length + 10);
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\0': sb.Append("\\0"); break;
                    default:
                        if (c < ' ')
                            sb.Append("\\x" + ((int)c).ToString("x2"));
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        private static bool IsValidPythonIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (!char.IsLetter(name[0]) && name[0] != '_') return false;
            for (int i = 1; i < name.Length; i++)
            {
                if (!char.IsLetterOrDigit(name[i]) && name[i] != '_') return false;
            }
            return true;
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
                    if (tooltipProvider != null)
                    {
                        tooltipProvider.SetModuleTooltips(moduleData, CommonImportAliases);
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
            var subObjMap = new Dictionary<string, Dictionary<string, List<string>>>();
            foreach (var kvp in inMemoryDataTypes)
            {
                var flatProps = PythonVisibleHelper.GetFlattenedProperties(kvp.Value);
                var colNames = new List<string>();
                var dsSubObjs = new Dictionary<string, List<string>>();
                foreach (var fp in flatProps)
                {
                    colNames.Add(fp.ColumnName);
                    if (fp.IsSubObject)
                        CollectSubObjectProps(fp.ColumnName, fp.LeafType, dsSubObjs);
                }
                colMap[kvp.Key] = colNames;
                if (dsSubObjs.Count > 0)
                    subObjMap[kvp.Key] = dsSubObjs;
            }
            symbolAnalyzer.SetDatasetColumns(colMap);

            if (completionProvider != null)
            {
                var allNames = new List<string>(names);
                allNames.AddRange(inMemoryDataTypes.Keys);
                completionProvider.SetDynamicSymbols(allNames);
                completionProvider.SetDataSources(colMap);
                completionProvider.SetSubObjectProperties(subObjMap);
                completionProvider.SetRegisteredClasses(registeredPythonClasses);
                completionProvider.SetContextVariables(contextVariables);
            }
        }

        private void CollectSubObjectProps(string path, Type type, Dictionary<string, List<string>> map)
        {
            var props = PythonVisibleHelper.GetVisibleProperties(type);
            var propNames = new List<string>();
            foreach (var p in props)
            {
                if (p.GetIndexParameters().Length > 0) continue;
                propNames.Add(p.Name);
                if (PythonVisibleHelper.IsSubObjectType(p.PropertyType))
                    CollectSubObjectProps(path + "." + p.Name, p.PropertyType, map);
            }
            map[path] = propNames;
        }

        private Dictionary<string, IInMemoryDataSource> SerializeInMemoryData()
        {
            var result = new Dictionary<string, IInMemoryDataSource>();
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

        public void RunScript()
        {
            OnRunScript(this, EventArgs.Empty);
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

        private void OnClearOutput(object sender, EventArgs e)
        {
            RunOnUIThread(() => outputBox.Clear());
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string OutputText
        {
            get
            {
                if (!IsHandleCreated || !InvokeRequired)
                    return outputBox.Text;
                string result = null;
                Invoke(new Action(() => result = outputBox.Text));
                return result;
            }
        }

        private void SetupToolbarActions()
        {
            clearOutputBtn.Click += (s, e) => outputBox.Clear();
            viewDataRefBtn.Click += (s, e) => TogglePanel(referenceDockContent);
            resetLayoutBtn.Click += (s, e) => ResetDockLayout();
        }

        public MenuStrip CreateMenuStrip()
        {
            return null;
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
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var snippetResources = new List<string>();
            foreach (var name in assembly.GetManifestResourceNames())
            {
                if (name.EndsWith(".py") && name.Contains("Snippets"))
                    snippetResources.Add(name);
            }
            snippetResources.Sort();

            foreach (var resourceName in snippetResources)
            {
                string content;
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                using (var reader = new System.IO.StreamReader(stream))
                    content = reader.ReadToEnd();

                string label = null;
                bool separatorBefore = false;
                var lines = content.Split('\n');
                int codeStart = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].TrimEnd('\r');
                    if (line.StartsWith("# snippet:"))
                    {
                        label = line.Substring("# snippet:".Length).Trim();
                        codeStart = i + 1;
                    }
                    else if (line.StartsWith("# separator:"))
                    {
                        separatorBefore = true;
                        codeStart = i + 1;
                    }
                    else break;
                }

                if (label == null) continue;

                string code = string.Join("\n", lines, codeStart, lines.Length - codeStart);

                if (separatorBefore)
                    insertSnippetBtn.DropDownItems.Add(new ToolStripSeparator());

                var item = new ToolStripMenuItem(label);
                item.Click += (s, e) => InsertSnippet(code);
                insertSnippetBtn.DropDownItems.Add(item);
            }
        }

        private void RegisterAllDatasetsInMemory()
        {
            RegisterInMemoryData<Customer>("customers", () => customers);
            RegisterInMemoryData<Employee>("employees", () => employees);

            var streamQueue = new DataQueue<Customer>();
            streamQueue.SetSource(GenerateStreamingCustomers(500));
            RegisterStreamingData<Customer>("customer_stream", streamQueue);
        }

        private IEnumerable<Customer> GenerateStreamingCustomers(int count)
        {
            var streamGen = new DataGenerator(123);
            var products = streamGen.GenerateProducts(50);
            for (int i = 0; i < count; i++)
            {
                var batch = streamGen.GenerateCustomers(1, products);
                yield return batch[0];
            }
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
                    var node = refTreeView.Nodes.Add(name + "  (" + count + ")");
                    node.Tag = name;
                    node.NodeFont = new Font(refTreeView.Font, FontStyle.Bold);

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
                    var classNode = refTreeView.Nodes.Add("Registered Classes  (" + matchingClasses.Count + ")");
                    classNode.NodeFont = new Font(refTreeView.Font, FontStyle.Bold);
                    classNode.Tag = "regclasses";
                    foreach (var name in matchingClasses)
                    {
                        var child = classNode.Nodes.Add(name);
                        child.Tag = "regclass_" + name;
                        child.ForeColor = Color.FromArgb(0, 100, 160);
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
                    var ctxNode = refTreeView.Nodes.Add("Context Hub  (" + matchingCtx.Count + ")");
                    ctxNode.NodeFont = new Font(refTreeView.Font, FontStyle.Bold);
                    ctxNode.Tag = "contexthub";
                    foreach (var kvp in matchingCtx)
                    {
                        var child = ctxNode.Nodes.Add(kvp.Key + "  :  " + kvp.Value.TypeDescription);
                        child.Tag = "ctx_" + kvp.Key;
                        child.ForeColor = Color.FromArgb(128, 0, 128);
                    }
                    ctxNode.Expand();
                }
            }

            var helperInfos = PythonRunner.GetHelperFunctionInfos();
            if (helperInfos.Count > 0)
            {
                var matchingHelpers = new List<PythonRunner.HelperFunctionInfo>();
                bool headerMatches = "Helper Functions".IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
                foreach (var info in helperInfos)
                {
                    if (headerMatches || info.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        info.Signature.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        info.Docstring.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                        matchingHelpers.Add(info);
                }
                if (matchingHelpers.Count > 0)
                {
                    var helpNode = refTreeView.Nodes.Add("Helper Functions  (" + matchingHelpers.Count + ")");
                    helpNode.NodeFont = new Font(refTreeView.Font, FontStyle.Bold);
                    helpNode.Tag = "helperfuncs";
                    foreach (var info in matchingHelpers)
                    {
                        var child = helpNode.Nodes.Add(info.Name);
                        child.Tag = "helper_" + info.Name;
                        child.ForeColor = Color.FromArgb(0, 128, 128);
                    }
                    helpNode.Expand();
                }
            }

            refTreeView.EndUpdate();
        }

        private void AddHierarchicalColumns(TreeNode parentNode, string datasetName, Type type, HashSet<string> filterColumns)
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
                if (fp.IsSubObject)
                {
                    string groupTypeName = fp.LeafType.Name;
                    var groupNode = parentNode.Nodes.Add(fp.ColumnName + "  (" + groupTypeName + ")");
                    groupNode.Tag = new string[] { "subobject", datasetName, fp.ColumnName };
                    groupNode.ForeColor = Color.FromArgb(0, 100, 130);
                    groupNode.NodeFont = new Font(refTreeView.Font, FontStyle.Italic);
                    AddSubObjectChildren(groupNode, datasetName, fp.ColumnName, fp.LeafType);
                    continue;
                }

                string typeName = PythonVisibleHelper.GetPythonTypeName(fp.LeafType);
                if (fp.IsComputed) typeName += " (computed)";
                var child = parentNode.Nodes.Add(fp.ColumnName + "  :  " + typeName);
                child.Tag = new string[] { "field", datasetName, fp.ColumnName };
                child.ForeColor = Color.FromArgb(80, 80, 80);

                if (PythonVisibleHelper.IsDictionaryType(fp.LeafType))
                    AddDictClassChildren(child, datasetName, fp.ColumnName, fp.LeafType);
            }

            foreach (var kvp in groupChildren)
            {
                string groupName = kvp.Key;
                var groupProps = kvp.Value;
                string groupTypeName = groupProps[0].PropertyPath[0].PropertyType.Name;
                string groupPrefix = groupName + "_";
                var groupNode = parentNode.Nodes.Add(groupName + "  (" + groupTypeName + ")");
                groupNode.Tag = new string[] { "subclass", datasetName, groupPrefix };
                groupNode.ForeColor = Color.FromArgb(0, 100, 130);
                groupNode.NodeFont = new Font(refTreeView.Font, FontStyle.Italic);

                AddSubclassChildren(groupNode, datasetName, groupProps, 1, filterColumns, groupPrefix);
            }
        }

        private void AddSubclassChildren(TreeNode parentNode, string datasetName, List<FlattenedProperty> groupProps, int depth, HashSet<string> filterColumns, string pathPrefix)
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
                var child = parentNode.Nodes.Add(leafName + "  \u2192  " + fp.ColumnName + "  :  " + typeName);
                child.Tag = new string[] { "field", datasetName, fp.ColumnName };
                child.ForeColor = Color.FromArgb(80, 80, 80);

                if (PythonVisibleHelper.IsDictionaryType(fp.LeafType))
                    AddDictClassChildren(child, datasetName, fp.ColumnName, fp.LeafType);
            }

            foreach (var kvp in subGroups)
            {
                string subGroupName = kvp.Key;
                var subGroupProps = kvp.Value;
                string subGroupTypeName = subGroupProps[0].PropertyPath[depth].PropertyType.Name;
                string subPrefix = pathPrefix + subGroupName + "_";
                var subGroupNode = parentNode.Nodes.Add(subGroupName + "  (" + subGroupTypeName + ")");
                subGroupNode.Tag = new string[] { "subclass", datasetName, subPrefix };
                subGroupNode.ForeColor = Color.FromArgb(0, 100, 130);
                subGroupNode.NodeFont = new Font(refTreeView.Font, FontStyle.Italic);

                AddSubclassChildren(subGroupNode, datasetName, subGroupProps, depth + 1, filterColumns, subPrefix);
            }
        }

        private void AddSubObjectChildren(TreeNode parentNode, string datasetName, string columnName, Type subObjType)
        {
            var props = PythonVisibleHelper.GetVisibleProperties(subObjType);
            foreach (var p in props)
            {
                if (p.GetIndexParameters().Length > 0) continue;
                string typeName = PythonVisibleHelper.GetPythonTypeName(p.PropertyType);
                bool isComputed = p.GetSetMethod() == null;
                if (isComputed) typeName += " (computed)";

                if (PythonVisibleHelper.IsSubObjectType(p.PropertyType))
                {
                    var subNode = parentNode.Nodes.Add(p.Name + "  (" + p.PropertyType.Name + ")");
                    subNode.Tag = new string[] { "subobject", datasetName, columnName + "." + p.Name };
                    subNode.ForeColor = Color.FromArgb(0, 100, 130);
                    subNode.NodeFont = new Font(refTreeView.Font, FontStyle.Italic);
                    AddSubObjectChildren(subNode, datasetName, columnName + "." + p.Name, p.PropertyType);
                }
                else
                {
                    var child = parentNode.Nodes.Add(p.Name + "  :  " + typeName);
                    child.Tag = new string[] { "subobjprop", datasetName, columnName, p.Name };
                    child.ForeColor = Color.FromArgb(80, 80, 80);

                    if (PythonVisibleHelper.IsDictionaryType(p.PropertyType))
                        AddDictClassChildren(child, datasetName, columnName + "." + p.Name, p.PropertyType);
                }
            }
        }

        private void AddDictClassChildren(TreeNode fieldNode, string datasetName, string fieldName, Type dictType)
        {
            var genArgs = dictType.GetGenericArguments();
            if (genArgs.Length != 2) return;

            for (int i = 0; i < 2; i++)
            {
                string role = i == 0 ? "Key" : "Value";
                Type argType = genArgs[i];
                Type underlying = argType;
                if (argType.IsGenericType && argType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    underlying = Nullable.GetUnderlyingType(argType);

                bool isExpandableClass = (underlying.IsClass && underlying != typeof(string))
                    || (underlying.IsValueType && !underlying.IsPrimitive && !underlying.IsEnum);
                if (!isExpandableClass) continue;

                var props = PythonVisibleHelper.GetVisibleProperties(underlying);
                if (props.Count == 0) continue;

                var groupNode = fieldNode.Nodes.Add(role + "  (" + underlying.Name + ")");
                groupNode.Tag = new string[] { "dictclass", datasetName, fieldName, role, underlying.Name };
                groupNode.ForeColor = Color.FromArgb(0, 100, 130);
                groupNode.NodeFont = new Font(refTreeView.Font, FontStyle.Italic);

                foreach (var p in props)
                {
                    string pType = PythonVisibleHelper.GetPythonTypeName(p.PropertyType);
                    var propNode = groupNode.Nodes.Add(p.Name + "  :  " + pType);
                    propNode.Tag = new string[] { "dictclassprop", datasetName, fieldName, role, p.Name };
                    propNode.ForeColor = Color.FromArgb(80, 80, 80);
                }
            }
        }

        private void PopulateReferenceTree()
        {
            refTreeView.Nodes.Clear();

            foreach (var kvp in inMemoryDataTypes)
            {
                string name = kvp.Key;
                Type type = kvp.Value;
                int count = GetRecordCountForTag(name);
                var node = refTreeView.Nodes.Add(name + "  (" + count + ")");
                node.Tag = name;
                node.NodeFont = new Font(refTreeView.Font, FontStyle.Bold);

                AddHierarchicalColumns(node, name, type, null);
                node.Expand();
            }

            if (registeredPythonClasses.Count > 0)
            {
                var classNode = refTreeView.Nodes.Add("Registered Classes  (" + registeredPythonClasses.Count + ")");
                classNode.NodeFont = new Font(refTreeView.Font, FontStyle.Bold);
                classNode.Tag = "regclasses";
                foreach (var name in registeredPythonClasses.Keys)
                {
                    var child = classNode.Nodes.Add(name);
                    child.Tag = "regclass_" + name;
                    child.ForeColor = Color.FromArgb(0, 100, 160);
                }
                classNode.Expand();
            }

            if (contextVariables.Count > 0)
            {
                var ctxNode = refTreeView.Nodes.Add("Context Hub  (" + contextVariables.Count + ")");
                ctxNode.NodeFont = new Font(refTreeView.Font, FontStyle.Bold);
                ctxNode.Tag = "contexthub";
                foreach (var kvp in contextVariables)
                {
                    var child = ctxNode.Nodes.Add(kvp.Key + "  :  " + kvp.Value.TypeDescription);
                    child.Tag = "ctx_" + kvp.Key;
                    child.ForeColor = Color.FromArgb(128, 0, 128);
                }
                ctxNode.Expand();
            }

            var helperInfos = PythonRunner.GetHelperFunctionInfos();
            if (helperInfos.Count > 0)
            {
                var helpNode = refTreeView.Nodes.Add("Helper Functions  (" + helperInfos.Count + ")");
                helpNode.NodeFont = new Font(refTreeView.Font, FontStyle.Bold);
                helpNode.Tag = "helperfuncs";
                foreach (var info in helperInfos)
                {
                    var child = helpNode.Nodes.Add(info.Name);
                    child.Tag = "helper_" + info.Name;
                    child.ForeColor = Color.FromArgb(0, 128, 128);
                }
                helpNode.Expand();
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

        private void OnRefTreeSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null || e.Node.Tag == null) return;

            var tagArr = e.Node.Tag as string[];
            if (tagArr != null && tagArr.Length == 3 && tagArr[0] == "field")
            {
                ShowFieldDetail(tagArr[1], tagArr[2]);
                return;
            }
            if (tagArr != null && tagArr.Length == 3 && tagArr[0] == "subclass")
            {
                ShowSubclassDetail(tagArr[1], tagArr[2], e.Node);
                return;
            }
            if (tagArr != null && tagArr.Length == 3 && tagArr[0] == "subobject")
            {
                ShowSubObjectDetail(tagArr[1], tagArr[2]);
                return;
            }
            if (tagArr != null && tagArr.Length == 4 && tagArr[0] == "subobjprop")
            {
                ShowSubObjectPropDetail(tagArr[1], tagArr[2], tagArr[3]);
                return;
            }
            if (tagArr != null && tagArr.Length == 5 && tagArr[0] == "dictclass")
            {
                ShowDictClassDetail(tagArr[1], tagArr[2], tagArr[3], tagArr[4]);
                return;
            }
            if (tagArr != null && tagArr.Length == 5 && tagArr[0] == "dictclassprop")
            {
                ShowDictClassPropDetail(tagArr[1], tagArr[2], tagArr[3], tagArr[4]);
                return;
            }

            string tag = e.Node.Tag.ToString();

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

            if (tag == "helperfuncs" || tag.StartsWith("helper_"))
            {
                ShowHelperFunctionDetail(tag);
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

            bool isNullable = PythonVisibleHelper.IsNullableType(fp.LeafType);
            bool isEnum = PythonVisibleHelper.IsEnumType(fp.LeafType);
            Type enumType = PythonVisibleHelper.GetUnderlyingEnumType(fp.LeafType);

            if (isNullable)
            {
                AppendRefText("Nullable\n", Color.FromArgb(0, 100, 0), true, 10);
                AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
                AppendRefText("This field may contain empty/missing values (None/NaN in Python).\n\n", Color.FromArgb(60, 60, 60), false, 10);
            }

            if (isEnum && enumType != null)
            {
                string[] enumNames = Enum.GetNames(enumType);
                AppendRefText("Enum Values\n", Color.FromArgb(0, 100, 0), true, 10);
                AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
                AppendRefText("Serialized as string. Possible values:\n", Color.FromArgb(100, 100, 100), false, 10);
                foreach (string name in enumNames)
                    AppendRefText("  \u2022 " + name + "\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("\n", Color.FromArgb(60, 60, 60), false, 10);
            }

            if (PythonVisibleHelper.IsDictionaryType(fp.LeafType))
            {
                var genArgs = fp.LeafType.GetGenericArguments();
                bool isSorted = PythonVisibleHelper.IsSortedDictionaryType(fp.LeafType);
                AppendRefText(isSorted ? "Sorted Dictionary\n" : "Dictionary\n", Color.FromArgb(0, 100, 0), true, 10);
                AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
                AppendRefText("Serialized as a Python dict" + (isSorted ? " (key-sorted)" : "") + ".\n", Color.FromArgb(60, 60, 60), false, 10);
                if (isSorted)
                    AppendRefText("Keys are ordered by .NET sort guarantees.\n", Color.FromArgb(100, 100, 100), false, 10);
                AppendRefText("\n", Color.FromArgb(100, 100, 100), false, 10);
                AppendDictTypeDetail("Key", genArgs[0]);
                AppendDictTypeDetail("Value", genArgs[1]);
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
            string baseTypeName = typeName;
            if (baseTypeName.Contains(" (nullable)"))
                baseTypeName = baseTypeName.Replace(" (nullable)", "");
            if (baseTypeName.StartsWith("string (enum:"))
                baseTypeName = "enum";
            if (baseTypeName.StartsWith("dict ("))
                baseTypeName = "dict";

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
            else if (baseTypeName == "enum")
            {
                AppendRefText("# Value counts\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText(datasetName + "." + fieldName + ".value_counts()\n\n", Color.FromArgb(60, 60, 60), false, 10);
                if (isNullable)
                {
                    AppendRefText("# Drop missing values\n", Color.FromArgb(0, 128, 0), false, 10);
                    AppendRefText(datasetName + "." + fieldName + ".dropna().value_counts()\n\n", Color.FromArgb(60, 60, 60), false, 10);
                }
                if (enumType != null)
                {
                    string[] enumNames = Enum.GetNames(enumType);
                    if (enumNames.Length > 0)
                    {
                        AppendRefText("# Filter by specific value\n", Color.FromArgb(0, 128, 0), false, 10);
                        AppendRefText(datasetName + "[" + datasetName + "." + fieldName + " == '" + enumNames[0] + "']\n\n", Color.FromArgb(60, 60, 60), false, 10);
                    }
                    AppendRefText("# Group by this field\n", Color.FromArgb(0, 128, 0), false, 10);
                    AppendRefText(datasetName + ".groupby('" + fieldName + "').size()\n", Color.FromArgb(60, 60, 60), false, 10);
                }
            }
            else if (baseTypeName == "string")
            {
                AppendRefText("# Value counts\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText(datasetName + "." + fieldName + ".value_counts()\n\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("# Unique values\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText(datasetName + "." + fieldName + ".unique()\n", Color.FromArgb(60, 60, 60), false, 10);
            }
            else if (baseTypeName == "bool")
            {
                AppendRefText("# Count true values\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText(datasetName + "." + fieldName + ".sum()\n\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("# Filter to true rows\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText(datasetName + "[" + datasetName + "." + fieldName + "]\n", Color.FromArgb(60, 60, 60), false, 10);
            }
            else if (baseTypeName == "datetime")
            {
                AppendRefText("# Date range\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText(datasetName + "." + fieldName + ".min(), " + datasetName + "." + fieldName + ".max()\n\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("# Extract year\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText(datasetName + "." + fieldName + ".dt.year\n", Color.FromArgb(60, 60, 60), false, 10);
                if (isNullable)
                {
                    AppendRefText("\n# Drop missing dates\n", Color.FromArgb(0, 128, 0), false, 10);
                    AppendRefText(datasetName + "." + fieldName + ".dropna()\n", Color.FromArgb(60, 60, 60), false, 10);
                }
            }
            else if (baseTypeName == "dict")
            {
                AppendRefText("# Access the dict for a specific row\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText("d = " + datasetName + "[0]." + fieldName + "\n\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("# Iterate over keys and values\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText("for k, v in d.items():\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("    print(k, v)\n\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("# Get all keys or values\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText("list(d.keys())\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("list(d.values())\n", Color.FromArgb(60, 60, 60), false, 10);
            }
            else if (baseTypeName == "image")
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
                if (isNullable)
                {
                    AppendRefText("\n# Drop missing values\n", Color.FromArgb(0, 128, 0), false, 10);
                    AppendRefText(datasetName + "." + fieldName + ".dropna().describe()\n", Color.FromArgb(60, 60, 60), false, 10);
                }
            }

            refDetailBox.SelectionStart = 0;
            SafeScrollToCaret(refDetailBox);
        }

        private void ShowSubclassDetail(string datasetName, string prefix, TreeNode node)
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
            SafeScrollToCaret(refDetailBox);
        }

        private void ShowSubObjectDetail(string datasetName, string dotPath)
        {
            Type type;
            if (!inMemoryDataTypes.TryGetValue(datasetName, out type)) return;

            string[] segments = dotPath.Split('.');
            Type currentType = type;
            for (int i = 0; i < segments.Length; i++)
            {
                var prop = currentType.GetProperty(segments[i]);
                if (prop == null) return;
                currentType = prop.PropertyType;
            }

            string displayName = segments[segments.Length - 1];
            string subObjTypeName = currentType.Name;

            refDetailBox.Clear();

            AppendRefText(displayName, Color.FromArgb(0, 100, 130), true, 12);
            AppendRefText("  (" + subObjTypeName + ")\n\n", Color.FromArgb(100, 100, 100), false, 12);

            AppendRefText("Nested object from ", Color.FromArgb(60, 60, 60), false, 10);
            AppendRefText(datasetName, Color.FromArgb(0, 0, 180), true, 10);
            AppendRefText("\n", Color.Black, false, 10);
            AppendRefText("Access via dot notation: ", Color.FromArgb(60, 60, 60), false, 10);
            AppendRefText(datasetName + "[i]." + dotPath + "\n\n", Color.FromArgb(128, 0, 0), false, 10);

            var props = PythonVisibleHelper.GetVisibleProperties(currentType);
            AppendRefText("Properties (" + props.Count + ")\n", Color.FromArgb(0, 100, 0), true, 10);
            AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);

            int maxLen = 0;
            foreach (var p in props)
            {
                if (p.GetIndexParameters().Length > 0) continue;
                if (p.Name.Length > maxLen) maxLen = p.Name.Length;
            }
            foreach (var p in props)
            {
                if (p.GetIndexParameters().Length > 0) continue;
                string typeName = PythonVisibleHelper.GetPythonTypeName(p.PropertyType);
                if (p.GetSetMethod() == null) typeName += " (computed)";
                AppendRefText("  " + p.Name.PadRight(maxLen + 2), Color.FromArgb(0, 0, 0), false, 10);
                AppendRefText(":  " + typeName + "\n", Color.FromArgb(100, 100, 100), false, 10);
            }

            AppendRefText("\n", Color.Black, false, 10);
            AppendRefText("Example Python Code\n", Color.FromArgb(0, 100, 0), true, 10);
            AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
            AppendRefText("from DotNetData import " + datasetName + "\n\n", Color.FromArgb(60, 60, 60), false, 10);

            AppendRefText("# Access a property\n", Color.FromArgb(0, 128, 0), false, 10);
            if (props.Count > 0)
            {
                string firstProp = props[0].Name;
                AppendRefText(datasetName + "[0]." + dotPath + "." + firstProp + "\n\n", Color.FromArgb(60, 60, 60), false, 10);
            }

            AppendRefText("# Iterate over rows\n", Color.FromArgb(0, 128, 0), false, 10);
            AppendRefText("for row in " + datasetName + ":\n", Color.FromArgb(60, 60, 60), false, 10);
            AppendRefText("    print(row." + dotPath + ")\n\n", Color.FromArgb(60, 60, 60), false, 10);

            AppendRefText("# Extract column for analysis\n", Color.FromArgb(0, 128, 0), false, 10);
            if (props.Count > 0)
            {
                string firstProp = props[0].Name;
                AppendRefText(datasetName + "." + segments[0] + ".apply(lambda x: x." + string.Join(".", segments, 1, segments.Length - 1) + (segments.Length > 1 ? "." : "") + firstProp + ")\n", Color.FromArgb(60, 60, 60), false, 10);
            }

            refDetailBox.SelectionStart = 0;
            SafeScrollToCaret(refDetailBox);
        }

        private void ShowSubObjectPropDetail(string datasetName, string columnPath, string propName)
        {
            Type type;
            if (!inMemoryDataTypes.TryGetValue(datasetName, out type)) return;

            string[] segments = columnPath.Split('.');
            Type currentType = type;
            for (int i = 0; i < segments.Length; i++)
            {
                var prop = currentType.GetProperty(segments[i]);
                if (prop == null) return;
                currentType = prop.PropertyType;
            }

            var propInfo = currentType.GetProperty(propName);
            if (propInfo == null) return;

            string typeName = PythonVisibleHelper.GetPythonTypeName(propInfo.PropertyType);
            bool isComputed = propInfo.GetSetMethod() == null;
            if (isComputed) typeName += " (computed)";

            refDetailBox.Clear();

            AppendRefText(propName, Color.FromArgb(0, 0, 180), true, 12);
            AppendRefText("  :  " + typeName + "\n\n", Color.FromArgb(100, 100, 100), false, 12);

            string fullPath = columnPath + "." + propName;
            AppendRefText("Property of ", Color.FromArgb(60, 60, 60), false, 10);
            AppendRefText(segments[segments.Length - 1], Color.FromArgb(0, 100, 130), true, 10);
            AppendRefText(" in ", Color.FromArgb(60, 60, 60), false, 10);
            AppendRefText(datasetName + "\n\n", Color.FromArgb(0, 0, 180), true, 10);

            AppendRefText("Example Python Code\n", Color.FromArgb(0, 100, 0), true, 10);
            AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
            AppendRefText("from DotNetData import " + datasetName + "\n\n", Color.FromArgb(60, 60, 60), false, 10);

            AppendRefText("# Access from a row\n", Color.FromArgb(0, 128, 0), false, 10);
            AppendRefText(datasetName + "[0]." + fullPath + "\n\n", Color.FromArgb(60, 60, 60), false, 10);

            AppendRefText("# Extract as a series\n", Color.FromArgb(0, 128, 0), false, 10);
            AppendRefText(datasetName + "." + segments[0] + ".apply(lambda x: x." + string.Join(".", segments, 1, segments.Length - 1) + (segments.Length > 1 ? "." : "") + propName + ")\n\n", Color.FromArgb(60, 60, 60), false, 10);

            if (PythonVisibleHelper.IsEnumType(propInfo.PropertyType))
            {
                Type enumType = PythonVisibleHelper.GetUnderlyingEnumType(propInfo.PropertyType);
                if (enumType != null)
                {
                    string[] names = Enum.GetNames(enumType);
                    AppendRefText("Possible Values\n", Color.FromArgb(0, 100, 0), true, 10);
                    AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
                    foreach (string name in names)
                        AppendRefText("  \u2022 " + name + "\n", Color.FromArgb(60, 60, 60), false, 10);
                }
            }

            refDetailBox.SelectionStart = 0;
            SafeScrollToCaret(refDetailBox);
        }

        private void AppendDictTypeDetail(string role, Type t)
        {
            string pyType = PythonVisibleHelper.GetPythonTypeName(t);
            AppendRefText("  " + role + " Type:  ", Color.FromArgb(100, 100, 100), true, 10);
            AppendRefText(pyType, Color.FromArgb(0, 100, 160), false, 10);
            Type underlying = t;
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                underlying = Nullable.GetUnderlyingType(t);
            if (!string.IsNullOrEmpty(underlying.Namespace) && underlying.Namespace.StartsWith("System"))
            {
                AppendRefText("\n", Color.Black, false, 10);
                return;
            }
            AppendRefText("  (" + underlying.Name + ")\n", Color.FromArgb(150, 150, 150), false, 10);
            if (underlying.IsEnum)
            {
                string[] names = Enum.GetNames(underlying);
                AppendRefText("    Possible values:\n", Color.FromArgb(100, 100, 100), false, 10);
                foreach (string name in names)
                    AppendRefText("      \u2022 " + name + "\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("\n", Color.Black, false, 10);
            }
            else if (underlying.IsClass && underlying != typeof(string))
            {
                var props = PythonVisibleHelper.GetVisibleProperties(underlying);
                if (props.Count > 0)
                {
                    AppendRefText("    Properties:\n", Color.FromArgb(100, 100, 100), false, 10);
                    foreach (var p in props)
                    {
                        string pType = PythonVisibleHelper.GetPythonTypeName(p.PropertyType);
                        AppendRefText("      \u2022 " + p.Name, Color.FromArgb(60, 60, 60), false, 10);
                        AppendRefText("  :  " + pType + "\n", Color.FromArgb(130, 130, 130), false, 10);
                    }
                    AppendRefText("\n", Color.Black, false, 10);
                }
            }
            else if (underlying.IsValueType && !underlying.IsPrimitive)
            {
                var props = PythonVisibleHelper.GetVisibleProperties(underlying);
                if (props.Count > 0)
                {
                    AppendRefText("    Properties:\n", Color.FromArgb(100, 100, 100), false, 10);
                    foreach (var p in props)
                    {
                        string pType = PythonVisibleHelper.GetPythonTypeName(p.PropertyType);
                        AppendRefText("      \u2022 " + p.Name, Color.FromArgb(60, 60, 60), false, 10);
                        AppendRefText("  :  " + pType + "\n", Color.FromArgb(130, 130, 130), false, 10);
                    }
                    AppendRefText("\n", Color.Black, false, 10);
                }
            }
        }

        private Type ResolveDictArgType(string datasetName, string fieldName, string role)
        {
            Type type;
            if (!inMemoryDataTypes.TryGetValue(datasetName, out type)) return null;

            var flatProps = PythonVisibleHelper.GetFlattenedProperties(type);
            foreach (var fp in flatProps)
            {
                if (fp.ColumnName == fieldName && PythonVisibleHelper.IsDictionaryType(fp.LeafType))
                {
                    var genArgs = fp.LeafType.GetGenericArguments();
                    if (genArgs.Length == 2)
                    {
                        Type argType = role == "Key" ? genArgs[0] : genArgs[1];
                        if (argType.IsGenericType && argType.GetGenericTypeDefinition() == typeof(Nullable<>))
                            argType = Nullable.GetUnderlyingType(argType);
                        return argType;
                    }
                }
            }
            return null;
        }

        private void ShowDictClassDetail(string datasetName, string fieldName, string role, string className)
        {
            Type argType = ResolveDictArgType(datasetName, fieldName, role);

            refDetailBox.Clear();
            AppendRefText(className, Color.FromArgb(0, 100, 130), true, 12);
            AppendRefText("  (" + role.ToLower() + " type)\n\n", Color.FromArgb(100, 100, 100), false, 12);

            AppendRefText("Dictionary " + role + "\n", Color.FromArgb(0, 100, 0), true, 10);
            AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
            AppendRefText(role + " type for ", Color.FromArgb(60, 60, 60), false, 10);
            AppendRefText(fieldName, Color.FromArgb(0, 0, 180), true, 10);
            AppendRefText(" in ", Color.FromArgb(60, 60, 60), false, 10);
            AppendRefText(datasetName, Color.FromArgb(0, 0, 180), true, 10);
            AppendRefText(".\n\n", Color.FromArgb(60, 60, 60), false, 10);

            if (argType != null)
            {
                var props = PythonVisibleHelper.GetVisibleProperties(argType);
                if (props.Count > 0)
                {
                    AppendRefText("Properties (" + props.Count + ")\n", Color.FromArgb(0, 100, 0), true, 10);
                    AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);

                    int maxLen = 0;
                    foreach (var p in props)
                        if (p.Name.Length > maxLen) maxLen = p.Name.Length;

                    foreach (var p in props)
                    {
                        string pType = PythonVisibleHelper.GetPythonTypeName(p.PropertyType);
                        AppendRefText("  " + p.Name.PadRight(maxLen + 2), Color.FromArgb(0, 0, 0), false, 10);
                        AppendRefText(":  " + pType + "\n", Color.FromArgb(100, 100, 100), false, 10);
                    }
                    AppendRefText("\n", Color.Black, false, 10);
                }
            }

            AppendRefText("Example Python Code\n", Color.FromArgb(0, 100, 0), true, 10);
            AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
            AppendRefText("from DotNetData import " + datasetName + "\n\n", Color.FromArgb(60, 60, 60), false, 10);
            AppendRefText("# Access dict " + role.ToLower() + "s\n", Color.FromArgb(0, 128, 0), false, 10);
            if (role == "Key")
                AppendRefText("keys = " + datasetName + "." + fieldName + ".iloc[0].keys()\n", Color.FromArgb(60, 60, 60), false, 10);
            else
                AppendRefText("val = " + datasetName + "." + fieldName + ".iloc[0][some_key]\n", Color.FromArgb(60, 60, 60), false, 10);

            refDetailBox.SelectionStart = 0;
            SafeScrollToCaret(refDetailBox);
        }

        private void ShowDictClassPropDetail(string datasetName, string fieldName, string role, string propName)
        {
            Type argType = ResolveDictArgType(datasetName, fieldName, role);

            refDetailBox.Clear();
            AppendRefText(propName, Color.FromArgb(0, 0, 180), true, 12);

            string pTypeName = "";
            string desc = "";
            if (argType != null)
            {
                foreach (var p in PythonVisibleHelper.GetVisibleProperties(argType))
                {
                    if (p.Name == propName)
                    {
                        pTypeName = PythonVisibleHelper.GetPythonTypeName(p.PropertyType);
                        var attrs = p.GetCustomAttributes(typeof(PythonVisibleAttribute), true);
                        if (attrs.Length > 0)
                            desc = ((PythonVisibleAttribute)attrs[0]).Description;
                        break;
                    }
                }
            }

            AppendRefText("  :  " + pTypeName + "\n\n", Color.FromArgb(100, 100, 100), false, 12);

            AppendRefText("Description\n", Color.FromArgb(0, 100, 0), true, 10);
            AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
            if (!string.IsNullOrEmpty(desc))
                AppendRefText(desc + "\n\n", Color.FromArgb(60, 60, 60), false, 10);
            else
                AppendRefText("Property of " + role.ToLower() + " type in dictionary " + fieldName + ".\n\n", Color.FromArgb(60, 60, 60), false, 10);

            AppendRefText("Context\n", Color.FromArgb(0, 100, 0), true, 10);
            AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
            AppendRefText("  Dictionary field:  " + fieldName + "\n", Color.FromArgb(60, 60, 60), false, 10);
            AppendRefText("  " + role + " type:  " + (argType != null ? argType.Name : "?") + "\n", Color.FromArgb(60, 60, 60), false, 10);
            AppendRefText("  Dataset:  " + datasetName + "\n\n", Color.FromArgb(60, 60, 60), false, 10);

            AppendRefText("Example Python Code\n", Color.FromArgb(0, 100, 0), true, 10);
            AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
            AppendRefText("from DotNetData import " + datasetName + "\n\n", Color.FromArgb(60, 60, 60), false, 10);
            if (role == "Value")
            {
                AppendRefText("# Access " + propName + " from a dict value\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText("val = " + datasetName + "." + fieldName + ".iloc[0][some_key]\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("print(val['" + propName + "'])\n", Color.FromArgb(60, 60, 60), false, 10);
            }
            else
            {
                AppendRefText("# Access " + propName + " from a dict key\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText("for key in " + datasetName + "." + fieldName + ".iloc[0]:\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("    print(key['" + propName + "'])\n", Color.FromArgb(60, 60, 60), false, 10);
            }

            refDetailBox.SelectionStart = 0;
            SafeScrollToCaret(refDetailBox);
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
            SafeScrollToCaret(refDetailBox);
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
            SafeScrollToCaret(refDetailBox);
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
            SafeScrollToCaret(refDetailBox);
        }

        private void ShowHelperFunctionDetail(string tag)
        {
            refDetailBox.Clear();
            var helperInfos = PythonRunner.GetHelperFunctionInfos();

            if (tag == "helperfuncs")
            {
                AppendRefText("Helper Functions\n\n", Color.FromArgb(0, 0, 180), true, 12);
                AppendRefText("Built-in helper functions available via DotNetData.\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("Import with:  ", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("from DotNetData import <function>\n\n", Color.FromArgb(0, 100, 160), false, 10);

                foreach (var info in helperInfos)
                {
                    AppendRefText("  " + info.Name, Color.FromArgb(0, 128, 128), true, 10);
                    string firstLine = info.Docstring;
                    int nlIdx = firstLine.IndexOf('\n');
                    if (nlIdx >= 0) firstLine = firstLine.Substring(0, nlIdx);
                    if (!string.IsNullOrEmpty(firstLine))
                        AppendRefText("  \u2014 " + firstLine, Color.FromArgb(130, 130, 130), false, 10);
                    AppendRefText("\n", Color.Black, false, 10);
                }

                AppendRefText("\n", Color.Black, false, 10);
                AppendRefText("Usage\n", Color.FromArgb(0, 100, 0), true, 10);
                AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
                AppendRefText("from DotNetData import display_images\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("display_images(my_image_list)\n", Color.FromArgb(60, 60, 60), false, 10);
            }
            else
            {
                string funcName = tag.Substring("helper_".Length);
                PythonRunner.HelperFunctionInfo found = null;
                foreach (var info in helperInfos)
                {
                    if (info.Name == funcName) { found = info; break; }
                }

                if (found != null)
                {
                    AppendRefText(found.Name + "  (Helper Function)\n\n", Color.FromArgb(0, 0, 180), true, 12);

                    AppendRefText("Signature\n", Color.FromArgb(0, 100, 0), true, 10);
                    AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
                    AppendRefText(found.Signature + "\n\n", Color.FromArgb(0, 100, 160), false, 10);

                    if (!string.IsNullOrEmpty(found.Docstring))
                    {
                        AppendRefText("Description\n", Color.FromArgb(0, 100, 0), true, 10);
                        AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
                        foreach (string line in found.Docstring.Split('\n'))
                        {
                            string trimmed = line.TrimStart();
                            if (trimmed.StartsWith("Args:") || trimmed.StartsWith("Returns:"))
                                AppendRefText(line + "\n", Color.FromArgb(0, 100, 0), true, 10);
                            else
                                AppendRefText(line + "\n", Color.FromArgb(60, 60, 60), false, 10);
                        }
                        AppendRefText("\n", Color.Black, false, 10);
                    }

                    AppendRefText("Import\n", Color.FromArgb(0, 100, 0), true, 10);
                    AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
                    AppendRefText("from DotNetData import " + found.Name + "\n", Color.FromArgb(60, 60, 60), false, 10);
                }
                else
                {
                    AppendRefText(funcName + "  (Helper Function)\n\n", Color.FromArgb(0, 0, 180), true, 12);
                    AppendRefText("No details available for this function.\n", Color.FromArgb(150, 150, 150), false, 10);
                }
            }

            refDetailBox.SelectionStart = 0;
            SafeScrollToCaret(refDetailBox);
        }

        private void SafeScrollToCaret(RichTextBox rtb)
        {
            if (!rtb.IsHandleCreated) return;
            try { rtb.ScrollToCaret(); }
            catch (System.Runtime.InteropServices.ExternalException) { }
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

        private void TogglePanel(ToolDockContent panel)
        {
            if (panel.DockPanel != null && panel.DockState != DockState.Hidden && panel.DockState != DockState.Unknown)
            {
                panel.DockPanel = null;
                return;
            }
            panel.Show(dockPanel, DockState.DockRight);
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

        private void OnRunScript(object sender, EventArgs e)
        {
            if (pythonRunner.IsScriptRunning)
            {
                AppendOutput("A script is already running.\n", Color.FromArgb(180, 140, 0));
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

            var config = GetActiveRunConfiguration();
            string script = null;
            string runFileName = "untitled";
            string scriptArgs = config != null ? config.Arguments : null;
            string inputFile = ResolveInputFilePath(config);

            if (config != null && !config.UseCurrentFile)
            {
                string resolvedPath = ResolveScriptPath(config);
                if (resolvedPath == null || !File.Exists(resolvedPath))
                {
                    AppendOutput("Script not found: " + (config.ScriptPath ?? "(none)") + "\n", Color.FromArgb(200, 0, 0));
                    return;
                }

                var matchingTab = openFiles.Find(f =>
                    f.FilePath != null &&
                    f.FilePath.Equals(resolvedPath, StringComparison.OrdinalIgnoreCase));
                if (matchingTab != null && matchingTab.Editor != null)
                {
                    script = matchingTab.Editor.GetText();
                    runFileName = matchingTab.FileName;
                }
                else
                {
                    script = File.ReadAllText(resolvedPath);
                    runFileName = Path.GetFileName(resolvedPath);
                }
            }
            else
            {
                if (pythonEditor == null) return;
                script = pythonEditor.GetText();
                runFileName = activeFile != null ? activeFile.FileName : "untitled";
            }

            if (string.IsNullOrWhiteSpace(script))
            {
                AppendOutput("No script to run.\n", Color.FromArgb(180, 140, 0));
                return;
            }

            SaveAllFilesToDisk();
            ShowDockPanel(outputDockContent);

            string configLabel = config != null ? " [" + config.Name + "]" : "";
            string argsLabel = !string.IsNullOrEmpty(scriptArgs) ? " args: " + scriptArgs : "";
            string inputLabel = inputFile != null ? " stdin: " + Path.GetFileName(inputFile) : "";
            RaiseStatus("Running " + runFileName + configLabel + "...");
            AppendOutput("--- Running [" + runFileName + "]" + configLabel + " as __main__ at " + DateTime.Now.ToString("HH:mm:ss") + argsLabel + inputLabel + " ---\n", Color.FromArgb(0, 100, 180));

            SetScriptRunningUI(true);

            Dictionary<string, IInMemoryDataSource> memData = null;
            if (inMemoryDataSources.Count > 0)
                memData = SerializeInMemoryData();

            Dictionary<string, IStreamingDataSource> streamData = null;
            if (streamingDataSources.Count > 0)
                streamData = new Dictionary<string, IStreamingDataSource>(streamingDataSources);

            string preamble = BuildPreamble();

            pythonRunner.ExecuteAsync(script, memData, streamData, preamble,
                outputChunk => RunOnUIThread(() =>
                {
                    string pendingInput = "";
                    if (inputStartPosition >= 0 && outputBox.TextLength > inputStartPosition)
                    {
                        pendingInput = outputBox.Text.Substring(inputStartPosition);
                        outputBox.SelectionStart = inputStartPosition;
                        outputBox.SelectionLength = pendingInput.Length;
                        outputBox.SelectedText = "";
                    }
                    AppendOutput(outputChunk, Color.FromArgb(0, 0, 0));
                    inputStartPosition = outputBox.TextLength;
                    if (pendingInput.Length > 0)
                    {
                        outputBox.SelectionStart = outputBox.TextLength;
                        outputBox.SelectionLength = 0;
                        outputBox.SelectionColor = Color.FromArgb(0, 100, 0);
                        outputBox.AppendText(pendingInput);
                        SafeScrollToCaret(outputBox);
                    }
                }),
                errorLine => RunOnUIThread(() =>
                {
                    AppendOutput(errorLine + "\n", Color.FromArgb(200, 0, 0));
                }),
                result => RunOnUIThread(() =>
                {
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
                    SetScriptRunningUI(false);
                }),
                scriptArgs,
                inputFile
            );
        }

        private void OnStopScript(object sender, EventArgs e)
        {
            if (!pythonRunner.IsScriptRunning) return;
            pythonRunner.CancelExecution();
            AppendOutput("\n--- Script terminated by user ---\n", Color.FromArgb(180, 30, 30));
        }

        private void SetScriptRunningUI(bool running)
        {
            runToolBtn.Visible = !running;
            stopToolBtn.Visible = running;
            syntaxCheckToolBtn.Enabled = !running;
            configDropDown.Enabled = !running;

            outputBox.ReadOnly = !running;
            if (running)
            {
                inputStartPosition = outputBox.TextLength;
                outputBox.Focus();
            }
            else
            {
                inputStartPosition = -1;
                outputBox.ReadOnly = true;
            }
        }

        private void OnOutputBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (!pythonRunner.IsScriptRunning || inputStartPosition < 0)
            {
                if (e.KeyCode == Keys.C && e.Control)
                    return;
                e.SuppressKeyPress = true;
                return;
            }

            if (e.KeyCode == Keys.C && e.Control && !e.Shift)
            {
                if (outputBox.SelectionLength == 0)
                {
                    e.SuppressKeyPress = true;
                    pythonRunner.CancelExecution();
                    AppendOutput("\n--- Script interrupted (Ctrl+C) ---\n", Color.FromArgb(180, 30, 30));
                }
                return;
            }

            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                string inputText = "";
                if (outputBox.TextLength > inputStartPosition)
                    inputText = outputBox.Text.Substring(inputStartPosition);
                AppendOutput("\n", Color.FromArgb(0, 0, 0));
                inputStartPosition = outputBox.TextLength;
                pythonRunner.SendInput(inputText);
                return;
            }

            if (e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete)
            {
                if (outputBox.SelectionStart <= inputStartPosition && outputBox.SelectionLength == 0)
                {
                    e.SuppressKeyPress = true;
                    return;
                }
                if (outputBox.SelectionStart < inputStartPosition)
                {
                    e.SuppressKeyPress = true;
                    return;
                }
            }

            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Home ||
                e.KeyCode == Keys.Up || e.KeyCode == Keys.PageUp)
            {
                if (outputBox.SelectionStart <= inputStartPosition && !e.Shift)
                {
                    e.SuppressKeyPress = true;
                    return;
                }
            }

            if (e.KeyCode == Keys.X && e.Control)
            {
                if (outputBox.SelectionStart < inputStartPosition)
                {
                    e.SuppressKeyPress = true;
                    return;
                }
            }

            if (e.KeyCode == Keys.A && e.Control)
            {
                e.SuppressKeyPress = true;
                if (outputBox.TextLength > inputStartPosition)
                {
                    outputBox.SelectionStart = inputStartPosition;
                    outputBox.SelectionLength = outputBox.TextLength - inputStartPosition;
                }
                return;
            }
        }

        private void OnOutputBoxKeyPress(object sender, KeyPressEventArgs e)
        {
            if (!pythonRunner.IsScriptRunning || inputStartPosition < 0)
            {
                e.Handled = true;
                return;
            }

            if (outputBox.SelectionStart < inputStartPosition)
            {
                outputBox.SelectionStart = outputBox.TextLength;
                outputBox.SelectionLength = 0;
            }
        }

        private void OnCheckSyntax(object sender, EventArgs e)
        {
            if (pythonEditor == null) return;
            string script = pythonEditor.GetText();
            if (string.IsNullOrWhiteSpace(script))
            {
                AppendOutput("No script to check.\n", Color.FromArgb(180, 140, 0));
                return;
            }

            if (!pythonRunner.PythonAvailable)
            {
                AppendOutput("Cannot check syntax: " + pythonRunner.PythonError + "\n", Color.FromArgb(200, 0, 0));
                RaiseStatus("Python not available");
                return;
            }

            RaiseStatus("Checking syntax...");
            var result = pythonRunner.CheckSyntax(script);

            if (result.Success)
            {
                syntaxDiagnostics.Clear();
                MergeDiagnostics();
                AppendOutput("Syntax OK - no errors found.\n", Color.FromArgb(0, 128, 0));
                RaiseStatus("Syntax check passed.");
            }
            else
            {
                AppendOutput("Syntax Error:\n" + result.Error + "\n", Color.FromArgb(200, 0, 0));

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
                }
                RaiseStatus("Syntax error on line " + errorLine);
            }
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

        private void LoadRunConfigurations()
        {
            int savedIndex;
            runConfigurations = RunConfigurationStore.Load(configFilePath, out savedIndex);
            selectedConfigIndex = savedIndex;
            RefreshConfigDropDown();
        }

        private void SaveRunConfigurations()
        {
            RunConfigurationStore.Save(configFilePath, runConfigurations, selectedConfigIndex);
        }

        private void RefreshConfigDropDown()
        {
            if (configDropDown == null) return;
            configDropDown.DropDownItems.Clear();

            var currentFileItem = new ToolStripMenuItem("Current File");
            currentFileItem.Checked = (selectedConfigIndex < 0);
            currentFileItem.Click += (s, ev) =>
            {
                selectedConfigIndex = -1;
                configDropDown.Text = "Current File";
                SaveRunConfigurations();
                RefreshConfigDropDown();
            };
            configDropDown.DropDownItems.Add(currentFileItem);

            if (runConfigurations.Count > 0)
                configDropDown.DropDownItems.Add(new ToolStripSeparator());

            for (int i = 0; i < runConfigurations.Count; i++)
            {
                int idx = i;
                var item = new ToolStripMenuItem(runConfigurations[i].Name);
                item.Checked = (selectedConfigIndex == idx);
                item.Click += (s, ev) =>
                {
                    selectedConfigIndex = idx;
                    configDropDown.Text = runConfigurations[idx].Name;
                    SaveRunConfigurations();
                    RefreshConfigDropDown();
                };
                configDropDown.DropDownItems.Add(item);
            }

            configDropDown.DropDownItems.Add(new ToolStripSeparator());
            var editItem = new ToolStripMenuItem("Edit Configurations...");
            editItem.Click += OnEditConfigurations;
            configDropDown.DropDownItems.Add(editItem);

            if (selectedConfigIndex >= 0 && selectedConfigIndex < runConfigurations.Count)
                configDropDown.Text = runConfigurations[selectedConfigIndex].Name;
            else
                configDropDown.Text = "Current File";
        }

        private void OnEditConfigurations(object sender, EventArgs e)
        {
            using (var dlg = new RunConfigurationDialog(runConfigurations, Math.Max(0, selectedConfigIndex), scriptsDir))
            {
                if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK)
                {
                    runConfigurations = dlg.Configurations;
                    selectedConfigIndex = dlg.SelectedIndex;
                    if (selectedConfigIndex >= runConfigurations.Count)
                        selectedConfigIndex = -1;
                    SaveRunConfigurations();
                    RefreshConfigDropDown();
                }
            }
        }

        private RunConfiguration GetActiveRunConfiguration()
        {
            if (selectedConfigIndex >= 0 && selectedConfigIndex < runConfigurations.Count)
                return runConfigurations[selectedConfigIndex];
            return null;
        }

        private string ResolveScriptPath(RunConfiguration config)
        {
            if (config == null || config.UseCurrentFile)
                return null;
            if (string.IsNullOrEmpty(config.ScriptPath))
                return null;

            string scriptPath = config.ScriptPath;
            if (!Path.IsPathRooted(scriptPath))
            {
                string pythonDir = Path.GetDirectoryName(scriptsDir);
                scriptPath = Path.Combine(pythonDir, scriptPath);
            }
            return scriptPath;
        }

        private string ResolveInputFilePath(RunConfiguration config)
        {
            if (config == null || string.IsNullOrEmpty(config.InputFilePath))
                return null;

            string inputPath = config.InputFilePath;
            if (!Path.IsPathRooted(inputPath))
            {
                string pythonDir = Path.GetDirectoryName(scriptsDir);
                inputPath = Path.Combine(pythonDir, inputPath);
            }
            return inputPath;
        }

        private void InitializeFileSystem()
        {
            scriptsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python", "scripts");
            if (!Directory.Exists(scriptsDir))
                Directory.CreateDirectory(scriptsDir);

            configFilePath = Path.Combine(Path.GetDirectoryName(scriptsDir), "run_configurations.ini");
            LoadRunConfigurations();

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
            fileTreeView.AfterLabelEdit += OnFileTreeAfterLabelEdit;
            fileNewBtn.Click += OnNewFile;
            fileOpenBtn.Click += OnOpenFile;
            fileCloseBtn.Click += OnCloseFile;

            fileContextMenu = new ContextMenuStrip();
            fileTreeView.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    var node = fileTreeView.GetNodeAt(e.X, e.Y);
                    fileTreeView.SelectedNode = node;
                    BuildFileContextMenu(node);
                    fileContextMenu.Show(fileTreeView.PointToScreen(e.Location));
                }
            };
        }

        private void BeginNodeRename(TreeNode node)
        {
            if (node == null) return;
            string tag = node.Tag as string;
            if (tag != null && !tag.StartsWith("UNSAVED:"))
                node.Text = Path.GetFileName(tag);
            else if (tag != null && tag.StartsWith("UNSAVED:"))
                node.Text = tag.Substring("UNSAVED:".Length);
            fileTreeView.LabelEdit = true;
            node.BeginEdit();
        }

        private void BuildFileContextMenu(TreeNode node)
        {
            fileContextMenu.Items.Clear();

            var newFileCtx = new ToolStripMenuItem("New File");
            newFileCtx.Click += OnNewFile;
            fileContextMenu.Items.Add(newFileCtx);
            var newFolderCtx = new ToolStripMenuItem("New Folder");
            newFolderCtx.Click += OnNewFolder;
            fileContextMenu.Items.Add(newFolderCtx);

            if (node != null)
            {
                string path = (string)node.Tag;
                bool isFolder = Directory.Exists(path);

                fileContextMenu.Items.Add(new ToolStripSeparator());

                if (!isFolder)
                {
                    var openItem = new ToolStripMenuItem("Open");
                    openItem.Click += (s, e) => OpenFileFromTree(node);
                    fileContextMenu.Items.Add(openItem);
                }

                var renameItem = new ToolStripMenuItem("Rename");
                renameItem.Click += (s, e) => { BeginNodeRename(node); };
                fileContextMenu.Items.Add(renameItem);

                var deleteItem = new ToolStripMenuItem("Delete");
                deleteItem.Click += (s, e) => OnDeleteFileOrFolder(node);
                fileContextMenu.Items.Add(deleteItem);
            }
        }

        private void OnFileTreeNodeClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            OpenFileFromTree(e.Node);
        }

        private void OnFileTreeNodeDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            OpenFileFromTree(e.Node);
        }

        private void OpenFileFromTree(TreeNode node)
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
            {
                fileTreeView.SelectedNode = node;
                BeginNodeRename(node);
            }

            RaiseStatus("Created folder: " + Path.GetFileName(folderPath));
        }

        private void OnDeleteFileOrFolder(TreeNode node)
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

        private void OnFileTreeAfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            fileTreeView.LabelEdit = false;

            if (e.Label == null)
            {
                e.CancelEdit = true;
                return;
            }

            string newName = e.Label.Trim();
            if (string.IsNullOrEmpty(newName))
            {
                e.CancelEdit = true;
                return;
            }

            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in newName)
            {
                if (Array.IndexOf(invalidChars, c) >= 0)
                {
                    e.CancelEdit = true;
                    MessageBox.Show("The name contains invalid characters.", "Invalid Name",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            string oldPath = e.Node.Tag as string;
            if (oldPath == null || oldPath.StartsWith("UNSAVED:"))
            {
                e.CancelEdit = true;
                return;
            }

            bool isFolder = Directory.Exists(oldPath);
            string parentDir = Path.GetDirectoryName(oldPath);

            if (!isFolder && !newName.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
                newName += ".py";

            string newPath = Path.Combine(parentDir, newName);

            if (newPath.Equals(oldPath, StringComparison.OrdinalIgnoreCase))
            {
                e.CancelEdit = true;
                return;
            }

            if ((isFolder && Directory.Exists(newPath)) || (!isFolder && File.Exists(newPath)))
            {
                e.CancelEdit = true;
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

                e.CancelEdit = true;
                RefreshFileList();
                RaiseStatus("Renamed to: " + newName);
            }
            catch (Exception ex)
            {
                e.CancelEdit = true;
                MessageBox.Show("Failed to rename: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshFileList()
        {
            fileTreeView.BeginUpdate();
            fileTreeView.Nodes.Clear();
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

        private void PopulateTreeNode(TreeNodeCollection parentNodes, string dirPath)
        {
            foreach (var subDir in Directory.GetDirectories(dirPath).OrderBy(d => d))
            {
                string dirName = Path.GetFileName(subDir);
                var dirNode = new TreeNode(dirName);
                dirNode.Tag = subDir;
                dirNode.ForeColor = Color.FromArgb(80, 80, 80);
                dirNode.ImageKey = "folder";
                dirNode.SelectedImageKey = "folder_open";
                parentNodes.Add(dirNode);
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

                var fileNode = new TreeNode(displayName);
                fileNode.Tag = filePath;
                fileNode.ImageKey = "python";
                fileNode.SelectedImageKey = "python";

                if (isActive)
                    fileNode.ForeColor = Color.FromArgb(0, 90, 180);
                else if (fileTab != null)
                    fileNode.ForeColor = Color.FromArgb(30, 30, 30);
                else
                    fileNode.ForeColor = Color.FromArgb(100, 100, 100);

                parentNodes.Add(fileNode);
            }

            foreach (var ft in openFiles)
            {
                if (ft.FilePath == null)
                {
                    bool isActive = (ft == activeFile);
                    string displayName = isActive ? "\u25b8 " + ft.FileName
                        : ft.IsModified ? "\u2022 " + ft.FileName
                        : ft.FileName;
                    var node = new TreeNode(displayName);
                    node.Tag = "UNSAVED:" + ft.FileName;
                    node.ImageKey = "python";
                    node.SelectedImageKey = "python";
                    node.ForeColor = isActive ? Color.FromArgb(0, 90, 180) : Color.FromArgb(128, 128, 128);
                    fileTreeView.Nodes.Add(node);
                }
            }
        }

        private TreeNode FindNodeByPath(TreeNodeCollection nodes, string path)
        {
            foreach (TreeNode node in nodes)
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
            {
                fileTreeView.SelectedNode = node;
                BeginNodeRename(node);
            }

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

        private ContextMenuStrip CreateTabContextMenu(FileTab tab)
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Close", null, (s, e) => OnCloseFileTab(tab));
            menu.Items.Add("Close Others", null, (s, e) => OnCloseOtherTabs(tab));
            menu.Items.Add("Close All", null, (s, e) => OnCloseAllTabs());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Save", null, (s, e) =>
            {
                if (tab.Editor != null) tab.Content = tab.Editor.GetText();
                if (tab.FilePath != null)
                {
                    File.WriteAllText(tab.FilePath, tab.Content ?? "");
                    tab.IsModified = false;
                    if (tab.DockContent != null)
                        tab.DockContent.Text = tab.FileName;
                    RefreshFileList();
                    RaiseStatus("Saved " + tab.FileName);
                }
            });
            menu.Opening += (s, e) =>
            {
                menu.Items[1].Enabled = openFiles.Count > 1;
                menu.Items[2].Enabled = openFiles.Count > 1;
            };
            return menu;
        }

        private void SetupTabMiddleClick(FileDockContent content)
        {
            if (content.Pane == null) return;
            var strip = content.Pane.TabStripControl;
            if (strip == null) return;
            if (strip.Tag != null) return;
            strip.Tag = "hooked";
            strip.MouseClick += (s, e) =>
            {
                if (e.Button != MouseButtons.Middle) return;
                var pane = content.Pane;
                if (pane == null) return;
                var hoverContent = pane.MouseOverTab as FileDockContent;
                if (hoverContent == null) return;
                var tab = openFiles.Find(f => f.DockContent == hoverContent);
                if (tab != null)
                    OnCloseFileTab(tab);
            };
        }

        private void OnCloseOtherTabs(FileTab keepTab)
        {
            var toClose = new List<FileTab>();
            foreach (var f in openFiles)
            {
                if (f != keepTab)
                    toClose.Add(f);
            }
            foreach (var f in toClose)
            {
                if (f.IsModified)
                {
                    var result = MessageBox.Show(
                        "Save changes to " + f.FileName + "?",
                        "Unsaved Changes",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);
                    if (result == DialogResult.Cancel) return;
                    if (result == DialogResult.Yes)
                    {
                        string content = f.Editor != null ? f.Editor.GetText() : f.Content ?? "";
                        if (f.FilePath != null)
                            File.WriteAllText(f.FilePath, content);
                    }
                }
                openFiles.Remove(f);
                if (f.DockContent != null)
                {
                    f.DockContent.AllowClose = true;
                    f.DockContent.Close();
                    f.DockContent = null;
                }
                f.Editor = null;
            }
            if (activeFile == null || !openFiles.Contains(activeFile))
            {
                activeFile = null;
                SwitchToFile(keepTab);
            }
            RefreshFileList();
        }

        private void OnCloseAllTabs()
        {
            if (openFiles.Count <= 1) return;
            var toClose = new List<FileTab>(openFiles);
            var keepTab = toClose[0];
            toClose.RemoveAt(0);
            foreach (var f in toClose)
            {
                if (f.IsModified)
                {
                    var result = MessageBox.Show(
                        "Save changes to " + f.FileName + "?",
                        "Unsaved Changes",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);
                    if (result == DialogResult.Cancel) return;
                    if (result == DialogResult.Yes)
                    {
                        string content = f.Editor != null ? f.Editor.GetText() : f.Content ?? "";
                        if (f.FilePath != null)
                            File.WriteAllText(f.FilePath, content);
                    }
                }
                openFiles.Remove(f);
                if (f.DockContent != null)
                {
                    f.DockContent.AllowClose = true;
                    f.DockContent.Close();
                    f.DockContent = null;
                }
                f.Editor = null;
            }
            if (activeFile == null || !openFiles.Contains(activeFile))
            {
                activeFile = null;
                SwitchToFile(keepTab);
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
                packageNameBox.Text = quickCombo.SelectedItem.ToString();
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
            tips.SetToolTip(refTreeView, "Browse available datasets, columns, registered classes, context variables, and helper functions");
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
                SafeScrollToCaret(outputBox);
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
