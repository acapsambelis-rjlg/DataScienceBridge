namespace DataScienceWorkbench
{
    partial class DataScienceControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.mainTabs = new System.Windows.Forms.TabControl();
            this.editorTab = new System.Windows.Forms.TabPage();
            this.mainSplit = new System.Windows.Forms.SplitContainer();
            this.topSplit = new System.Windows.Forms.SplitContainer();
            this.treePanel = new System.Windows.Forms.Panel();
            this.dataTreeView = new System.Windows.Forms.TreeView();
            this.treeLabel = new System.Windows.Forms.Label();
            this.editorPanel = new System.Windows.Forms.Panel();
            this.pythonEditor = new System.Windows.Forms.RichTextBox();
            this.lineNumberPanel = new DataScienceWorkbench.LineNumberPanel();
            this.toolBar = new System.Windows.Forms.ToolStrip();
            this.runBtn = new System.Windows.Forms.ToolStripButton();
            this.toolSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.checkSyntaxBtn = new System.Windows.Forms.ToolStripButton();
            this.toolSep2 = new System.Windows.Forms.ToolStripSeparator();
            this.clearBtn = new System.Windows.Forms.ToolStripButton();
            this.toolSep3 = new System.Windows.Forms.ToolStripSeparator();
            this.insertSnippetBtn = new System.Windows.Forms.ToolStripDropDownButton();
            this.outputPanel = new System.Windows.Forms.Panel();
            this.outputBox = new System.Windows.Forms.RichTextBox();
            this.outputLabel = new System.Windows.Forms.Label();
            this.dataTab = new System.Windows.Forms.TabPage();
            this.dataGrid = new System.Windows.Forms.DataGridView();
            this.dataTopPanel = new System.Windows.Forms.Panel();
            this.recordCountLabel = new System.Windows.Forms.Label();
            this.datasetCombo = new System.Windows.Forms.ComboBox();
            this.datasetLabel = new System.Windows.Forms.Label();
            this.packagesTab = new System.Windows.Forms.TabPage();
            this.pkgRightPanel = new System.Windows.Forms.Panel();
            this.packageListBox = new System.Windows.Forms.ListBox();
            this.pkgListLabel = new System.Windows.Forms.Label();
            this.pkgLeftPanel = new System.Windows.Forms.Panel();
            this.refreshBtn = new System.Windows.Forms.Button();
            this.installGroup = new System.Windows.Forms.GroupBox();
            this.quickInstallBtn = new System.Windows.Forms.Button();
            this.quickCombo = new System.Windows.Forms.ComboBox();
            this.quickInstallLabel = new System.Windows.Forms.Label();
            this.uninstallBtn = new System.Windows.Forms.Button();
            this.installBtn = new System.Windows.Forms.Button();
            this.packageNameBox = new System.Windows.Forms.TextBox();
            this.pkgLabel = new System.Windows.Forms.Label();
            this.mainTabs.SuspendLayout();
            this.editorTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mainSplit)).BeginInit();
            this.mainSplit.Panel1.SuspendLayout();
            this.mainSplit.Panel2.SuspendLayout();
            this.mainSplit.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.topSplit)).BeginInit();
            this.topSplit.Panel1.SuspendLayout();
            this.topSplit.Panel2.SuspendLayout();
            this.topSplit.SuspendLayout();
            this.treePanel.SuspendLayout();
            this.editorPanel.SuspendLayout();
            this.toolBar.SuspendLayout();
            this.outputPanel.SuspendLayout();
            this.dataTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGrid)).BeginInit();
            this.dataTopPanel.SuspendLayout();
            this.packagesTab.SuspendLayout();
            this.pkgRightPanel.SuspendLayout();
            this.pkgLeftPanel.SuspendLayout();
            this.installGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainTabs
            // 
            this.mainTabs.Controls.Add(this.editorTab);
            this.mainTabs.Controls.Add(this.dataTab);
            this.mainTabs.Controls.Add(this.packagesTab);
            this.mainTabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainTabs.Location = new System.Drawing.Point(0, 0);
            this.mainTabs.Name = "mainTabs";
            this.mainTabs.SelectedIndex = 0;
            this.mainTabs.Size = new System.Drawing.Size(800, 600);
            this.mainTabs.TabIndex = 0;
            this.mainTabs.SelectedIndexChanged += new System.EventHandler(this.mainTabs_SelectedIndexChanged);
            // 
            // editorTab
            // 
            this.editorTab.Controls.Add(this.mainSplit);
            this.editorTab.Location = new System.Drawing.Point(4, 22);
            this.editorTab.Name = "editorTab";
            this.editorTab.Size = new System.Drawing.Size(792, 574);
            this.editorTab.TabIndex = 0;
            this.editorTab.Text = "Python Editor";
            this.editorTab.UseVisualStyleBackColor = true;
            // 
            // mainSplit
            // 
            this.mainSplit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainSplit.Location = new System.Drawing.Point(0, 0);
            this.mainSplit.Name = "mainSplit";
            this.mainSplit.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // mainSplit.Panel1
            // 
            this.mainSplit.Panel1.Controls.Add(this.topSplit);
            // 
            // mainSplit.Panel2
            // 
            this.mainSplit.Panel2.Controls.Add(this.outputPanel);
            this.mainSplit.Size = new System.Drawing.Size(792, 574);
            this.mainSplit.SplitterDistance = 450;
            this.mainSplit.SplitterWidth = 6;
            this.mainSplit.TabIndex = 0;
            // 
            // topSplit
            // 
            this.topSplit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topSplit.Location = new System.Drawing.Point(0, 0);
            this.topSplit.Name = "topSplit";
            // 
            // topSplit.Panel1
            // 
            this.topSplit.Panel1.Controls.Add(this.treePanel);
            // 
            // topSplit.Panel2
            // 
            this.topSplit.Panel2.Controls.Add(this.editorPanel);
            this.topSplit.Size = new System.Drawing.Size(792, 450);
            this.topSplit.SplitterDistance = 200;
            this.topSplit.SplitterWidth = 6;
            this.topSplit.TabIndex = 0;
            // 
            // treePanel
            // 
            this.treePanel.Controls.Add(this.dataTreeView);
            this.treePanel.Controls.Add(this.treeLabel);
            this.treePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treePanel.Location = new System.Drawing.Point(0, 0);
            this.treePanel.Name = "treePanel";
            this.treePanel.Size = new System.Drawing.Size(200, 450);
            this.treePanel.TabIndex = 0;
            // 
            // dataTreeView
            // 
            this.dataTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataTreeView.Font = new System.Drawing.Font("Monospace", 9F);
            this.dataTreeView.Location = new System.Drawing.Point(0, 22);
            this.dataTreeView.Name = "dataTreeView";
            this.dataTreeView.Size = new System.Drawing.Size(200, 428);
            this.dataTreeView.TabIndex = 0;
            // 
            // treeLabel
            // 
            this.treeLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.treeLabel.Font = new System.Drawing.Font("Sans", 9F, System.Drawing.FontStyle.Bold);
            this.treeLabel.Location = new System.Drawing.Point(0, 0);
            this.treeLabel.Name = "treeLabel";
            this.treeLabel.Padding = new System.Windows.Forms.Padding(4, 4, 0, 0);
            this.treeLabel.Size = new System.Drawing.Size(200, 22);
            this.treeLabel.TabIndex = 1;
            this.treeLabel.Text = "Available Data:";
            // 
            // editorPanel
            // 
            this.editorPanel.Controls.Add(this.pythonEditor);
            this.editorPanel.Controls.Add(this.lineNumberPanel);
            this.editorPanel.Controls.Add(this.toolBar);
            this.editorPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.editorPanel.Location = new System.Drawing.Point(0, 0);
            this.editorPanel.Name = "editorPanel";
            this.editorPanel.Size = new System.Drawing.Size(586, 450);
            this.editorPanel.TabIndex = 0;
            // 
            // pythonEditor
            // 
            this.pythonEditor.AcceptsTab = true;
            this.pythonEditor.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.pythonEditor.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.pythonEditor.DetectUrls = false;
            this.pythonEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pythonEditor.Font = new System.Drawing.Font("Monospace", 10F);
            this.pythonEditor.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(212)))), ((int)(((byte)(212)))), ((int)(((byte)(212)))));
            this.pythonEditor.Location = new System.Drawing.Point(45, 25);
            this.pythonEditor.Name = "pythonEditor";
            this.pythonEditor.Size = new System.Drawing.Size(541, 425);
            this.pythonEditor.TabIndex = 0;
            this.pythonEditor.Text = "";
            this.pythonEditor.WordWrap = false;
            // 
            // lineNumberPanel
            // 
            this.lineNumberPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.lineNumberPanel.Location = new System.Drawing.Point(0, 25);
            this.lineNumberPanel.Name = "lineNumberPanel";
            this.lineNumberPanel.Size = new System.Drawing.Size(45, 425);
            this.lineNumberPanel.TabIndex = 2;
            // 
            // toolBar
            // 
            this.toolBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.runBtn,
            this.toolSep1,
            this.checkSyntaxBtn,
            this.toolSep2,
            this.clearBtn,
            this.toolSep3,
            this.insertSnippetBtn});
            this.toolBar.Location = new System.Drawing.Point(0, 0);
            this.toolBar.Name = "toolBar";
            this.toolBar.Size = new System.Drawing.Size(586, 25);
            this.toolBar.TabIndex = 1;
            // 
            // runBtn
            // 
            this.runBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.runBtn.Name = "runBtn";
            this.runBtn.Size = new System.Drawing.Size(54, 22);
            this.runBtn.Text = "Run (F5)";
            this.runBtn.Click += new System.EventHandler(this.OnRunScript);
            // 
            // toolSep1
            // 
            this.toolSep1.Name = "toolSep1";
            this.toolSep1.Size = new System.Drawing.Size(6, 25);
            // 
            // checkSyntaxBtn
            // 
            this.checkSyntaxBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.checkSyntaxBtn.Name = "checkSyntaxBtn";
            this.checkSyntaxBtn.Size = new System.Drawing.Size(82, 22);
            this.checkSyntaxBtn.Text = "Check Syntax";
            this.checkSyntaxBtn.Click += new System.EventHandler(this.OnCheckSyntax);
            // 
            // toolSep2
            // 
            this.toolSep2.Name = "toolSep2";
            this.toolSep2.Size = new System.Drawing.Size(6, 25);
            // 
            // clearBtn
            // 
            this.clearBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.clearBtn.Name = "clearBtn";
            this.clearBtn.Size = new System.Drawing.Size(76, 22);
            this.clearBtn.Text = "Clear Output";
            this.clearBtn.Click += new System.EventHandler(this.OnClearOutput);
            // 
            // toolSep3
            // 
            this.toolSep3.Name = "toolSep3";
            this.toolSep3.Size = new System.Drawing.Size(6, 25);
            // 
            // insertSnippetBtn
            // 
            this.insertSnippetBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.insertSnippetBtn.Name = "insertSnippetBtn";
            this.insertSnippetBtn.Size = new System.Drawing.Size(92, 22);
            this.insertSnippetBtn.Text = "Insert Snippet";
            // 
            // outputPanel
            // 
            this.outputPanel.Controls.Add(this.outputBox);
            this.outputPanel.Controls.Add(this.outputLabel);
            this.outputPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.outputPanel.Location = new System.Drawing.Point(0, 0);
            this.outputPanel.Name = "outputPanel";
            this.outputPanel.Size = new System.Drawing.Size(792, 118);
            this.outputPanel.TabIndex = 0;
            // 
            // outputBox
            // 
            this.outputBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.outputBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.outputBox.Font = new System.Drawing.Font("Monospace", 9F);
            this.outputBox.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.outputBox.Location = new System.Drawing.Point(0, 22);
            this.outputBox.Name = "outputBox";
            this.outputBox.ReadOnly = true;
            this.outputBox.Size = new System.Drawing.Size(792, 96);
            this.outputBox.TabIndex = 0;
            this.outputBox.Text = "";
            this.outputBox.WordWrap = false;
            // 
            // outputLabel
            // 
            this.outputLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.outputLabel.Font = new System.Drawing.Font("Sans", 9F, System.Drawing.FontStyle.Bold);
            this.outputLabel.Location = new System.Drawing.Point(0, 0);
            this.outputLabel.Name = "outputLabel";
            this.outputLabel.Padding = new System.Windows.Forms.Padding(4, 4, 0, 0);
            this.outputLabel.Size = new System.Drawing.Size(792, 22);
            this.outputLabel.TabIndex = 1;
            this.outputLabel.Text = "Output:";
            // 
            // dataTab
            // 
            this.dataTab.Controls.Add(this.dataGrid);
            this.dataTab.Controls.Add(this.dataTopPanel);
            this.dataTab.Location = new System.Drawing.Point(4, 22);
            this.dataTab.Name = "dataTab";
            this.dataTab.Size = new System.Drawing.Size(792, 574);
            this.dataTab.TabIndex = 1;
            this.dataTab.Text = "Data Browser";
            this.dataTab.UseVisualStyleBackColor = true;
            // 
            // dataGrid
            // 
            this.dataGrid.AllowUserToAddRows = false;
            this.dataGrid.AllowUserToDeleteRows = false;
            this.dataGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dataGrid.BackgroundColor = System.Drawing.Color.White;
            this.dataGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGrid.Font = new System.Drawing.Font("Sans", 9F);
            this.dataGrid.Location = new System.Drawing.Point(0, 40);
            this.dataGrid.Name = "dataGrid";
            this.dataGrid.ReadOnly = true;
            this.dataGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGrid.Size = new System.Drawing.Size(792, 534);
            this.dataGrid.TabIndex = 0;
            // 
            // dataTopPanel
            // 
            this.dataTopPanel.Controls.Add(this.recordCountLabel);
            this.dataTopPanel.Controls.Add(this.datasetCombo);
            this.dataTopPanel.Controls.Add(this.datasetLabel);
            this.dataTopPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.dataTopPanel.Location = new System.Drawing.Point(0, 0);
            this.dataTopPanel.Name = "dataTopPanel";
            this.dataTopPanel.Size = new System.Drawing.Size(792, 40);
            this.dataTopPanel.TabIndex = 1;
            // 
            // recordCountLabel
            // 
            this.recordCountLabel.AutoSize = true;
            this.recordCountLabel.Location = new System.Drawing.Point(290, 10);
            this.recordCountLabel.Name = "recordCountLabel";
            this.recordCountLabel.Size = new System.Drawing.Size(0, 13);
            this.recordCountLabel.TabIndex = 2;
            // 
            // datasetCombo
            // 
            this.datasetCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.datasetCombo.Items.AddRange(new object[] {
            "Products",
            "Customers",
            "Orders",
            "Employees",
            "Sensor Readings",
            "Stock Prices",
            "Web Events"});
            this.datasetCombo.Location = new System.Drawing.Point(70, 7);
            this.datasetCombo.Name = "datasetCombo";
            this.datasetCombo.Size = new System.Drawing.Size(200, 21);
            this.datasetCombo.TabIndex = 1;
            this.datasetCombo.SelectedIndexChanged += new System.EventHandler(this.OnDatasetChanged);
            // 
            // datasetLabel
            // 
            this.datasetLabel.AutoSize = true;
            this.datasetLabel.Location = new System.Drawing.Point(10, 10);
            this.datasetLabel.Name = "datasetLabel";
            this.datasetLabel.Size = new System.Drawing.Size(46, 13);
            this.datasetLabel.TabIndex = 0;
            this.datasetLabel.Text = "Dataset:";
            // 
            // packagesTab
            // 
            this.packagesTab.Controls.Add(this.pkgRightPanel);
            this.packagesTab.Controls.Add(this.pkgLeftPanel);
            this.packagesTab.Location = new System.Drawing.Point(4, 22);
            this.packagesTab.Name = "packagesTab";
            this.packagesTab.Size = new System.Drawing.Size(792, 574);
            this.packagesTab.TabIndex = 2;
            this.packagesTab.Text = "Package Manager";
            this.packagesTab.UseVisualStyleBackColor = true;
            // 
            // pkgRightPanel
            // 
            this.pkgRightPanel.Controls.Add(this.packageListBox);
            this.pkgRightPanel.Controls.Add(this.pkgListLabel);
            this.pkgRightPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pkgRightPanel.Location = new System.Drawing.Point(350, 0);
            this.pkgRightPanel.Name = "pkgRightPanel";
            this.pkgRightPanel.Padding = new System.Windows.Forms.Padding(10);
            this.pkgRightPanel.Size = new System.Drawing.Size(442, 574);
            this.pkgRightPanel.TabIndex = 1;
            // 
            // packageListBox
            // 
            this.packageListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.packageListBox.Font = new System.Drawing.Font("Monospace", 9F);
            this.packageListBox.FormattingEnabled = true;
            this.packageListBox.Location = new System.Drawing.Point(10, 32);
            this.packageListBox.Name = "packageListBox";
            this.packageListBox.Size = new System.Drawing.Size(422, 532);
            this.packageListBox.TabIndex = 0;
            // 
            // pkgListLabel
            // 
            this.pkgListLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.pkgListLabel.Font = new System.Drawing.Font("Sans", 9F, System.Drawing.FontStyle.Bold);
            this.pkgListLabel.Location = new System.Drawing.Point(10, 10);
            this.pkgListLabel.Name = "pkgListLabel";
            this.pkgListLabel.Size = new System.Drawing.Size(422, 22);
            this.pkgListLabel.TabIndex = 1;
            this.pkgListLabel.Text = "Installed Packages:";
            // 
            // pkgLeftPanel
            // 
            this.pkgLeftPanel.Controls.Add(this.refreshBtn);
            this.pkgLeftPanel.Controls.Add(this.installGroup);
            this.pkgLeftPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.pkgLeftPanel.Location = new System.Drawing.Point(0, 0);
            this.pkgLeftPanel.Name = "pkgLeftPanel";
            this.pkgLeftPanel.Padding = new System.Windows.Forms.Padding(10);
            this.pkgLeftPanel.Size = new System.Drawing.Size(350, 574);
            this.pkgLeftPanel.TabIndex = 0;
            // 
            // refreshBtn
            // 
            this.refreshBtn.Dock = System.Windows.Forms.DockStyle.Top;
            this.refreshBtn.Location = new System.Drawing.Point(10, 170);
            this.refreshBtn.Name = "refreshBtn";
            this.refreshBtn.Size = new System.Drawing.Size(330, 30);
            this.refreshBtn.TabIndex = 1;
            this.refreshBtn.Text = "Refresh Installed Packages";
            this.refreshBtn.UseVisualStyleBackColor = true;
            this.refreshBtn.Click += new System.EventHandler(this.OnRefreshPackages);
            // 
            // installGroup
            // 
            this.installGroup.Controls.Add(this.quickInstallBtn);
            this.installGroup.Controls.Add(this.quickCombo);
            this.installGroup.Controls.Add(this.quickInstallLabel);
            this.installGroup.Controls.Add(this.uninstallBtn);
            this.installGroup.Controls.Add(this.installBtn);
            this.installGroup.Controls.Add(this.packageNameBox);
            this.installGroup.Controls.Add(this.pkgLabel);
            this.installGroup.Dock = System.Windows.Forms.DockStyle.Top;
            this.installGroup.Location = new System.Drawing.Point(10, 10);
            this.installGroup.Name = "installGroup";
            this.installGroup.Padding = new System.Windows.Forms.Padding(10);
            this.installGroup.Size = new System.Drawing.Size(330, 160);
            this.installGroup.TabIndex = 0;
            this.installGroup.TabStop = false;
            this.installGroup.Text = "Install / Uninstall Packages";
            // 
            // quickInstallBtn
            // 
            this.quickInstallBtn.Location = new System.Drawing.Point(220, 99);
            this.quickInstallBtn.Name = "quickInstallBtn";
            this.quickInstallBtn.Size = new System.Drawing.Size(90, 23);
            this.quickInstallBtn.TabIndex = 6;
            this.quickInstallBtn.Text = "Install";
            this.quickInstallBtn.UseVisualStyleBackColor = true;
            this.quickInstallBtn.Click += new System.EventHandler(this.OnQuickInstall);
            // 
            // quickCombo
            // 
            this.quickCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.quickCombo.Items.AddRange(new object[] {
            "scipy",
            "scikit-learn",
            "seaborn",
            "statsmodels",
            "plotly",
            "bokeh",
            "pillow",
            "openpyxl",
            "requests",
            "beautifulsoup4",
            "sympy",
            "networkx"});
            this.quickCombo.Location = new System.Drawing.Point(15, 100);
            this.quickCombo.Name = "quickCombo";
            this.quickCombo.Size = new System.Drawing.Size(200, 21);
            this.quickCombo.TabIndex = 5;
            // 
            // quickInstallLabel
            // 
            this.quickInstallLabel.AutoSize = true;
            this.quickInstallLabel.Location = new System.Drawing.Point(15, 80);
            this.quickInstallLabel.Name = "quickInstallLabel";
            this.quickInstallLabel.Size = new System.Drawing.Size(69, 13);
            this.quickInstallLabel.TabIndex = 4;
            this.quickInstallLabel.Text = "Quick install:";
            // 
            // uninstallBtn
            // 
            this.uninstallBtn.Location = new System.Drawing.Point(220, 74);
            this.uninstallBtn.Name = "uninstallBtn";
            this.uninstallBtn.Size = new System.Drawing.Size(90, 23);
            this.uninstallBtn.TabIndex = 3;
            this.uninstallBtn.Text = "Uninstall";
            this.uninstallBtn.UseVisualStyleBackColor = true;
            this.uninstallBtn.Click += new System.EventHandler(this.OnUninstallPackage);
            // 
            // installBtn
            // 
            this.installBtn.Location = new System.Drawing.Point(220, 44);
            this.installBtn.Name = "installBtn";
            this.installBtn.Size = new System.Drawing.Size(90, 23);
            this.installBtn.TabIndex = 2;
            this.installBtn.Text = "Install";
            this.installBtn.UseVisualStyleBackColor = true;
            this.installBtn.Click += new System.EventHandler(this.OnInstallPackage);
            // 
            // packageNameBox
            // 
            this.packageNameBox.Location = new System.Drawing.Point(15, 45);
            this.packageNameBox.Name = "packageNameBox";
            this.packageNameBox.Size = new System.Drawing.Size(200, 20);
            this.packageNameBox.TabIndex = 1;
            // 
            // pkgLabel
            // 
            this.pkgLabel.AutoSize = true;
            this.pkgLabel.Location = new System.Drawing.Point(15, 25);
            this.pkgLabel.Name = "pkgLabel";
            this.pkgLabel.Size = new System.Drawing.Size(80, 13);
            this.pkgLabel.TabIndex = 0;
            this.pkgLabel.Text = "Package name:";
            // 
            // DataScienceControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.mainTabs);
            this.Name = "DataScienceControl";
            this.Size = new System.Drawing.Size(800, 600);
            this.mainTabs.ResumeLayout(false);
            this.editorTab.ResumeLayout(false);
            this.mainSplit.Panel1.ResumeLayout(false);
            this.mainSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.mainSplit)).EndInit();
            this.mainSplit.ResumeLayout(false);
            this.topSplit.Panel1.ResumeLayout(false);
            this.topSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.topSplit)).EndInit();
            this.topSplit.ResumeLayout(false);
            this.treePanel.ResumeLayout(false);
            this.editorPanel.ResumeLayout(false);
            this.editorPanel.PerformLayout();
            this.toolBar.ResumeLayout(false);
            this.toolBar.PerformLayout();
            this.outputPanel.ResumeLayout(false);
            this.dataTab.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGrid)).EndInit();
            this.dataTopPanel.ResumeLayout(false);
            this.dataTopPanel.PerformLayout();
            this.packagesTab.ResumeLayout(false);
            this.pkgRightPanel.ResumeLayout(false);
            this.pkgLeftPanel.ResumeLayout(false);
            this.installGroup.ResumeLayout(false);
            this.installGroup.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TabControl mainTabs;
        private System.Windows.Forms.TabPage editorTab;
        private System.Windows.Forms.SplitContainer mainSplit;
        private System.Windows.Forms.SplitContainer topSplit;
        private System.Windows.Forms.Panel treePanel;
        private System.Windows.Forms.TreeView dataTreeView;
        private System.Windows.Forms.Label treeLabel;
        private System.Windows.Forms.Panel editorPanel;
        private System.Windows.Forms.RichTextBox pythonEditor;
        private DataScienceWorkbench.LineNumberPanel lineNumberPanel;
        private System.Windows.Forms.ToolStrip toolBar;
        private System.Windows.Forms.ToolStripButton runBtn;
        private System.Windows.Forms.ToolStripSeparator toolSep1;
        private System.Windows.Forms.ToolStripButton checkSyntaxBtn;
        private System.Windows.Forms.ToolStripSeparator toolSep2;
        private System.Windows.Forms.ToolStripButton clearBtn;
        private System.Windows.Forms.ToolStripSeparator toolSep3;
        private System.Windows.Forms.ToolStripDropDownButton insertSnippetBtn;
        private System.Windows.Forms.Panel outputPanel;
        private System.Windows.Forms.RichTextBox outputBox;
        private System.Windows.Forms.Label outputLabel;
        private System.Windows.Forms.TabPage dataTab;
        private System.Windows.Forms.DataGridView dataGrid;
        private System.Windows.Forms.Panel dataTopPanel;
        private System.Windows.Forms.Label recordCountLabel;
        private System.Windows.Forms.ComboBox datasetCombo;
        private System.Windows.Forms.Label datasetLabel;
        private System.Windows.Forms.TabPage packagesTab;
        private System.Windows.Forms.Panel pkgRightPanel;
        private System.Windows.Forms.ListBox packageListBox;
        private System.Windows.Forms.Label pkgListLabel;
        private System.Windows.Forms.Panel pkgLeftPanel;
        private System.Windows.Forms.Button refreshBtn;
        private System.Windows.Forms.GroupBox installGroup;
        private System.Windows.Forms.Button quickInstallBtn;
        private System.Windows.Forms.ComboBox quickCombo;
        private System.Windows.Forms.Label quickInstallLabel;
        private System.Windows.Forms.Button uninstallBtn;
        private System.Windows.Forms.Button installBtn;
        private System.Windows.Forms.TextBox packageNameBox;
        private System.Windows.Forms.Label pkgLabel;
    }
}
