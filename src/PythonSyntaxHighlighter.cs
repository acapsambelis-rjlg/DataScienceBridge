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
        private static readonly Color FStringBraceColor = Color.FromArgb(0, 0, 255);

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
        private static readonly Regex DoubleQuoteRegex = new Regex("\"(?:[^\"\\\\\\r\\n]|\\\\[^\\r\\n])*\"", RegexOptions.Compiled);
        private static readonly Regex SingleQuoteRegex = new Regex("'(?:[^'\\\\\\r\\n]|\\\\[^\\r\\n])*'", RegexOptions.Compiled);
        private static readonly Regex FStringDoubleRegex = new Regex("[fF]\"(?:[^\"\\\\\\r\\n]|\\\\[^\\r\\n])*\"", RegexOptions.Compiled);
        private static readonly Regex FStringSingleRegex = new Regex("[fF]'(?:[^'\\\\\\r\\n]|\\\\[^\\r\\n])*'", RegexOptions.Compiled);
        private static readonly Regex FStringTripleDoubleRegex = new Regex("[fF]\"\"\"[\\s\\S]*?\"\"\"", RegexOptions.Compiled);
        private static readonly Regex FStringTripleSingleRegex = new Regex("[fF]'''[\\s\\S]*?'''", RegexOptions.Compiled);
        private static readonly Regex PrefixedStringRegex = new Regex("[rRbBuU]{1,2}\"(?:[^\"\\\\\\r\\n]|\\\\[^\\r\\n])*\"", RegexOptions.Compiled);
        private static readonly Regex PrefixedSingleRegex = new Regex("[rRbBuU]{1,2}'(?:[^'\\\\\\r\\n]|\\\\[^\\r\\n])*'", RegexOptions.Compiled);
        private static readonly Regex CommentRegex = new Regex("#[^\\r\\n]*", RegexOptions.Compiled);
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

                var fstringMatches = new List<Match>();
                CollectFStringMatches(text, painted, FStringTripleDoubleRegex, fstringMatches);
                CollectFStringMatches(text, painted, FStringTripleSingleRegex, fstringMatches);
                CollectFStringMatches(text, painted, FStringDoubleRegex, fstringMatches);
                CollectFStringMatches(text, painted, FStringSingleRegex, fstringMatches);

                foreach (var m in fstringMatches)
                {
                    editor.Select(m.Index, m.Length);
                    editor.SelectionColor = StringColor;
                    MarkPainted(painted, m.Index, m.Length);
                }

                ApplyPatternColor(editor, text, painted, PrefixedStringRegex, StringColor);
                ApplyPatternColor(editor, text, painted, PrefixedSingleRegex, StringColor);
                ApplyPatternColor(editor, text, painted, DoubleQuoteRegex, StringColor);
                ApplyPatternColor(editor, text, painted, SingleQuoteRegex, StringColor);

                HighlightFStringExpressions(editor, text, painted, fstringMatches);

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

        private void CollectFStringMatches(string text, bool[] painted, Regex pattern, List<Match> results)
        {
            var matches = pattern.Matches(text);
            foreach (Match m in matches)
            {
                if (IsStartPainted(painted, m.Index)) continue;
                char first = text[m.Index];
                if (first != 'f' && first != 'F') continue;
                results.Add(m);
            }
        }

        private void HighlightFStringExpressions(RichTextBox editor, string text, bool[] painted, List<Match> fstringMatches)
        {
            foreach (var m in fstringMatches)
            {
                int prefixLen = 1;
                int quoteLen = 1;
                if (m.Length > 4)
                {
                    int afterPrefix = m.Index + prefixLen;
                    if (afterPrefix + 2 < text.Length && text[afterPrefix] == text[afterPrefix + 1] && text[afterPrefix] == text[afterPrefix + 2])
                        quoteLen = 3;
                }

                int contentStart = m.Index + prefixLen + quoteLen;
                int contentEnd = m.Index + m.Length - quoteLen;

                var expressions = FindFStringExpressions(text, contentStart, contentEnd);

                foreach (var expr in expressions)
                {
                    editor.Select(expr.BraceOpen, 1);
                    editor.SelectionColor = FStringBraceColor;

                    if (expr.ExprStart < expr.ExprEnd)
                    {
                        int exprLen = expr.ExprEnd - expr.ExprStart;
                        editor.Select(expr.ExprStart, exprLen);
                        editor.SelectionColor = DefaultColor;
                        UnmarkPainted(painted, expr.ExprStart, exprLen);

                        HighlightExpressionTokens(editor, text, painted, expr.ExprStart, expr.ExprEnd);
                    }

                    editor.Select(expr.BraceClose, 1);
                    editor.SelectionColor = FStringBraceColor;
                }
            }
        }

        private struct FStringExpression
        {
            public int BraceOpen;
            public int BraceClose;
            public int ExprStart;
            public int ExprEnd;
        }

        private List<FStringExpression> FindFStringExpressions(string text, int start, int end)
        {
            var results = new List<FStringExpression>();
            int i = start;
            while (i < end)
            {
                if (text[i] == '{')
                {
                    if (i + 1 < end && text[i + 1] == '{')
                    {
                        i += 2;
                        continue;
                    }

                    int braceOpen = i;
                    int depth = 1;
                    int exprStart = i + 1;
                    i++;

                    while (i < end && depth > 0)
                    {
                        char c = text[i];
                        if (c == '{') depth++;
                        else if (c == '}') { depth--; if (depth == 0) break; }
                        else if (c == '\'' || c == '"')
                        {
                            char q = c;
                            i++;
                            while (i < end && text[i] != q)
                            {
                                if (text[i] == '\\') i++;
                                i++;
                            }
                        }
                        i++;
                    }

                    if (depth == 0)
                    {
                        int exprEnd = i;
                        int searchEnd = exprEnd;
                        int colonDepth = 0;
                        for (int j = exprStart; j < searchEnd; j++)
                        {
                            char c = text[j];
                            if (c == '(' || c == '[' || c == '{') colonDepth++;
                            else if (c == ')' || c == ']' || c == '}') colonDepth--;
                            else if (c == ':' && colonDepth == 0)
                            {
                                exprEnd = j;
                                break;
                            }
                            else if (c == '!' && colonDepth == 0 && j + 1 < searchEnd && (text[j + 1] == 'r' || text[j + 1] == 's' || text[j + 1] == 'a'))
                            {
                                exprEnd = j;
                                break;
                            }
                        }

                        results.Add(new FStringExpression
                        {
                            BraceOpen = braceOpen,
                            BraceClose = i,
                            ExprStart = exprStart,
                            ExprEnd = exprEnd
                        });
                        i++;
                    }
                }
                else if (text[i] == '}' && i + 1 < end && text[i + 1] == '}')
                {
                    i += 2;
                }
                else
                {
                    i++;
                }
            }
            return results;
        }

        private void HighlightExpressionTokens(RichTextBox editor, string text, bool[] painted, int exprStart, int exprEnd)
        {
            string exprText = text.Substring(exprStart, exprEnd - exprStart);

            var numberMatches = NumberRegex.Matches(exprText);
            foreach (Match nm in numberMatches)
            {
                int absIdx = exprStart + nm.Index;
                editor.Select(absIdx, nm.Length);
                editor.SelectionColor = NumberColor;
            }

            var wordMatches = WordRegex.Matches(exprText);
            foreach (Match wm in wordMatches)
            {
                string word = wm.Value;
                Color? color = null;

                if (word == "self")
                    color = SelfColor;
                else if (Keywords.Contains(word))
                    color = KeywordColor;
                else if (Builtins.Contains(word))
                    color = BuiltinColor;

                if (color.HasValue)
                {
                    int absIdx = exprStart + wm.Index;
                    editor.Select(absIdx, wm.Length);
                    editor.SelectionColor = color.Value;
                }
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

        private void UnmarkPainted(bool[] painted, int start, int length)
        {
            int end = Math.Min(start + length, painted.Length);
            for (int i = start; i < end; i++)
                painted[i] = false;
        }
    }

    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool LockWindowUpdate(IntPtr hWndLock);
    }

}
