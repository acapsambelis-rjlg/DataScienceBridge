using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DataScienceWorkbench
{
    public class DataScienceControl : UserControl
    {
        private TabControl mainTabs;
        private TextBox pythonEditor;
        private RichTextBox outputBox;
        private TreeView dataTreeView;
        private DataGridView dataGrid;
        private TextBox packageNameBox;
        private ListBox packageListBox;
        private ComboBox datasetCombo;
        private Label recordCountLabel;

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

        public event EventHandler<string> StatusChanged;

        public DataScienceControl()
        {
            InitializeData();
            InitializeComponents();
            ExportAllData();
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
            set { pythonEditor.Text = value; }
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

            var editMenu = new ToolStripMenuItem("Edit");
            editMenu.DropDownItems.Add("Cut", null, (s, e) => { if (pythonEditor.Focused) pythonEditor.Cut(); });
            editMenu.DropDownItems.Add("Copy", null, (s, e) => { if (pythonEditor.Focused) pythonEditor.Copy(); });
            editMenu.DropDownItems.Add("Paste", null, (s, e) => { if (pythonEditor.Focused) pythonEditor.Paste(); });
            editMenu.DropDownItems.Add(new ToolStripSeparator());
            editMenu.DropDownItems.Add("Select All", null, (s, e) => { if (pythonEditor.Focused) pythonEditor.SelectAll(); });
            editMenu.DropDownItems.Add("Clear Output", null, (s, e) => outputBox.Clear());

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

        private void InitializeComponents()
        {
            this.Dock = DockStyle.Fill;

            mainTabs = new TabControl();
            mainTabs.Dock = DockStyle.Fill;

            var editorTab = CreateEditorTab();
            var dataTab = CreateDataBrowserTab();
            var packagesTab = CreatePackageManagerTab();
            mainTabs.TabPages.Add(editorTab);
            mainTabs.TabPages.Add(dataTab);
            mainTabs.TabPages.Add(packagesTab);

            mainTabs.SelectedIndexChanged += (s, e) =>
            {
                if (mainTabs.SelectedIndex == 2 && !packagesLoaded)
                {
                    packagesLoaded = true;
                    OnRefreshPackages(null, null);
                }
            };

            this.Controls.Add(mainTabs);
        }

        private TabPage CreateEditorTab()
        {
            var tab = new TabPage("Python Editor");

            var mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 450,
                SplitterWidth = 6
            };

            var topSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 200,
                SplitterWidth = 6
            };

            dataTreeView = new TreeView
            {
                Dock = DockStyle.Fill,
                Font = new Font("Monospace", 9f)
            };
            PopulateDataTree();

            var treePanel = new Panel { Dock = DockStyle.Fill };
            var treeLabel = new Label { Text = "Available Data:", Dock = DockStyle.Top, Height = 22, Padding = new Padding(4, 4, 0, 0), Font = new Font("Sans", 9f, FontStyle.Bold) };
            treePanel.Controls.Add(dataTreeView);
            treePanel.Controls.Add(treeLabel);

            var editorPanel = new Panel { Dock = DockStyle.Fill };

            var toolBar = new ToolStrip();
            var runBtn = new ToolStripButton("Run (F5)") { DisplayStyle = ToolStripItemDisplayStyle.Text };
            runBtn.Click += OnRunScript;
            var clearBtn = new ToolStripButton("Clear Output") { DisplayStyle = ToolStripItemDisplayStyle.Text };
            clearBtn.Click += (s, e) => outputBox.Clear();
            var insertSnippetBtn = new ToolStripDropDownButton("Insert Snippet");
            insertSnippetBtn.DropDownItems.Add("Load CSV Data", null, (s, e) => InsertSnippet(GetLoadDataSnippet()));
            insertSnippetBtn.DropDownItems.Add("Basic Statistics", null, (s, e) => InsertSnippet(GetStatsSnippet()));
            insertSnippetBtn.DropDownItems.Add("Plot Histogram", null, (s, e) => InsertSnippet(GetHistogramSnippet()));
            insertSnippetBtn.DropDownItems.Add("Scatter Plot", null, (s, e) => InsertSnippet(GetScatterSnippet()));
            insertSnippetBtn.DropDownItems.Add("Group By Analysis", null, (s, e) => InsertSnippet(GetGroupBySnippet()));
            insertSnippetBtn.DropDownItems.Add("Correlation Matrix", null, (s, e) => InsertSnippet(GetCorrelationSnippet()));
            insertSnippetBtn.DropDownItems.Add("Time Series Plot", null, (s, e) => InsertSnippet(GetTimeSeriesSnippet()));

            toolBar.Items.AddRange(new ToolStripItem[] { runBtn, new ToolStripSeparator(), clearBtn, new ToolStripSeparator(), insertSnippetBtn });

            pythonEditor = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                AcceptsTab = true,
                Font = new Font("Monospace", 10f),
                Text = GetDefaultScript()
            };

            editorPanel.Controls.Add(pythonEditor);
            editorPanel.Controls.Add(toolBar);

            topSplit.Panel1.Controls.Add(treePanel);
            topSplit.Panel2.Controls.Add(editorPanel);

            var outputPanel = new Panel { Dock = DockStyle.Fill };
            var outputLabel = new Label { Text = "Output:", Dock = DockStyle.Top, Height = 22, Padding = new Padding(4, 4, 0, 0), Font = new Font("Sans", 9f, FontStyle.Bold) };
            outputBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Monospace", 9f),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(220, 220, 220),
                WordWrap = false
            };
            outputPanel.Controls.Add(outputBox);
            outputPanel.Controls.Add(outputLabel);

            mainSplit.Panel1.Controls.Add(topSplit);
            mainSplit.Panel2.Controls.Add(outputPanel);

            tab.Controls.Add(mainSplit);
            return tab;
        }

        private TabPage CreateDataBrowserTab()
        {
            var tab = new TabPage("Data Browser");

            var topPanel = new Panel { Dock = DockStyle.Top, Height = 40 };
            var lbl = new Label { Text = "Dataset:", Location = new Point(10, 10), AutoSize = true };
            datasetCombo = new ComboBox
            {
                Location = new Point(70, 7),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            datasetCombo.Items.AddRange(new object[] { "Products", "Customers", "Orders", "Employees", "Sensor Readings", "Stock Prices", "Web Events" });
            datasetCombo.SelectedIndex = 0;
            datasetCombo.SelectedIndexChanged += OnDatasetChanged;

            recordCountLabel = new Label { Location = new Point(290, 10), AutoSize = true };

            topPanel.Controls.AddRange(new Control[] { lbl, datasetCombo, recordCountLabel });

            dataGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                Font = new Font("Sans", 9f)
            };

            tab.Controls.Add(dataGrid);
            tab.Controls.Add(topPanel);

            OnDatasetChanged(null, null);
            return tab;
        }

        private TabPage CreatePackageManagerTab()
        {
            var tab = new TabPage("Package Manager");

            var leftPanel = new Panel { Dock = DockStyle.Left, Width = 350, Padding = new Padding(10) };

            var installGroup = new GroupBox { Text = "Install / Uninstall Packages", Dock = DockStyle.Top, Height = 160, Padding = new Padding(10) };
            var pkgLabel = new Label { Text = "Package name:", Location = new Point(15, 25), AutoSize = true };
            packageNameBox = new TextBox { Location = new Point(15, 45), Width = 200 };

            var installBtn = new Button { Text = "Install", Location = new Point(220, 44), Width = 90 };
            installBtn.Click += OnInstallPackage;
            var uninstallBtn = new Button { Text = "Uninstall", Location = new Point(220, 74), Width = 90 };
            uninstallBtn.Click += OnUninstallPackage;

            var quickInstallLabel = new Label { Text = "Quick install:", Location = new Point(15, 80), AutoSize = true };
            var quickCombo = new ComboBox { Location = new Point(15, 100), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            quickCombo.Items.AddRange(new object[] {
                "scipy", "scikit-learn", "seaborn", "statsmodels",
                "plotly", "bokeh", "pillow", "openpyxl",
                "requests", "beautifulsoup4", "sympy", "networkx"
            });
            var quickInstallBtn = new Button { Text = "Install", Location = new Point(220, 99), Width = 90 };
            quickInstallBtn.Click += (s, e) =>
            {
                if (quickCombo.SelectedItem != null)
                {
                    packageNameBox.Text = quickCombo.SelectedItem.ToString();
                    OnInstallPackage(s, e);
                }
            };

            installGroup.Controls.AddRange(new Control[] { pkgLabel, packageNameBox, installBtn, uninstallBtn, quickInstallLabel, quickCombo, quickInstallBtn });

            var refreshBtn = new Button { Text = "Refresh Installed Packages", Dock = DockStyle.Top, Height = 30 };
            refreshBtn.Click += OnRefreshPackages;

            leftPanel.Controls.Add(refreshBtn);
            leftPanel.Controls.Add(installGroup);

            packageListBox = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Monospace", 9f)
            };

            var rightPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            var pkgListLabel = new Label { Text = "Installed Packages:", Dock = DockStyle.Top, Height = 22, Font = new Font("Sans", 9f, FontStyle.Bold) };
            rightPanel.Controls.Add(packageListBox);
            rightPanel.Controls.Add(pkgListLabel);

            tab.Controls.Add(rightPanel);
            tab.Controls.Add(leftPanel);

            return tab;
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

        private void OnOpenScript(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog { Filter = "Python files (*.py)|*.py|All files (*.*)|*.*" })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    pythonEditor.Text = File.ReadAllText(dlg.FileName);
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
            pythonEditor.Text = pythonEditor.Text.Insert(pos, code);
            pythonEditor.SelectionStart = pos + code.Length;
            pythonEditor.Focus();
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
