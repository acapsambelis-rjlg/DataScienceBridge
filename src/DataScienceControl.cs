using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DataScienceWorkbench
{
    public partial class DataScienceControl : UserControl
    {
        private PythonRunner pythonRunner;
        private DataGenerator dataGen;
        private List<Product> products;
        private List<Customer> customers;
        private List<Order> orders;
        private List<Employee> employees;
        private List<SensorReading> sensorReadings;
        private List<StockPrice> stockPrices;
        private List<WebEvent> webEvents;

        private bool packagesLoaded;
        private PythonSyntaxHighlighter syntaxHighlighter;
        private Timer highlightTimer;
        private bool suppressHighlight;
        private bool textDirty;

        private List<UndoEntry> undoStack = new List<UndoEntry>();
        private List<UndoEntry> redoStack = new List<UndoEntry>();
        private bool isUndoRedoAction;
        private const int MaxUndoLevels = 100;

        private AutoCompletePopup autoComplete;
        private bool suppressAutoComplete;
        private PythonSymbolAnalyzer symbolAnalyzer = new PythonSymbolAnalyzer();
        private Dictionary<string, Func<string>> inMemoryDataSources = new Dictionary<string, Func<string>>();
        private HashSet<int> bookmarks = new HashSet<int>();
        private Dictionary<int, bool> foldedRegions = new Dictionary<int, bool>();
        private List<FoldRegion> foldRegions = new List<FoldRegion>();

        private class FoldRegion
        {
            public int StartLine;
            public int EndLine;
            public string HeaderText;
            public bool Collapsed;
        }

        private static readonly Dictionary<char, char> BracketPairs = new Dictionary<char, char>
        {
            { '(', ')' }, { '[', ']' }, { '{', '}' }, { '"', '"' }, { '\'', '\'' }
        };

        private class UndoEntry
        {
            public string Text;
            public int CursorPos;
        }

        public event EventHandler<string> StatusChanged;

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
            var monoFont10 = ResolveMonoFont(10f);
            var monoFont9 = ResolveMonoFont(9f);
            var uiFont = ResolveUIFont(9f);
            var uiFontBold = ResolveUIFont(9f, FontStyle.Bold);

            pythonEditor.Font = monoFont10;
            outputBox.Font = monoFont9;
            dataTreeView.Font = monoFont9;
            packageListBox.Font = monoFont9;
            lineNumberPanel.UpdateFont(monoFont9);

            treeLabel.Font = uiFontBold;
            outputLabel.Font = uiFontBold;
            pkgListLabel.Font = uiFontBold;
            dataGrid.Font = uiFont;
        }

        public DataScienceControl()
        {
            InitializeComponent();
            ResolveRuntimeFonts();
            InitializeData();
            SetupSnippetMenu();
            SetupSyntaxHighlighting();
            suppressHighlight = true;
            pythonEditor.Text = GetDefaultScript();
            suppressHighlight = false;
            ResetUndoStack();
            ApplySyntaxHighlighting();
            PopulateDataTree();
            RegisterAllDatasetsInMemory();
            PopulateReferenceTree();
            datasetCombo.SelectedIndex = 0;
        }

        private void SetupSyntaxHighlighting()
        {
            syntaxHighlighter = new PythonSyntaxHighlighter();
            lineNumberPanel.AttachEditor(pythonEditor);
            lineNumberPanel.BookmarkToggled += (s, line) =>
            {
                if (bookmarks.Contains(line))
                    bookmarks.Remove(line);
                else
                    bookmarks.Add(line);
                lineNumberPanel.SetBookmarks(bookmarks);
                RaiseStatus(bookmarks.Contains(line) ? "Bookmark set on line " + (line + 1) : "Bookmark removed from line " + (line + 1));
            };
            lineNumberPanel.FoldToggled += (s, line) =>
            {
                ToggleFold(line);
            };
            autoComplete = new AutoCompletePopup(pythonEditor);

            highlightTimer = new Timer();
            highlightTimer.Interval = 500;
            highlightTimer.Tick += (s, e) =>
            {
                highlightTimer.Stop();
                if (!textDirty) return;
                textDirty = false;
                ApplySyntaxHighlighting();
                RunLiveSyntaxCheck();
            };

            pythonEditor.TextChanged += (s, e) =>
            {
                if (!suppressHighlight)
                {
                    textDirty = true;
                    highlightTimer.Stop();
                    highlightTimer.Start();

                    if (!isUndoRedoAction)
                    {
                        PushUndo(pythonEditor.Text, pythonEditor.SelectionStart);
                    }

                    if (!suppressAutoComplete)
                    {
                        autoComplete.OnTextChanged();
                    }
                }
            };

            pythonEditor.SelectionChanged += (s, e) =>
            {
                if (!suppressHighlight)
                {
                    pythonEditor.UpdateBracketMatching();
                    pythonEditor.Invalidate();
                    UpdateCursorPositionStatus();
                    autoComplete.OnSelectionChanged();
                }
            };

            pythonEditor.KeyDown += (s, e) =>
            {
                if (autoComplete.IsShowing && autoComplete.HandleKeyDown(e.KeyCode, e.Modifiers))
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }

                if (e.Control && e.KeyCode == Keys.Z)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    PerformUndo();
                }
                else if (e.Control && e.KeyCode == Keys.Y)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    PerformRedo();
                }
                else if (e.KeyCode == Keys.Escape && autoComplete.IsShowing)
                {
                    autoComplete.Hide();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Back && !e.Control && pythonEditor.SelectionLength == 0)
                {
                    if (HandleBracketAutoDelete(false))
                    {
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                }
                else if (e.KeyCode == Keys.Delete && !e.Control && pythonEditor.SelectionLength == 0)
                {
                    if (HandleBracketAutoDelete(true))
                    {
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                }
                else if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    HandleAutoIndent();
                }
                else if (e.KeyCode == Keys.Tab && pythonEditor.SelectionLength > 0)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    BlockIndent(!e.Shift);
                }
                else if (e.KeyCode == Keys.Tab && e.Shift)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    BlockIndent(false);
                }
                else if (e.Control && e.KeyCode == Keys.D)
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
                else if (e.Control && e.KeyCode == Keys.B)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    ToggleBookmarkAtCursor();
                }
                else if (e.KeyCode == Keys.F2 && !e.Shift)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    GoToNextBookmark();
                }
                else if (e.KeyCode == Keys.F2 && e.Shift)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    GoToPreviousBookmark();
                }
            };

            pythonEditor.KeyPress += (s, e) =>
            {
                if (HandleBracketAutoClose(e.KeyChar))
                {
                    e.Handled = true;
                    return;
                }

                if (e.KeyChar == '\t')
                {
                    e.Handled = true;
                    suppressAutoComplete = true;
                    pythonEditor.SelectedText = "    ";
                    suppressAutoComplete = false;
                }
            };
        }

        private bool HandleBracketAutoClose(char typed)
        {
            string text = pythonEditor.Text;
            int pos = pythonEditor.SelectionStart;

            if (typed == '"' || typed == '\'')
            {
                if (pos < text.Length && text[pos] == typed)
                {
                    pythonEditor.SelectionStart = pos + 1;
                    return true;
                }

                bool atWordChar = pos > 0 && (char.IsLetterOrDigit(text[pos - 1]) || text[pos - 1] == '_');
                if (!atWordChar)
                {
                    suppressAutoComplete = true;
                    int selLen = pythonEditor.SelectionLength;
                    if (selLen > 0)
                    {
                        string selected = pythonEditor.SelectedText;
                        pythonEditor.SelectedText = typed.ToString() + selected + typed.ToString();
                        pythonEditor.SelectionStart = pos + 1;
                        pythonEditor.SelectionLength = selLen;
                    }
                    else
                    {
                        pythonEditor.SelectedText = typed.ToString() + typed.ToString();
                        pythonEditor.SelectionStart = pos + 1;
                    }
                    suppressAutoComplete = false;
                    return true;
                }
                return false;
            }

            if (BracketPairs.ContainsKey(typed) && typed != '"' && typed != '\'')
            {
                char close = BracketPairs[typed];

                if (typed == close && pos < text.Length && text[pos] == typed)
                {
                    pythonEditor.SelectionStart = pos + 1;
                    return true;
                }

                suppressAutoComplete = true;
                int selLen = pythonEditor.SelectionLength;
                if (selLen > 0)
                {
                    string selected = pythonEditor.SelectedText;
                    pythonEditor.SelectedText = typed.ToString() + selected + close.ToString();
                    pythonEditor.SelectionStart = pos + 1;
                    pythonEditor.SelectionLength = selLen;
                }
                else
                {
                    pythonEditor.SelectedText = typed.ToString() + close.ToString();
                    pythonEditor.SelectionStart = pos + 1;
                }
                suppressAutoComplete = false;
                return true;
            }

            bool isClosing = typed == ')' || typed == ']' || typed == '}';
            if (isClosing && pos < text.Length && text[pos] == typed)
            {
                pythonEditor.SelectionStart = pos + 1;
                return true;
            }

            return false;
        }

        private bool HandleBracketAutoDelete(bool isDeleteKey)
        {
            string text = pythonEditor.Text;
            int pos = pythonEditor.SelectionStart;

            int openPos, closePos;
            if (isDeleteKey)
            {
                openPos = pos;
                closePos = pos + 1;
            }
            else
            {
                openPos = pos - 1;
                closePos = pos;
            }

            if (openPos < 0 || closePos >= text.Length) return false;

            char openChar = text[openPos];
            char closeChar = text[closePos];

            if (!BracketPairs.ContainsKey(openChar)) return false;
            if (BracketPairs[openChar] != closeChar) return false;

            PushUndo(text, pos);
            suppressHighlight = true;
            pythonEditor.Select(openPos, 2);
            pythonEditor.SelectedText = "";
            pythonEditor.SelectionStart = openPos;
            suppressHighlight = false;
            ApplySyntaxHighlighting();

            return true;
        }

        private void HandleAutoIndent()
        {
            int pos = pythonEditor.SelectionStart;
            int lineIndex = pythonEditor.GetLineFromCharIndex(pos);
            string currentLine = lineIndex < pythonEditor.Lines.Length ? pythonEditor.Lines[lineIndex] : "";

            int charInLine = pos - pythonEditor.GetFirstCharIndexFromLine(lineIndex);
            string textBeforeCursor = charInLine <= currentLine.Length ? currentLine.Substring(0, charInLine) : currentLine;

            string indent = "";
            foreach (char c in textBeforeCursor)
            {
                if (c == ' ') indent += ' ';
                else break;
            }

            string trimmed = textBeforeCursor.TrimEnd();
            if (trimmed.EndsWith(":"))
            {
                indent += "    ";
            }

            suppressHighlight = true;
            suppressAutoComplete = true;
            PushUndo(pythonEditor.Text, pos);
            pythonEditor.SelectedText = "\n" + indent;
            suppressHighlight = false;
            suppressAutoComplete = false;
            ApplySyntaxHighlighting();
        }

        private void BlockIndent(bool indent)
        {
            string text = pythonEditor.Text;
            int selStart = pythonEditor.SelectionStart;
            int selEnd = selStart + pythonEditor.SelectionLength;
            int firstLine = pythonEditor.GetLineFromCharIndex(selStart);
            int lastLine = pythonEditor.GetLineFromCharIndex(selEnd > selStart ? selEnd - 1 : selEnd);
            if (lastLine < firstLine) lastLine = firstLine;

            suppressHighlight = true;
            var lines = new List<string>(pythonEditor.Lines);
            int totalDelta = 0;
            int firstLineDelta = 0;

            for (int i = firstLine; i <= lastLine && i < lines.Count; i++)
            {
                string line = lines[i];
                if (indent)
                {
                    lines[i] = "    " + line;
                    if (i == firstLine) firstLineDelta = 4;
                    totalDelta += 4;
                }
                else
                {
                    int removed = 0;
                    while (removed < 4 && removed < line.Length && line[removed] == ' ')
                        removed++;
                    if (removed > 0)
                    {
                        lines[i] = line.Substring(removed);
                        if (i == firstLine) firstLineDelta = -removed;
                        totalDelta -= removed;
                    }
                }
            }

            pythonEditor.Text = string.Join("\n", lines);

            int newSelStart = Math.Max(0, selStart + firstLineDelta);
            int newSelEnd = Math.Max(newSelStart, selEnd + totalDelta);
            pythonEditor.SelectionStart = newSelStart;
            pythonEditor.SelectionLength = newSelEnd - newSelStart;

            suppressHighlight = false;
            textDirty = true;
            highlightTimer.Stop();
            highlightTimer.Start();
            PushUndo(pythonEditor.Text, pythonEditor.SelectionStart);
        }

        private void DuplicateLine()
        {
            int pos = pythonEditor.SelectionStart;
            int lineIndex = pythonEditor.GetLineFromCharIndex(pos);
            if (lineIndex >= pythonEditor.Lines.Length) return;

            string line = pythonEditor.Lines[lineIndex];
            int lineStart = pythonEditor.GetFirstCharIndexFromLine(lineIndex);
            int colOffset = pos - lineStart;
            int lineEnd = lineStart + line.Length;

            suppressHighlight = true;
            string insertText = "\n" + line;
            pythonEditor.SelectionStart = lineEnd;
            pythonEditor.SelectionLength = 0;
            pythonEditor.SelectedText = insertText;

            int newLineStart = pythonEditor.GetFirstCharIndexFromLine(lineIndex + 1);
            pythonEditor.SelectionStart = newLineStart + Math.Min(colOffset, line.Length);
            pythonEditor.SelectionLength = 0;
            suppressHighlight = false;

            textDirty = true;
            highlightTimer.Stop();
            highlightTimer.Start();
            PushUndo(pythonEditor.Text, pythonEditor.SelectionStart);
        }

        private void MoveLine(bool up)
        {
            int pos = pythonEditor.SelectionStart;
            int lineIndex = pythonEditor.GetLineFromCharIndex(pos);
            var lines = new List<string>(pythonEditor.Lines);

            if (up && lineIndex <= 0) return;
            if (!up && lineIndex >= lines.Count - 1) return;

            int swapWith = up ? lineIndex - 1 : lineIndex + 1;
            string temp = lines[lineIndex];
            lines[lineIndex] = lines[swapWith];
            lines[swapWith] = temp;

            int colOffset = pos - pythonEditor.GetFirstCharIndexFromLine(lineIndex);
            string destLine = lines[swapWith];

            suppressHighlight = true;
            pythonEditor.Text = string.Join("\n", lines);

            int newLineStart = pythonEditor.GetFirstCharIndexFromLine(swapWith);
            int clampedCol = Math.Min(colOffset, destLine.Length);
            pythonEditor.SelectionStart = Math.Min(newLineStart + clampedCol, pythonEditor.Text.Length);
            pythonEditor.SelectionLength = 0;
            suppressHighlight = false;

            textDirty = true;
            highlightTimer.Stop();
            highlightTimer.Start();
            PushUndo(pythonEditor.Text, pythonEditor.SelectionStart);
        }

        private void ToggleBookmarkAtCursor()
        {
            int line = pythonEditor.GetLineFromCharIndex(pythonEditor.SelectionStart);
            if (bookmarks.Contains(line))
                bookmarks.Remove(line);
            else
                bookmarks.Add(line);
            lineNumberPanel.SetBookmarks(bookmarks);
            RaiseStatus(bookmarks.Contains(line) ? "Bookmark set on line " + (line + 1) : "Bookmark removed from line " + (line + 1));
        }

        private void GoToNextBookmark()
        {
            if (bookmarks.Count == 0) { RaiseStatus("No bookmarks set"); return; }
            int currentLine = pythonEditor.GetLineFromCharIndex(pythonEditor.SelectionStart);
            var sorted = bookmarks.OrderBy(b => b).ToList();
            int next = sorted.FirstOrDefault(b => b > currentLine);
            if (next == 0 && !bookmarks.Contains(0))
                next = sorted.FirstOrDefault(b => b != currentLine);
            if (next == 0 && sorted.Count > 0) next = sorted[0];
            GoToLine(next);
        }

        private void GoToPreviousBookmark()
        {
            if (bookmarks.Count == 0) { RaiseStatus("No bookmarks set"); return; }
            int currentLine = pythonEditor.GetLineFromCharIndex(pythonEditor.SelectionStart);
            var sorted = bookmarks.OrderByDescending(b => b).ToList();
            int prev = sorted.FirstOrDefault(b => b < currentLine);
            if (prev == 0 && !bookmarks.Contains(0))
                prev = sorted.FirstOrDefault(b => b != currentLine);
            if (prev == 0 && sorted.Count > 0) prev = sorted[0];
            GoToLine(prev);
        }

        private void GoToLine(int lineIndex)
        {
            if (lineIndex < 0 || lineIndex >= pythonEditor.Lines.Length) return;
            int charIdx = pythonEditor.GetFirstCharIndexFromLine(lineIndex);
            pythonEditor.SelectionStart = charIdx;
            pythonEditor.SelectionLength = 0;
            pythonEditor.ScrollToCaret();
            RaiseStatus("Ln " + (lineIndex + 1));
        }

        public HashSet<int> GetBookmarks() { return bookmarks; }

        private void ComputeFoldRegions()
        {
            foldRegions.Clear();
            var lines = pythonEditor.Lines;
            if (lines.Length == 0) return;

            var stack = new Stack<Tuple<int, int, string>>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.TrimStart();
                if (trimmed.Length == 0) continue;

                int indent = line.Length - trimmed.Length;

                while (stack.Count > 0 && stack.Peek().Item2 >= indent)
                {
                    var popped = stack.Pop();
                    int endLine = i - 1;
                    while (endLine > popped.Item1 && lines[endLine].Trim().Length == 0)
                        endLine--;
                    if (endLine > popped.Item1)
                    {
                        bool wasFolded = foldedRegions.ContainsKey(popped.Item1) && foldedRegions[popped.Item1];
                        foldRegions.Add(new FoldRegion
                        {
                            StartLine = popped.Item1,
                            EndLine = endLine,
                            HeaderText = popped.Item3,
                            Collapsed = wasFolded
                        });
                    }
                }

                if (trimmed.StartsWith("def ") || trimmed.StartsWith("class ") ||
                    trimmed.StartsWith("for ") || trimmed.StartsWith("while ") ||
                    trimmed.StartsWith("if ") || trimmed.StartsWith("elif ") ||
                    trimmed.StartsWith("else:") || trimmed.StartsWith("try:") ||
                    trimmed.StartsWith("except") || trimmed.StartsWith("finally:") ||
                    trimmed.StartsWith("with "))
                {
                    if (trimmed.EndsWith(":"))
                    {
                        stack.Push(Tuple.Create(i, indent, trimmed));
                    }
                }
            }

            while (stack.Count > 0)
            {
                var popped = stack.Pop();
                int endLine = lines.Length - 1;
                while (endLine > popped.Item1 && lines[endLine].Trim().Length == 0)
                    endLine--;
                if (endLine > popped.Item1)
                {
                    bool wasFolded = foldedRegions.ContainsKey(popped.Item1) && foldedRegions[popped.Item1];
                    foldRegions.Add(new FoldRegion
                    {
                        StartLine = popped.Item1,
                        EndLine = endLine,
                        HeaderText = popped.Item3,
                        Collapsed = wasFolded
                    });
                }
            }

            foldRegions.Sort((a, b) => a.StartLine.CompareTo(b.StartLine));
        }

        public void ToggleFold(int lineNumber)
        {
            ComputeFoldRegions();
            var region = foldRegions.FirstOrDefault(r => r.StartLine == lineNumber);
            if (region == null) return;

            region.Collapsed = !region.Collapsed;
            foldedRegions[lineNumber] = region.Collapsed;

            ApplyFolding();
        }

        private void ApplyFolding()
        {
            ComputeFoldRegions();
            lineNumberPanel.SetFoldRegions(foldRegions.Select(r => new LineNumberPanel.FoldInfo
            {
                StartLine = r.StartLine,
                EndLine = r.EndLine,
                Collapsed = r.Collapsed
            }).ToList());
            lineNumberPanel.Invalidate();
        }

        private void UpdateCursorPositionStatus()
        {
            int pos = pythonEditor.SelectionStart;
            int line = pythonEditor.GetLineFromCharIndex(pos) + 1;
            int firstChar = pythonEditor.GetFirstCharIndexFromLine(line - 1);
            int col = pos - firstChar + 1;
            RaiseStatus("Ln " + line + ", Col " + col);
        }

        private void ResetUndoStack()
        {
            undoStack.Clear();
            redoStack.Clear();
            undoStack.Add(new UndoEntry { Text = pythonEditor.Text, CursorPos = pythonEditor.SelectionStart });
        }

        private void PushUndo(string text, int cursorPos)
        {
            if (undoStack.Count > 0 && undoStack[undoStack.Count - 1].Text == text)
                return;

            undoStack.Add(new UndoEntry { Text = text, CursorPos = cursorPos });
            if (undoStack.Count > MaxUndoLevels)
                undoStack.RemoveAt(0);

            redoStack.Clear();
        }

        private void PerformUndo()
        {
            if (undoStack.Count <= 1) return;

            var current = undoStack[undoStack.Count - 1];
            undoStack.RemoveAt(undoStack.Count - 1);
            redoStack.Add(current);

            var prev = undoStack[undoStack.Count - 1];
            ApplyUndoRedoText(prev);
        }

        private void PerformRedo()
        {
            if (redoStack.Count == 0) return;

            var entry = redoStack[redoStack.Count - 1];
            redoStack.RemoveAt(redoStack.Count - 1);
            undoStack.Add(entry);

            ApplyUndoRedoText(entry);
        }

        private void ApplyUndoRedoText(UndoEntry entry)
        {
            isUndoRedoAction = true;
            suppressHighlight = true;
            try
            {
                pythonEditor.Text = entry.Text;
                pythonEditor.SelectionStart = Math.Min(entry.CursorPos, pythonEditor.Text.Length);
                pythonEditor.SelectionLength = 0;
            }
            finally
            {
                suppressHighlight = false;
                isUndoRedoAction = false;
            }

            textDirty = true;
            highlightTimer.Stop();
            highlightTimer.Start();
        }

        public bool CanUndo { get { return undoStack.Count > 1; } }
        public bool CanRedo { get { return redoStack.Count > 0; } }

        private void ApplySyntaxHighlighting()
        {
            if (pythonEditor.Text.Length > 50000) return;
            suppressAutoComplete = true;
            suppressHighlight = true;
            try
            {
                syntaxHighlighter.Highlight(pythonEditor);
            }
            catch { }
            suppressHighlight = false;
            suppressAutoComplete = false;
            RunSymbolAnalysis();
            ApplyFolding();
        }

        private void RunSymbolAnalysis()
        {
            try
            {
                string code = pythonEditor.Text;
                if (string.IsNullOrWhiteSpace(code))
                {
                    pythonEditor.ClearSymbolErrors();
                    return;
                }
                var errors = symbolAnalyzer.Analyze(code);
                pythonEditor.SetSymbolErrors(errors);
            }
            catch
            {
                pythonEditor.ClearSymbolErrors();
            }
        }

        private void RunLiveSyntaxCheck()
        {
            string script = pythonEditor.Text;
            if (string.IsNullOrWhiteSpace(script))
            {
                pythonEditor.ClearError();
                RaiseStatus("Ready");
                return;
            }

            try
            {
                var result = pythonRunner.CheckSyntax(script);

                if (result.Success)
                {
                    pythonEditor.ClearError();
                    RaiseStatus("Ready");
                }
                else
                {
                    int errorLine = ParseErrorLine(result.Error);
                    if (errorLine > 0)
                    {
                        var errorMsg = result.Error.Trim();
                        var firstLine = errorMsg.Split('\n')[0];
                        pythonEditor.SetErrorLine(errorLine, firstLine);
                        RaiseStatus("Line " + errorLine + ": " + firstLine);
                    }
                }
            }
            catch { }
        }

        public void LoadData(
            List<Product> products,
            List<Customer> customers,
            List<Order> orders,
            List<Employee> employees,
            List<SensorReading> sensorReadings,
            List<StockPrice> stockPrices,
            List<WebEvent> webEvents)
        {
            this.products = products;
            this.customers = customers;
            this.orders = orders;
            this.employees = employees;
            this.sensorReadings = sensorReadings;
            this.stockPrices = stockPrices;
            this.webEvents = webEvents;

            RegisterAllDatasetsInMemory();
            PopulateDataTree();
            PopulateReferenceTree();
            OnDatasetChanged(null, null);
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
                return sb.ToString();
            };
            PopulateReferenceTree();
        }

        public void RegisterInMemoryData<T>(string name, Func<List<T>> dataProvider) where T : class
        {
            inMemoryDataSources[name] = () =>
            {
                var data = dataProvider();
                var props = typeof(T).GetProperties();
                var sb = new System.Text.StringBuilder();

                var headerParts = new List<string>();
                foreach (var p in props)
                {
                    if (p.GetIndexParameters().Length > 0) continue;
                    if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(List<>)) continue;
                    if (p.PropertyType.IsClass && p.PropertyType != typeof(string)) continue;
                    headerParts.Add(p.Name);
                }
                sb.AppendLine(string.Join(",", headerParts));

                foreach (var item in data)
                {
                    var vals = new List<string>();
                    foreach (var p in props)
                    {
                        if (p.GetIndexParameters().Length > 0) continue;
                        if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(List<>)) continue;
                        if (p.PropertyType.IsClass && p.PropertyType != typeof(string)) continue;
                        var val = p.GetValue(item);
                        string s = val != null ? val.ToString() : "";
                        if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
                            s = "\"" + s.Replace("\"", "\"\"") + "\"";
                        vals.Add(s);
                    }
                    sb.AppendLine(string.Join(",", vals));
                }
                return sb.ToString();
            };
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
                return sb.ToString();
            };
            PopulateReferenceTree();
        }

        public void UnregisterInMemoryData(string name)
        {
            inMemoryDataSources.Remove(name);
            PopulateReferenceTree();
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

        public void RunScript()
        {
            OnRunScript(this, EventArgs.Empty);
        }

        public string ScriptText
        {
            get { return pythonEditor.Text; }
            set { pythonEditor.Text = value; ResetUndoStack(); }
        }

        public string OutputText
        {
            get { return outputBox.Text; }
        }

        public void ClearOutput()
        {
            outputBox.Clear();
        }

        public MenuStrip CreateMenuStrip()
        {
            var menuBar = new MenuStrip();

            var fileMenu = new ToolStripMenuItem("File");
            fileMenu.DropDownItems.Add("Open Script...", null, OnOpenScript);
            fileMenu.DropDownItems.Add("Save Script...", null, OnSaveScript);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());

            var editMenu = new ToolStripMenuItem("&Edit");

            var undoItem = new ToolStripMenuItem("Undo", null, (s, e) => { if (pythonEditor.Focused) PerformUndo(); });
            undoItem.ShortcutKeyDisplayString = "Ctrl+Z";
            editMenu.DropDownItems.Add(undoItem);

            var redoItem = new ToolStripMenuItem("Redo", null, (s, e) => { if (pythonEditor.Focused) PerformRedo(); });
            redoItem.ShortcutKeyDisplayString = "Ctrl+Y";
            editMenu.DropDownItems.Add(redoItem);

            editMenu.DropDownItems.Add(new ToolStripSeparator());

            var cutItem = new ToolStripMenuItem("Cut", null, (s, e) => { if (pythonEditor.Focused) pythonEditor.Cut(); });
            cutItem.ShortcutKeys = Keys.Control | Keys.X;
            editMenu.DropDownItems.Add(cutItem);

            var copyItem = new ToolStripMenuItem("Copy", null, (s, e) => { if (pythonEditor.Focused) pythonEditor.Copy(); });
            copyItem.ShortcutKeys = Keys.Control | Keys.C;
            editMenu.DropDownItems.Add(copyItem);

            var pasteItem = new ToolStripMenuItem("Paste", null, (s, e) => { if (pythonEditor.Focused) pythonEditor.Paste(); });
            pasteItem.ShortcutKeys = Keys.Control | Keys.V;
            editMenu.DropDownItems.Add(pasteItem);

            var deleteItem = new ToolStripMenuItem("Delete", null, (s, e) => { if (pythonEditor.Focused && pythonEditor.SelectionLength > 0) pythonEditor.SelectedText = ""; });
            deleteItem.ShortcutKeyDisplayString = "Del";
            editMenu.DropDownItems.Add(deleteItem);

            editMenu.DropDownItems.Add(new ToolStripSeparator());

            var selectAllItem = new ToolStripMenuItem("Select All", null, (s, e) => { if (pythonEditor.Focused) pythonEditor.SelectAll(); });
            selectAllItem.ShortcutKeys = Keys.Control | Keys.A;
            editMenu.DropDownItems.Add(selectAllItem);

            editMenu.DropDownItems.Add(new ToolStripSeparator());

            var findItem = new ToolStripMenuItem("Find && Replace...", null, (s, e) => ShowFindReplace());
            findItem.ShortcutKeys = Keys.Control | Keys.H;
            editMenu.DropDownItems.Add(findItem);

            editMenu.DropDownItems.Add(new ToolStripSeparator());

            var dupLineItem = new ToolStripMenuItem("Duplicate Line", null, (s, e) => { if (pythonEditor.Focused) DuplicateLine(); });
            dupLineItem.ShortcutKeyDisplayString = "Ctrl+D";
            editMenu.DropDownItems.Add(dupLineItem);

            var moveUpItem = new ToolStripMenuItem("Move Line Up", null, (s, e) => { if (pythonEditor.Focused) MoveLine(true); });
            moveUpItem.ShortcutKeyDisplayString = "Alt+Up";
            editMenu.DropDownItems.Add(moveUpItem);

            var moveDownItem = new ToolStripMenuItem("Move Line Down", null, (s, e) => { if (pythonEditor.Focused) MoveLine(false); });
            moveDownItem.ShortcutKeyDisplayString = "Alt+Down";
            editMenu.DropDownItems.Add(moveDownItem);

            editMenu.DropDownItems.Add(new ToolStripSeparator());

            var toggleBookmarkItem = new ToolStripMenuItem("Toggle Bookmark", null, (s, e) => { if (pythonEditor.Focused) ToggleBookmarkAtCursor(); });
            toggleBookmarkItem.ShortcutKeyDisplayString = "Ctrl+B";
            editMenu.DropDownItems.Add(toggleBookmarkItem);

            var nextBookmarkItem = new ToolStripMenuItem("Next Bookmark", null, (s, e) => GoToNextBookmark());
            nextBookmarkItem.ShortcutKeyDisplayString = "F2";
            editMenu.DropDownItems.Add(nextBookmarkItem);

            var prevBookmarkItem = new ToolStripMenuItem("Previous Bookmark", null, (s, e) => GoToPreviousBookmark());
            prevBookmarkItem.ShortcutKeyDisplayString = "Shift+F2";
            editMenu.DropDownItems.Add(prevBookmarkItem);

            editMenu.DropDownItems.Add(new ToolStripSeparator());
            editMenu.DropDownItems.Add("Clear Output", null, (s, e) => outputBox.Clear());

            editMenu.DropDownOpening += (s, e) =>
            {
                undoItem.Enabled = pythonEditor.Focused && CanUndo;
                redoItem.Enabled = pythonEditor.Focused && CanRedo;
                bool hasSelection = pythonEditor.Focused && pythonEditor.SelectionLength > 0;
                cutItem.Enabled = hasSelection;
                copyItem.Enabled = hasSelection;
                deleteItem.Enabled = hasSelection;
            };

            var runMenu = new ToolStripMenuItem("Run");
            runMenu.DropDownItems.Add("Execute Script (F5)", null, OnRunScript);

            var helpMenu = new ToolStripMenuItem("Help");
            helpMenu.DropDownItems.Add("Quick Start Guide", null, OnShowHelp);
            helpMenu.DropDownItems.Add("About", null, (s, e) => MessageBox.Show(
                "Data Science Workbench v1.0\n\n" +
                "A .NET Windows Forms control with\n" +
                "integrated Python scripting for data analysis.\n\n" +
                "Built with Mono + Python 3",
                "About", MessageBoxButtons.OK, MessageBoxIcon.Information));

            menuBar.Items.AddRange(new ToolStripItem[] { fileMenu, editMenu, runMenu, helpMenu });
            return menuBar;
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

        private void InitializeData()
        {
            dataGen = new DataGenerator(42);
            products = dataGen.GenerateProducts(200);
            customers = dataGen.GenerateCustomers(150, products);
            orders = dataGen.GenerateOrders(customers, products, 500);
            employees = dataGen.GenerateEmployees(100);
            sensorReadings = dataGen.GenerateSensorReadings(1000);
            stockPrices = dataGen.GenerateStockPrices(365);
            webEvents = dataGen.GenerateWebEvents(2000);

            pythonRunner = new PythonRunner();
        }

        private void SetupSnippetMenu()
        {
            insertSnippetBtn.DropDownItems.Add("List Datasets", null, (s, e) => InsertSnippet(GetLoadDataSnippet()));
            insertSnippetBtn.DropDownItems.Add("Basic Statistics", null, (s, e) => InsertSnippet(GetStatsSnippet()));
            insertSnippetBtn.DropDownItems.Add("Plot Histogram", null, (s, e) => InsertSnippet(GetHistogramSnippet()));
            insertSnippetBtn.DropDownItems.Add("Scatter Plot", null, (s, e) => InsertSnippet(GetScatterSnippet()));
            insertSnippetBtn.DropDownItems.Add("Group By Analysis", null, (s, e) => InsertSnippet(GetGroupBySnippet()));
            insertSnippetBtn.DropDownItems.Add("Correlation Matrix", null, (s, e) => InsertSnippet(GetCorrelationSnippet()));
            insertSnippetBtn.DropDownItems.Add("Time Series Plot", null, (s, e) => InsertSnippet(GetTimeSeriesSnippet()));
            insertSnippetBtn.DropDownItems.Add("Custom Data Access", null, (s, e) => InsertSnippet(GetCustomDataSnippet()));
        }

        private void RegisterAllDatasetsInMemory()
        {
            RegisterInMemoryData<Product>("products", () => products);
            RegisterInMemoryData<Customer>("customers", () => customers);
            RegisterInMemoryData<Employee>("employees", () => employees);
            RegisterInMemoryData<SensorReading>("sensor_readings", () => sensorReadings);
            RegisterInMemoryData<StockPrice>("stock_prices", () => stockPrices);
            RegisterInMemoryData<WebEvent>("web_events", () => webEvents);

            inMemoryDataSources["orders"] = () =>
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Id,CustomerId,OrderDate,ShipDate,Status,ShipMethod,ShippingCost,PaymentMethod,Subtotal,Total,ItemCount");
                foreach (var o in orders)
                {
                    sb.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}",
                        o.Id, o.CustomerId, o.OrderDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        o.ShipDate.HasValue ? o.ShipDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "",
                        o.Status, o.ShipMethod, o.ShippingCost, o.PaymentMethod,
                        Math.Round(o.Subtotal, 2), Math.Round(o.Total, 2), o.ItemCount));
                }
                return sb.ToString();
            };

            inMemoryDataSources["order_items"] = () =>
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("OrderId,ProductId,ProductName,Quantity,UnitPrice,Discount,LineTotal");
                foreach (var o in orders)
                {
                    foreach (var item in o.Items)
                    {
                        string prodName = item.ProductName.Contains(",") ? "\"" + item.ProductName + "\"" : item.ProductName;
                        sb.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6}",
                            o.Id, item.ProductId, prodName, item.Quantity,
                            item.UnitPrice, item.Discount, Math.Round(item.LineTotal, 2)));
                    }
                }
                return sb.ToString();
            };
        }

        private void PopulateDataTree()
        {
            dataTreeView.Nodes.Clear();
            var root = dataTreeView.Nodes.Add("Datasets");

            var prodNode = root.Nodes.Add("products (" + products.Count + " records)");
            prodNode.Nodes.Add("Id, Name, Category, SubCategory");
            prodNode.Nodes.Add("SKU, Price, Cost, Margin");
            prodNode.Nodes.Add("StockQuantity, ReorderLevel, Weight");
            prodNode.Nodes.Add("Supplier, Rating, ReviewCount");

            var custNode = root.Nodes.Add("customers (" + customers.Count + " records)");
            custNode.Nodes.Add("Id, FirstName, LastName, Email");
            custNode.Nodes.Add("Phone, DateOfBirth, RegistrationDate");
            custNode.Nodes.Add("Tier, CreditLimit, IsActive");
            custNode.Nodes.Add("Address fields (Street, City, State...)");

            var orderNode = root.Nodes.Add("orders (" + orders.Count + " records)");
            orderNode.Nodes.Add("Id, CustomerId, OrderDate, ShipDate");
            orderNode.Nodes.Add("Status, ShipMethod, ShippingCost");
            orderNode.Nodes.Add("PaymentMethod, Subtotal, Total");

            var itemsNode = root.Nodes.Add("order_items (line items)");
            itemsNode.Nodes.Add("OrderId, ProductId, ProductName");
            itemsNode.Nodes.Add("Quantity, UnitPrice, Discount, LineTotal");

            var empNode = root.Nodes.Add("employees (" + employees.Count + " records)");
            empNode.Nodes.Add("Id, FirstName, LastName, Department");
            empNode.Nodes.Add("Title, HireDate, Salary");
            empNode.Nodes.Add("PerformanceScore, ManagerId, IsRemote");

            var sensorNode = root.Nodes.Add("sensor_readings (" + sensorReadings.Count + " records)");
            sensorNode.Nodes.Add("SensorId, SensorType, Location");
            sensorNode.Nodes.Add("Timestamp, Value, Unit, Status");

            var stockNode = root.Nodes.Add("stock_prices (" + stockPrices.Count + " records)");
            stockNode.Nodes.Add("Symbol, CompanyName, Date");
            stockNode.Nodes.Add("Open, High, Low, Close, Volume");

            var webNode = root.Nodes.Add("web_events (" + webEvents.Count + " records)");
            webNode.Nodes.Add("SessionId, UserId, Timestamp");
            webNode.Nodes.Add("EventType, Page, Referrer");
            webNode.Nodes.Add("Browser, Device, Country, Duration");

            root.Expand();
        }

        private void PopulateReferenceTree()
        {
            refTreeView.Nodes.Clear();

            var datasets = new[]
            {
                new { Name = "products", Class = "Product", Count = products.Count, Tag = "products" },
                new { Name = "customers", Class = "Customer", Count = customers.Count, Tag = "customers" },
                new { Name = "orders", Class = "Order", Count = orders.Count, Tag = "orders" },
                new { Name = "order_items", Class = "OrderItem", Count = orders.Sum(o => o.Items != null ? o.Items.Count : 0), Tag = "order_items" },
                new { Name = "employees", Class = "Employee", Count = employees.Count, Tag = "employees" },
                new { Name = "sensor_readings", Class = "SensorReading", Count = sensorReadings.Count, Tag = "sensor_readings" },
                new { Name = "stock_prices", Class = "StockPrice", Count = stockPrices.Count, Tag = "stock_prices" },
                new { Name = "web_events", Class = "WebEvent", Count = webEvents.Count, Tag = "web_events" },
            };

            foreach (var ds in datasets)
            {
                var node = refTreeView.Nodes.Add(ds.Name + "  (" + ds.Count + ")");
                node.Tag = ds.Tag;
                node.NodeFont = new Font(refTreeView.Font, FontStyle.Bold);

                var columns = GetColumnsForDataset(ds.Tag);
                foreach (var col in columns)
                {
                    var child = node.Nodes.Add(col.Item1 + "  :  " + col.Item2);
                    child.Tag = ds.Tag;
                    child.ForeColor = Color.FromArgb(80, 80, 80);
                }
            }

            var customSources = new List<string>();
            foreach (var name in inMemoryDataSources.Keys)
            {
                bool isBuiltin = name == "products" || name == "customers" || name == "orders" ||
                                 name == "order_items" || name == "employees" || name == "sensor_readings" ||
                                 name == "stock_prices" || name == "web_events";
                if (!isBuiltin)
                    customSources.Add(name);
            }

            if (customSources.Count > 0)
            {
                var memNode = refTreeView.Nodes.Add("Custom Data Sources");
                memNode.NodeFont = new Font(refTreeView.Font, FontStyle.Bold);
                memNode.Tag = "inmemory";
                foreach (var name in customSources)
                {
                    var child = memNode.Nodes.Add(name);
                    child.Tag = "inmemory_" + name;
                    child.ForeColor = Color.FromArgb(80, 80, 80);
                }
                memNode.Expand();
            }

            if (refTreeView.Nodes.Count > 0)
                refTreeView.SelectedNode = refTreeView.Nodes[0];
        }

        private List<Tuple<string, string>> GetColumnsForDataset(string tag)
        {
            var cols = new List<Tuple<string, string>>();
            switch (tag)
            {
                case "products":
                    cols.Add(Tuple.Create("Id", "int"));
                    cols.Add(Tuple.Create("Name", "string"));
                    cols.Add(Tuple.Create("Category", "string"));
                    cols.Add(Tuple.Create("SubCategory", "string"));
                    cols.Add(Tuple.Create("SKU", "string"));
                    cols.Add(Tuple.Create("Price", "float"));
                    cols.Add(Tuple.Create("Cost", "float"));
                    cols.Add(Tuple.Create("StockQuantity", "int"));
                    cols.Add(Tuple.Create("ReorderLevel", "int"));
                    cols.Add(Tuple.Create("Weight", "float"));
                    cols.Add(Tuple.Create("Supplier", "string"));
                    cols.Add(Tuple.Create("Rating", "float"));
                    cols.Add(Tuple.Create("ReviewCount", "int"));
                    cols.Add(Tuple.Create("IsDiscontinued", "bool"));
                    cols.Add(Tuple.Create("DateAdded", "datetime"));
                    cols.Add(Tuple.Create("Margin", "float (computed)"));
                    cols.Add(Tuple.Create("MarginPercent", "float (computed)"));
                    break;
                case "customers":
                    cols.Add(Tuple.Create("Id", "int"));
                    cols.Add(Tuple.Create("FirstName", "string"));
                    cols.Add(Tuple.Create("LastName", "string"));
                    cols.Add(Tuple.Create("Email", "string"));
                    cols.Add(Tuple.Create("Phone", "string"));
                    cols.Add(Tuple.Create("DateOfBirth", "datetime"));
                    cols.Add(Tuple.Create("RegistrationDate", "datetime"));
                    cols.Add(Tuple.Create("Tier", "string"));
                    cols.Add(Tuple.Create("CreditLimit", "float"));
                    cols.Add(Tuple.Create("IsActive", "bool"));
                    cols.Add(Tuple.Create("Street", "string (Address)"));
                    cols.Add(Tuple.Create("City", "string (Address)"));
                    cols.Add(Tuple.Create("State", "string (Address)"));
                    cols.Add(Tuple.Create("ZipCode", "string (Address)"));
                    cols.Add(Tuple.Create("Country", "string (Address)"));
                    cols.Add(Tuple.Create("Latitude", "float (Address)"));
                    cols.Add(Tuple.Create("Longitude", "float (Address)"));
                    cols.Add(Tuple.Create("FullName", "string (computed)"));
                    cols.Add(Tuple.Create("Age", "int (computed)"));
                    break;
                case "orders":
                    cols.Add(Tuple.Create("Id", "int"));
                    cols.Add(Tuple.Create("CustomerId", "int"));
                    cols.Add(Tuple.Create("OrderDate", "datetime"));
                    cols.Add(Tuple.Create("ShipDate", "datetime"));
                    cols.Add(Tuple.Create("Status", "string"));
                    cols.Add(Tuple.Create("ShipMethod", "string"));
                    cols.Add(Tuple.Create("ShippingCost", "float"));
                    cols.Add(Tuple.Create("PaymentMethod", "string"));
                    cols.Add(Tuple.Create("Subtotal", "float (computed)"));
                    cols.Add(Tuple.Create("Total", "float (computed)"));
                    cols.Add(Tuple.Create("ItemCount", "int (computed)"));
                    break;
                case "order_items":
                    cols.Add(Tuple.Create("OrderId", "int"));
                    cols.Add(Tuple.Create("ProductId", "int"));
                    cols.Add(Tuple.Create("ProductName", "string"));
                    cols.Add(Tuple.Create("Quantity", "int"));
                    cols.Add(Tuple.Create("UnitPrice", "float"));
                    cols.Add(Tuple.Create("Discount", "float"));
                    cols.Add(Tuple.Create("LineTotal", "float (computed)"));
                    break;
                case "employees":
                    cols.Add(Tuple.Create("Id", "int"));
                    cols.Add(Tuple.Create("FirstName", "string"));
                    cols.Add(Tuple.Create("LastName", "string"));
                    cols.Add(Tuple.Create("Department", "string"));
                    cols.Add(Tuple.Create("Title", "string"));
                    cols.Add(Tuple.Create("HireDate", "datetime"));
                    cols.Add(Tuple.Create("Salary", "float"));
                    cols.Add(Tuple.Create("PerformanceScore", "float"));
                    cols.Add(Tuple.Create("ManagerId", "int"));
                    cols.Add(Tuple.Create("IsRemote", "bool"));
                    cols.Add(Tuple.Create("Office", "string"));
                    cols.Add(Tuple.Create("FullName", "string (computed)"));
                    cols.Add(Tuple.Create("YearsEmployed", "int (computed)"));
                    break;
                case "sensor_readings":
                    cols.Add(Tuple.Create("SensorId", "int"));
                    cols.Add(Tuple.Create("SensorType", "string"));
                    cols.Add(Tuple.Create("Location", "string"));
                    cols.Add(Tuple.Create("Timestamp", "datetime"));
                    cols.Add(Tuple.Create("Value", "float"));
                    cols.Add(Tuple.Create("Unit", "string"));
                    cols.Add(Tuple.Create("Status", "string"));
                    cols.Add(Tuple.Create("BatteryLevel", "float"));
                    break;
                case "stock_prices":
                    cols.Add(Tuple.Create("Symbol", "string"));
                    cols.Add(Tuple.Create("CompanyName", "string"));
                    cols.Add(Tuple.Create("Date", "datetime"));
                    cols.Add(Tuple.Create("Open", "float"));
                    cols.Add(Tuple.Create("High", "float"));
                    cols.Add(Tuple.Create("Low", "float"));
                    cols.Add(Tuple.Create("Close", "float"));
                    cols.Add(Tuple.Create("Volume", "int"));
                    cols.Add(Tuple.Create("AdjClose", "float"));
                    break;
                case "web_events":
                    cols.Add(Tuple.Create("SessionId", "string"));
                    cols.Add(Tuple.Create("UserId", "string"));
                    cols.Add(Tuple.Create("Timestamp", "datetime"));
                    cols.Add(Tuple.Create("EventType", "string"));
                    cols.Add(Tuple.Create("Page", "string"));
                    cols.Add(Tuple.Create("Referrer", "string"));
                    cols.Add(Tuple.Create("Browser", "string"));
                    cols.Add(Tuple.Create("Device", "string"));
                    cols.Add(Tuple.Create("Country", "string"));
                    cols.Add(Tuple.Create("Duration", "int"));
                    break;
            }
            return cols;
        }

        private void OnRefTreeSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null || e.Node.Tag == null) return;
            string tag = e.Node.Tag.ToString();

            if (tag.StartsWith("inmemory"))
            {
                ShowInMemoryDetail(tag);
                return;
            }

            ShowDatasetDetail(tag);
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

        private void ShowInMemoryDetail(string tag)
        {
            refDetailBox.Clear();

            if (tag == "inmemory")
            {
                AppendRefText("Custom Data Sources\n\n", Color.FromArgb(0, 0, 180), true, 12);
                AppendRefText("Data registered via RegisterInMemoryData() from .NET.\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("Accessed directly as Python variables.\n\n", Color.FromArgb(60, 60, 60), false, 10);

                foreach (var name in inMemoryDataSources.Keys)
                {
                    bool isBuiltin = name == "products" || name == "customers" || name == "orders" ||
                                     name == "order_items" || name == "employees" || name == "sensor_readings" ||
                                     name == "stock_prices" || name == "web_events";
                    if (!isBuiltin)
                        AppendRefText("  " + name + "\n", Color.FromArgb(128, 0, 128), false, 10);
                }

                AppendRefText("\n", Color.Black, false, 10);
                AppendRefText("Example Python Code\n", Color.FromArgb(0, 100, 0), true, 10);
                AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
                AppendRefText("# Access custom data:\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText("print(dataset_name.head())\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("print(dataset_name.column.mean())\n", Color.FromArgb(60, 60, 60), false, 10);
            }
            else
            {
                string name = tag.Substring("inmemory_".Length);
                AppendRefText(name + "\n\n", Color.FromArgb(0, 0, 180), true, 12);
                AppendRefText("Custom data source registered from .NET.\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("Streamed via stdin (no file I/O).\n\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("Example Python Code\n", Color.FromArgb(0, 100, 0), true, 10);
                AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
                AppendRefText("# Access this data source:\n", Color.FromArgb(0, 128, 0), false, 10);
                AppendRefText("print(" + name + ".head())\n", Color.FromArgb(60, 60, 60), false, 10);
                AppendRefText("print(" + name + ".describe())\n", Color.FromArgb(60, 60, 60), false, 10);
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
            switch (tag)
            {
                case "products": return "Product";
                case "customers": return "Customer";
                case "orders": return "Order";
                case "order_items": return "OrderItem";
                case "employees": return "Employee";
                case "sensor_readings": return "SensorReading";
                case "stock_prices": return "StockPrice";
                case "web_events": return "WebEvent";
                default: return tag;
            }
        }

        private int GetRecordCountForTag(string tag)
        {
            switch (tag)
            {
                case "products": return products.Count;
                case "customers": return customers.Count;
                case "orders": return orders.Count;
                case "order_items": return orders.Sum(o => o.Items != null ? o.Items.Count : 0);
                case "employees": return employees.Count;
                case "sensor_readings": return sensorReadings.Count;
                case "stock_prices": return stockPrices.Count;
                case "web_events": return webEvents.Count;
                default: return 0;
            }
        }

        private string GetExampleCode(string tag)
        {
            switch (tag)
            {
                case "products":
                    return "# Top 10 most expensive products\nprint(products.df.nlargest(10, 'Price')[['Name', 'Price', 'Category']])\n\n# Average price by category\nprint(products.df.groupby('Category')['Price'].mean().sort_values(ascending=False))\n\n# Direct column access:\nprint(products.Price.mean())";
                case "customers":
                    return "# Customer count by tier\nprint(customers.Tier.value_counts())\n\n# Average credit limit by tier\nprint(customers.df.groupby('Tier')['CreditLimit'].mean())";
                case "orders":
                    return "# Orders by status\nprint(orders.Status.value_counts())\n\n# Revenue by payment method\nprint(orders.df.groupby('PaymentMethod')['Total'].sum().sort_values(ascending=False))";
                case "order_items":
                    return "# Best selling products by quantity\nprint(order_items.df.groupby('ProductName')['Quantity'].sum().nlargest(10))\n\n# Average discount by product\nprint(order_items.df.groupby('ProductName')['Discount'].mean().nlargest(10))";
                case "employees":
                    return "# Average salary by department\nprint(employees.df.groupby('Department')['Salary'].mean().sort_values(ascending=False))\n\n# Remote vs office distribution\nprint(employees.IsRemote.value_counts())";
                case "sensor_readings":
                    return "# Average value by sensor type\nprint(sensor_readings.df.groupby('SensorType')['Value'].mean())\n\n# Readings by status\nprint(sensor_readings.Status.value_counts())";
                case "stock_prices":
                    return "import pandas as pd\n\n# Latest closing prices\ndf = stock_prices.df.copy()\ndf['Date'] = pd.to_datetime(df['Date'])\nlatest = df.sort_values('Date').groupby('Symbol').last()\nprint(latest[['CompanyName', 'Close', 'Volume']])";
                case "web_events":
                    return "# Events by type\nprint(web_events.EventType.value_counts())\n\n# Average duration by page\nprint(web_events.df.groupby('Page')['Duration'].mean().sort_values(ascending=False).head(10))";
                default:
                    return "print(" + tag + ".head())\nprint(" + tag + ".describe())";
            }
        }

        private void mainTabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (mainTabs.SelectedIndex == 3 && !packagesLoaded)
            {
                packagesLoaded = true;
                OnRefreshPackages(null, null);
            }
        }

        private void OnDatasetChanged(object sender, EventArgs e)
        {
            dataGrid.DataSource = null;
            switch (datasetCombo.SelectedIndex)
            {
                case 0:
                    dataGrid.DataSource = products.Select(p => new { p.Id, p.Name, p.Category, p.SubCategory, p.SKU, p.Price, p.Cost, p.Margin, p.StockQuantity, p.Supplier, p.Rating, p.ReviewCount, p.IsDiscontinued }).ToList();
                    recordCountLabel.Text = products.Count + " records";
                    break;
                case 1:
                    dataGrid.DataSource = customers.Select(c => new { c.Id, c.FullName, c.Email, c.Phone, Age = c.Age, c.Tier, c.CreditLimit, c.IsActive, City = c.Address.City, State = c.Address.State, OrderCount = c.Orders.Count }).ToList();
                    recordCountLabel.Text = customers.Count + " records";
                    break;
                case 2:
                    dataGrid.DataSource = orders.Select(o => new { o.Id, o.CustomerId, OrderDate = o.OrderDate.ToString("yyyy-MM-dd"), o.Status, o.ShipMethod, o.ShippingCost, o.PaymentMethod, Subtotal = Math.Round(o.Subtotal, 2), Total = Math.Round(o.Total, 2), o.ItemCount }).ToList();
                    recordCountLabel.Text = orders.Count + " records";
                    break;
                case 3:
                    dataGrid.DataSource = employees.Select(em => new { em.Id, em.FullName, em.Department, em.Title, HireDate = em.HireDate.ToString("yyyy-MM-dd"), em.Salary, em.PerformanceScore, em.IsRemote, em.Office, em.YearsEmployed }).ToList();
                    recordCountLabel.Text = employees.Count + " records";
                    break;
                case 4:
                    dataGrid.DataSource = sensorReadings.Take(500).Select(sr => new { sr.SensorId, sr.SensorType, sr.Location, Timestamp = sr.Timestamp.ToString("yyyy-MM-dd HH:mm"), sr.Value, sr.Unit, sr.Status, sr.BatteryLevel }).ToList();
                    recordCountLabel.Text = sensorReadings.Count + " records (showing 500)";
                    break;
                case 5:
                    dataGrid.DataSource = stockPrices.Take(500).Select(sp => new { sp.Symbol, sp.CompanyName, Date = sp.Date.ToString("yyyy-MM-dd"), sp.Open, sp.High, sp.Low, sp.Close, sp.Volume }).ToList();
                    recordCountLabel.Text = stockPrices.Count + " records (showing 500)";
                    break;
                case 6:
                    dataGrid.DataSource = webEvents.Take(500).Select(we => new { we.SessionId, we.UserId, Timestamp = we.Timestamp.ToString("yyyy-MM-dd HH:mm"), we.EventType, we.Page, we.Referrer, we.Browser, we.Device, we.Country, we.Duration }).ToList();
                    recordCountLabel.Text = webEvents.Count + " records (showing 500)";
                    break;
            }
        }

        private void OnRunScript(object sender, EventArgs e)
        {
            string script = pythonEditor.Text;
            if (string.IsNullOrWhiteSpace(script))
            {
                AppendOutput("No script to run.\n", Color.Yellow);
                return;
            }

            RaiseStatus("Running script...");
            AppendOutput("--- Running script at " + DateTime.Now.ToString("HH:mm:ss") + " ---\n", Color.FromArgb(0, 100, 180));

            Application.DoEvents();

            Dictionary<string, string> memData = null;
            if (inMemoryDataSources.Count > 0)
                memData = SerializeInMemoryData();

            var result = pythonRunner.Execute(script, memData);

            if (!string.IsNullOrEmpty(result.Output))
                AppendOutput(result.Output, Color.FromArgb(0, 0, 0));

            if (!string.IsNullOrEmpty(result.Error))
            {
                if (result.Success)
                    AppendOutput(result.Error, Color.FromArgb(140, 120, 0));
                else
                    AppendOutput("ERROR:\n" + result.Error, Color.FromArgb(200, 0, 0));
            }

            AppendOutput("--- Finished (exit code: " + result.ExitCode + ") ---\n\n", Color.FromArgb(0, 100, 180));
            RaiseStatus(result.Success ? "Script completed successfully." : "Script failed with errors.");
        }

        private void OnClearOutput(object sender, EventArgs e)
        {
            outputBox.Clear();
        }

        private void OnCheckSyntax(object sender, EventArgs e)
        {
            string script = pythonEditor.Text;
            if (string.IsNullOrWhiteSpace(script))
            {
                AppendOutput("No script to check.\n", Color.Yellow);
                return;
            }

            RaiseStatus("Checking syntax...");
            var result = pythonRunner.CheckSyntax(script);

            if (result.Success)
            {
                pythonEditor.ClearError();
                AppendOutput("Syntax OK - no errors found.\n", Color.LightGreen);
                RaiseStatus("Syntax check passed.");
            }
            else
            {
                AppendOutput("Syntax Error:\n" + result.Error + "\n", Color.FromArgb(255, 100, 100));

                int errorLine = ParseErrorLine(result.Error);
                if (errorLine > 0)
                {
                    var errorMsg = result.Error.Trim();
                    var firstLine = errorMsg.Split('\n')[0];
                    pythonEditor.SetErrorLine(errorLine, firstLine);
                }
                RaiseStatus("Syntax error on line " + errorLine);
            }
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


        private Form findReplaceForm;

        private void ShowFindReplace()
        {
            if (findReplaceForm != null && !findReplaceForm.IsDisposed)
            {
                findReplaceForm.Activate();
                return;
            }

            findReplaceForm = new Form
            {
                Text = "Find & Replace",
                Size = new Size(420, 200),
                MinimumSize = new Size(400, 200),
                MaximizeBox = false,
                FormBorderStyle = FormBorderStyle.SizableToolWindow,
                StartPosition = FormStartPosition.CenterParent,
                TopMost = true
            };

            var findLabel = new Label { Text = "Find:", Location = new Point(12, 18), Size = new Size(60, 20) };
            var findBox = new TextBox { Location = new Point(80, 15), Size = new Size(220, 22), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            var replaceLabel = new Label { Text = "Replace:", Location = new Point(12, 48), Size = new Size(60, 20) };
            var replaceBox = new TextBox { Location = new Point(80, 45), Size = new Size(220, 22), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            var matchCaseCheck = new CheckBox { Text = "Match case", Location = new Point(80, 75), Size = new Size(120, 20) };

            var findNextBtn = new Button { Text = "Find Next", Location = new Point(310, 14), Size = new Size(85, 25), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            var replaceBtn = new Button { Text = "Replace", Location = new Point(310, 44), Size = new Size(85, 25), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            var replaceAllBtn = new Button { Text = "Replace All", Location = new Point(310, 74), Size = new Size(85, 25), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            var closeBtn = new Button { Text = "Close", Location = new Point(310, 114), Size = new Size(85, 25), Anchor = AnchorStyles.Top | AnchorStyles.Right };

            int searchStart = 0;

            findNextBtn.Click += (s, ev) =>
            {
                string find = findBox.Text;
                if (string.IsNullOrEmpty(find)) return;

                var comparison = matchCaseCheck.Checked ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                int idx = pythonEditor.Text.IndexOf(find, searchStart, comparison);
                if (idx < 0)
                {
                    idx = pythonEditor.Text.IndexOf(find, 0, comparison);
                    if (idx < 0)
                    {
                        RaiseStatus("Not found: " + find);
                        return;
                    }
                }
                pythonEditor.Select(idx, find.Length);
                pythonEditor.ScrollToCaret();
                pythonEditor.Focus();
                searchStart = idx + find.Length;
                RaiseStatus("Found at position " + idx);
            };

            replaceBtn.Click += (s, ev) =>
            {
                if (pythonEditor.SelectionLength > 0)
                {
                    pythonEditor.SelectedText = replaceBox.Text;
                }
                findNextBtn.PerformClick();
            };

            replaceAllBtn.Click += (s, ev) =>
            {
                string find = findBox.Text;
                string replace = replaceBox.Text;
                if (string.IsNullOrEmpty(find)) return;

                var comparison = matchCaseCheck.Checked ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                int count = 0;
                int idx = 0;
                string text = pythonEditor.Text;
                var sb = new System.Text.StringBuilder();
                while (true)
                {
                    int found = text.IndexOf(find, idx, comparison);
                    if (found < 0)
                    {
                        sb.Append(text.Substring(idx));
                        break;
                    }
                    sb.Append(text.Substring(idx, found - idx));
                    sb.Append(replace);
                    idx = found + find.Length;
                    count++;
                }
                if (count > 0)
                {
                    pythonEditor.Text = sb.ToString();
                }
                RaiseStatus("Replaced " + count + " occurrence(s).");
            };

            closeBtn.Click += (s, ev) => findReplaceForm.Close();

            findBox.KeyDown += (s, ev) =>
            {
                if (ev.KeyCode == Keys.Enter) { findNextBtn.PerformClick(); ev.Handled = true; ev.SuppressKeyPress = true; }
                if (ev.KeyCode == Keys.Escape) { findReplaceForm.Close(); ev.Handled = true; }
            };

            replaceBox.KeyDown += (s, ev) =>
            {
                if (ev.KeyCode == Keys.Enter) { replaceBtn.PerformClick(); ev.Handled = true; ev.SuppressKeyPress = true; }
                if (ev.KeyCode == Keys.Escape) { findReplaceForm.Close(); ev.Handled = true; }
            };

            findReplaceForm.Controls.AddRange(new Control[] { findLabel, findBox, replaceLabel, replaceBox, matchCaseCheck, findNextBtn, replaceBtn, replaceAllBtn, closeBtn });

            if (pythonEditor.SelectionLength > 0)
                findBox.Text = pythonEditor.SelectedText;

            findReplaceForm.Show(this.FindForm());
            findBox.Focus();
        }

        private void OnOpenScript(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog { Filter = "Python files (*.py)|*.py|All files (*.*)|*.*" })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    pythonEditor.Text = File.ReadAllText(dlg.FileName);
                    ResetUndoStack();
                    RaiseStatus("Loaded: " + dlg.FileName);
                }
            }
        }

        private void OnSaveScript(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog { Filter = "Python files (*.py)|*.py|All files (*.*)|*.*", DefaultExt = "py" })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(dlg.FileName, pythonEditor.Text);
                    RaiseStatus("Saved: " + dlg.FileName);
                }
            }
        }

        private void OnInstallPackage(object sender, EventArgs e)
        {
            string pkg = packageNameBox.Text.Trim();
            if (string.IsNullOrEmpty(pkg)) return;

            RaiseStatus("Installing " + pkg + "...");
            Application.DoEvents();

            var result = pythonRunner.InstallPackage(pkg);
            if (result.Success)
            {
                AppendOutput("Successfully installed: " + pkg + "\n", Color.LightGreen);
                RaiseStatus(pkg + " installed successfully.");
            }
            else
            {
                AppendOutput("Failed to install " + pkg + ":\n" + result.Error + "\n", Color.FromArgb(255, 100, 100));
                RaiseStatus("Installation failed for " + pkg);
            }

            mainTabs.SelectedIndex = 0;
            OnRefreshPackages(null, null);
        }

        private void OnUninstallPackage(object sender, EventArgs e)
        {
            string pkg = packageNameBox.Text.Trim();
            if (string.IsNullOrEmpty(pkg)) return;

            var confirm = MessageBox.Show("Uninstall " + pkg + "?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            RaiseStatus("Uninstalling " + pkg + "...");
            Application.DoEvents();

            var result = pythonRunner.UninstallPackage(pkg);
            if (result.Success)
            {
                AppendOutput("Successfully uninstalled: " + pkg + "\n", Color.LightGreen);
                RaiseStatus(pkg + " uninstalled.");
            }
            else
            {
                AppendOutput("Failed to uninstall " + pkg + ":\n" + result.Error + "\n", Color.FromArgb(255, 100, 100));
                RaiseStatus("Uninstall failed for " + pkg);
            }
            OnRefreshPackages(null, null);
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
            packageListBox.Items.Clear();
            string list = pythonRunner.ListPackages();
            if (!string.IsNullOrEmpty(list))
            {
                foreach (var line in list.Split('\n'))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        packageListBox.Items.Add(line.Trim());
                }
            }
        }

        private void OnShowHelp(object sender, EventArgs e)
        {
            string help = @"=== Data Science Workbench - Quick Start ===

AVAILABLE DATASETS (pre-loaded as variables):
  products         - 200 products with prices, ratings, stock
  customers        - 150 customers with demographics
  orders           - 500 orders with status, totals
  order_items      - Individual line items for orders
  employees        - 100 employees with salary, dept
  sensor_readings  - 1000 IoT sensor readings
  stock_prices     - 365 days of 10 stock symbols
  web_events       - 2000 web analytics events

HOW TO USE:
  1. Write Python code in the editor
  2. Press F5 or click Run to execute
  3. All datasets are pre-loaded as variables
  4. Access columns directly: products.Price.mean()
  5. Use .df for full DataFrame: products.df.describe()
  6. Install packages via Package Manager tab

EXAMPLE:
  print(products.Price.mean())
  print(products.df.groupby('Category')['Price'].mean())

TIPS:
  - Use 'Insert Snippet' for ready-made code
  - Matplotlib plots save as PNG files
  - All standard Python libraries available
  - Install any pip package via Package Manager
  - .NET controls can push data via RegisterInMemoryData()";

            MessageBox.Show(help, "Quick Start Guide", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void AppendOutput(string text, Color color)
        {
            outputBox.SelectionStart = outputBox.TextLength;
            outputBox.SelectionLength = 0;
            outputBox.SelectionColor = color;
            outputBox.AppendText(text);
            outputBox.ScrollToCaret();
        }

        private void RaiseStatus(string msg)
        {
            StatusChanged?.Invoke(this, msg);
        }

        private void InsertSnippet(string code)
        {
            int pos = pythonEditor.SelectionStart;
            suppressHighlight = true;
            pythonEditor.SelectionStart = pos;
            pythonEditor.SelectionLength = 0;
            pythonEditor.SelectedText = code;
            suppressHighlight = false;
            pythonEditor.SelectionStart = pos + code.Length;
            pythonEditor.Focus();
            ApplySyntaxHighlighting();
        }

        private string GetDefaultScript()
        {
            return @"# All datasets are pre-loaded as variables
# Access columns directly: products.Cost.mean()
# Use .df for full DataFrame: products.df.describe()

print('=== Data Science Workbench ===')
print()

# Quick look at products
print(f'Products: {len(products)} records')
print(f'Average price: ${products.Price.mean():.2f}')
print(f'Average cost:  ${products.Cost.mean():.2f}')
print()
print('=== Product Summary ===')
print(products.df.describe())
";
        }

        private string GetLoadDataSnippet()
        {
            return @"
# All datasets are pre-loaded as variables
for name, ds in [('products', products), ('customers', customers),
                  ('orders', orders), ('order_items', order_items),
                  ('employees', employees), ('sensor_readings', sensor_readings),
                  ('stock_prices', stock_prices), ('web_events', web_events)]:
    print(f'  {name}: {len(ds)} rows, {len(ds.df.columns)} columns')
";
        }

        private string GetStatsSnippet()
        {
            return @"
print('=== Descriptive Statistics ===')
print(products.df.describe())
print()
print('=== Data Types ===')
print(products.df.dtypes)
print()
print('=== Missing Values ===')
print(products.df.isnull().sum())
";
        }

        private string GetHistogramSnippet()
        {
            return @"
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt

fig, ax = plt.subplots(figsize=(10, 6))
ax.hist(products.Price, bins=30, edgecolor='black', alpha=0.7)
ax.set_xlabel('Price ($)')
ax.set_ylabel('Count')
ax.set_title('Product Price Distribution')
plt.tight_layout()
plt.savefig('histogram.png', dpi=100)
print('Histogram saved to histogram.png')
";
        }

        private string GetScatterSnippet()
        {
            return @"
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt

fig, ax = plt.subplots(figsize=(10, 6))
scatter = ax.scatter(products.Price, products.Rating, c=products.ReviewCount,
                     cmap='viridis', alpha=0.6, edgecolors='black', linewidth=0.5)
ax.set_xlabel('Price ($)')
ax.set_ylabel('Rating')
ax.set_title('Price vs Rating (colored by Review Count)')
plt.colorbar(scatter, label='Review Count')
plt.tight_layout()
plt.savefig('scatter.png', dpi=100)
print('Scatter plot saved to scatter.png')
";
        }

        private string GetGroupBySnippet()
        {
            return @"
print('=== Average Price by Category ===')
group = products.df.groupby('Category').agg(
    Count=('Id', 'count'),
    Avg_Price=('Price', 'mean'),
    Avg_Rating=('Rating', 'mean'),
    Total_Stock=('StockQuantity', 'sum')
).round(2)
print(group.sort_values('Avg_Price', ascending=False))
";
        }

        private string GetCorrelationSnippet()
        {
            return @"
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt

numeric_cols = products.df.select_dtypes(include='number')
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
ax.set_title('Correlation Matrix')
plt.tight_layout()
plt.savefig('correlation.png', dpi=100)
print('Correlation matrix saved to correlation.png')
";
        }

        private string GetTimeSeriesSnippet()
        {
            return @"
import pandas as pd
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt

df = stock_prices.df.copy()
df['Date'] = pd.to_datetime(df['Date'])

fig, ax = plt.subplots(figsize=(12, 6))
for symbol in df['Symbol'].unique()[:5]:
    subset = df[df['Symbol'] == symbol].sort_values('Date')
    ax.plot(subset['Date'], subset['Close'], label=symbol, linewidth=1)
ax.set_xlabel('Date')
ax.set_ylabel('Close Price ($)')
ax.set_title('Stock Prices Over Time')
ax.legend()
plt.xticks(rotation=45)
plt.tight_layout()
plt.savefig('timeseries.png', dpi=100)
print('Time series chart saved to timeseries.png')
";
        }

        private string GetCustomDataSnippet()
        {
            return @"
# All datasets are pre-loaded as top-level Python variables
# Access columns directly as attributes:
#   products.Cost.mean()
#   products.Price.tolist()
# Use .df to get the raw pandas DataFrame:
#   products.df.describe()

print('=== .NET In-Memory Data: Products ===')
print(f'Count: {len(products)}')
print()
print('Price Statistics:')
print(f'  Sum:    {products.Price.sum():.2f}')
print(f'  Mean:   {products.Price.mean():.2f}')
print(f'  Median: {products.Price.median():.2f}')
print(f'  Std:    {products.Price.std():.2f}')
print(f'  Min:    {products.Price.min():.2f}')
print(f'  Max:    {products.Price.max():.2f}')
";
        }
    }
}
