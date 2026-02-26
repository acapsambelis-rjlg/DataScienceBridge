using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace RJLG.IntelliSEM.UI.Controls.PythonDataScience
{
    public class SymbolError
    {
        public int StartIndex { get; set; }
        public int Length { get; set; }
        public string Name { get; set; }
        public string CustomMessage { get; set; }
        public string Message { get { return CustomMessage ?? ("Undefined name '" + Name + "'"); } }
    }

    public class PythonSymbolAnalyzer
    {
        private HashSet<string> dynamicKnownSymbols = new HashSet<string>();
        private Dictionary<string, HashSet<string>> datasetColumns = new Dictionary<string, HashSet<string>>();

        private HashSet<string> knownModules;
        private HashSet<string> knownMembers;
        private HashSet<string> magicNames;
        private Dictionary<string, HashSet<string>> moduleSymbols = new Dictionary<string, HashSet<string>>();

        public string ScriptsDirectory { get; set; }

        private Func<string, string> fileContentResolver;

        public PythonSymbolAnalyzer()
        {
            knownModules = new HashSet<string>(DefaultModules);
            knownMembers = new HashSet<string>(DefaultMembers);
            magicNames = new HashSet<string>(DefaultMagicNames);
        }

        public void SetFileContentResolver(Func<string, string> resolver)
        {
            fileContentResolver = resolver;
        }

        public void SetDynamicKnownSymbols(IEnumerable<string> symbols)
        {
            dynamicKnownSymbols = new HashSet<string>(symbols);
        }

        public void SetDatasetColumns(Dictionary<string, List<string>> columns)
        {
            datasetColumns.Clear();
            if (columns == null) return;
            foreach (var kvp in columns)
                datasetColumns[kvp.Key] = new HashSet<string>(kvp.Value);
        }

        public void AddKnownModules(IEnumerable<string> modules)
        {
            foreach (var m in modules)
                knownModules.Add(m);
        }

        public void RemoveKnownModules(IEnumerable<string> modules)
        {
            foreach (var m in modules)
                knownModules.Remove(m);
        }

        public void SetKnownModules(IEnumerable<string> modules)
        {
            knownModules = new HashSet<string>(modules);
        }

        public void AddKnownMembers(IEnumerable<string> members)
        {
            foreach (var m in members)
                knownMembers.Add(m);
        }

        public void RemoveKnownMembers(IEnumerable<string> members)
        {
            foreach (var m in members)
                knownMembers.Remove(m);
        }

        public void SetKnownMembers(IEnumerable<string> members)
        {
            knownMembers = new HashSet<string>(members);
        }

        public void AddMagicNames(IEnumerable<string> names)
        {
            foreach (var n in names)
                magicNames.Add(n);
        }

        public void LoadModuleSymbols(string moduleName, IEnumerable<string> symbols)
        {
            knownModules.Add(moduleName);
            var set = new HashSet<string>(symbols);
            moduleSymbols[moduleName] = set;
            foreach (var s in set)
                knownMembers.Add(s);
        }

        public void LoadSymbolsFromVenv(string venvPath)
        {
            if (string.IsNullOrEmpty(venvPath)) return;

            string libDir = Path.Combine(venvPath, "lib");
            if (!Directory.Exists(libDir)) return;

            foreach (string pyDir in Directory.GetDirectories(libDir, "python*"))
            {
                string sitePackages = Path.Combine(pyDir, "site-packages");
                if (!Directory.Exists(sitePackages)) continue;

                foreach (string dir in Directory.GetDirectories(sitePackages))
                {
                    string dirName = Path.GetFileName(dir);
                    if (dirName.StartsWith(".") || dirName.StartsWith("_") ||
                        dirName.EndsWith(".dist-info") || dirName.EndsWith(".egg-info") ||
                        dirName == "__pycache__")
                        continue;
                    if (File.Exists(Path.Combine(dir, "__init__.py")) ||
                        Directory.GetFiles(dir, "*.py").Length > 0)
                        knownModules.Add(dirName);
                }

                foreach (string file in Directory.GetFiles(sitePackages, "*.py"))
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    if (!fileName.StartsWith("_"))
                        knownModules.Add(fileName);
                }
            }
        }

        public IReadOnlyCollection<string> KnownModules { get { return knownModules; } }
        public IReadOnlyCollection<string> KnownMembers { get { return knownMembers; } }
        public IReadOnlyDictionary<string, HashSet<string>> ModuleSymbols { get { return moduleSymbols; } }

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

        private static readonly HashSet<string> DefaultModules = new HashSet<string> {
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

        private static readonly HashSet<string> DefaultMagicNames = new HashSet<string> {
            "__init__", "__str__", "__repr__", "__len__", "__getitem__",
            "__setitem__", "__delitem__", "__iter__", "__next__", "__call__",
            "__enter__", "__exit__", "__eq__", "__ne__", "__lt__", "__gt__",
            "__le__", "__ge__", "__add__", "__sub__", "__mul__", "__truediv__",
            "__floordiv__", "__mod__", "__pow__", "__contains__", "__hash__",
            "__bool__", "__del__", "__new__", "__class__", "__dict__",
            "__slots__", "__all__", "_"
        };

        private static readonly HashSet<string> DefaultMembers = new HashSet<string> {
            "df", "head", "tail", "describe", "info", "shape", "dtypes", "columns",
            "index", "values", "iloc", "loc", "at", "iat", "T",
            "mean", "sum", "min", "max", "std", "var", "median", "count",
            "sort_values", "sort_index", "reset_index", "set_index",
            "groupby", "merge", "join", "concat", "append", "drop", "dropna", "fillna",
            "apply", "map", "applymap", "transform", "agg", "aggregate",
            "filter", "query", "where", "mask", "clip",
            "astype", "copy", "rename", "replace", "sample",
            "to_csv", "to_json", "to_excel", "to_dict", "to_numpy", "to_list",
            "plot", "hist", "corr", "cov", "unique", "nunique", "value_counts",
            "isnull", "notnull", "isna", "notna", "any", "all", "empty", "size",
            "iterrows", "itertuples", "items",
            "str", "dt", "cat", "sparse",
            "rolling", "expanding", "ewm", "resample", "pipe",
            "assign", "eval", "melt", "pivot", "pivot_table", "stack", "unstack",
            "nlargest", "nsmallest", "idxmin", "idxmax", "pct_change", "diff", "shift",
            "cumsum", "cumprod", "cummin", "cummax",
            "between", "isin", "duplicated", "drop_duplicates",
            "add_prefix", "add_suffix", "equals", "abs", "round",
            "to_string", "to_markdown", "to_frame",
            "name", "dtype", "ndim", "nbytes"
        };

        private static readonly Regex WordRegex = new Regex(@"\b[a-zA-Z_]\w*\b", RegexOptions.Compiled);
        private static readonly Regex TripleDoubleQuoteRegex = new Regex("\"\"\"[\\s\\S]*?\"\"\"", RegexOptions.Compiled);
        private static readonly Regex TripleSingleQuoteRegex = new Regex("'''[\\s\\S]*?'''", RegexOptions.Compiled);
        private static readonly Regex DoubleQuoteRegex = new Regex("\"(?:[^\"\\\\\\r\\n]|\\\\[^\\r\\n])*\"", RegexOptions.Compiled);
        private static readonly Regex SingleQuoteRegex = new Regex("'(?:[^'\\\\\\r\\n]|\\\\[^\\r\\n])*'", RegexOptions.Compiled);
        private static readonly Regex FStringRegex = new Regex("(?<![a-zA-Z0-9_])[fFrRbBuU]{1,2}\"(?:[^\"\\\\\\r\\n]|\\\\[^\\r\\n])*\"", RegexOptions.Compiled);
        private static readonly Regex FSingleRegex = new Regex("(?<![a-zA-Z0-9_])[fFrRbBuU]{1,2}'(?:[^'\\\\\\r\\n]|\\\\[^\\r\\n])*'", RegexOptions.Compiled);
        private static readonly Regex CommentRegex = new Regex("#[^\\r\\n]*", RegexOptions.Compiled);

        public List<SymbolError> Analyze(string code)
        {
            if (string.IsNullOrEmpty(code) || code.Length > 50000)
                return new List<SymbolError>();

            var masked = MaskStringsAndComments(code);
            var defined = CollectDefinitions(code, masked);
            var errors = FindUndefinedReferences(code, masked, defined);
            if (datasetColumns.Count > 0)
                errors.AddRange(FindColumnErrors(code, masked, defined));
            return errors;
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
                    var fullFromMatch = Regex.Match(cline, @"\bfrom\s+(\S+)\s+import\s+(.+)");
                    if (fullFromMatch.Success)
                    {
                        string fromModule = fullFromMatch.Groups[1].Value;
                        string importList = fullFromMatch.Groups[2].Value.Trim();

                        if (importList == "*")
                        {
                            ResolveStarImport(fromModule, defined);
                        }
                        else
                        {
                            foreach (string part in importList.Split(','))
                            {
                                string item = part.Trim();
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

        private void ResolveStarImport(string moduleName, HashSet<string> defined)
        {
            if (moduleSymbols.ContainsKey(moduleName))
            {
                foreach (var sym in moduleSymbols[moduleName])
                    defined.Add(sym);
                return;
            }

            string fileContent = null;

            if (fileContentResolver != null)
            {
                fileContent = fileContentResolver(moduleName);
                if (fileContent == null && moduleName.Contains("."))
                {
                    string leafName = moduleName.Substring(moduleName.LastIndexOf('.') + 1);
                    fileContent = fileContentResolver(leafName);
                }
            }

            if (fileContent == null && ScriptsDirectory != null)
            {
                string modulePath = moduleName.Replace('.', Path.DirectorySeparatorChar);
                string filePath = Path.Combine(ScriptsDirectory, modulePath + ".py");
                if (File.Exists(filePath))
                {
                    try { fileContent = File.ReadAllText(filePath); }
                    catch { }
                }
                else
                {
                    string pkgInit = Path.Combine(ScriptsDirectory, modulePath, "__init__.py");
                    if (File.Exists(pkgInit))
                    {
                        try { fileContent = File.ReadAllText(pkgInit); }
                        catch { }
                    }
                }
            }

            if (fileContent != null)
            {
                var exportedNames = ExtractTopLevelNames(fileContent);
                moduleSymbols[moduleName] = exportedNames;
                foreach (var name in exportedNames)
                    defined.Add(name);
            }
        }

        private HashSet<string> ExtractTopLevelNames(string code)
        {
            var allExport = ParseDunderAll(code);
            if (allExport != null)
                return allExport;

            var names = new HashSet<string>();
            string[] lines = code.Split('\n');
            foreach (string rawLine in lines)
            {
                string trimmed = rawLine.TrimStart();
                if (trimmed.Length == 0 || trimmed[0] == '#') continue;

                if (rawLine.Length > 0 && rawLine[0] != ' ' && rawLine[0] != '\t')
                {
                    if (trimmed.StartsWith("def "))
                    {
                        var m = Regex.Match(trimmed, @"^def\s+(\w+)");
                        if (m.Success && !m.Groups[1].Value.StartsWith("_"))
                            names.Add(m.Groups[1].Value);
                    }
                    else if (trimmed.StartsWith("class "))
                    {
                        var m = Regex.Match(trimmed, @"^class\s+(\w+)");
                        if (m.Success && !m.Groups[1].Value.StartsWith("_"))
                            names.Add(m.Groups[1].Value);
                    }
                    else
                    {
                        var m = Regex.Match(trimmed, @"^([a-zA-Z_]\w*)\s*=");
                        if (m.Success && !m.Groups[1].Value.StartsWith("_"))
                            names.Add(m.Groups[1].Value);
                    }
                }
            }
            return names;
        }

        private HashSet<string> ParseDunderAll(string code)
        {
            var match = Regex.Match(code, @"^__all__\s*=\s*\[([^\]]*)\]", RegexOptions.Multiline);
            if (!match.Success)
                return null;

            var names = new HashSet<string>();
            foreach (Match nameMatch in Regex.Matches(match.Groups[1].Value, @"['""](\w+)['""]"))
            {
                names.Add(nameMatch.Groups[1].Value);
            }
            return names.Count > 0 ? names : null;
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
                if (knownModules.Contains(name)) continue;
                if (dynamicKnownSymbols.Contains(name)) continue;
                if (magicNames.Contains(name)) continue;
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

        private List<SymbolError> FindColumnErrors(string code, string masked, HashSet<string> defined)
        {
            var errors = new List<SymbolError>();
            var reported = new HashSet<string>();

            var localAliases = new Dictionary<string, string>();
            foreach (var kvp in datasetColumns)
                localAliases[kvp.Key] = kvp.Key;

            string[] maskedLines = masked.Split('\n');
            string[] codeLines = code.Split('\n');
            for (int i = 0; i < maskedLines.Length; i++)
            {
                string cl = i < codeLines.Length ? codeLines[i] : "";

                var fromImport = Regex.Match(cl, @"^\s*from\s+DotNetData\s+import\s+(.+)$");
                if (fromImport.Success)
                {
                    foreach (string part in fromImport.Groups[1].Value.Split(','))
                    {
                        string item = part.Trim();
                        var asMatch = Regex.Match(item, @"^(\w+)\s+as\s+(\w+)$");
                        if (asMatch.Success)
                        {
                            string orig = asMatch.Groups[1].Value;
                            string alias = asMatch.Groups[2].Value;
                            if (datasetColumns.ContainsKey(orig))
                                localAliases[alias] = orig;
                        }
                    }
                }

                var assignMatch = Regex.Match(cl, @"^\s*(\w+)\s*=\s*(\w+)\s*$");
                if (assignMatch.Success)
                {
                    string target = assignMatch.Groups[1].Value;
                    string source = assignMatch.Groups[2].Value;
                    if (localAliases.ContainsKey(source))
                        localAliases[target] = localAliases[source];
                }
            }

            var attrPattern = new Regex(@"\b(\w+)\s*(?:\[[^\]]*\]\s*)?\.(\w+)\b", RegexOptions.Compiled);

            foreach (Match m in attrPattern.Matches(masked))
            {
                if (masked[m.Index] == '\x01') continue;
                for (int i = m.Index; i < m.Index + m.Length && i < masked.Length; i++)
                {
                    if (masked[i] == '\x01') goto nextMatch;
                }

                string varName = m.Groups[1].Value;
                string attrName = m.Groups[2].Value;

                if (!localAliases.ContainsKey(varName)) continue;

                string datasetName = localAliases[varName];
                if (!datasetColumns.ContainsKey(datasetName)) continue;

                var columns = datasetColumns[datasetName];

                if (columns.Contains(attrName)) continue;
                if (knownMembers.Contains(attrName)) continue;
                if (attrName.StartsWith("_")) continue;

                int attrStart = m.Groups[2].Index;
                string key = attrName + ":" + attrStart;
                if (reported.Contains(key)) continue;
                reported.Add(key);

                var suggestion = FindClosestColumn(attrName, columns);
                string msg = "'" + varName + "' has no column '" + attrName + "'";
                if (suggestion != null)
                    msg += ". Did you mean '" + suggestion + "'?";

                errors.Add(new SymbolError
                {
                    StartIndex = attrStart,
                    Length = attrName.Length,
                    Name = attrName,
                    CustomMessage = msg
                });

                nextMatch:;
            }

            return errors;
        }

        private string FindClosestColumn(string input, HashSet<string> columns)
        {
            string best = null;
            int bestDist = int.MaxValue;
            string inputLower = input.ToLowerInvariant();

            foreach (var col in columns)
            {
                string colLower = col.ToLowerInvariant();
                if (colLower.Contains(inputLower) || inputLower.Contains(colLower))
                    return col;
            }

            foreach (var col in columns)
            {
                int dist = LevenshteinDistance(input.ToLowerInvariant(), col.ToLowerInvariant());
                if (dist < bestDist && dist <= Math.Max(input.Length, col.Length) / 2)
                {
                    bestDist = dist;
                    best = col;
                }
            }

            return best;
        }

        private static int LevenshteinDistance(string a, string b)
        {
            int[,] d = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) d[0, j] = j;
            for (int i = 1; i <= a.Length; i++)
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            return d[a.Length, b.Length];
        }
    }
}
