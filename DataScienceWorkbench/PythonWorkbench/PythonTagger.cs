using System.Collections.Generic;
using System.Text.RegularExpressions;
using Telerik.WinForms.SyntaxEditor.Core.Editor;
using Telerik.WinForms.SyntaxEditor.Core.Tagging;
using Telerik.WinForms.SyntaxEditor.Core.Text;

namespace RJLG.IntelliSEM.UI.Controls.PythonDataScience
{
    // FIX: Inherits TaggerBase<ClassificationTag> from Telerik.WinForms.SyntaxEditor.Core.Tagging
    //       Constructor takes ITextDocumentEditor from Telerik.WinForms.SyntaxEditor.Core.Editor
    public class PythonTagger : TaggerBase<ClassificationTag>
    {
        public static readonly ClassificationType KeywordType = new ClassificationType("PythonKeyword");
        public static readonly ClassificationType BuiltinType = new ClassificationType("PythonBuiltin");
        public static readonly ClassificationType StringType = new ClassificationType("PythonString");
        public static readonly ClassificationType CommentType = new ClassificationType("PythonComment");
        public static readonly ClassificationType NumberType = new ClassificationType("PythonNumber");
        public static readonly ClassificationType DecoratorType = new ClassificationType("PythonDecorator");
        public static readonly ClassificationType SelfType = new ClassificationType("PythonSelf");
        public static readonly ClassificationType FunctionDefType = new ClassificationType("PythonFunctionDef");
        public static readonly ClassificationType ClassDefType = new ClassificationType("PythonClassDef");
        public static readonly ClassificationType FStringBraceType = new ClassificationType("PythonFStringBrace");

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

        public PythonTagger(ITextDocumentEditor editor) : base(editor)
        {
        }

        // FIX: GetTags receives NormalizedSnapshotSpanCollection from the framework.
        //       We extract the full document text from the first span's snapshot,
        //       then yield TagSpan<ClassificationTag> for each token found.
        public override IEnumerable<TagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans == null || spans.Count == 0)
                yield break;

            // FIX: spans[0].Snapshot gives us the TextSnapshot for creating TextSnapshotSpan objects
            var snapshot = spans[0].Snapshot;
            // FIX: snapshot.GetText(snapshot.Span) — must use explicit Span overload
            string fullText = snapshot.GetText(snapshot.Span);
            var painted = new bool[fullText.Length];

            foreach (var tag in EmitPatternTags(snapshot, fullText, painted, TripleDoubleQuoteRegex, StringType)) yield return tag;
            foreach (var tag in EmitPatternTags(snapshot, fullText, painted, TripleSingleQuoteRegex, StringType)) yield return tag;

            var fstringMatches = new List<Match>();
            CollectFStringMatches(fullText, painted, FStringTripleDoubleRegex, fstringMatches);
            CollectFStringMatches(fullText, painted, FStringTripleSingleRegex, fstringMatches);
            CollectFStringMatches(fullText, painted, FStringDoubleRegex, fstringMatches);
            CollectFStringMatches(fullText, painted, FStringSingleRegex, fstringMatches);

            foreach (var m in fstringMatches)
            {
                yield return MakeTag(snapshot, m.Index, m.Length, StringType);
                MarkPainted(painted, m.Index, m.Length);
            }

            foreach (var m in fstringMatches)
            {
                foreach (var tag in EmitFStringExpressionTags(snapshot, fullText, painted, m))
                    yield return tag;
            }

            foreach (var tag in EmitPatternTags(snapshot, fullText, painted, PrefixedStringRegex, StringType)) yield return tag;
            foreach (var tag in EmitPatternTags(snapshot, fullText, painted, PrefixedSingleRegex, StringType)) yield return tag;
            foreach (var tag in EmitPatternTags(snapshot, fullText, painted, DoubleQuoteRegex, StringType)) yield return tag;
            foreach (var tag in EmitPatternTags(snapshot, fullText, painted, SingleQuoteRegex, StringType)) yield return tag;
            foreach (var tag in EmitPatternTags(snapshot, fullText, painted, CommentRegex, CommentType)) yield return tag;
            foreach (var tag in EmitPatternTags(snapshot, fullText, painted, DecoratorRegex, DecoratorType)) yield return tag;
            foreach (var tag in EmitPatternTags(snapshot, fullText, painted, NumberRegex, NumberType)) yield return tag;

