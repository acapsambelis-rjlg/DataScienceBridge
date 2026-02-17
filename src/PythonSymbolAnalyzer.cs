using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DataScienceWorkbench.PythonWorkbench
{
    public class SymbolError
    {
        public int StartIndex { get; set; }
        public int Length { get; set; }
        public string Name { get; set; }
        public string Message { get { return "Undefined name '" + Name + "'"; } }
    }

    public class PythonSymbolAnalyzer
    {
        private HashSet<string> dynamicKnownSymbols = new HashSet<string>();

        public void SetDynamicKnownSymbols(IEnumerable<string> symbols)
        {
            dynamicKnownSymbols = new HashSet<string>(symbols);
        }

        private static readonly HashSet<string> Builtins = new HashSet<string> {
            "abs", "all", "any", "ascii", "bin", "bool", "breakpoint", "bytearray",
            "bytes", "callable", "chr", "classmethod", "compile", "complex",
            "copyright", "credits", "delattr", "dict", "dir", "divmod", "enumerate",
            "eval", "exec", "exit", "filter", "float", "format", "frozenset",
            "getattr", "globals", "hasattr", "hash", "help", "hex", "id", "input",
            "int", "isinstance", "issubclass", "iter", "len", "license", "list",
            "locals", "map", "max", "memoryview", "min", "next", "object", "oct",
            "open", "ord", "pow", "print", "property", "quit", "range", "repr",
            "reversed", "round", "set", "setattr", "slice", "sorted",
            "staticmethod", "str", "sum", "super", "tuple", "type", "vars", "zip",
            "ArithmeticError", "AssertionError", "AttributeError", "BaseException",
            "BlockingIOError", "BrokenPipeError", "BufferError", "BytesWarning",
            "ChildProcessError", "ConnectionAbortedError", "ConnectionError",
            "ConnectionRefusedError", "ConnectionResetError", "DeprecationWarning",
            "EOFError", "Ellipsis", "EnvironmentError", "Exception",
            "FileExistsError", "FileNotFoundError", "FloatingPointError",
            "FutureWarning", "GeneratorExit", "IOError", "ImportError",
            "ImportWarning", "IndentationError", "IndexError",
            "InterruptedError", "IsADirectoryError", "KeyError",
            "KeyboardInterrupt", "LookupError", "MemoryError", "ModuleNotFoundError",
            "NameError", "NotADirectoryError", "NotImplemented",
            "NotImplementedError", "OSError", "OverflowError",
            "PendingDeprecationWarning", "PermissionError", "ProcessLookupError",
            "RecursionError", "ReferenceError", "ResourceWarning",
            "RuntimeError", "RuntimeWarning", "StopAsyncIteration",
            "StopIteration", "SyntaxError", "SyntaxWarning", "SystemError",
            "SystemExit", "TabError", "TimeoutError", "TypeError",
            "UnboundLocalError", "UnicodeDecodeError", "UnicodeEncodeError",
            "UnicodeError", "UnicodeTranslationError", "UnicodeWarning",
            "UserWarning", "ValueError", "Warning", "ZeroDivisionError",
            "__build_class__", "__import__", "__name__", "__file__", "__doc__",
            "__spec__", "__loader__", "__package__", "__builtins__"
        };

        private static readonly HashSet<string> Keywords = new HashSet<string> {
            "False", "None", "True", "and", "as", "assert", "async", "await",
            "break", "class", "continue", "def", "del", "elif", "else", "except",
            "finally", "for", "from", "global", "if", "import", "in", "is",
            "lambda", "nonlocal", "not", "or", "pass", "raise", "return",
            "try", "while", "with", "yield"
        };

        private static readonly HashSet<string> CommonModules = new HashSet<string> {
            "pd", "np", "plt", "sns", "scipy", "sklearn", "tf", "torch",
            "os", "sys", "re", "json", "csv", "math", "random", "datetime",
            "time", "collections", "itertools", "functools", "pathlib",
            "io", "string", "textwrap", "struct", "copy", "pprint",
            "typing", "abc", "dataclasses", "enum", "warnings",
            "pandas", "numpy", "matplotlib", "seaborn", "requests", "PIL",
            "subprocess", "threading", "multiprocessing", "logging",
            "unittest", "pytest", "argparse", "configparser",
            "hashlib", "hmac", "secrets", "base64", "pickle",
            "sqlite3", "xml", "html", "urllib", "http",
            "socket", "email", "smtplib", "ftplib",
            "glob", "shutil", "tempfile", "stat",
            "traceback", "inspect", "dis", "gc", "ctypes",
            "array", "queue", "heapq", "bisect",
            "decimal", "fractions", "statistics",
            "calendar", "locale", "gettext",
            "operator", "contextlib",
            "data_dir", "DotNetData",
            "self", "cls"
        };

        private static readonly HashSet<string> MagicNames = new HashSet<string> {
            "__init__", "__str__", "__repr__", "__len__", "__getitem__",
            "__setitem__", "__delitem__", "__iter__", "__next__", "__call__",
            "__enter__", "__exit__", "__eq__", "__ne__", "__lt__", "__gt__",
            "__le__", "__ge__", "__add__", "__sub__", "__mul__", "__truediv__",
            "__floordiv__", "__mod__", "__pow__", "__contains__", "__hash__",
            "__bool__", "__del__", "__new__", "__class__", "__dict__",
            "__slots__", "__all__", "_"
        };

        private static readonly Regex WordRegex = new Regex(@"\b[a-zA-Z_]\w*\b", RegexOptions.Compiled);
        private static readonly Regex TripleDoubleQuoteRegex = new Regex("\"\"\"[\\s\\S]*?\"\"\"", RegexOptions.Compiled);
        private static readonly Regex TripleSingleQuoteRegex = new Regex("'''[\\s\\S]*?'''", RegexOptions.Compiled);
        private static readonly Regex DoubleQuoteRegex = new Regex("\"(?:[^\"\\\\\\r\\n]|\\\\[^\\r\\n])*\"", RegexOptions.Compiled);
        private static readonly Regex SingleQuoteRegex = new Regex("'(?:[^'\\\\\\r\\n]|\\\\[^\\r\\n])*'", RegexOptions.Compiled);
        private static readonly Regex FStringRegex = new Regex("[fFrRbBuU]{1,2}\"(?:[^\"\\\\\\r\\n]|\\\\[^\\r\\n])*\"", RegexOptions.Compiled);
        private static readonly Regex FSingleRegex = new Regex("[fFrRbBuU]{1,2}'(?:[^'\\\\\\r\\n]|\\\\[^\\r\\n])*'", RegexOptions.Compiled);
        private static readonly Regex CommentRegex = new Regex("#[^\\r\\n]*", RegexOptions.Compiled);

        public List<SymbolError> Analyze(string code)
        {
            if (string.IsNullOrEmpty(code) || code.Length > 50000)
                return new List<SymbolError>();

            var masked = MaskStringsAndComments(code);
            var defined = CollectDefinitions(code, masked);
            return FindUndefinedReferences(code, masked, defined);
        }

        private string MaskStringsAndComments(string code)
        {
            char[] masked = code.ToCharArray();

            MaskPattern(masked, code, TripleDoubleQuoteRegex);
            MaskPattern(masked, code, TripleSingleQuoteRegex);
            MaskPattern(masked, code, FStringRegex);
            MaskPattern(masked, code, FSingleRegex);
            MaskPattern(masked, code, DoubleQuoteRegex);
            MaskPattern(masked, code, SingleQuoteRegex);
            MaskPattern(masked, code, CommentRegex);

            return new string(masked);
        }

        private void MaskPattern(char[] masked, string code, Regex pattern)
        {
            foreach (Match m in pattern.Matches(code))
            {
                bool alreadyMasked = false;
                for (int i = m.Index; i < m.Index + m.Length && i < masked.Length; i++)
                {
                    if (masked[i] == '\x01') { alreadyMasked = true; break; }
                }
                if (alreadyMasked) continue;

                for (int i = m.Index; i < m.Index + m.Length && i < masked.Length; i++)
                {
                    if (masked[i] != '\n') masked[i] = '\x01';
                }
            }
        }

        private HashSet<string> CollectDefinitions(string code, string masked)
        {
            var defined = new HashSet<string>();
            string[] lines = masked.Split('\n');
            string[] codeLines = code.Split('\n');
            int pos = 0;

            for (int lineIdx = 0; lineIdx < lines.Length; lineIdx++)
            {
                string mline = lines[lineIdx];
                string cline = lineIdx < codeLines.Length ? codeLines[lineIdx] : "";
                string trimmed = mline.TrimStart();

                if (trimmed.StartsWith("def "))
                {
                    var nameMatch = Regex.Match(cline, @"\bdef\s+(\w+)\s*\(([^)]*)\)");
                    if (nameMatch.Success)
                    {
                        defined.Add(nameMatch.Groups[1].Value);
                        string paramsStr = nameMatch.Groups[2].Value;
                        foreach (string p in paramsStr.Split(','))
                        {
                            string param = p.Trim().Split('=')[0].Trim().Split(':')[0].Trim();
                            if (param.StartsWith("*")) param = param.TrimStart('*');
                            if (param.Length > 0 && Regex.IsMatch(param, @"^[a-zA-Z_]\w*$"))
                                defined.Add(param);
                        }
                    }
                }
                else if (trimmed.StartsWith("class "))
                {
                    var nameMatch = Regex.Match(cline, @"\bclass\s+(\w+)");
                    if (nameMatch.Success)
                        defined.Add(nameMatch.Groups[1].Value);
                }
                else if (trimmed.StartsWith("import "))
                {
                    var importMatch = Regex.Match(cline, @"\bimport\s+(.+)");
                    if (importMatch.Success)
                    {
                        foreach (string part in importMatch.Groups[1].Value.Split(','))
                        {
                            string item = part.Trim();
                            var asMatch = Regex.Match(item, @"(\w+)\s+as\s+(\w+)");
                            if (asMatch.Success)
                                defined.Add(asMatch.Groups[2].Value);
                            else
                            {
                                string moduleName = item.Split('.')[0].Trim();
                                if (Regex.IsMatch(moduleName, @"^[a-zA-Z_]\w*$"))
                                    defined.Add(moduleName);
                            }
                        }
                    }
                }
                else if (trimmed.StartsWith("from "))
                {
                    var fromMatch = Regex.Match(cline, @"\bfrom\s+\S+\s+import\s+(.+)");
                    if (fromMatch.Success)
                    {
                        foreach (string part in fromMatch.Groups[1].Value.Split(','))
                        {
                            string item = part.Trim();
                            if (item == "*") continue;
                            var asMatch = Regex.Match(item, @"(\w+)\s+as\s+(\w+)");
                            if (asMatch.Success)
                                defined.Add(asMatch.Groups[2].Value);
                            else
                            {
                                string name = item.Split('(')[0].Trim();
                                if (Regex.IsMatch(name, @"^[a-zA-Z_]\w*$"))
                                    defined.Add(name);
                            }
                        }
                    }
                }

                if (trimmed.StartsWith("global ") || trimmed.StartsWith("nonlocal "))
                {
                    string keyword = trimmed.StartsWith("global ") ? "global" : "nonlocal";
                    var declMatch = Regex.Match(cline, @"\b" + keyword + @"\s+(.+)");
                    if (declMatch.Success)
                    {
                        foreach (string part in declMatch.Groups[1].Value.Split(','))
                        {
                            string name = part.Trim();
                            if (Regex.IsMatch(name, @"^[a-zA-Z_]\w*$"))
                                defined.Add(name);
                        }
                    }
                }

                if (trimmed.StartsWith("for "))
                {
                    var forMatch = Regex.Match(cline, @"\bfor\s+(.+?)\s+in\s+");
                    if (forMatch.Success)
                    {
                        string targets = forMatch.Groups[1].Value;
                        foreach (Match wm in Regex.Matches(targets, @"\b([a-zA-Z_]\w*)\b"))
                            defined.Add(wm.Groups[1].Value);
                    }
                }

                if (trimmed.StartsWith("with "))
                {
                    var withMatch = Regex.Match(cline, @"\bas\s+(\w+)");
                    if (withMatch.Success)
                        defined.Add(withMatch.Groups[1].Value);
                }

                if (trimmed.StartsWith("except"))
                {
                    var exceptMatch = Regex.Match(cline, @"\bexcept\s+\w+\s+as\s+(\w+)");
                    if (exceptMatch.Success)
                        defined.Add(exceptMatch.Groups[1].Value);
                }

                var assignments = Regex.Matches(mline, @"\b([a-zA-Z_]\w*)\s*=[^=]");
                foreach (Match am in assignments)
                {
                    string name = am.Groups[1].Value;
                    if (!Keywords.Contains(name))
                    {
                        if (am.Index > 0 && mline[am.Index - 1] == '.') continue;
                        defined.Add(name);
                    }
                }

                var tupleAssign = Regex.Match(mline, @"^(\s*)((?:[a-zA-Z_]\w*\s*,\s*)+[a-zA-Z_]\w*)\s*=\s*");
                if (tupleAssign.Success)
                {
                    string targets = tupleAssign.Groups[2].Value;
                    foreach (Match tm in Regex.Matches(targets, @"\b([a-zA-Z_]\w*)\b"))
                    {
                        if (!Keywords.Contains(tm.Groups[1].Value))
                            defined.Add(tm.Groups[1].Value);
                    }
                }

                var augAssignments = Regex.Matches(mline, @"\b([a-zA-Z_]\w*)\s*(?:\+=|-=|\*=|/=|//=|%=|\*\*=|&=|\|=|\^=|<<=|>>=)");
                foreach (Match am in augAssignments)
                {
                    string name = am.Groups[1].Value;
                    if (!Keywords.Contains(name))
                        defined.Add(name);
                }

                var listCompVars = Regex.Matches(mline, @"\bfor\s+(\w+)\s+in\b");
                foreach (Match lcm in listCompVars)
                    defined.Add(lcm.Groups[1].Value);

                var lambdaMatches = Regex.Matches(mline, @"\blambda\s+([^:]+):");
                foreach (Match lm in lambdaMatches)
                {
                    foreach (string p in lm.Groups[1].Value.Split(','))
                    {
                        string param = p.Trim().Split('=')[0].Trim();
                        if (param.StartsWith("*")) param = param.TrimStart('*');
                        if (param.Length > 0 && Regex.IsMatch(param, @"^[a-zA-Z_]\w*$"))
                            defined.Add(param);
                    }
                }

                pos += cline.Length + 1;
            }

            return defined;
        }

        private List<SymbolError> FindUndefinedReferences(string code, string masked, HashSet<string> defined)
        {
            var errors = new List<SymbolError>();
            var reported = new HashSet<string>();

            var matches = WordRegex.Matches(masked);
            foreach (Match m in matches)
            {
                if (masked[m.Index] == '\x01') continue;

                string name = m.Value;

                if (Keywords.Contains(name)) continue;
                if (Builtins.Contains(name)) continue;
                if (CommonModules.Contains(name)) continue;
                if (dynamicKnownSymbols.Contains(name)) continue;
                if (MagicNames.Contains(name)) continue;
                if (defined.Contains(name)) continue;
                if (name.StartsWith("__") && name.EndsWith("__")) continue;
                if (name.StartsWith("_")) continue;
                if (name.Length == 1) continue;

                if (m.Index > 0 && code[m.Index - 1] == '.') continue;

                int afterName = m.Index + m.Length;
                if (afterName < masked.Length)
                {
                    int peek = afterName;
                    while (peek < masked.Length && masked[peek] == ' ') peek++;
                    if (peek < masked.Length && masked[peek] == '=' && peek + 1 < masked.Length && masked[peek + 1] != '=')
                    {
                        bool insideCall = false;
                        int depth = 0;
                        for (int k = m.Index - 1; k >= 0; k--)
                        {
                            if (masked[k] == ')') depth++;
                            else if (masked[k] == '(') { depth--; if (depth < 0) { insideCall = true; break; } }
                            else if (masked[k] == '\n') break;
                        }
                        if (insideCall) continue;
                    }
                }

                if (afterName < code.Length && code[afterName] == '(')
                {
                    string before = code.Substring(0, m.Index).TrimEnd();
                    if (before.EndsWith("def") || before.EndsWith("class"))
                        continue;
                }

                int lineStart = code.LastIndexOf('\n', Math.Max(m.Index - 1, 0));
                lineStart = lineStart < 0 ? 0 : lineStart + 1;
                string linePrefix = code.Substring(lineStart, m.Index - lineStart).TrimStart();
                if (linePrefix.StartsWith("def ") || linePrefix.StartsWith("class ") ||
                    linePrefix.StartsWith("import ") || linePrefix.StartsWith("from ") ||
                    linePrefix.StartsWith("@") ||
                    linePrefix.StartsWith("global ") || linePrefix.StartsWith("nonlocal ") ||
                    linePrefix.StartsWith("except ") || linePrefix.StartsWith("except:"))
                    continue;

                string key = name + ":" + m.Index;
                if (reported.Contains(key)) continue;
                reported.Add(key);

                errors.Add(new SymbolError
                {
                    StartIndex = m.Index,
                    Length = m.Length,
                    Name = name
                });
            }

            return errors;
        }
    }
}
