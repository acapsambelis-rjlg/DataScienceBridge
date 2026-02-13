using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace DataScienceWorkbench
{
    public class PythonSyntaxHighlighter
    {
        private static readonly Color KeywordColor = Color.FromArgb(0, 0, 255);
        private static readonly Color BuiltinColor = Color.FromArgb(38, 127, 153);
        private static readonly Color StringColor = Color.FromArgb(163, 21, 21);
        private static readonly Color CommentColor = Color.FromArgb(0, 128, 0);
        private static readonly Color NumberColor = Color.FromArgb(9, 134, 88);
        private static readonly Color DecoratorColor = Color.FromArgb(155, 100, 0);
        private static readonly Color DefaultColor = Color.FromArgb(0, 0, 0);
        private static readonly Color BackgroundColor = Color.FromArgb(255, 255, 255);
        private static readonly Color SelfColor = Color.FromArgb(0, 0, 255);
        private static readonly Color FunctionDefColor = Color.FromArgb(116, 83, 0);
        private static readonly Color ClassDefColor = Color.FromArgb(38, 127, 153);

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
                if (IsStartPainted(painted, m.Index)) continue;

                editor.Select(m.Index, m.Length);
                editor.SelectionColor = color;
                MarkPainted(painted, m.Index, m.Length);
            }
        }

        private bool IsStartPainted(bool[] painted, int start)
        {
            if (start >= painted.Length) return false;
            return painted[start];
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

}
