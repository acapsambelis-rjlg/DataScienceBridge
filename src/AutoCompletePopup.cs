using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DataScienceWorkbench
{
    public class AutoCompletePopup : IDisposable
    {
        private readonly RichTextBox editor;
        private readonly ListBox listBox;
        private readonly Form popupForm;
        private int triggerStart = -1;
        private bool isShowing;

        private static readonly List<string> AllItems = new List<string>();

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
                "pd.pivot_table", "pd.crosstab", "pd.melt",
                ".head()", ".tail()", ".info()", ".describe()", ".shape",
                ".columns", ".dtypes", ".index", ".values", ".iloc", ".loc",
                ".groupby()", ".sort_values()", ".sort_index()",
                ".merge()", ".join()", ".concat()",
                ".drop()", ".dropna()", ".fillna()", ".isna()", ".notna()",
                ".apply()", ".map()", ".applymap()",
                ".value_counts()", ".unique()", ".nunique()",
                ".mean()", ".median()", ".std()", ".sum()", ".min()", ".max()", ".count()",
                ".agg()", ".transform()",
                ".reset_index()", ".set_index()", ".rename()",
                ".astype()", ".copy()", ".replace()",
                ".to_csv()", ".to_excel()", ".to_json()",
                ".plot()", ".plot.bar()", ".plot.hist()", ".plot.scatter()",
                ".corr()", ".cov()", ".sample()", ".nlargest()", ".nsmallest()",
                ".str.contains()", ".str.replace()", ".str.split()", ".str.lower()", ".str.upper()",
                ".dt.year", ".dt.month", ".dt.day", ".dt.hour"
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
                BackColor = Color.FromArgb(37, 37, 38),
                ForeColor = Color.FromArgb(212, 212, 212)
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

            popupForm = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                TopMost = true,
                BackColor = Color.FromArgb(37, 37, 38)
            };
            popupForm.Controls.Add(listBox);
            listBox.Dock = DockStyle.Fill;

            popupForm.Deactivate += (s, e) =>
            {
                if (isShowing)
                    BeginInvokeHide();
            };
        }

        private void BeginInvokeHide()
        {
            if (editor.IsHandleCreated)
            {
                editor.BeginInvoke(new Action(() => Hide()));
            }
        }

        private void ListBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();

            string item = listBox.Items[e.Index].ToString();
            bool selected = (e.State & DrawItemState.Selected) != 0;
            Color bgColor = selected ? Color.FromArgb(4, 57, 94) : Color.FromArgb(37, 37, 38);
            Color fgColor = Color.FromArgb(212, 212, 212);

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
            if (editor.SelectionStart <= 0) { Hide(); return; }

            string text = editor.Text;
            int pos = editor.SelectionStart;
            if (pos > text.Length) { Hide(); return; }

            int wordStart = pos - 1;
            while (wordStart >= 0 && (char.IsLetterOrDigit(text[wordStart]) || text[wordStart] == '_' || text[wordStart] == '.'))
                wordStart--;
            wordStart++;

            if (pos - wordStart < 2) { Hide(); return; }

            string prefix = text.Substring(wordStart, pos - wordStart);
            triggerStart = wordStart;

            var matches = AllItems
                .Where(item => item.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                               item.TrimStart('.').StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Take(15)
                .ToList();

            if (matches.Count == 0 || (matches.Count == 1 && matches[0].Equals(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                Hide();
                return;
            }

            Show(matches);
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
            if (isShowing && editor.SelectionLength > 0)
                Hide();
        }

        private void AcceptCompletion()
        {
            if (listBox.SelectedIndex < 0 || !isShowing) return;

            string selected = listBox.SelectedItem.ToString();
            Hide();

            int pos = editor.SelectionStart;
            string text = editor.Text;
            string prefix = text.Substring(triggerStart, pos - triggerStart);

            string insertion = selected;
            if (selected.StartsWith(".") && prefix.Length > 0 && !prefix.StartsWith("."))
            {
                insertion = selected.TrimStart('.');
            }

            editor.Select(triggerStart, pos - triggerStart);
            editor.SelectedText = insertion;
            editor.SelectionStart = triggerStart + insertion.Length;
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
