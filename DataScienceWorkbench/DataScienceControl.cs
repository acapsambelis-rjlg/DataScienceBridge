using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DataScienceWorkbench
{
    public partial class DataScienceControl : UserControl
    {
        private PythonRunner pythonRunner;
        private DataGenerator dataGen;
        private List<Product> products;
        private List<Customer> customers;
        private List<Order> orders;
        private List<Employee> employees;
        private List<SensorReading> sensorReadings;
        private List<StockPrice> stockPrices;
        private List<WebEvent> webEvents;

        private string dataExportDir;
        private bool packagesLoaded;
        private PythonSyntaxHighlighter syntaxHighlighter;
        private Timer highlightTimer;
        private bool suppressHighlight;
        private bool textDirty;

        private List<UndoEntry> undoStack = new List<UndoEntry>();
        private List<UndoEntry> redoStack = new List<UndoEntry>();
        private bool isUndoRedoAction;
        private const int MaxUndoLevels = 100;

        private AutoCompletePopup autoComplete;
        private bool suppressAutoComplete;
        private PythonSymbolAnalyzer symbolAnalyzer = new PythonSymbolAnalyzer();

        private static readonly Dictionary<char, char> BracketPairs = new Dictionary<char, char>
        {
            { '(', ')' }, { '[', ']' }, { '{', '}' }, { '"', '"' }, { '\'', '\'' }
        };

        private class UndoEntry
        {
            public string Text;
            public int CursorPos;
        }

        public event EventHandler<string> StatusChanged;

        private static Font ResolveMonoFont(float size)
        {
            string[] preferred = { "Consolas", "DejaVu Sans Mono", "Liberation Mono", "Courier New" };
            foreach (var name in preferred)
            {
                using (var test = new Font(name, size))
                {
                    if (test.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        return new Font(name, size);
                }
            }
            return new Font(System.Drawing.FontFamily.GenericMonospace, size);
        }

        private static Font ResolveUIFont(float size, FontStyle style = FontStyle.Regular)
        {
            string[] preferred = { "Segoe UI", "DejaVu Sans", "Liberation Sans", "Arial" };
            foreach (var name in preferred)
            {
                using (var test = new Font(name, size, style))
                {
                    if (test.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        return new Font(name, size, style);
                }
            }
            return new Font(System.Drawing.FontFamily.GenericSansSerif, size, style);
        }

        private void ResolveRuntimeFonts()
        {
            var monoFont10 = ResolveMonoFont(10f);
            var monoFont9 = ResolveMonoFont(9f);
            var uiFont = ResolveUIFont(9f);
            var uiFontBold = ResolveUIFont(9f, FontStyle.Bold);

            pythonEditor.Font = monoFont10;
            outputBox.Font = monoFont9;
            dataTreeView.Font = monoFont9;
            packageListBox.Font = monoFont9;
            lineNumberPanel.UpdateFont(monoFont9);

            treeLabel.Font = uiFontBold;
            outputLabel.Font = uiFontBold;
            pkgListLabel.Font = uiFontBold;
            dataGrid.Font = uiFont;
        }

        public DataScienceControl()
        {
            InitializeComponent();
            ResolveRuntimeFonts();
            InitializeData();
            SetupSnippetMenu();
            SetupSyntaxHighlighting();
            suppressHighlight = true;
            pythonEditor.Text = GetDefaultScript();
            suppressHighlight = false;
            ResetUndoStack();
            ApplySyntaxHighlighting();
            PopulateDataTree();
            datasetCombo.SelectedIndex = 0;
            ExportAllData();
        }

        private void SetupSyntaxHighlighting()
        {
            syntaxHighlighter = new PythonSyntaxHighlighter();
            lineNumberPanel.AttachEditor(pythonEditor);
            autoComplete = new AutoCompletePopup(pythonEditor);

            highlightTimer = new Timer();
            highlightTimer.Interval = 500;
            highlightTimer.Tick += (s, e) =>
            {
                highlightTimer.Stop();
                if (!textDirty) return;
                textDirty = false;
                ApplySyntaxHighlighting();
                RunLiveSyntaxCheck();
            };

            pythonEditor.TextChanged += (s, e) =>
            {
                if (!suppressHighlight)
                {
                    textDirty = true;
                    highlightTimer.Stop();
                    highlightTimer.Start();

                    if (!isUndoRedoAction)
                    {
                        PushUndo(pythonEditor.Text, pythonEditor.SelectionStart);
                    }

                    if (!suppressAutoComplete)
                    {
                        autoComplete.OnTextChanged();
                    }
                }
            };

            pythonEditor.SelectionChanged += (s, e) =>
            {
                if (!suppressHighlight)
                {
                    pythonEditor.UpdateBracketMatching();
                    pythonEditor.Invalidate();
                    UpdateCursorPositionStatus();
                    autoComplete.OnSelectionChanged();
                }
            };

            pythonEditor.KeyDown += (s, e) =>
            {
                if (autoComplete.IsShowing && autoComplete.HandleKeyDown(e.KeyCode, e.Modifiers))
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }

                if (e.Control && e.KeyCode == Keys.Z)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    PerformUndo();
                }
                else if (e.Control && e.KeyCode == Keys.Y)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    PerformRedo();
                }
                else if (e.KeyCode == Keys.Escape && autoComplete.IsShowing)
                {
                    autoComplete.Hide();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Back && !e.Control && pythonEditor.SelectionLength == 0)
                {
                    if (HandleBracketAutoDelete(false))
                    {
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                }
                else if (e.KeyCode == Keys.Delete && !e.Control && pythonEditor.SelectionLength == 0)
                {
                    if (HandleBracketAutoDelete(true))
                    {
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                }
            };

            pythonEditor.KeyPress += (s, e) =>
            {
                if (HandleBracketAutoClose(e.KeyChar))
                {
                    e.Handled = true;
                    return;
                }

                if (e.KeyChar == '\r')
                {
                    e.Handled = true;
                    HandleAutoIndent();
                }
                else if (e.KeyChar == '\n')
                {
                    e.Handled = true;
                }

                if (e.KeyChar == '\t')
                {
                    e.Handled = true;
                    suppressAutoComplete = true;
                    pythonEditor.SelectedText = "    ";
                    suppressAutoComplete = false;
                }
            };
        }

        private bool HandleBracketAutoClose(char typed)
        {
            string text = pythonEditor.Text;
            int pos = pythonEditor.SelectionStart;

            if (typed == '"' || typed == '\'')
            {
                if (pos < text.Length && text[pos] == typed)
                {
                    pythonEditor.SelectionStart = pos + 1;
                    return true;
                }

                bool atWordChar = pos > 0 && (char.IsLetterOrDigit(text[pos - 1]) || text[pos - 1] == '_');
                if (!atWordChar)
                {
                    suppressAutoComplete = true;
                    int selLen = pythonEditor.SelectionLength;
                    if (selLen > 0)
                    {
                        string selected = pythonEditor.SelectedText;
                        pythonEditor.SelectedText = typed.ToString() + selected + typed.ToString();
                        pythonEditor.SelectionStart = pos + 1;
                        pythonEditor.SelectionLength = selLen;
                    }
                    else
                    {
                        pythonEditor.SelectedText = typed.ToString() + typed.ToString();
                        pythonEditor.SelectionStart = pos + 1;
                    }
                    suppressAutoComplete = false;
                    return true;
                }
                return false;
            }

            if (BracketPairs.ContainsKey(typed) && typed != '"' && typed != '\'')
            {
                char close = BracketPairs[typed];

                if (typed == close && pos < text.Length && text[pos] == typed)
                {
                    pythonEditor.SelectionStart = pos + 1;
                    return true;
                }

                suppressAutoComplete = true;
                int selLen = pythonEditor.SelectionLength;
                if (selLen > 0)
                {
                    string selected = pythonEditor.SelectedText;
                    pythonEditor.SelectedText = typed.ToString() + selected + close.ToString();
                    pythonEditor.SelectionStart = pos + 1;
                    pythonEditor.SelectionLength = selLen;
                }
                else
                {
                    pythonEditor.SelectedText = typed.ToString() + close.ToString();
                    pythonEditor.SelectionStart = pos + 1;
                }
                suppressAutoComplete = false;
                return true;
            }

            bool isClosing = typed == ')' || typed == ']' || typed == '}';
            if (isClosing && pos < text.Length && text[pos] == typed)
            {
                pythonEditor.SelectionStart = pos + 1;
                return true;
            }

            return false;
        }

        private bool HandleBracketAutoDelete(bool isDeleteKey)
        {
            string text = pythonEditor.Text;
            int pos = pythonEditor.SelectionStart;

            int openPos, closePos;
            if (isDeleteKey)
            {
                openPos = pos;
                closePos = pos + 1;
            }
            else
            {
                openPos = pos - 1;
                closePos = pos;
            }

            if (openPos < 0 || closePos >= text.Length) return false;

            char openChar = text[openPos];
            char closeChar = text[closePos];

            if (!BracketPairs.ContainsKey(openChar)) return false;
            if (BracketPairs[openChar] != closeChar) return false;

            PushUndo(text, pos);
            suppressHighlight = true;
            pythonEditor.Select(openPos, 2);
            pythonEditor.SelectedText = "";
            pythonEditor.SelectionStart = openPos;
            suppressHighlight = false;
            ApplySyntaxHighlighting();

            return true;
        }

        private void HandleAutoIndent()
        {
            int pos = pythonEditor.SelectionStart;
            int lineIndex = pythonEditor.GetLineFromCharIndex(pos);
            string currentLine = lineIndex < pythonEditor.Lines.Length ? pythonEditor.Lines[lineIndex] : "";

            int charInLine = pos - pythonEditor.GetFirstCharIndexFromLine(lineIndex);
            string textBeforeCursor = charInLine <= currentLine.Length ? currentLine.Substring(0, charInLine) : currentLine;

            string indent = "";
            foreach (char c in textBeforeCursor)
            {
                if (c == ' ') indent += ' ';
                else break;
            }

            string trimmed = textBeforeCursor.TrimEnd();
            if (trimmed.EndsWith(":"))
            {
                indent += "    ";
            }

            suppressHighlight = true;
            suppressAutoComplete = true;
            PushUndo(pythonEditor.Text, pos);
            pythonEditor.SelectedText = "\n" + indent;
            suppressHighlight = false;
            suppressAutoComplete = false;
            ApplySyntaxHighlighting();
        }

        private void UpdateCursorPositionStatus()
        {
            int pos = pythonEditor.SelectionStart;
            int line = pythonEditor.GetLineFromCharIndex(pos) + 1;
            int firstChar = pythonEditor.GetFirstCharIndexFromLine(line - 1);
            int col = pos - firstChar + 1;
            RaiseStatus("Ln " + line + ", Col " + col);
        }

        private void ResetUndoStack()
        {
            undoStack.Clear();
            redoStack.Clear();
            undoStack.Add(new UndoEntry { Text = pythonEditor.Text, CursorPos = pythonEditor.SelectionStart });
        }

        private void PushUndo(string text, int cursorPos)
        {
            if (undoStack.Count > 0 && undoStack[undoStack.Count - 1].Text == text)
                return;

            undoStack.Add(new UndoEntry { Text = text, CursorPos = cursorPos });
            if (undoStack.Count > MaxUndoLevels)
                undoStack.RemoveAt(0);

            redoStack.Clear();
        }

        private void PerformUndo()
        {
            if (undoStack.Count <= 1) return;

            var current = undoStack[undoStack.Count - 1];
            undoStack.RemoveAt(undoStack.Count - 1);
            redoStack.Add(current);

            var prev = undoStack[undoStack.Count - 1];
            ApplyUndoRedoText(prev);
        }

        private void PerformRedo()
        {
            if (redoStack.Count == 0) return;

            var entry = redoStack[redoStack.Count - 1];
            redoStack.RemoveAt(redoStack.Count - 1);
            undoStack.Add(entry);

            ApplyUndoRedoText(entry);
        }

        private void ApplyUndoRedoText(UndoEntry entry)
        {
            isUndoRedoAction = true;
            suppressHighlight = true;
            try
            {
                pythonEditor.Text = entry.Text;
                pythonEditor.SelectionStart = Math.Min(entry.CursorPos, pythonEditor.Text.Length);
                pythonEditor.SelectionLength = 0;
            }
            finally
            {
                suppressHighlight = false;
                isUndoRedoAction = false;
            }

            textDirty = true;
            highlightTimer.Stop();
            highlightTimer.Start();
        }

        public bool CanUndo { get { return undoStack.Count > 1; } }
        public bool CanRedo { get { return redoStack.Count > 0; } }

        private void ApplySyntaxHighlighting()
        {
            if (pythonEditor.Text.Length > 50000) return;
            try
            {
                syntaxHighlighter.Highlight(pythonEditor);
            }
            catch { }
            RunSymbolAnalysis();
        }

        private void RunSymbolAnalysis()
        {
            try
            {
                string code = pythonEditor.Text;
                if (string.IsNullOrWhiteSpace(code))
                {
                    pythonEditor.ClearSymbolErrors();
                    return;
                }
                var errors = symbolAnalyzer.Analyze(code);
                pythonEditor.SetSymbolErrors(errors);
            }
            catch
            {
                pythonEditor.ClearSymbolErrors();
            }
        }

        private void RunLiveSyntaxCheck()
        {
            string script = pythonEditor.Text;
            if (string.IsNullOrWhiteSpace(script))
            {
                pythonEditor.ClearError();
                RaiseStatus("Ready");
                return;
            }

            try
            {
                var result = pythonRunner.CheckSyntax(script);

                if (result.Success)
                {
                    pythonEditor.ClearError();
                    RaiseStatus("Ready");
                }
                else
                {
                    int errorLine = ParseErrorLine(result.Error);
                    if (errorLine > 0)
                    {
                        pythonEditor.SetErrorLine(errorLine);
                        var errorMsg = result.Error.Trim();
                        var firstLine = errorMsg.Split('\n')[0];
                        RaiseStatus("Line " + errorLine + ": " + firstLine);
                    }
                }
            }
            catch { }
        }

        public void LoadData(
            List<Product> products,
            List<Customer> customers,
            List<Order> orders,
            List<Employee> employees,
            List<SensorReading> sensorReadings,
            List<StockPrice> stockPrices,
            List<WebEvent> webEvents)
        {
            this.products = products;
            this.customers = customers;
            this.orders = orders;
            this.employees = employees;
            this.sensorReadings = sensorReadings;
            this.stockPrices = stockPrices;
            this.webEvents = webEvents;

            ExportAllData();
            PopulateDataTree();
            OnDatasetChanged(null, null);
        }

        public void RunScript()
        {
            OnRunScript(this, EventArgs.Empty);
        }

        public string ScriptText
        {
            get { return pythonEditor.Text; }
            set { pythonEditor.Text = value; ResetUndoStack(); }
        }

        public string OutputText
        {
            get { return outputBox.Text; }
        }

        public void ClearOutput()
        {
            outputBox.Clear();
        }

        public MenuStrip CreateMenuStrip()
        {
            var menuBar = new MenuStrip();

            var fileMenu = new ToolStripMenuItem("File");
            fileMenu.DropDownItems.Add("Open Script...", null, OnOpenScript);
            fileMenu.DropDownItems.Add("Save Script...", null, OnSaveScript);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Re-export Data", null, (s, e) => { ExportAllData(); RaiseStatus("Data re-exported successfully."); });

            var editMenu = new ToolStripMenuItem("&Edit");

            var undoItem = new ToolStripMenuItem("Undo", null, (s, e) => { if (pythonEditor.Focused) PerformUndo(); });
            undoItem.ShortcutKeyDisplayString = "Ctrl+Z";
            editMenu.DropDownItems.Add(undoItem);

            var redoItem = new ToolStripMenuItem("Redo", null, (s, e) => { if (pythonEditor.Focused) PerformRedo(); });
            redoItem.ShortcutKeyDisplayString = "Ctrl+Y";
            editMenu.DropDownItems.Add(redoItem);

            editMenu.DropDownItems.Add(new ToolStripSeparator());

            var cutItem = new ToolStripMenuItem("Cut", null, (s, e) => { if (pythonEditor.Focused) pythonEditor.Cut(); });
            cutItem.ShortcutKeys = Keys.Control | Keys.X;
            editMenu.DropDownItems.Add(cutItem);

            var copyItem = new ToolStripMenuItem("Copy", null, (s, e) => { if (pythonEditor.Focused) pythonEditor.Copy(); });
            copyItem.ShortcutKeys = Keys.Control | Keys.C;
            editMenu.DropDownItems.Add(copyItem);

            var pasteItem = new ToolStripMenuItem("Paste", null, (s, e) => { if (pythonEditor.Focused) pythonEditor.Paste(); });
            pasteItem.ShortcutKeys = Keys.Control | Keys.V;
            editMenu.DropDownItems.Add(pasteItem);

            var deleteItem = new ToolStripMenuItem("Delete", null, (s, e) => { if (pythonEditor.Focused && pythonEditor.SelectionLength > 0) pythonEditor.SelectedText = ""; });
            deleteItem.ShortcutKeyDisplayString = "Del";
            editMenu.DropDownItems.Add(deleteItem);

            editMenu.DropDownItems.Add(new ToolStripSeparator());

            var selectAllItem = new ToolStripMenuItem("Select All", null, (s, e) => { if (pythonEditor.Focused) pythonEditor.SelectAll(); });
            selectAllItem.ShortcutKeys = Keys.Control | Keys.A;
            editMenu.DropDownItems.Add(selectAllItem);

            editMenu.DropDownItems.Add(new ToolStripSeparator());

            var findItem = new ToolStripMenuItem("Find && Replace...", null, (s, e) => ShowFindReplace());
            findItem.ShortcutKeys = Keys.Control | Keys.H;
            editMenu.DropDownItems.Add(findItem);

            editMenu.DropDownItems.Add(new ToolStripSeparator());
            editMenu.DropDownItems.Add("Clear Output", null, (s, e) => outputBox.Clear());

            editMenu.DropDownOpening += (s, e) =>
            {
                undoItem.Enabled = pythonEditor.Focused && CanUndo;
                redoItem.Enabled = pythonEditor.Focused && CanRedo;
                bool hasSelection = pythonEditor.Focused && pythonEditor.SelectionLength > 0;
                cutItem.Enabled = hasSelection;
                copyItem.Enabled = hasSelection;
                deleteItem.Enabled = hasSelection;
            };

            var runMenu = new ToolStripMenuItem("Run");
            runMenu.DropDownItems.Add("Execute Script (F5)", null, OnRunScript);

            var helpMenu = new ToolStripMenuItem("Help");
            helpMenu.DropDownItems.Add("Quick Start Guide", null, OnShowHelp);
            helpMenu.DropDownItems.Add("About", null, (s, e) => MessageBox.Show(
                "Data Science Workbench v1.0\n\n" +
                "A .NET Windows Forms control with\n" +
                "integrated Python scripting for data analysis.\n\n" +
                "Built with Mono + Python 3",
                "About", MessageBoxButtons.OK, MessageBoxIcon.Information));

            menuBar.Items.AddRange(new ToolStripItem[] { fileMenu, editMenu, runMenu, helpMenu });
            return menuBar;
        }

        public bool HandleKeyDown(Keys keyCode)
        {
            if (keyCode == Keys.F5)
            {
                OnRunScript(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        private void InitializeData()
        {
            dataGen = new DataGenerator(42);
            products = dataGen.GenerateProducts(200);
            customers = dataGen.GenerateCustomers(150, products);
            orders = dataGen.GenerateOrders(customers, products, 500);
            employees = dataGen.GenerateEmployees(100);
            sensorReadings = dataGen.GenerateSensorReadings(1000);
            stockPrices = dataGen.GenerateStockPrices(365);
            webEvents = dataGen.GenerateWebEvents(2000);

            pythonRunner = new PythonRunner();
        }

        private void SetupSnippetMenu()
        {
            insertSnippetBtn.DropDownItems.Add("Load CSV Data", null, (s, e) => InsertSnippet(GetLoadDataSnippet()));
            insertSnippetBtn.DropDownItems.Add("Basic Statistics", null, (s, e) => InsertSnippet(GetStatsSnippet()));
            insertSnippetBtn.DropDownItems.Add("Plot Histogram", null, (s, e) => InsertSnippet(GetHistogramSnippet()));
            insertSnippetBtn.DropDownItems.Add("Scatter Plot", null, (s, e) => InsertSnippet(GetScatterSnippet()));
            insertSnippetBtn.DropDownItems.Add("Group By Analysis", null, (s, e) => InsertSnippet(GetGroupBySnippet()));
            insertSnippetBtn.DropDownItems.Add("Correlation Matrix", null, (s, e) => InsertSnippet(GetCorrelationSnippet()));
            insertSnippetBtn.DropDownItems.Add("Time Series Plot", null, (s, e) => InsertSnippet(GetTimeSeriesSnippet()));
        }

        private void ExportAllData()
        {
            dataExportDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data_exports");
            if (!Directory.Exists(dataExportDir))
                Directory.CreateDirectory(dataExportDir);

            try
            {
                ExportCsv(products, "products");
                ExportCsv(customers, "customers");
                ExportCsv(employees, "employees");
                ExportCsv(sensorReadings, "sensor_readings");
                ExportCsv(stockPrices, "stock_prices");
                ExportCsv(webEvents, "web_events");
                ExportOrdersCsv();
                ExportOrderItemsCsv();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Warning: Data export failed - " + ex.Message);
            }
        }

        private void ExportCsv<T>(List<T> data, string name)
        {
            string path = Path.Combine(dataExportDir, name + ".csv");
            var props = typeof(T).GetProperties();
            var lines = new List<string>();

            var headerParts = new List<string>();
            foreach (var p in props)
            {
                if (p.GetIndexParameters().Length > 0) continue;
                if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(List<>)) continue;
                if (p.PropertyType.IsClass && p.PropertyType != typeof(string)) continue;
                headerParts.Add(p.Name);
            }
            lines.Add(string.Join(",", headerParts));

            foreach (var item in data)
            {
                var vals = new List<string>();
                foreach (var p in props)
                {
                    if (p.GetIndexParameters().Length > 0) continue;
                    if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(List<>)) continue;
                    if (p.PropertyType.IsClass && p.PropertyType != typeof(string)) continue;
                    var val = p.GetValue(item);
                    string s = val != null ? val.ToString() : "";
                    if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
                        s = "\"" + s.Replace("\"", "\"\"") + "\"";
                    vals.Add(s);
                }
                lines.Add(string.Join(",", vals));
            }
            File.WriteAllLines(path, lines);
        }

        private void ExportOrdersCsv()
        {
            string path = Path.Combine(dataExportDir, "orders.csv");
            var lines = new List<string>();
            lines.Add("Id,CustomerId,OrderDate,ShipDate,Status,ShipMethod,ShippingCost,PaymentMethod,Subtotal,Total,ItemCount");
            foreach (var o in orders)
            {
                lines.Add(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}",
                    o.Id, o.CustomerId, o.OrderDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    o.ShipDate.HasValue ? o.ShipDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "",
                    o.Status, o.ShipMethod, o.ShippingCost, o.PaymentMethod,
                    Math.Round(o.Subtotal, 2), Math.Round(o.Total, 2), o.ItemCount));
            }
            File.WriteAllLines(path, lines);
        }

        private void ExportOrderItemsCsv()
        {
            string path = Path.Combine(dataExportDir, "order_items.csv");
            var lines = new List<string>();
            lines.Add("OrderId,ProductId,ProductName,Quantity,UnitPrice,Discount,LineTotal");
            foreach (var o in orders)
            {
                foreach (var item in o.Items)
                {
                    string prodName = item.ProductName.Contains(",") ? "\"" + item.ProductName + "\"" : item.ProductName;
                    lines.Add(string.Format("{0},{1},{2},{3},{4},{5},{6}",
                        o.Id, item.ProductId, prodName, item.Quantity,
                        item.UnitPrice, item.Discount, Math.Round(item.LineTotal, 2)));
                }
            }
            File.WriteAllLines(path, lines);
        }

        private void PopulateDataTree()
        {
            dataTreeView.Nodes.Clear();
            var root = dataTreeView.Nodes.Add("Datasets");

            var prodNode = root.Nodes.Add("products (" + products.Count + " records)");
            prodNode.Nodes.Add("Id, Name, Category, SubCategory");
            prodNode.Nodes.Add("SKU, Price, Cost, Margin");
            prodNode.Nodes.Add("StockQuantity, ReorderLevel, Weight");
            prodNode.Nodes.Add("Supplier, Rating, ReviewCount");

            var custNode = root.Nodes.Add("customers (" + customers.Count + " records)");
            custNode.Nodes.Add("Id, FirstName, LastName, Email");
            custNode.Nodes.Add("Phone, DateOfBirth, RegistrationDate");
            custNode.Nodes.Add("Tier, CreditLimit, IsActive");
            custNode.Nodes.Add("Address fields (Street, City, State...)");

            var orderNode = root.Nodes.Add("orders (" + orders.Count + " records)");
            orderNode.Nodes.Add("Id, CustomerId, OrderDate, ShipDate");
            orderNode.Nodes.Add("Status, ShipMethod, ShippingCost");
            orderNode.Nodes.Add("PaymentMethod, Subtotal, Total");

            var itemsNode = root.Nodes.Add("order_items (line items)");
            itemsNode.Nodes.Add("OrderId, ProductId, ProductName");
            itemsNode.Nodes.Add("Quantity, UnitPrice, Discount, LineTotal");

            var empNode = root.Nodes.Add("employees (" + employees.Count + " records)");
            empNode.Nodes.Add("Id, FirstName, LastName, Department");
            empNode.Nodes.Add("Title, HireDate, Salary");
            empNode.Nodes.Add("PerformanceScore, ManagerId, IsRemote");

            var sensorNode = root.Nodes.Add("sensor_readings (" + sensorReadings.Count + " records)");
            sensorNode.Nodes.Add("SensorId, SensorType, Location");
            sensorNode.Nodes.Add("Timestamp, Value, Unit, Status");

            var stockNode = root.Nodes.Add("stock_prices (" + stockPrices.Count + " records)");
            stockNode.Nodes.Add("Symbol, CompanyName, Date");
            stockNode.Nodes.Add("Open, High, Low, Close, Volume");

            var webNode = root.Nodes.Add("web_events (" + webEvents.Count + " records)");
            webNode.Nodes.Add("SessionId, UserId, Timestamp");
            webNode.Nodes.Add("EventType, Page, Referrer");
            webNode.Nodes.Add("Browser, Device, Country, Duration");

            root.Expand();
        }

        private void mainTabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (mainTabs.SelectedIndex == 2 && !packagesLoaded)
            {
                packagesLoaded = true;
                OnRefreshPackages(null, null);
            }
        }

        private void OnDatasetChanged(object sender, EventArgs e)
        {
            dataGrid.DataSource = null;
            switch (datasetCombo.SelectedIndex)
            {
                case 0:
                    dataGrid.DataSource = products.Select(p => new { p.Id, p.Name, p.Category, p.SubCategory, p.SKU, p.Price, p.Cost, p.Margin, p.StockQuantity, p.Supplier, p.Rating, p.ReviewCount, p.IsDiscontinued }).ToList();
                    recordCountLabel.Text = products.Count + " records";
                    break;
                case 1:
                    dataGrid.DataSource = customers.Select(c => new { c.Id, c.FullName, c.Email, c.Phone, Age = c.Age, c.Tier, c.CreditLimit, c.IsActive, City = c.Address.City, State = c.Address.State, OrderCount = c.Orders.Count }).ToList();
                    recordCountLabel.Text = customers.Count + " records";
                    break;
                case 2:
                    dataGrid.DataSource = orders.Select(o => new { o.Id, o.CustomerId, OrderDate = o.OrderDate.ToString("yyyy-MM-dd"), o.Status, o.ShipMethod, o.ShippingCost, o.PaymentMethod, Subtotal = Math.Round(o.Subtotal, 2), Total = Math.Round(o.Total, 2), o.ItemCount }).ToList();
                    recordCountLabel.Text = orders.Count + " records";
                    break;
                case 3:
                    dataGrid.DataSource = employees.Select(em => new { em.Id, em.FullName, em.Department, em.Title, HireDate = em.HireDate.ToString("yyyy-MM-dd"), em.Salary, em.PerformanceScore, em.IsRemote, em.Office, em.YearsEmployed }).ToList();
                    recordCountLabel.Text = employees.Count + " records";
                    break;
                case 4:
                    dataGrid.DataSource = sensorReadings.Take(500).Select(sr => new { sr.SensorId, sr.SensorType, sr.Location, Timestamp = sr.Timestamp.ToString("yyyy-MM-dd HH:mm"), sr.Value, sr.Unit, sr.Status, sr.BatteryLevel }).ToList();
                    recordCountLabel.Text = sensorReadings.Count + " records (showing 500)";
                    break;
                case 5:
                    dataGrid.DataSource = stockPrices.Take(500).Select(sp => new { sp.Symbol, sp.CompanyName, Date = sp.Date.ToString("yyyy-MM-dd"), sp.Open, sp.High, sp.Low, sp.Close, sp.Volume }).ToList();
                    recordCountLabel.Text = stockPrices.Count + " records (showing 500)";
                    break;
                case 6:
                    dataGrid.DataSource = webEvents.Take(500).Select(we => new { we.SessionId, we.UserId, Timestamp = we.Timestamp.ToString("yyyy-MM-dd HH:mm"), we.EventType, we.Page, we.Referrer, we.Browser, we.Device, we.Country, we.Duration }).ToList();
                    recordCountLabel.Text = webEvents.Count + " records (showing 500)";
                    break;
            }
        }

        private void OnRunScript(object sender, EventArgs e)
        {
            string script = pythonEditor.Text;
            if (string.IsNullOrWhiteSpace(script))
            {
                AppendOutput("No script to run.\n", Color.Yellow);
                return;
            }

            RaiseStatus("Running script...");
            AppendOutput("--- Running script at " + DateTime.Now.ToString("HH:mm:ss") + " ---\n", Color.Cyan);

            Application.DoEvents();

            var result = pythonRunner.Execute(script, dataExportDir);

            if (!string.IsNullOrEmpty(result.Output))
                AppendOutput(result.Output, Color.FromArgb(220, 220, 220));

            if (!string.IsNullOrEmpty(result.Error))
            {
                if (result.Success)
                    AppendOutput(result.Error, Color.FromArgb(200, 200, 100));
                else
                    AppendOutput("ERROR:\n" + result.Error, Color.FromArgb(255, 100, 100));
            }

            AppendOutput("--- Finished (exit code: " + result.ExitCode + ") ---\n\n", Color.Cyan);
            RaiseStatus(result.Success ? "Script completed successfully." : "Script failed with errors.");
        }

        private void OnClearOutput(object sender, EventArgs e)
        {
            outputBox.Clear();
        }

        private void OnCheckSyntax(object sender, EventArgs e)
        {
            string script = pythonEditor.Text;
            if (string.IsNullOrWhiteSpace(script))
            {
                AppendOutput("No script to check.\n", Color.Yellow);
                return;
            }

            RaiseStatus("Checking syntax...");
            var result = pythonRunner.CheckSyntax(script);

            if (result.Success)
            {
                pythonEditor.ClearError();
                AppendOutput("Syntax OK - no errors found.\n", Color.LightGreen);
                RaiseStatus("Syntax check passed.");
            }
            else
            {
                AppendOutput("Syntax Error:\n" + result.Error + "\n", Color.FromArgb(255, 100, 100));

                int errorLine = ParseErrorLine(result.Error);
                if (errorLine > 0)
                {
                    pythonEditor.SetErrorLine(errorLine);
                }
                RaiseStatus("Syntax error on line " + errorLine);
            }
        }

        private int ParseErrorLine(string error)
        {
            var match = System.Text.RegularExpressions.Regex.Match(error, @"[Ll]ine (\d+)");
            if (match.Success)
            {
                int line;
                if (int.TryParse(match.Groups[1].Value, out line))
                    return line;
            }
            return -1;
        }


        private Form findReplaceForm;

        private void ShowFindReplace()
        {
            if (findReplaceForm != null && !findReplaceForm.IsDisposed)
            {
                findReplaceForm.Activate();
                return;
            }

            findReplaceForm = new Form
            {
                Text = "Find & Replace",
                Size = new Size(420, 200),
                MinimumSize = new Size(400, 200),
                MaximizeBox = false,
                FormBorderStyle = FormBorderStyle.SizableToolWindow,
                StartPosition = FormStartPosition.CenterParent,
                TopMost = true
            };

            var findLabel = new Label { Text = "Find:", Location = new Point(12, 18), Size = new Size(60, 20) };
            var findBox = new TextBox { Location = new Point(80, 15), Size = new Size(220, 22), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            var replaceLabel = new Label { Text = "Replace:", Location = new Point(12, 48), Size = new Size(60, 20) };
            var replaceBox = new TextBox { Location = new Point(80, 45), Size = new Size(220, 22), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            var matchCaseCheck = new CheckBox { Text = "Match case", Location = new Point(80, 75), Size = new Size(120, 20) };

            var findNextBtn = new Button { Text = "Find Next", Location = new Point(310, 14), Size = new Size(85, 25), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            var replaceBtn = new Button { Text = "Replace", Location = new Point(310, 44), Size = new Size(85, 25), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            var replaceAllBtn = new Button { Text = "Replace All", Location = new Point(310, 74), Size = new Size(85, 25), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            var closeBtn = new Button { Text = "Close", Location = new Point(310, 114), Size = new Size(85, 25), Anchor = AnchorStyles.Top | AnchorStyles.Right };

            int searchStart = 0;

            findNextBtn.Click += (s, ev) =>
            {
                string find = findBox.Text;
                if (string.IsNullOrEmpty(find)) return;

                var comparison = matchCaseCheck.Checked ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                int idx = pythonEditor.Text.IndexOf(find, searchStart, comparison);
                if (idx < 0)
                {
                    idx = pythonEditor.Text.IndexOf(find, 0, comparison);
                    if (idx < 0)
                    {
                        RaiseStatus("Not found: " + find);
                        return;
                    }
                }
                pythonEditor.Select(idx, find.Length);
                pythonEditor.ScrollToCaret();
                pythonEditor.Focus();
                searchStart = idx + find.Length;
                RaiseStatus("Found at position " + idx);
            };

            replaceBtn.Click += (s, ev) =>
            {
                if (pythonEditor.SelectionLength > 0)
                {
                    pythonEditor.SelectedText = replaceBox.Text;
                }
                findNextBtn.PerformClick();
            };

            replaceAllBtn.Click += (s, ev) =>
            {
                string find = findBox.Text;
                string replace = replaceBox.Text;
                if (string.IsNullOrEmpty(find)) return;

                var comparison = matchCaseCheck.Checked ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                int count = 0;
                int idx = 0;
                string text = pythonEditor.Text;
                var sb = new System.Text.StringBuilder();
                while (true)
                {
                    int found = text.IndexOf(find, idx, comparison);
                    if (found < 0)
                    {
                        sb.Append(text.Substring(idx));
                        break;
                    }
                    sb.Append(text.Substring(idx, found - idx));
                    sb.Append(replace);
                    idx = found + find.Length;
                    count++;
                }
                if (count > 0)
                {
                    pythonEditor.Text = sb.ToString();
                }
                RaiseStatus("Replaced " + count + " occurrence(s).");
            };

            closeBtn.Click += (s, ev) => findReplaceForm.Close();

            findBox.KeyDown += (s, ev) =>
            {
                if (ev.KeyCode == Keys.Enter) { findNextBtn.PerformClick(); ev.Handled = true; ev.SuppressKeyPress = true; }
                if (ev.KeyCode == Keys.Escape) { findReplaceForm.Close(); ev.Handled = true; }
            };

            replaceBox.KeyDown += (s, ev) =>
            {
                if (ev.KeyCode == Keys.Enter) { replaceBtn.PerformClick(); ev.Handled = true; ev.SuppressKeyPress = true; }
                if (ev.KeyCode == Keys.Escape) { findReplaceForm.Close(); ev.Handled = true; }
            };

            findReplaceForm.Controls.AddRange(new Control[] { findLabel, findBox, replaceLabel, replaceBox, matchCaseCheck, findNextBtn, replaceBtn, replaceAllBtn, closeBtn });

            if (pythonEditor.SelectionLength > 0)
                findBox.Text = pythonEditor.SelectedText;

            findReplaceForm.Show(this.FindForm());
            findBox.Focus();
        }

        private void OnOpenScript(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog { Filter = "Python files (*.py)|*.py|All files (*.*)|*.*" })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    pythonEditor.Text = File.ReadAllText(dlg.FileName);
                    ResetUndoStack();
                    RaiseStatus("Loaded: " + dlg.FileName);
                }
            }
        }

        private void OnSaveScript(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog { Filter = "Python files (*.py)|*.py|All files (*.*)|*.*", DefaultExt = "py" })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(dlg.FileName, pythonEditor.Text);
                    RaiseStatus("Saved: " + dlg.FileName);
                }
            }
        }

        private void OnInstallPackage(object sender, EventArgs e)
        {
            string pkg = packageNameBox.Text.Trim();
            if (string.IsNullOrEmpty(pkg)) return;

            RaiseStatus("Installing " + pkg + "...");
            Application.DoEvents();

            var result = pythonRunner.InstallPackage(pkg);
            if (result.Success)
            {
                AppendOutput("Successfully installed: " + pkg + "\n", Color.LightGreen);
                RaiseStatus(pkg + " installed successfully.");
            }
            else
            {
                AppendOutput("Failed to install " + pkg + ":\n" + result.Error + "\n", Color.FromArgb(255, 100, 100));
                RaiseStatus("Installation failed for " + pkg);
            }

            mainTabs.SelectedIndex = 0;
            OnRefreshPackages(null, null);
        }

        private void OnUninstallPackage(object sender, EventArgs e)
        {
            string pkg = packageNameBox.Text.Trim();
            if (string.IsNullOrEmpty(pkg)) return;

            var confirm = MessageBox.Show("Uninstall " + pkg + "?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            RaiseStatus("Uninstalling " + pkg + "...");
            Application.DoEvents();

            var result = pythonRunner.UninstallPackage(pkg);
            if (result.Success)
            {
                AppendOutput("Successfully uninstalled: " + pkg + "\n", Color.LightGreen);
                RaiseStatus(pkg + " uninstalled.");
            }
            else
            {
                AppendOutput("Failed to uninstall " + pkg + ":\n" + result.Error + "\n", Color.FromArgb(255, 100, 100));
                RaiseStatus("Uninstall failed for " + pkg);
            }
            OnRefreshPackages(null, null);
        }

        private void OnQuickInstall(object sender, EventArgs e)
        {
            if (quickCombo.SelectedItem != null)
            {
                packageNameBox.Text = quickCombo.SelectedItem.ToString();
                OnInstallPackage(sender, e);
            }
        }

        private void OnRefreshPackages(object sender, EventArgs e)
        {
            packageListBox.Items.Clear();
            string list = pythonRunner.ListPackages();
            if (!string.IsNullOrEmpty(list))
            {
                foreach (var line in list.Split('\n'))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        packageListBox.Items.Add(line.Trim());
                }
            }
        }

        private void OnShowHelp(object sender, EventArgs e)
        {
            string help = @"=== Data Science Workbench - Quick Start ===

AVAILABLE DATASETS (as CSV files):
  products.csv      - 200 products with prices, ratings, stock
  customers.csv     - 150 customers with demographics
  orders.csv        - 500 orders with status, totals
  order_items.csv   - Individual line items for orders
  employees.csv     - 100 employees with salary, dept
  sensor_readings.csv - 1000 IoT sensor readings
  stock_prices.csv  - 365 days of 10 stock symbols
  web_events.csv    - 2000 web analytics events

HOW TO USE:
  1. Write Python code in the editor
  2. Press F5 or click Run to execute
  3. CSV files are in the working directory
  4. Use pandas to load: pd.read_csv('products.csv')
  5. Install packages via Package Manager tab

EXAMPLE:
  import pandas as pd
  df = pd.read_csv('products.csv')
  print(df.describe())
  print(df.groupby('Category')['Price'].mean())

TIPS:
  - Use 'Insert Snippet' for ready-made code
  - Matplotlib plots save as PNG files
  - All standard Python libraries available
  - Install any pip package via Package Manager";

            MessageBox.Show(help, "Quick Start Guide", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void AppendOutput(string text, Color color)
        {
            outputBox.SelectionStart = outputBox.TextLength;
            outputBox.SelectionLength = 0;
            outputBox.SelectionColor = color;
            outputBox.AppendText(text);
            outputBox.ScrollToCaret();
        }

        private void RaiseStatus(string msg)
        {
            StatusChanged?.Invoke(this, msg);
        }

        private void InsertSnippet(string code)
        {
            int pos = pythonEditor.SelectionStart;
            suppressHighlight = true;
            pythonEditor.SelectionStart = pos;
            pythonEditor.SelectionLength = 0;
            pythonEditor.SelectedText = code;
            suppressHighlight = false;
            pythonEditor.SelectionStart = pos + code.Length;
            pythonEditor.Focus();
            ApplySyntaxHighlighting();
        }

        private string GetDefaultScript()
        {
            return @"import pandas as pd
import os

print('=== Data Science Workbench ===')
print('Working directory:', os.getcwd())
print()

# List available data files
csv_files = [f for f in os.listdir('.') if f.endswith('.csv')]
print('Available datasets:')
for f in csv_files:
    df = pd.read_csv(f)
    print(f'  {f}: {len(df)} rows, {len(df.columns)} columns')
print()

# Quick look at products
products = pd.read_csv('products.csv')
print('=== Product Summary ===')
print(products.describe())
";
        }

        private string GetLoadDataSnippet()
        {
            return @"
import pandas as pd

# Load all datasets
products = pd.read_csv('products.csv')
customers = pd.read_csv('customers.csv')
orders = pd.read_csv('orders.csv')
order_items = pd.read_csv('order_items.csv')
employees = pd.read_csv('employees.csv')
sensor_readings = pd.read_csv('sensor_readings.csv')
stock_prices = pd.read_csv('stock_prices.csv')
web_events = pd.read_csv('web_events.csv')

print('All datasets loaded successfully!')
for name, df in [('products', products), ('customers', customers),
                  ('orders', orders), ('order_items', order_items),
                  ('employees', employees), ('sensor_readings', sensor_readings),
                  ('stock_prices', stock_prices), ('web_events', web_events)]:
    print(f'  {name}: {df.shape[0]} rows x {df.shape[1]} columns')
";
        }

        private string GetStatsSnippet()
        {
            return @"
import pandas as pd

df = pd.read_csv('products.csv')
print('=== Descriptive Statistics ===')
print(df.describe())
print()
print('=== Data Types ===')
print(df.dtypes)
print()
print('=== Missing Values ===')
print(df.isnull().sum())
";
        }

        private string GetHistogramSnippet()
        {
            return @"
import pandas as pd
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt

df = pd.read_csv('products.csv')
fig, ax = plt.subplots(figsize=(10, 6))
ax.hist(df['Price'], bins=30, edgecolor='black', alpha=0.7)
ax.set_xlabel('Price ($)')
ax.set_ylabel('Count')
ax.set_title('Product Price Distribution')
plt.tight_layout()
plt.savefig('histogram.png', dpi=100)
print('Histogram saved to histogram.png')
";
        }

        private string GetScatterSnippet()
        {
            return @"
import pandas as pd
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt

df = pd.read_csv('products.csv')
fig, ax = plt.subplots(figsize=(10, 6))
scatter = ax.scatter(df['Price'], df['Rating'], c=df['ReviewCount'],
                     cmap='viridis', alpha=0.6, edgecolors='black', linewidth=0.5)
ax.set_xlabel('Price ($)')
ax.set_ylabel('Rating')
ax.set_title('Price vs Rating (colored by Review Count)')
plt.colorbar(scatter, label='Review Count')
plt.tight_layout()
plt.savefig('scatter.png', dpi=100)
print('Scatter plot saved to scatter.png')
";
        }

        private string GetGroupBySnippet()
        {
            return @"
import pandas as pd

df = pd.read_csv('products.csv')
print('=== Average Price by Category ===')
group = df.groupby('Category').agg(
    Count=('Id', 'count'),
    Avg_Price=('Price', 'mean'),
    Avg_Rating=('Rating', 'mean'),
    Total_Stock=('StockQuantity', 'sum')
).round(2)
print(group.sort_values('Avg_Price', ascending=False))
";
        }

        private string GetCorrelationSnippet()
        {
            return @"
import pandas as pd
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt

df = pd.read_csv('products.csv')
numeric_cols = df.select_dtypes(include='number')
corr = numeric_cols.corr()
print('=== Correlation Matrix ===')
print(corr.round(3))

fig, ax = plt.subplots(figsize=(10, 8))
im = ax.imshow(corr, cmap='coolwarm', vmin=-1, vmax=1)
ax.set_xticks(range(len(corr.columns)))
ax.set_yticks(range(len(corr.columns)))
ax.set_xticklabels(corr.columns, rotation=45, ha='right')
ax.set_yticklabels(corr.columns)
plt.colorbar(im)
ax.set_title('Correlation Matrix')
plt.tight_layout()
plt.savefig('correlation.png', dpi=100)
print('Correlation matrix saved to correlation.png')
";
        }

        private string GetTimeSeriesSnippet()
        {
            return @"
import pandas as pd
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt

df = pd.read_csv('stock_prices.csv')
df['Date'] = pd.to_datetime(df['Date'])

fig, ax = plt.subplots(figsize=(12, 6))
for symbol in df['Symbol'].unique()[:5]:
    subset = df[df['Symbol'] == symbol].sort_values('Date')
    ax.plot(subset['Date'], subset['Close'], label=symbol, linewidth=1)
ax.set_xlabel('Date')
ax.set_ylabel('Close Price ($)')
ax.set_title('Stock Prices Over Time')
ax.legend()
plt.xticks(rotation=45)
plt.tight_layout()
plt.savefig('timeseries.png', dpi=100)
print('Time series chart saved to timeseries.png')
";
        }
    }
}
