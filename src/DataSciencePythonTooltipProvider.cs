using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using CodeEditor;

namespace RJLG.IntelliSEM.UI.Controls.PythonDataScience
{
    public class DataSciencePythonTooltipProvider : TooltipProviderBase
    {
        public void LoadFromEmbeddedResources()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            foreach (var resName in assembly.GetManifestResourceNames())
            {
                if (!resName.EndsWith(".py")) continue;
                string code;
                using (var stream = assembly.GetManifestResourceStream(resName))
                using (var reader = new StreamReader(stream))
                    code = reader.ReadToEnd();

                ParsePythonSource(code);
            }
        }

        public void LoadFromPythonSource(string code)
        {
            ParsePythonSource(code);
        }

        private void ParsePythonSource(string code)
        {
            var defPattern = new Regex(@"^def\s+([a-zA-Z_]\w*)\s*\(([^)]*)\)\s*:", RegexOptions.Multiline);

            foreach (Match m in defPattern.Matches(code))
            {
                string funcName = m.Groups[1].Value;
                if (funcName.StartsWith("_")) continue;

                string args = m.Groups[2].Value.Trim();
                string signature = funcName + "(" + args + ")";

                string docstring = ExtractDocstring(code, m.Index + m.Length);

                RegisterTooltip(funcName, signature, docstring ?? "");
            }

            var classPattern = new Regex(@"^class\s+([a-zA-Z_]\w*)\s*(?:\([^)]*\))?\s*:", RegexOptions.Multiline);
            foreach (Match m in classPattern.Matches(code))
            {
                string className = m.Groups[1].Value;
                if (className.StartsWith("_")) continue;

                string docstring = ExtractDocstring(code, m.Index + m.Length);

                string initSig = null;
                var initPattern = new Regex(@"^\s+def\s+__init__\s*\(self\s*,?\s*([^)]*)\)\s*:", RegexOptions.Multiline);
                string afterClass = ExtractClassBody(code, m.Index + m.Length);
                var initMatch = initPattern.Match(afterClass);
                if (initMatch.Success)
                {
                    string initArgs = initMatch.Groups[1].Value.Trim();
                    initSig = className + "(" + initArgs + ")";
                    string initDoc = ExtractDocstring(afterClass, initMatch.Index + initMatch.Length);
                    if (!string.IsNullOrEmpty(initDoc) && string.IsNullOrEmpty(docstring))
                        docstring = initDoc;
                }

                RegisterTooltip(className, new TooltipInfo(
                    initSig ?? className + "()",
                    docstring ?? ""
                ));
            }
        }

        private string ExtractClassBody(string code, int afterClassColon)
        {
            var topLevelDef = new Regex(@"^(?:class\s|def\s|[a-zA-Z_]\w*\s*=)", RegexOptions.Multiline);
            int searchStart = afterClassColon;
            while (searchStart < code.Length && code[searchStart] != '\n')
                searchStart++;
            if (searchStart >= code.Length) return code.Substring(afterClassColon);
            searchStart++;

            var nextTop = topLevelDef.Match(code, searchStart);
            int endPos = nextTop.Success ? nextTop.Index : code.Length;
            return code.Substring(afterClassColon, endPos - afterClassColon);
        }

        private string ExtractDocstring(string code, int afterDefColon)
        {
            int pos = afterDefColon;
            while (pos < code.Length && (code[pos] == ' ' || code[pos] == '\t' || code[pos] == '\r' || code[pos] == '\n'))
                pos++;

            if (pos >= code.Length - 2) return null;

            string tripleQuote = null;
            if (code.Substring(pos).StartsWith("\"\"\""))
                tripleQuote = "\"\"\"";
            else if (code.Substring(pos).StartsWith("'''"))
                tripleQuote = "'''";

            if (tripleQuote == null) return null;

            int start = pos + 3;
            int end = code.IndexOf(tripleQuote, start);
            if (end < 0) return null;

            string raw = code.Substring(start, end - start);
            var lines = raw.Split('\n');
            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                string trimmed = line.TrimStart();
                if (sb.Length > 0 || trimmed.Length > 0)
                    sb.AppendLine(trimmed);
            }
            return sb.ToString().Trim();
        }

        public void SetModuleTooltips(Dictionary<string, ModuleIntrospection> moduleData,
                                       Dictionary<string, string> importAliases)
        {
            foreach (var kvp in moduleData)
            {
                string moduleName = kvp.Key;
                string alias = null;
                foreach (var a in importAliases)
                {
                    if (a.Value == moduleName)
                    {
                        alias = a.Key;
                        break;
                    }
                }

                string prefix = alias ?? moduleName;
                foreach (var f in kvp.Value.Functions)
                {
                    string qualName = prefix + "." + f;
                    if (!HasTooltip(qualName))
                        RegisterTooltip(qualName, f + "(...)", moduleName + "." + f);
                }
            }
        }
    }
}