            var wordMatches = WordRegex.Matches(fullText);
            foreach (Match m in wordMatches)
            {
                if (IsRegionPainted(painted, m.Index, m.Length)) continue;

                string word = m.Value;
                ClassificationType type = null;

                if (word == "self")
                    type = SelfType;
                else if (Keywords.Contains(word))
                    type = KeywordType;
                else if (Builtins.Contains(word))
                    type = BuiltinType;

                if (type != null)
                    yield return MakeTag(snapshot, m.Index, m.Length, type);

                if ((word == "def" || word == "class") && m.Index + m.Length < fullText.Length)
                {
                    var nameMatch = WordRegex.Match(fullText, m.Index + m.Length);
                    if (nameMatch.Success && !IsRegionPainted(painted, nameMatch.Index, nameMatch.Length))
                    {
                        bool onlySpaces = true;
                        for (int i = m.Index + m.Length; i < nameMatch.Index; i++)
                        {
                            if (fullText[i] != ' ' && fullText[i] != '\t') { onlySpaces = false; break; }
                        }
                        if (onlySpaces && nameMatch.Index - (m.Index + m.Length) > 0)
                        {
                            var defType = word == "def" ? FunctionDefType : ClassDefType;
                            yield return MakeTag(snapshot, nameMatch.Index, nameMatch.Length, defType);
                        }
                    }
                }
            }
        }

        private IEnumerable<TagSpan<ClassificationTag>> EmitPatternTags(TextSnapshot snapshot, string text, bool[] painted, Regex pattern, ClassificationType type)
        {
            var matches = pattern.Matches(text);
            foreach (Match m in matches)
            {
                if (IsStartPainted(painted, m.Index)) continue;
                yield return MakeTag(snapshot, m.Index, m.Length, type);
                MarkPainted(painted, m.Index, m.Length);
            }
        }

        private IEnumerable<TagSpan<ClassificationTag>> EmitFStringExpressionTags(
            TextSnapshot snapshot, string text, bool[] painted, Match fstringMatch)
        {
            int prefixLen = 1;
            int quoteLen = 1;
            if (fstringMatch.Length > 4)
            {
                int afterPrefix = fstringMatch.Index + prefixLen;
                if (afterPrefix + 2 < text.Length &&
                    text[afterPrefix] == text[afterPrefix + 1] &&
                    text[afterPrefix] == text[afterPrefix + 2])
                    quoteLen = 3;
            }

            int contentStart = fstringMatch.Index + prefixLen + quoteLen;
            int contentEnd = fstringMatch.Index + fstringMatch.Length - quoteLen;

            int i = contentStart;
            while (i < contentEnd)
            {
                if (text[i] == '{')
                {
                    if (i + 1 < contentEnd && text[i + 1] == '{') { i += 2; continue; }

                    int braceOpen = i;
                    int depth = 1;
                    i++;
                    while (i < contentEnd && depth > 0)
                    {
                        char c = text[i];
                        if (c == '{') depth++;
                        else if (c == '}') { depth--; if (depth == 0) break; }
                        else if (c == '\'' || c == '"')
                        {
                            char q = c; i++;
                            while (i < contentEnd && text[i] != q) { if (text[i] == '\\') i++; i++; }
                        }
                        i++;
                    }

                    if (depth == 0)
                    {
                        yield return MakeTag(snapshot, braceOpen, 1, FStringBraceType);
                        yield return MakeTag(snapshot, i, 1, FStringBraceType);

                        int exprStart = braceOpen + 1;
                        int exprEnd = i;
                        var exprWordMatches = WordRegex.Matches(text.Substring(exprStart, exprEnd - exprStart));
                        foreach (Match wm in exprWordMatches)
                        {
                            string word = wm.Value;
                            ClassificationType type = null;
                            if (word == "self") type = SelfType;
                            else if (Keywords.Contains(word)) type = KeywordType;
                            else if (Builtins.Contains(word)) type = BuiltinType;
                            if (type != null)
                                yield return MakeTag(snapshot, exprStart + wm.Index, wm.Length, type);
                        }
                        i++;
                    }
                }
                else if (text[i] == '}' && i + 1 < contentEnd && text[i + 1] == '}') { i += 2; }
                else { i++; }
            }
        }

        // FIX: TagSpan<T> requires TextSnapshotSpan, not raw Span.
        //       TextSnapshotSpan constructor: new TextSnapshotSpan(TextSnapshot, Span)
        //       The snapshot parameter ties the span to a specific document version.
        private TagSpan<ClassificationTag> MakeTag(TextSnapshot snapshot, int start, int length, ClassificationType type)
        {
            // FIX: Must wrap Span in TextSnapshotSpan with snapshot reference
            var snapshotSpan = new TextSnapshotSpan(snapshot, new Span(start, length));
            return new TagSpan<ClassificationTag>(snapshotSpan, new ClassificationTag(type));
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

        private bool IsStartPainted(bool[] painted, int start)
        {
            if (start >= painted.Length) return false;
            return painted[start];
        }

        private bool IsRegionPainted(bool[] painted, int start, int length)
        {
            if (start >= painted.Length) return false;
            int end = System.Math.Min(start + length, painted.Length);
            for (int i = start; i < end; i++)
            {
                if (painted[i]) return true;
            }
            return false;
        }

        private void MarkPainted(bool[] painted, int start, int length)
        {
            int end = System.Math.Min(start + length, painted.Length);
            for (int i = start; i < end; i++)
                painted[i] = true;
        }
    }
}
