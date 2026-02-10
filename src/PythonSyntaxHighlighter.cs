using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace DataScienceWorkbench
{
    public class PythonSyntaxHighlighter
    {
        private static readonly Color KeywordColor = Color.FromArgb(86, 156, 214);
        private static readonly Color BuiltinColor = Color.FromArgb(78, 201, 176);
        private static readonly Color StringColor = Color.FromArgb(206, 145, 120);
        private static readonly Color CommentColor = Color.FromArgb(106, 153, 85);
        private static readonly Color NumberColor = Color.FromArgb(181, 206, 168);
        private static readonly Color DecoratorColor = Color.FromArgb(220, 220, 170);
        private static readonly Color DefaultColor = Color.FromArgb(212, 212, 212);
        private static readonly Color BackgroundColor = Color.FromArgb(30, 30, 30);
        private static readonly Color SelfColor = Color.FromArgb(86, 156, 214);
        private static readonly Color FunctionDefColor = Color.FromArgb(220, 220, 170);
        private static readonly Color ClassDefColor = Color.FromArgb(78, 201, 176);

        private static readonly HashSet<string> Keywords = new HashSet<string> {
            "False", "None", "True", "and", "as", "assert", "async", "await",
            "break", "class", "continue", "def", "del", "elif", "else", "except",
            "finally", "for", "from", "global", "if", "import", "in", "is",
            "lambda", "nonlocal", "not", "or", "pass", "raise", "return",
            "try", "while", "with", "yield"
        };

        private static readonly HashSet<string> Builtins = new HashSet<string> {
            "abs", "all", "any", "bin", "bool", "bytearray", "bytes", "callable",
            "chr", "classmethod", "compile", "complex", "delattr", "dict", "dir",
            "divmod", "enumerate", "eval", "exec", "filter", "float", "format",
            "frozenset", "getattr", "globals", "hasattr", "hash", "help", "hex",
            "id", "input", "int", "isinstance", "issubclass", "iter", "len",
            "list", "locals", "map", "max", "memoryview", "min", "next", "object",
            "oct", "open", "ord", "pow", "print", "property", "range", "repr",
            "reversed", "round", "set", "setattr", "slice", "sorted",
            "staticmethod", "str", "sum", "super", "tuple", "type", "vars", "zip"
        };

        private static readonly Regex TripleDoubleQuoteRegex = new Regex("\"\"\"[\\s\\S]*?\"\"\"", RegexOptions.Compiled);
        private static readonly Regex TripleSingleQuoteRegex = new Regex("'''[\\s\\S]*?'''", RegexOptions.Compiled);
        private static readonly Regex DoubleQuoteRegex = new Regex("\"(?:[^\"\\\\]|\\\\.)*\"", RegexOptions.Compiled);
        private static readonly Regex SingleQuoteRegex = new Regex("'(?:[^'\\\\]|\\\\.)*'", RegexOptions.Compiled);
        private static readonly Regex FStringRegex = new Regex("[fFrRbBuU]{1,2}\"(?:[^\"\\\\]|\\\\.)*\"", RegexOptions.Compiled);
        private static readonly Regex FSingleRegex = new Regex("[fFrRbBuU]{1,2}'(?:[^'\\\\]|\\\\.)*'", RegexOptions.Compiled);
        private static readonly Regex CommentRegex = new Regex("#[^\n]*", RegexOptions.Compiled);
        private static readonly Regex NumberRegex = new Regex(@"\b\d+\.?\d*(?:[eE][+-]?\d+)?\b", RegexOptions.Compiled);
        private static readonly Regex DecoratorRegex = new Regex(@"^[ \t]*@\w+", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex WordRegex = new Regex(@"\b[a-zA-Z_]\w*\b", RegexOptions.Compiled);

        private bool isHighlighting;

        public void Highlight(RichTextBox editor)
        {
            if (isHighlighting) return;
            isHighlighting = true;

            try
            {
                int selStart = editor.SelectionStart;
                int selLen = editor.SelectionLength;

                NativeMethods.LockWindowUpdate(editor.Handle);

                editor.SelectAll();
                editor.SelectionColor = DefaultColor;
                editor.SelectionBackColor = BackgroundColor;

                string text = editor.Text;
                var painted = new bool[text.Length];

                ApplyPatternColor(editor, text, painted, TripleDoubleQuoteRegex, StringColor);
                ApplyPatternColor(editor, text, painted, TripleSingleQuoteRegex, StringColor);
                ApplyPatternColor(editor, text, painted, FStringRegex, StringColor);
                ApplyPatternColor(editor, text, painted, FSingleRegex, StringColor);
                ApplyPatternColor(editor, text, painted, DoubleQuoteRegex, StringColor);
                ApplyPatternColor(editor, text, painted, SingleQuoteRegex, StringColor);
                ApplyPatternColor(editor, text, painted, CommentRegex, CommentColor);
                ApplyPatternColor(editor, text, painted, DecoratorRegex, DecoratorColor);
                ApplyPatternColor(editor, text, painted, NumberRegex, NumberColor);

                var matches = WordRegex.Matches(text);
                foreach (Match m in matches)
                {
                    if (IsRegionPainted(painted, m.Index, m.Length)) continue;

                    string word = m.Value;
                    Color? color = null;

                    if (word == "self")
                        color = SelfColor;
                    else if (Keywords.Contains(word))
                        color = KeywordColor;
                    else if (Builtins.Contains(word))
                        color = BuiltinColor;

                    if (color.HasValue)
                    {
                        editor.Select(m.Index, m.Length);
                        editor.SelectionColor = color.Value;
                    }

                    if ((word == "def" || word == "class") && m.Index + m.Length < text.Length)
                    {
                        var nameMatch = WordRegex.Match(text, m.Index + m.Length);
                        if (nameMatch.Success && !IsRegionPainted(painted, nameMatch.Index, nameMatch.Length))
                        {
                            int gap = nameMatch.Index - (m.Index + m.Length);
                            bool onlySpaces = true;
                            for (int i = m.Index + m.Length; i < nameMatch.Index; i++)
                            {
                                if (text[i] != ' ' && text[i] != '\t') { onlySpaces = false; break; }
                            }
                            if (onlySpaces && gap > 0)
                            {
                                editor.Select(nameMatch.Index, nameMatch.Length);
                                editor.SelectionColor = word == "def" ? FunctionDefColor : ClassDefColor;
                            }
                        }
                    }
                }

                editor.Select(selStart, selLen);
            }
            finally
            {
                NativeMethods.LockWindowUpdate(IntPtr.Zero);
                isHighlighting = false;
            }
        }

        private void ApplyPatternColor(RichTextBox editor, string text, bool[] painted, Regex pattern, Color color)
        {
            var matches = pattern.Matches(text);
            foreach (Match m in matches)
            {
                if (IsRegionPainted(painted, m.Index, m.Length)) continue;

                editor.Select(m.Index, m.Length);
                editor.SelectionColor = color;
                MarkPainted(painted, m.Index, m.Length);
            }
        }

        private bool IsRegionPainted(bool[] painted, int start, int length)
        {
            if (start >= painted.Length) return false;
            int end = Math.Min(start + length, painted.Length);
            for (int i = start; i < end; i++)
            {
                if (painted[i]) return true;
            }
            return false;
        }

        private void MarkPainted(bool[] painted, int start, int length)
        {
            int end = Math.Min(start + length, painted.Length);
            for (int i = start; i < end; i++)
                painted[i] = true;
        }
    }

    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool LockWindowUpdate(IntPtr hWndLock);
    }

    public class LineNumberPanel : Panel
    {
        private RichTextBox editor;
        private Font lineFont;

        public LineNumberPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            lineFont = new Font("Monospace", 9f);
        }

        public void AttachEditor(RichTextBox editorBox)
        {
            editor = editorBox;
            editor.VScroll += (s, e) => this.Invalidate();
            editor.TextChanged += (s, e) => this.Invalidate();
            editor.Resize += (s, e) => this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (editor == null) return;

            e.Graphics.Clear(Color.FromArgb(40, 40, 40));

            int firstCharIndex = editor.GetCharIndexFromPosition(new Point(0, 0));
            int firstLine = editor.GetLineFromCharIndex(firstCharIndex);

            int totalLines = editor.Lines.Length;
            if (totalLines == 0) totalLines = 1;

            using (var brush = new SolidBrush(Color.FromArgb(140, 140, 140)))
            using (var sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Far;
                sf.LineAlignment = StringAlignment.Near;

                for (int i = firstLine; i < totalLines; i++)
                {
                    int charIdx = editor.GetFirstCharIndexFromLine(i);
                    if (charIdx < 0) break;

                    Point pos = editor.GetPositionFromCharIndex(charIdx);
                    int y = pos.Y;

                    if (y > editor.Height) break;

                    var rect = new RectangleF(0, y, this.Width - 6, lineFont.Height);
                    e.Graphics.DrawString((i + 1).ToString(), lineFont, brush, rect, sf);
                }
            }

            using (var pen = new Pen(Color.FromArgb(60, 60, 60)))
            {
                e.Graphics.DrawLine(pen, this.Width - 1, 0, this.Width - 1, this.Height);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && lineFont != null)
            {
                lineFont.Dispose();
                lineFont = null;
            }
            base.Dispose(disposing);
        }
    }
}
