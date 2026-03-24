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

        private List<string> _dynamicSymbols = new List<string>();
        private Dictionary<string, List<string>> _datasetColumns = new Dictionary<string, List<string>>();
        private Dictionary<string, Dictionary<string, List<string>>> _subObjectProperties = new Dictionary<string, Dictionary<string, List<string>>>();
        private Dictionary<string, PythonClassInfo> _registeredClasses = new Dictionary<string, PythonClassInfo>();
        private Dictionary<string, ContextVariable> _contextVariables = new Dictionary<string, ContextVariable>();
        private List<string> _helperFunctions = new List<string>();

        public void SetModuleCompletions(Dictionary<string, ModuleIntrospection> moduleData)
        {
            _moduleData = new Dictionary<string, ModuleIntrospection>(moduleData);
        }

        public void SetImportAliases(Dictionary<string, string> aliases)
        {
            _importAliases = new Dictionary<string, string>(aliases);
        }

        public void SetDynamicSymbols(IEnumerable<string> symbols)
        {
            _dynamicSymbols = new List<string>(symbols);
        }

        public void SetHelperFunctions(IEnumerable<string> names)
        {
            _helperFunctions = new List<string>(names);
        }

        public void SetDataSources(Dictionary<string, List<string>> datasetColumns)
        {
            _datasetColumns = new Dictionary<string, List<string>>(datasetColumns);
        }

        public void SetSubObjectProperties(Dictionary<string, Dictionary<string, List<string>>> subObjProps)
        {
            _subObjectProperties = subObjProps ?? new Dictionary<string, Dictionary<string, List<string>>>();
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
                || text[wordStart] == '[' || text[wordStart] == ']' || text[wordStart] == ':'
                || text[wordStart] == '\'' || text[wordStart] == '"'))
                wordStart--;
            wordStart++;

            string prefix = text.Substring(wordStart, pos - wordStart);
#if DEBUG
            if (prefix.Contains("TierSpending") || prefix.Contains("Dict"))
                System.Diagnostics.Debug.WriteLine("[Completion] prefix='" + prefix + "'");
#endif

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

                importItems.AddRange(_helperFunctions
                    .Where(k => k.StartsWith(lastToken, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(k => k)
                    .Select(k => new CompletionItem(k, CompletionItemKind.Function, "helper")));

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

            foreach (var alias in _importAliases.Keys)
            {
                if (alias.StartsWith(partialWord, StringComparison.OrdinalIgnoreCase) && seen.Add(alias))
                    items.Add(new CompletionItem(alias, CompletionItemKind.Module, "module"));
            }

            foreach (var modName in _moduleData.Keys)
            {
                if (!_importAliases.ContainsValue(modName) &&
                    modName.StartsWith(partialWord, StringComparison.OrdinalIgnoreCase) && seen.Add(modName))
                    items.Add(new CompletionItem(modName, CompletionItemKind.Module, "module"));
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
            var bracketMatch = RowAccessPattern.Match(objectName);
            if (bracketMatch.Success)
            {
                baseName = bracketMatch.Groups[1].Value;
                isRowAccess = true;
            }

            var subObjMembers = TryResolveSubObjectMembers(objectName, baseName, isRowAccess);
            if (subObjMembers != null)
            {
                members.AddRange(subObjMembers);
            }
            else if (objectName == "DotNetData")
            {
                members.AddRange(_datasetColumns.Keys);
                members.AddRange(_helperFunctions);
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

        private List<string> TryResolveSubObjectMembers(string objectName, string baseName, bool isRowAccess)
        {
            foreach (var dsKvp in _subObjectProperties)
            {
                string dsName = dsKvp.Key;
                var subObjs = dsKvp.Value;

                string subPath = null;

                if (isRowAccess && baseName == dsName)
                {
                    int dotIdx = objectName.IndexOf('.');
                    if (dotIdx >= 0)
                        subPath = objectName.Substring(dotIdx + 1);
                }
                else
                {
                    var bracketDsMatch = RowAccessDotChainPattern.Match(objectName);
                    if (bracketDsMatch.Success)
                    {
                        string leadDs = bracketDsMatch.Groups[1].Value;
                        if (leadDs != dsName) continue;
                        string trail = bracketDsMatch.Groups[2].Value;
                        if (trail.Length > 0)
                            subPath = trail.Substring(1);
                    }
                    else
                    {
                        string normalizedObj = BracketStripPattern.Replace(objectName, "");
                        string bestMatch = null;
                        foreach (var soPath in subObjs.Keys)
                        {
                            string suffix = "." + soPath;
                            if (objectName.EndsWith(suffix) || normalizedObj.EndsWith(suffix))
                            {
                                if (bestMatch == null || soPath.Length > bestMatch.Length)
                                    bestMatch = soPath;
                            }
                        }
                        subPath = bestMatch;
                    }
                }

                if (subPath != null)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("[Completion] TryResolve: dsName='" + dsName + "', subPath='" + subPath + "', containsKey=" + subObjs.ContainsKey(subPath));
#endif
                    if (subObjs.ContainsKey(subPath))
                        return new List<string>(subObjs[subPath]);

                    string normalized = BracketStripPattern.Replace(subPath, "");
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("[Completion] TryResolve normalized: '" + normalized + "', containsKey=" + subObjs.ContainsKey(normalized));
#endif
                    if (normalized != subPath && subObjs.ContainsKey(normalized))
                        return new List<string>(subObjs[normalized]);
                }
            }
            return null;
        }

        private static readonly Regex RowAccessDotChainPattern = new Regex(@"^(\w+)\[.+?\](\..*)?$", RegexOptions.Compiled);
        private static readonly Regex BracketStripPattern = new Regex(@"\[.*?\]", RegexOptions.Compiled);

        private static readonly Regex RowAccessPattern = new Regex(@"^(\w+)\[.+\]$", RegexOptions.Compiled);

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
