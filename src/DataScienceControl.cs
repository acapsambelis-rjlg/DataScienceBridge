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
        private List<Customer> customers;
        private List<Employee> employees;

        private bool packagesLoaded;
        private List<string> allPackageItems = new List<string>();
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
        private Dictionary<string, Type> inMemoryDataTypes = new Dictionary<string, Type>();
        private Dictionary<string, PythonClassInfo> registeredPythonClasses = new Dictionary<string, PythonClassInfo>();
        private Dictionary<string, ContextVariable> contextVariables = new Dictionary<string, ContextVariable>();
        private float editorFontSize = 10f;
        private const float MinFontSize = 6f;
        private const float MaxFontSize = 28f;
        private const float DefaultFontSize = 10f;
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
            var uiFontBold = ResolveUIFont(9f, FontStyle.Bold);

            pythonEditor.Font = monoFont10;
            outputBox.Font = monoFont9;
            packageListBox.Font = monoFont9;
            lineNumberPanel.UpdateFont(monoFont9);

            outputLabel.Font = uiFontBold;
            pkgListLabel.Font = uiFontBold;
        }

        public DataScienceControl()
        {
            InitializeComponent();
            ResolveRuntimeFonts();
            InitializeData();
            SetupEditorMenuBar();
            SetupSnippetMenu();
            SetupSyntaxHighlighting();
            RegisterAllDatasetsInMemory();
            PopulateReferenceTree();
            SetupRefSearch();
            SetupPkgSearch();
            suppressHighlight = true;
            pythonEditor.Text = GetDefaultScript();
            suppressHighlight = false;
            ResetUndoStack();
            ApplySyntaxHighlighting();
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
            UpdateDynamicSymbols();

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

                    pythonEditor.UpdateWordHighlight();
                }
            };

            pythonEditor.SelectionChanged += (s, e) =>
            {
                if (!suppressHighlight)
                {
                    pythonEditor.UpdateBracketMatching();
                    pythonEditor.UpdateWordHighlight();
                    pythonEditor.Invalidate();
                    UpdateCursorPositionStatus();
                    autoComplete.OnSelectionChanged();
                }
            };

            pythonEditor.MouseWheel += (s, e) =>
            {
                if (Control.ModifierKeys.HasFlag(Keys.Control))
                {
                    ((HandledMouseEventArgs)e).Handled = true;
                    if (e.Delta > 0) ZoomEditor(1f);
                    else if (e.Delta < 0) ZoomEditor(-1f);
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
                else if (e.KeyCode == Keys.Escape)
                {
                    if (autoComplete.IsShowing)
                        autoComplete.Hide();
                    if (findReplacePanel != null && findReplacePanel.Visible)
                        HideFindReplace();
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
                else if (e.Control && (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add))
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    ZoomEditor(1f);
                }
                else if (e.Control && (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract))
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    ZoomEditor(-1f);
                }
                else if (e.Control && e.KeyCode == Keys.D0)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    ZoomEditor(0f);
                }
                else if (e.Control && e.KeyCode == Keys.F)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    ShowFindReplace();
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

            int savedPos = pythonEditor.SelectionStart;
            int savedLen = pythonEditor.SelectionLength;

            var newFont = ResolveMonoFont(editorFontSize);
            pythonEditor.Font = newFont;
            lineNumberPanel.UpdateFont(ResolveMonoFont(Math.Max(editorFontSize - 1f, MinFontSize)));

            pythonEditor.SelectionStart = savedPos;
            pythonEditor.SelectionLength = savedLen;
            pythonEditor.ScrollToCaret();

            UpdateCursorPositionStatus();
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
            if (!pythonRunner.PythonAvailable) return;

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
                return sb.ToString();
            };
            PopulateReferenceTree();
        }

        public void RegisterInMemoryData<T>(string name, Func<List<T>> dataProvider) where T : class
        {
            inMemoryDataTypes[name] = typeof(T);
            inMemoryDataSources[name] = () =>
            {
                var data = dataProvider();
                var visibleProps = UserVisibleHelper.GetVisibleProperties(typeof(T));
                var sb = new System.Text.StringBuilder();

                var headerParts = new List<string>();
                foreach (var p in visibleProps)
                    headerParts.Add(p.Name);
                sb.AppendLine(string.Join(",", headerParts));

                foreach (var item in data)
                {
                    var vals = new List<string>();
                    foreach (var p in visibleProps)
                    {
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
                return sb.ToString();
            };
            PopulateReferenceTree();
        }

        public void UnregisterInMemoryData(string name)
        {
            inMemoryDataSources.Remove(name);
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

            sb.AppendLine("import os as _os, tempfile as _tempfile, atexit as _atexit");
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

        private void UpdateDynamicSymbols()
        {
            var names = new List<string>();
            names.AddRange(inMemoryDataTypes.Keys);
            names.AddRange(registeredPythonClasses.Keys);
            names.AddRange(contextVariables.Keys);
            symbolAnalyzer.SetDynamicKnownSymbols(names);
            if (autoComplete != null)
            {
                autoComplete.SetDynamicSymbols(names);

                var colMap = new Dictionary<string, List<string>>();
                foreach (var kvp in inMemoryDataTypes)
                {
                    var visibleProps = UserVisibleHelper.GetVisibleProperties(kvp.Value);
                    var colNames = new List<string>();
                    foreach (var p in visibleProps)
                        colNames.Add(p.Name);
                    colMap[kvp.Key] = colNames;
                }
                autoComplete.SetDatasetColumns(colMap);
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
            AppendOutput(message + "\n", Color.FromArgb(100, 100, 100));
            RaiseStatus(message);
            Application.DoEvents();
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

        private void SetupEditorMenuBar()
        {
            var fileMenu = new ToolStripMenuItem("File");
            fileMenu.DropDownItems.Add("Open Script...", null, OnOpenScript);
            fileMenu.DropDownItems.Add("Save Script...", null, OnSaveScript);

            var editMenu = new ToolStripMenuItem("&Edit");

            var undoItem = new ToolStripMenuItem("Undo", null, (s, e) => { if (pythonEditor.Focused) PerformUndo(); });
            undoItem.ShortcutKeyDisplayString = "Ctrl+Z";
            editMenu.DropDownItems.Add(undoItem);

            var redoItem = new ToolStripMenuItem("Redo", null, (s, e) => { if (pythonEditor.Focused) PerformRedo(); });
            redoItem.ShortcutKeyDisplayString = "Ctrl+Y";
            editMenu.DropDownItems.Add(redoItem);

            editMenu.DropDownItems.Add(new ToolStripSeparator());

            var cutItem = new ToolStripMenuItem("Cut", null, (s, e) => { if (pythonEditor.Focused) pythonEditor.Cut(); });
            cutItem.ShortcutKeyDisplayString = "Ctrl+X";
            editMenu.DropDownItems.Add(cutItem);

            var copyItem = new ToolStripMenuItem("Copy", null, (s, e) => { if (pythonEditor.Focused) pythonEditor.Copy(); });
            copyItem.ShortcutKeyDisplayString = "Ctrl+C";
            editMenu.DropDownItems.Add(copyItem);

            var pasteItem = new ToolStripMenuItem("Paste", null, (s, e) => { if (pythonEditor.Focused) pythonEditor.Paste(); });
            pasteItem.ShortcutKeyDisplayString = "Ctrl+V";
            editMenu.DropDownItems.Add(pasteItem);

            var deleteItem = new ToolStripMenuItem("Delete", null, (s, e) => { if (pythonEditor.Focused && pythonEditor.SelectionLength > 0) pythonEditor.SelectedText = ""; });
            deleteItem.ShortcutKeyDisplayString = "Del";
            editMenu.DropDownItems.Add(deleteItem);

            editMenu.DropDownItems.Add(new ToolStripSeparator());

            var selectAllItem = new ToolStripMenuItem("Select All", null, (s, e) => { if (pythonEditor.Focused) pythonEditor.SelectAll(); });
            selectAllItem.ShortcutKeyDisplayString = "Ctrl+A";
            editMenu.DropDownItems.Add(selectAllItem);

            editMenu.DropDownItems.Add(new ToolStripSeparator());

            var findItem = new ToolStripMenuItem("Find && Replace...", null, (s, e) => ShowFindReplace());
            findItem.ShortcutKeyDisplayString = "Ctrl+H";
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
            runMenu.DropDownItems.Add("Check Syntax", null, OnCheckSyntax);
            runMenu.DropDownItems.Add(new ToolStripSeparator());
            runMenu.DropDownItems.Add("Reset Python Environment", null, (s, e) =>
            {
                var confirm = MessageBox.Show(
                    "This will delete the virtual environment and recreate it.\nAll installed packages will need to be reinstalled.\n\nContinue?",
                    "Reset Python Environment", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm == DialogResult.Yes)
                    ResetPythonEnvironment();
            });

            var helpMenu = new ToolStripMenuItem("Help");
            helpMenu.DropDownItems.Add("Quick Start Guide", null, OnShowHelp);
            helpMenu.DropDownItems.Add("About", null, (s, e) => MessageBox.Show(
                "Data Science Workbench v1.0\n\n" +
                "A .NET Windows Forms control with\n" +
                "integrated Python scripting for data analysis.\n\n" +
                "Built with Mono + Python 3",
                "About", MessageBoxButtons.OK, MessageBoxIcon.Information));

            editorMenuBar.Items.Insert(0, fileMenu);
            editorMenuBar.Items.Insert(1, editMenu);
            editorMenuBar.Items.Insert(2, runMenu);
            editorMenuBar.Items.Add(helpMenu);
        }

        public MenuStrip CreateMenuStrip()
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
                pythonRunner.SetupProgress += OnPythonSetupProgress;
                pythonRunner.EnsureVenv();
                pythonRunner.SetupProgress -= OnPythonSetupProgress;

                if (pythonRunner.VenvReady)
                    RaiseStatus("Ready (" + pythonRunner.PythonVersion + ", venv)");
                else
                {
                    AppendOutput("Virtual environment setup failed: " + pythonRunner.VenvError + "\n", Color.FromArgb(200, 120, 0));
                    AppendOutput("Using system Python instead.\n\n", Color.FromArgb(140, 100, 0));
                    RaiseStatus("Ready (" + pythonRunner.PythonVersion + ", system)");
                }
            }
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

            foreach (var kvp in inMemoryDataTypes)
            {
                string name = kvp.Key;
                Type type = kvp.Value;
                int count = GetRecordCountForTag(name);
                var columns = GetColumnsForDataset(name);

                bool datasetMatches = name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
                var matchedColNames = new HashSet<string>(StringComparer.Ordinal);
                var matchingCols = new List<Tuple<string, string>>();

                foreach (var col in columns)
                {
                    if (col.Item1.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        col.Item2.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        matchingCols.Add(col);
                        matchedColNames.Add(col.Item1);
                    }
                }

                var visibleProps = UserVisibleHelper.GetVisibleProperties(type);
                foreach (var p in visibleProps)
                {
                    if (matchedColNames.Contains(p.Name)) continue;
                    var attr = p.GetCustomAttributes(typeof(UserVisibleAttribute), true);
                    if (attr.Length > 0)
                    {
                        string desc = ((UserVisibleAttribute)attr[0]).Description;
                        if (!string.IsNullOrEmpty(desc) && desc.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            var colEntry = columns.Find(c => c.Item1 == p.Name);
                            if (colEntry != null)
                            {
                                matchingCols.Add(colEntry);
                                matchedColNames.Add(p.Name);
                            }
                        }
                    }
                }

                if (datasetMatches || matchingCols.Count > 0)
                {
                    var node = refTreeView.Nodes.Add(name + "  (" + count + ")");
                    node.Tag = name;
                    node.NodeFont = new Font(refTreeView.Font, FontStyle.Bold);

                    var colsToShow = datasetMatches ? columns : matchingCols;
                    foreach (var col in colsToShow)
                    {
                        var child = node.Nodes.Add(col.Item1 + "  :  " + col.Item2);
                        child.Tag = new string[] { "field", name, col.Item1 };
                        child.ForeColor = Color.FromArgb(80, 80, 80);
                    }
                    node.Expand();
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

            refTreeView.EndUpdate();
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

                var columns = GetColumnsForDataset(name);
                foreach (var col in columns)
                {
                    var child = node.Nodes.Add(col.Item1 + "  :  " + col.Item2);
                    child.Tag = new string[] { "field", name, col.Item1 };
                    child.ForeColor = Color.FromArgb(80, 80, 80);
                }
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

            if (refTreeView.Nodes.Count > 0)
                refTreeView.SelectedNode = refTreeView.Nodes[0];
        }

        private List<Tuple<string, string>> GetColumnsForDataset(string tag)
        {
            var cols = new List<Tuple<string, string>>();
            Type type;
            if (!inMemoryDataTypes.TryGetValue(tag, out type))
                return cols;

            var visibleProps = UserVisibleHelper.GetVisibleProperties(type);
            foreach (var p in visibleProps)
            {
                string typeName = UserVisibleHelper.GetPythonTypeName(p.PropertyType);
                bool isComputed = p.GetSetMethod() == null;
                if (isComputed)
                    typeName += " (computed)";
                cols.Add(Tuple.Create(p.Name, typeName));
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

            ShowDatasetDetail(tag);
        }

        private void ShowFieldDetail(string datasetName, string fieldName)
        {
            Type type;
            if (!inMemoryDataTypes.TryGetValue(datasetName, out type)) return;

            var prop = type.GetProperty(fieldName);
            if (prop == null) return;

            string typeName = UserVisibleHelper.GetPythonTypeName(prop.PropertyType);
            bool isComputed = prop.GetSetMethod() == null;

            refDetailBox.Clear();

            AppendRefText(fieldName, Color.FromArgb(0, 0, 180), true, 12);
            AppendRefText("  :  " + typeName + (isComputed ? " (computed)" : "") + "\n\n", Color.FromArgb(100, 100, 100), false, 12);

            AppendRefText("Description\n", Color.FromArgb(0, 100, 0), true, 10);
            AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
            var uvAttrs = prop.GetCustomAttributes(typeof(UserVisibleAttribute), true);
            string desc = null;
            if (uvAttrs.Length > 0)
                desc = ((UserVisibleAttribute)uvAttrs[0]).Description;
            if (!string.IsNullOrEmpty(desc))
            {
                AppendRefText(desc + "\n\n", Color.FromArgb(60, 60, 60), false, 10);
            }
            else
            {
                AppendRefText("No description available.\n\n", Color.FromArgb(150, 150, 150), false, 10);
            }

            AppendRefText("Dataset\n", Color.FromArgb(0, 100, 0), true, 10);
            AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);
            string className = GetClassNameForTag(datasetName);
            AppendRefText("  " + className + "." + fieldName + "  (variable: " + datasetName + ")\n\n", Color.FromArgb(60, 60, 60), false, 10);

            AppendRefText("Example Python Code\n", Color.FromArgb(0, 100, 0), true, 10);
            AppendRefText(new string('\u2500', 50) + "\n", Color.FromArgb(200, 200, 200), false, 10);

            string customExample = null;
            if (uvAttrs.Length > 0)
                customExample = ((UserVisibleAttribute)uvAttrs[0]).Example;

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
                    return "# Customer count by tier\nprint(customers.Tier.value_counts())\n\n# Average credit limit by tier\nprint(customers.df.groupby('Tier')['CreditLimit'].mean())";
                case "employees":
                    return "# Average salary by department\nprint(employees.df.groupby('Department')['Salary'].mean().sort_values(ascending=False))\n\n# Remote vs office distribution\nprint(employees.IsRemote.value_counts())";
                default:
                    return "print(" + tag + ".head())\nprint(" + tag + ".describe())";
            }
        }

        private void mainTabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (mainTabs.SelectedIndex == 2 && !packagesLoaded)
            {
                packagesLoaded = true;
                OnRefreshPackages(null, null);
            }
        }

        private void OnRunScript(object sender, EventArgs e)
        {
            string script = pythonEditor.Text;
            if (string.IsNullOrWhiteSpace(script))
            {
                AppendOutput("No script to run.\n", Color.FromArgb(180, 140, 0));
                return;
            }

            if (!pythonRunner.PythonAvailable)
            {
                AppendOutput("Cannot run script: " + pythonRunner.PythonError + "\n", Color.FromArgb(200, 0, 0));
                RaiseStatus("Python not available");
                return;
            }

            RaiseStatus("Running script...");
            AppendOutput("--- Running script at " + DateTime.Now.ToString("HH:mm:ss") + " ---\n", Color.FromArgb(0, 100, 180));

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

        private void OnClearOutput(object sender, EventArgs e)
        {
            outputBox.Clear();
        }

        private void OnCheckSyntax(object sender, EventArgs e)
        {
            string script = pythonEditor.Text;
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
                pythonEditor.ClearError();
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


        private Panel findReplacePanel;
        private TextBox frFindBox;
        private TextBox frReplaceBox;
        private CheckBox frMatchCase;
        private int frSearchStart;

        private void ShowFindReplace()
        {
            if (findReplacePanel != null && findReplacePanel.Visible)
            {
                frFindBox.Focus();
                frFindBox.SelectAll();
                return;
            }

            if (findReplacePanel == null)
            {
                findReplacePanel = new Panel
                {
                    Size = new Size(370, 90),
                    BackColor = Color.FromArgb(245, 245, 245),
                    BorderStyle = BorderStyle.FixedSingle,
                    Visible = false
                };

                var findLabel = new Label { Text = "Find:", Location = new Point(6, 8), Size = new Size(38, 18), Font = new Font("Segoe UI", 8.5F) };
                frFindBox = new TextBox { Location = new Point(54, 5), Size = new Size(180, 22), Font = new Font("Segoe UI", 9F) };
                var replaceLabel = new Label { Text = "Replace:", Location = new Point(6, 34), Size = new Size(48, 18), Font = new Font("Segoe UI", 8.5F) };
                frReplaceBox = new TextBox { Location = new Point(54, 31), Size = new Size(180, 22), Font = new Font("Segoe UI", 9F) };
                frMatchCase = new CheckBox { Text = "Match case", Location = new Point(54, 57), Size = new Size(100, 20), Font = new Font("Segoe UI", 8F) };

                var findNextBtn = new Button { Text = "Next", Location = new Point(240, 4), Size = new Size(55, 24), Font = new Font("Segoe UI", 8F), FlatStyle = FlatStyle.Flat };
                var replaceBtn = new Button { Text = "Replace", Location = new Point(240, 30), Size = new Size(55, 24), Font = new Font("Segoe UI", 8F), FlatStyle = FlatStyle.Flat };
                var replaceAllBtn = new Button { Text = "All", Location = new Point(298, 30), Size = new Size(38, 24), Font = new Font("Segoe UI", 8F), FlatStyle = FlatStyle.Flat };
                var closeBtn = new Button { Text = "\u2715", Location = new Point(340, 4), Size = new Size(24, 24), Font = new Font("Segoe UI", 9F), FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(100, 100, 100) };
                closeBtn.FlatAppearance.BorderSize = 0;

                findNextBtn.Click += (s, ev) => FindNext();
                replaceBtn.Click += (s, ev) => ReplaceNext();
                replaceAllBtn.Click += (s, ev) => ReplaceAll();
                closeBtn.Click += (s, ev) => HideFindReplace();

                Action<object, KeyEventArgs> handleKeys = (s, ev) =>
                {
                    if (ev.KeyCode == Keys.Enter) { FindNext(); ev.Handled = true; ev.SuppressKeyPress = true; }
                    if (ev.KeyCode == Keys.Escape) { HideFindReplace(); ev.Handled = true; ev.SuppressKeyPress = true; }
                };
                frFindBox.KeyDown += new KeyEventHandler(handleKeys);
                frReplaceBox.KeyDown += (s, ev) =>
                {
                    if (ev.KeyCode == Keys.Enter) { ReplaceNext(); ev.Handled = true; ev.SuppressKeyPress = true; }
                    if (ev.KeyCode == Keys.Escape) { HideFindReplace(); ev.Handled = true; ev.SuppressKeyPress = true; }
                };

                findReplacePanel.Controls.AddRange(new Control[] { findLabel, frFindBox, replaceLabel, frReplaceBox, frMatchCase, findNextBtn, replaceBtn, replaceAllBtn, closeBtn });
                editorPanel.Controls.Add(findReplacePanel);
                findReplacePanel.BringToFront();
            }

            PositionFindReplacePanel();
            editorPanel.Resize += (s, ev) => { if (findReplacePanel.Visible) PositionFindReplacePanel(); };

            if (pythonEditor.SelectionLength > 0)
                frFindBox.Text = pythonEditor.SelectedText;

            frSearchStart = 0;
            findReplacePanel.Visible = true;
            frFindBox.Focus();
            frFindBox.SelectAll();
        }

        private void PositionFindReplacePanel()
        {
            int x = editorPanel.Width - findReplacePanel.Width - 20;
            if (x < 0) x = 0;
            int y = editorMenuBar.Height + 2;
            findReplacePanel.Location = new Point(x, y);
        }

        private void HideFindReplace()
        {
            if (findReplacePanel != null)
                findReplacePanel.Visible = false;
            pythonEditor.Focus();
        }

        private void FindNext()
        {
            string find = frFindBox.Text;
            if (string.IsNullOrEmpty(find)) return;

            var comparison = frMatchCase.Checked ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            int idx = pythonEditor.Text.IndexOf(find, frSearchStart, comparison);
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
            frSearchStart = idx + find.Length;
            RaiseStatus("Found at position " + idx);
        }

        private void ReplaceNext()
        {
            if (pythonEditor.SelectionLength > 0)
            {
                pythonEditor.SelectedText = frReplaceBox.Text;
            }
            FindNext();
        }

        private void ReplaceAll()
        {
            string find = frFindBox.Text;
            string replace = frReplaceBox.Text;
            if (string.IsNullOrEmpty(find)) return;

            var comparison = frMatchCase.Checked ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
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

            if (!pythonRunner.PythonAvailable)
            {
                AppendOutput("Cannot install packages: " + pythonRunner.PythonError + "\n", Color.FromArgb(200, 0, 0));
                RaiseStatus("Python not available");
                return;
            }

            RaiseStatus("Installing " + pkg + "...");
            Application.DoEvents();

            var result = pythonRunner.InstallPackage(pkg);
            if (result.Success)
            {
                AppendOutput("Successfully installed: " + pkg + "\n", Color.FromArgb(0, 128, 0));
                RaiseStatus(pkg + " installed successfully.");
            }
            else
            {
                AppendOutput("Failed to install " + pkg + ":\n" + result.Error + "\n", Color.FromArgb(200, 0, 0));
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

            if (!pythonRunner.PythonAvailable)
            {
                AppendOutput("Cannot uninstall packages: " + pythonRunner.PythonError + "\n", Color.FromArgb(200, 0, 0));
                RaiseStatus("Python not available");
                return;
            }

            RaiseStatus("Uninstalling " + pkg + "...");
            Application.DoEvents();

            var result = pythonRunner.UninstallPackage(pkg);
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
            allPackageItems.Clear();
            packageListBox.Items.Clear();
            pkgSearchBox.Text = pkgSearchPlaceholder;
            pkgSearchBox.ForeColor = Color.Gray;
            pkgSearchPlaceholderActive = true;

            if (!pythonRunner.PythonAvailable)
            {
                packageListBox.Items.Add("(Python not available — cannot list packages)");
                return;
            }

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
                        allPackageItems.Add(parts[0] + "  " + parts[1]);
                    else if (parts.Length == 1)
                        allPackageItems.Add(parts[0]);
                }
                allPackageItems.Sort(StringComparer.OrdinalIgnoreCase);
            }
            else if (!result.Success)
            {
                packageListBox.Items.Add("(Failed to list packages: " + result.Error + ")");
            }

            PopulatePackageList("");
        }

        private bool pkgSearchPlaceholderActive = true;
        private readonly string pkgSearchPlaceholder = "Search packages...";

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
                if (hasFilter && item.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                packageListBox.Items.Add(item);
            }

            if (packageListBox.Items.Count == 0 && hasFilter)
                packageListBox.Items.Add("No packages match '" + filter + "'");
        }

        private void OnShowHelp(object sender, EventArgs e)
        {
            string help = @"=== Data Science Workbench - Quick Start ===

AVAILABLE DATASETS (pre-loaded as variables):
  customers        - 150 customers with demographics
  employees        - 100 employees with salary, dept

HOW TO USE:
  1. Write Python code in the editor
  2. Press F5 or click Run to execute
  3. Datasets are pre-loaded as variables
  4. Access columns directly: customers.CreditLimit.mean()
  5. Use .df for full DataFrame: customers.df.describe()
  6. Install packages via Package Manager tab

EXAMPLE:
  print(customers.CreditLimit.mean())
  print(employees.df.groupby('Department')['Salary'].mean())

TIPS:
  - Use 'Insert Snippet' for ready-made code
  - Matplotlib plots save as PNG files
  - All standard Python libraries available
  - Install any pip package via Package Manager
  - .NET host can register classes and context variables";

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
            return @"# Datasets are pre-loaded as variables
# Access columns directly: customers.CreditLimit.mean()
# Use .df for full DataFrame: customers.df.describe()

print('=== Data Science Workbench ===')
print()

# Quick look at customers
print(f'Customers: {len(customers)} records')
print(f'Average credit limit: ${customers.CreditLimit.mean():.2f}')
print()
print('=== Customer Summary ===')
print(customers.df.describe())
";
        }

        private string GetLoadDataSnippet()
        {
            return @"
# Datasets are pre-loaded as variables
for name, ds in [('customers', customers), ('employees', employees)]:
    print(f'  {name}: {len(ds)} rows, {len(ds.df.columns)} columns')
";
        }

        private string GetStatsSnippet()
        {
            return @"
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
