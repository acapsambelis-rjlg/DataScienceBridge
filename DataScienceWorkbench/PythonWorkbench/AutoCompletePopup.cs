using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace DataScienceWorkbench
{
    internal class NoActivateForm : Form
    {
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
                return cp;
            }
        }
    }

    public class AutoCompletePopup : IDisposable
    {
        private readonly RichTextBox editor;
        private readonly ListBox listBox;
        private readonly NoActivateForm popupForm;
        private int triggerStart = -1;
        private bool isShowing;

        private static readonly List<string> AllItems = new List<string>();
        private List<string> dynamicSymbols = new List<string>();

        public void SetDynamicSymbols(IEnumerable<string> symbols)
        {
            dynamicSymbols = new List<string>(symbols);
        }

        private static readonly List<string> DataFrameMethods = new List<string> {
            "head()", "tail()", "info()", "describe()", "shape",
            "columns", "dtypes", "index", "values", "iloc", "loc",
            "groupby()", "sort_values()", "sort_index()",
            "merge()", "join()",
            "drop()", "dropna()", "fillna()", "isna()", "notna()",
            "apply()", "map()", "applymap()",
            "value_counts()", "unique()", "nunique()",
            "mean()", "median()", "std()", "sum()", "min()", "max()", "count()",
            "agg()", "transform()",
            "reset_index()", "set_index()", "rename()",
            "astype()", "copy()", "replace()",
            "to_csv()", "to_excel()", "to_json()",
            "plot()", "plot.bar()", "plot.hist()", "plot.scatter()",
            "corr()", "cov()", "sample()", "nlargest()", "nsmallest()",
            "str.contains()", "str.replace()", "str.split()", "str.lower()", "str.upper()",
            "dt.year", "dt.month", "dt.day", "dt.hour",
            "df", "query()", "isin()", "between()", "clip()",
            "pivot_table()", "melt()", "stack()", "unstack()",
            "rolling()", "expanding()", "shift()", "diff()", "pct_change()",
            "nrows", "select_dtypes()", "memory_usage()"
        };

        private Dictionary<string, List<string>> DatasetColumns = new Dictionary<string, List<string>>();

        public void SetDatasetColumns(Dictionary<string, List<string>> columns)
        {
            DatasetColumns = new Dictionary<string, List<string>>(columns);
        }

        static AutoCompletePopup()
        {
            var keywords = new[] {
                "False", "None", "True", "and", "as", "assert", "async", "await",
                "break", "class", "continue", "def", "del", "elif", "else", "except",
                "finally", "for", "from", "global", "if", "import", "in", "is",
                "lambda", "nonlocal", "not", "or", "pass", "raise", "return",
                "try", "while", "with", "yield"
            };

            var builtins = new[] {
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

            var pandasMethods = new[] {
                "pd.read_csv", "pd.DataFrame", "pd.Series", "pd.concat", "pd.merge",
                "pd.to_datetime", "pd.to_numeric", "pd.get_dummies", "pd.cut", "pd.qcut",
                "pd.pivot_table", "pd.crosstab", "pd.melt"
            };

            var numpyMethods = new[] {
                "np.array", "np.zeros", "np.ones", "np.arange", "np.linspace",
                "np.reshape", "np.concatenate", "np.stack", "np.split",
                "np.mean", "np.median", "np.std", "np.var", "np.sum",
                "np.min", "np.max", "np.argmin", "np.argmax",
                "np.dot", "np.matmul", "np.transpose",
                "np.random.rand", "np.random.randn", "np.random.randint",
                "np.where", "np.unique", "np.sort", "np.argsort",
                "np.sqrt", "np.exp", "np.log", "np.log2", "np.log10",
                "np.abs", "np.round", "np.ceil", "np.floor",
                "np.nan", "np.inf", "np.isnan", "np.isinf",
                "np.corrcoef", "np.histogram", "np.percentile"
            };

            var matplotlibMethods = new[] {
                "plt.plot", "plt.scatter", "plt.bar", "plt.barh",
                "plt.hist", "plt.boxplot", "plt.pie",
                "plt.figure", "plt.subplot", "plt.subplots",
                "plt.xlabel", "plt.ylabel", "plt.title", "plt.legend",
                "plt.xlim", "plt.ylim", "plt.grid",
                "plt.show", "plt.savefig", "plt.tight_layout",
                "plt.colorbar", "plt.imshow", "plt.contour",
                "plt.annotate", "plt.text", "plt.axhline", "plt.axvline"
            };

            AllItems.AddRange(keywords);
            AllItems.AddRange(builtins);
            AllItems.AddRange(pandasMethods);
            AllItems.AddRange(numpyMethods);
            AllItems.AddRange(matplotlibMethods);

            AllItems.Sort(StringComparer.OrdinalIgnoreCase);
        }

        public AutoCompletePopup(RichTextBox editor)
        {
            this.editor = editor;

            listBox = new ListBox
            {
                Font = editor.Font,
                BorderStyle = BorderStyle.FixedSingle,
                IntegralHeight = false,
                BackColor = Color.FromArgb(255, 255, 255),
                ForeColor = Color.FromArgb(0, 0, 0)
            };

            listBox.DrawMode = DrawMode.OwnerDrawFixed;
            listBox.ItemHeight = (int)(editor.Font.GetHeight() + 4);
            listBox.DrawItem += ListBox_DrawItem;

            listBox.DoubleClick += (s, e) => AcceptCompletion();
            listBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    AcceptCompletion();
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    Hide();
                }
            };

            popupForm = new NoActivateForm
            {
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                TopMost = true,
                BackColor = Color.FromArgb(255, 255, 255)
            };
            popupForm.Controls.Add(listBox);
            listBox.Dock = DockStyle.Fill;
        }

        private void ListBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();

            string item = listBox.Items[e.Index].ToString();
            bool selected = (e.State & DrawItemState.Selected) != 0;
            Color bgColor = selected ? Color.FromArgb(0, 120, 215) : Color.FromArgb(255, 255, 255);
            Color fgColor = selected ? Color.White : Color.FromArgb(0, 0, 0);

            using (var bg = new SolidBrush(bgColor))
                e.Graphics.FillRectangle(bg, e.Bounds);

            using (var fg = new SolidBrush(fgColor))
                e.Graphics.DrawString(item, listBox.Font, fg, e.Bounds.X + 4, e.Bounds.Y + 1);
        }

        public bool HandleKeyDown(Keys keyCode, Keys modifiers)
        {
            if (!isShowing) return false;

            if (keyCode == Keys.Up || keyCode == Keys.Down)
            {
                if (keyCode == Keys.Up && listBox.SelectedIndex > 0)
                    listBox.SelectedIndex--;
                else if (keyCode == Keys.Down && listBox.SelectedIndex < listBox.Items.Count - 1)
                    listBox.SelectedIndex++;
                return true;
            }

            if (keyCode == Keys.Enter || keyCode == Keys.Tab)
            {
                AcceptCompletion();
                return true;
            }

            if (keyCode == Keys.Escape)
            {
                Hide();
                return true;
            }

            return false;
        }

        public void OnTextChanged()
        {
            string text = editor.Text;
            int pos = editor.SelectionStart;
            if (pos <= 0 || pos > text.Length) { Hide(); return; }

            char lastChar = text[pos - 1];
            if (!char.IsLetterOrDigit(lastChar) && lastChar != '_' && lastChar != '.')
            {
                Hide();
                return;
            }

            int wordStart = pos - 1;
            while (wordStart >= 0 && (char.IsLetterOrDigit(text[wordStart]) || text[wordStart] == '_' || text[wordStart] == '.'))
                wordStart--;
            wordStart++;

            string prefix = text.Substring(wordStart, pos - wordStart);
            triggerStart = wordStart;

            List<string> matches;
            int dotIndex = prefix.LastIndexOf('.');
            if (dotIndex >= 0)
            {
                string objectName = prefix.Substring(0, dotIndex);
                string memberPrefix = prefix.Substring(dotIndex + 1);
                matches = GetDotCompletions(objectName, memberPrefix, text);
            }
            else
            {
                if (prefix.Length < 2) { Hide(); return; }
                matches = AllItems.Concat(dynamicSymbols)
                    .Where(item => item.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .Distinct()
                    .Take(15)
                    .ToList();
            }

            if (matches.Count == 0 || (matches.Count == 1 && matches[0].Equals(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                Hide();
                return;
            }

            Show(matches);
        }

        private List<string> GetDotCompletions(string objectName, string memberPrefix, string code)
        {
            var members = new List<string>();

            if (DatasetColumns.ContainsKey(objectName))
            {
                members.AddRange(DatasetColumns[objectName]);
                members.AddRange(DataFrameMethods);
            }
            else if (objectName == "self")
            {
                var classMembers = ExtractClassMembersForSelf(code, editor.SelectionStart);
                members.AddRange(classMembers);
            }
            else
            {
                var staticItems = AllItems
                    .Where(item => item.StartsWith(objectName + ".", StringComparison.OrdinalIgnoreCase))
                    .Select(item => item.Substring(objectName.Length + 1))
                    .ToList();

                if (staticItems.Count > 0)
                {
                    members.AddRange(staticItems);
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
                        if (varType != null && classInfo.ContainsKey(varType))
                        {
                            members.AddRange(classInfo[varType]);
                        }
                        else
                        {
                            members.AddRange(DataFrameMethods);
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
                .ToList();

            return filtered;
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

        private void Show(List<string> items)
        {
            listBox.Items.Clear();
            foreach (var item in items)
                listBox.Items.Add(item);

            if (listBox.Items.Count > 0)
                listBox.SelectedIndex = 0;

            int visibleItems = Math.Min(items.Count, 8);
            int popupHeight = visibleItems * listBox.ItemHeight + 4;
            int popupWidth = 280;

            Point caretPos = editor.GetPositionFromCharIndex(editor.SelectionStart);
            Point screenPos = editor.PointToScreen(caretPos);
            screenPos.Y += editor.Font.Height + 2;

            popupForm.Size = new Size(popupWidth, popupHeight);
            popupForm.Location = screenPos;

            if (!isShowing)
            {
                popupForm.Show();
                isShowing = true;
            }

            editor.Focus();
        }

        public void Hide()
        {
            if (isShowing)
            {
                isShowing = false;
                popupForm.Hide();
            }
        }

        public bool IsShowing { get { return isShowing; } }

        public void OnSelectionChanged()
        {
            if (!isShowing) return;
            if (editor.SelectionLength > 0)
            {
                Hide();
                return;
            }
            int pos = editor.SelectionStart;
            if (pos < triggerStart)
            {
                Hide();
            }
        }

        private void AcceptCompletion()
        {
            if (listBox.SelectedIndex < 0 || !isShowing) return;

            string selected = listBox.SelectedItem.ToString();
            Hide();

            int pos = editor.SelectionStart;
            string text = editor.Text;
            string prefix = text.Substring(triggerStart, pos - triggerStart);

            int dotIdx = prefix.LastIndexOf('.');
            if (dotIdx >= 0)
            {
                int memberStart = triggerStart + dotIdx + 1;
                editor.Select(memberStart, pos - memberStart);
                editor.SelectedText = selected;
                editor.SelectionStart = memberStart + selected.Length;
            }
            else
            {
                string insertion = selected;
                if (selected.StartsWith(".") && prefix.Length > 0 && !prefix.StartsWith("."))
                {
                    insertion = selected.TrimStart('.');
                }

                editor.Select(triggerStart, pos - triggerStart);
                editor.SelectedText = insertion;
                editor.SelectionStart = triggerStart + insertion.Length;
            }

            editor.SelectionLength = 0;
            editor.Focus();
        }

        public void Dispose()
        {
            Hide();
            popupForm.Dispose();
            listBox.Dispose();
        }
    }
}
