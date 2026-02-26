using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CodeEditor;

namespace RJLG.IntelliSEM.UI.Controls.PythonDataScience
{
    public class DataSciencePythonCompletionProvider : ICompletionProvider
    {
        private static readonly string[] PythonKeywords = {
            "False", "None", "True", "and", "as", "assert", "async", "await",
            "break", "class", "continue", "def", "del", "elif", "else", "except",
            "finally", "for", "from", "global", "if", "import", "in", "is",
            "lambda", "nonlocal", "not", "or", "pass", "raise", "return",
            "try", "while", "with", "yield"
        };

        private static readonly string[] PythonBuiltins = {
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

        private Dictionary<string, ModuleIntrospection> _moduleData = new Dictionary<string, ModuleIntrospection>();
        private Dictionary<string, string> _importAliases = new Dictionary<string, string>();
        private List<string> _moduleTopLevelItems = new List<string>();

        private List<string> _dynamicSymbols = new List<string>();
        private Dictionary<string, List<string>> _datasetColumns = new Dictionary<string, List<string>>();
        private Dictionary<string, PythonClassInfo> _registeredClasses = new Dictionary<string, PythonClassInfo>();
        private Dictionary<string, ContextVariable> _contextVariables = new Dictionary<string, ContextVariable>();

        public void SetModuleCompletions(Dictionary<string, ModuleIntrospection> moduleData)
        {
            _moduleData = new Dictionary<string, ModuleIntrospection>(moduleData);
            RebuildModuleTopLevelItems();
        }

        public void SetImportAliases(Dictionary<string, string> aliases)
        {
            _importAliases = new Dictionary<string, string>(aliases);
            if (_moduleData.Count > 0)
                RebuildModuleTopLevelItems();
        }

        private void RebuildModuleTopLevelItems()
        {
            var items = new List<string>();
            foreach (var kvp in _moduleData)
            {
                string moduleName = kvp.Key;
                string alias = null;
                foreach (var a in _importAliases)
                {
                    if (a.Value == moduleName)
                    {
                        alias = a.Key;
                        break;
                    }
                }

                string prefix = alias ?? moduleName;
                foreach (var f in kvp.Value.Functions)
                    items.Add(prefix + "." + f);
                foreach (var c in kvp.Value.Constants)
                    items.Add(prefix + "." + c);
                foreach (var cls in kvp.Value.Classes.Keys)
                    items.Add(prefix + "." + cls);
            }
            items.Sort(StringComparer.OrdinalIgnoreCase);
            _moduleTopLevelItems = items;
        }

        public void SetDynamicSymbols(IEnumerable<string> symbols)
        {
            _dynamicSymbols = new List<string>(symbols);
        }

        public void SetDataSources(Dictionary<string, List<string>> datasetColumns)
        {
            _datasetColumns = new Dictionary<string, List<string>>(datasetColumns);
        }

        public void SetRegisteredClasses(Dictionary<string, PythonClassInfo> classes)
        {
            _registeredClasses = new Dictionary<string, PythonClassInfo>(classes);
        }

        public void SetContextVariables(Dictionary<string, ContextVariable> variables)
        {
            _contextVariables = new Dictionary<string, ContextVariable>(variables);
        }

        public List<CompletionItem> GetCompletions(string text, TextPosition caret, string partialWord)
        {
            if (caret.Line < 0) return new List<CompletionItem>();

            int pos = GetAbsolutePosition(text, caret);
            if (pos <= 0 || pos > text.Length) return new List<CompletionItem>();

            int wordStart = pos - 1;
            while (wordStart >= 0 && (char.IsLetterOrDigit(text[wordStart]) || text[wordStart] == '_' || text[wordStart] == '.'
                || text[wordStart] == '[' || text[wordStart] == ']' || text[wordStart] == ':'))
                wordStart--;
            wordStart++;

            string prefix = text.Substring(wordStart, pos - wordStart);

            string lineText = GetCurrentLine(text, pos);
            var fromImportMatch = Regex.Match(lineText, @"^\s*from\s+DotNetData\s+import\s+(.*)$");
            if (fromImportMatch.Success)
            {
                string afterImport = fromImportMatch.Groups[1].Value;
                string lastToken = "";
                int commaIdx = afterImport.LastIndexOf(',');
                if (commaIdx >= 0)
                    lastToken = afterImport.Substring(commaIdx + 1).Trim();
                else
                    lastToken = afterImport.Trim();

                var importItems = _datasetColumns.Keys
                    .Where(k => k.StartsWith(lastToken, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(k => k)
                    .Select(k => new CompletionItem(k, CompletionItemKind.Module, "dataset"))
                    .ToList();

                if (importItems.Count > 0)
                    return importItems;
            }

            int dotIndex = prefix.LastIndexOf('.');
            if (dotIndex >= 0)
            {
                string objectName = prefix.Substring(0, dotIndex);
                string memberPrefix = prefix.Substring(dotIndex + 1);
                return GetDotCompletions(objectName, memberPrefix, text, pos);
            }

            if (string.IsNullOrEmpty(partialWord) || partialWord.Length < 2)
                return new List<CompletionItem>();

            var items = new List<CompletionItem>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kw in PythonKeywords)
            {
                if (kw.StartsWith(partialWord, StringComparison.OrdinalIgnoreCase) && seen.Add(kw))
                    items.Add(new CompletionItem(kw, CompletionItemKind.Keyword, "keyword"));
            }

            foreach (var b in PythonBuiltins)
            {
                if (b.StartsWith(partialWord, StringComparison.OrdinalIgnoreCase) && seen.Add(b))
                    items.Add(new CompletionItem(b, CompletionItemKind.Function, "builtin"));
            }

            foreach (var item in _moduleTopLevelItems)
            {
                if (item.StartsWith(partialWord, StringComparison.OrdinalIgnoreCase) && seen.Add(item))
                    items.Add(new CompletionItem(item, CompletionItemKind.Function, "library"));
            }

            foreach (var sym in _dynamicSymbols)
            {
                if (sym.StartsWith(partialWord, StringComparison.OrdinalIgnoreCase) && seen.Add(sym))
                    items.Add(new CompletionItem(sym, CompletionItemKind.Variable, "symbol"));
            }

            foreach (var cv in _contextVariables.Values)
            {
                if (cv.Name.StartsWith(partialWord, StringComparison.OrdinalIgnoreCase) && seen.Add(cv.Name))
                    items.Add(new CompletionItem(cv.Name, CompletionItemKind.Variable, cv.TypeDescription ?? "context variable"));
            }

            foreach (var ds in _datasetColumns.Keys)
            {
                if (ds.StartsWith(partialWord, StringComparison.OrdinalIgnoreCase) && seen.Add(ds))
                    items.Add(new CompletionItem(ds, CompletionItemKind.Variable, "dataset"));
            }

            foreach (var rc in _registeredClasses.Keys)
            {
                if (rc.StartsWith(partialWord, StringComparison.OrdinalIgnoreCase) && seen.Add(rc))
                    items.Add(new CompletionItem(rc, CompletionItemKind.Type, _registeredClasses[rc].Description ?? "registered class"));
            }

            return items.Take(15).ToList();
        }

        private List<CompletionItem> GetDotCompletions(string objectName, string memberPrefix, string code, int cursorPos)
        {
            var members = new List<string>();

            string baseName = objectName;
            bool isRowAccess = false;
            var bracketMatch = BracketIndexRegex.Match(objectName);
            if (bracketMatch.Success)
            {
                baseName = bracketMatch.Groups[1].Value;
                isRowAccess = true;
            }

            if (objectName == "DotNetData")
            {
                members.AddRange(_datasetColumns.Keys);
            }
            else if (isRowAccess && _datasetColumns.ContainsKey(baseName))
            {
                members.AddRange(_datasetColumns[baseName]);
            }
            else if (_datasetColumns.ContainsKey(objectName))
            {
                members.AddRange(_datasetColumns[objectName]);
                var dfMembers = ResolveClassMembers("DataFrame");
                if (dfMembers.Count > 0)
                    members.AddRange(dfMembers);
            }
            else if (objectName == "self")
            {
                var classMembers = ExtractClassMembersForSelf(code, cursorPos);
                members.AddRange(classMembers);
            }
            else
            {
                var moduleMembers = ResolveModuleMembers(objectName);
                if (moduleMembers.Count > 0)
                {
                    members.AddRange(moduleMembers);
                }
                else
                {
                    var classInfo = ExtractClassMembers(code);
                    if (classInfo.ContainsKey(objectName))
                    {
                        members.AddRange(classInfo[objectName]);
                    }
                    else
                    {
                        string varType = FindVariableType(objectName, code);
                        if (varType != null)
                        {
                            var typeMembers = ResolveClassMembers(varType);
                            if (typeMembers.Count > 0)
                                members.AddRange(typeMembers);
                            else if (classInfo.ContainsKey(varType))
                                members.AddRange(classInfo[varType]);
                        }
                        else
                        {
                            var dfMembers = ResolveClassMembers("DataFrame");
                            if (dfMembers.Count > 0)
                                members.AddRange(dfMembers);
                        }
                    }
                }
            }

            var filtered = members
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(m => string.IsNullOrEmpty(memberPrefix) ||
                           m.StartsWith(memberPrefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m, StringComparer.OrdinalIgnoreCase)
                .Take(15)
                .Select(m => new CompletionItem(m, ClassifyMember(m), "member"))
                .ToList();

            return filtered;
        }

        private static CompletionItemKind ClassifyMember(string member)
        {
            if (member.EndsWith("()"))
                return CompletionItemKind.Function;
            return CompletionItemKind.Property;
        }

        private static readonly Regex BracketIndexRegex = new Regex(@"^(\w+)\[.+\]$", RegexOptions.Compiled);

        private List<string> ResolveModuleMembers(string objectName)
        {
            string resolvedModule = null;
            if (_importAliases.ContainsKey(objectName))
                resolvedModule = _importAliases[objectName];
            else if (_moduleData.ContainsKey(objectName))
                resolvedModule = objectName;

            if (resolvedModule != null && _moduleData.ContainsKey(resolvedModule))
            {
                return _moduleData[resolvedModule].GetAllMembers();
            }

            return new List<string>();
        }

        private List<string> ResolveClassMembers(string className)
        {
            foreach (var kvp in _moduleData)
            {
                if (kvp.Value.Classes.ContainsKey(className))
                    return kvp.Value.Classes[className];
            }

            return new List<string>();
        }

        private List<string> ExtractClassMembersForSelf(string code, int cursorPos)
        {
            var members = new List<string>();
            string className = FindEnclosingClass(code, cursorPos);
            if (className == null) return members;

            var allClassMembers = ExtractClassMembers(code);
            if (allClassMembers.ContainsKey(className))
                members.AddRange(allClassMembers[className]);

            return members;
        }

        private string FindEnclosingClass(string code, int cursorPos)
        {
            int searchPos = Math.Min(cursorPos, code.Length);
            var classMatches = Regex.Matches(code.Substring(0, searchPos), @"^(\s*)class\s+(\w+)", RegexOptions.Multiline);

            string bestClass = null;
            int bestPos = -1;

            foreach (Match m in classMatches)
            {
                string indent = m.Groups[1].Value;
                string name = m.Groups[2].Value;
                int classPos = m.Index;

                if (classPos < searchPos)
                {
                    if (bestPos == -1 || classPos > bestPos)
                    {
                        int classBodyStart = code.IndexOf(':', classPos);
                        if (classBodyStart >= 0 && classBodyStart < searchPos)
                        {
                            int lineStart = code.LastIndexOf('\n', Math.Max(searchPos - 1, 0));
                            if (lineStart < 0) lineStart = 0;
                            string curLine = code.Substring(lineStart, searchPos - lineStart);
                            int curIndent = curLine.Length - curLine.TrimStart().Length;
                            if (curIndent > indent.Length)
                            {
                                bestClass = name;
                                bestPos = classPos;
                            }
                        }
                    }
                }
            }

            return bestClass;
        }

        private Dictionary<string, List<string>> ExtractClassMembers(string code)
        {
            var result = new Dictionary<string, List<string>>();
            var classMatches = Regex.Matches(code, @"^(\s*)class\s+(\w+)[^:]*:", RegexOptions.Multiline);

            foreach (Match cm in classMatches)
            {
                string classIndent = cm.Groups[1].Value;
                string className = cm.Groups[2].Value;
                int classBodyStart = cm.Index + cm.Length;
                int directMemberIndent = classIndent.Length + 4;

                if (!result.ContainsKey(className))
                    result[className] = new List<string>();

                var memberSet = new HashSet<string>();

                string remaining = code.Substring(classBodyStart);
                string[] lines = remaining.Split('\n');
                int skipUntilIndent = -1;

                foreach (string line in lines)
                {
                    if (line.Trim().Length == 0) continue;
                    int lineIndent = line.Length - line.TrimStart().Length;

                    if (lineIndent <= classIndent.Length)
                        break;

                    if (skipUntilIndent >= 0)
                    {
                        if (lineIndent > skipUntilIndent)
                            continue;
                        skipUntilIndent = -1;
                    }

                    string trimmed = line.TrimStart();

                    if (trimmed.StartsWith("class "))
                    {
                        skipUntilIndent = lineIndent;
                        continue;
                    }

                    var defMatch = Regex.Match(trimmed, @"^def\s+(\w+)\s*\(");
                    if (defMatch.Success && lineIndent == directMemberIndent)
                    {
                        string methodName = defMatch.Groups[1].Value;
                        if (!methodName.StartsWith("__") || methodName == "__init__")
                            memberSet.Add(methodName + "()");
                    }

                    var selfAttrMatches = Regex.Matches(trimmed, @"\bself\.(\w+)\s*=");
                    foreach (Match sam in selfAttrMatches)
                    {
                        string attrName = sam.Groups[1].Value;
                        if (!attrName.StartsWith("_"))
                            memberSet.Add(attrName);
                    }

                    if (lineIndent == directMemberIndent)
                    {
                        var classVarMatch = Regex.Match(trimmed, @"^(\w+)\s*=");
                        if (classVarMatch.Success && !trimmed.StartsWith("def ") && !trimmed.StartsWith("class "))
                        {
                            memberSet.Add(classVarMatch.Groups[1].Value);
                        }
                    }
                }

                result[className].AddRange(memberSet);
            }

            return result;
        }

        private string FindVariableType(string varName, string code)
        {
            var pattern = new Regex(@"\b" + Regex.Escape(varName) + @"\s*=\s*(\w+)\s*\(", RegexOptions.Multiline);
            var matches = pattern.Matches(code);
            if (matches.Count > 0)
            {
                string typeName = matches[matches.Count - 1].Groups[1].Value;
                char first = typeName[0];
                if (char.IsUpper(first))
                    return typeName;
            }
            return null;
        }

        private static string GetCurrentLine(string text, int pos)
        {
            int lineStart = pos - 1;
            while (lineStart >= 0 && text[lineStart] != '\n')
                lineStart--;
            lineStart++;
            int lineEnd = text.IndexOf('\n', pos);
            if (lineEnd < 0) lineEnd = text.Length;
            return text.Substring(lineStart, lineEnd - lineStart);
        }

        private static int GetAbsolutePosition(string text, TextPosition pos)
        {
            int line = 0;
            int offset = 0;
            while (line < pos.Line && offset < text.Length)
            {
                if (text[offset] == '\n')
                    line++;
                offset++;
            }
            return offset + pos.Column;
        }
    }
}
