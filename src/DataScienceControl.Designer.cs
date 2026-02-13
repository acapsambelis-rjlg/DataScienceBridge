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
            this.editorPanel = new System.Windows.Forms.Panel();
            this.pythonEditor = new DataScienceWorkbench.SquiggleRichTextBox();
            this.lineNumberPanel = new DataScienceWorkbench.LineNumberPanel();
            this.editorMenuBar = new System.Windows.Forms.MenuStrip();
            this.insertSnippetBtn = new System.Windows.Forms.ToolStripMenuItem();
            this.outputPanel = new System.Windows.Forms.Panel();
            this.outputBox = new System.Windows.Forms.RichTextBox();
            this.outputLabel = new System.Windows.Forms.Label();
            this.referenceTab = new System.Windows.Forms.TabPage();
            this.refSplit = new System.Windows.Forms.SplitContainer();
            this.refTreeView = new System.Windows.Forms.TreeView();
            this.refSearchBox = new System.Windows.Forms.TextBox();
            this.refDetailBox = new System.Windows.Forms.RichTextBox();
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
            this.editorPanel.SuspendLayout();
            this.editorMenuBar.SuspendLayout();
            this.outputPanel.SuspendLayout();
            this.referenceTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.refSplit)).BeginInit();
            this.refSplit.Panel1.SuspendLayout();
            this.refSplit.Panel2.SuspendLayout();
            this.refSplit.SuspendLayout();
            this.packagesTab.SuspendLayout();
            this.pkgRightPanel.SuspendLayout();
            this.pkgLeftPanel.SuspendLayout();
            this.installGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainTabs
            // 
            this.mainTabs.Controls.Add(this.editorTab);
            this.mainTabs.Controls.Add(this.referenceTab);
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
            this.mainSplit.Panel1.Controls.Add(this.editorPanel);
            // 
            // mainSplit.Panel2
            // 
            this.mainSplit.Panel2.Controls.Add(this.outputPanel);
            this.mainSplit.Size = new System.Drawing.Size(792, 574);
            this.mainSplit.SplitterDistance = 380;
            this.mainSplit.SplitterWidth = 6;
            this.mainSplit.TabIndex = 0;
            // 
            // editorPanel
            // 
            this.editorPanel.Controls.Add(this.pythonEditor);
            this.editorPanel.Controls.Add(this.lineNumberPanel);
            this.editorPanel.Controls.Add(this.editorMenuBar);
            this.editorPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.editorPanel.Location = new System.Drawing.Point(0, 0);
            this.editorPanel.Name = "editorPanel";
            this.editorPanel.Size = new System.Drawing.Size(792, 380);
            this.editorPanel.TabIndex = 0;
            // 
            // pythonEditor
            // 
            this.pythonEditor.AcceptsTab = true;
            this.pythonEditor.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.pythonEditor.DetectUrls = false;
            this.pythonEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pythonEditor.Font = new System.Drawing.Font("Consolas", 10F);
            this.pythonEditor.HideSelection = false;
            this.pythonEditor.Location = new System.Drawing.Point(60, 24);
            this.pythonEditor.Name = "pythonEditor";
            this.pythonEditor.Size = new System.Drawing.Size(732, 355);
            this.pythonEditor.TabIndex = 0;
            this.pythonEditor.Text = "";
            this.pythonEditor.WordWrap = false;
            // 
            // lineNumberPanel
            // 
            this.lineNumberPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.lineNumberPanel.Location = new System.Drawing.Point(0, 24);
            this.lineNumberPanel.Name = "lineNumberPanel";
            this.lineNumberPanel.Size = new System.Drawing.Size(60, 355);
            this.lineNumberPanel.TabIndex = 1;
            // 
            // editorMenuBar
            // 
            this.editorMenuBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.insertSnippetBtn});
            this.editorMenuBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.editorMenuBar.Location = new System.Drawing.Point(0, 0);
            this.editorMenuBar.Name = "editorMenuBar";
            this.editorMenuBar.Size = new System.Drawing.Size(792, 24);
            this.editorMenuBar.TabIndex = 2;
            // 
            // insertSnippetBtn
            // 
            this.insertSnippetBtn.Name = "insertSnippetBtn";
            this.insertSnippetBtn.Size = new System.Drawing.Size(95, 20);
            this.insertSnippetBtn.Text = "Insert Snippet";
            // 
            // outputPanel
            // 
            this.outputPanel.Controls.Add(this.outputBox);
            this.outputPanel.Controls.Add(this.outputLabel);
            this.outputPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.outputPanel.Location = new System.Drawing.Point(0, 0);
            this.outputPanel.Name = "outputPanel";
            this.outputPanel.Size = new System.Drawing.Size(792, 188);
            this.outputPanel.TabIndex = 0;
            // 
            // outputBox
            // 
            this.outputBox.BackColor = System.Drawing.Color.White;
            this.outputBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.outputBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.outputBox.Font = new System.Drawing.Font("Consolas", 9.5F);
            this.outputBox.ForeColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.outputBox.Location = new System.Drawing.Point(0, 22);
            this.outputBox.Name = "outputBox";
            this.outputBox.ReadOnly = true;
            this.outputBox.Size = new System.Drawing.Size(792, 166);
            this.outputBox.TabIndex = 0;
            this.outputBox.Text = "";
            this.outputBox.WordWrap = false;
            // 
            // outputLabel
            // 
            this.outputLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.outputLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.outputLabel.Location = new System.Drawing.Point(0, 0);
            this.outputLabel.Name = "outputLabel";
            this.outputLabel.Padding = new System.Windows.Forms.Padding(4, 4, 0, 0);
            this.outputLabel.Size = new System.Drawing.Size(792, 22);
            this.outputLabel.TabIndex = 1;
            this.outputLabel.Text = "Output:";
            // 
            // referenceTab
            // 
            this.referenceTab.Controls.Add(this.refSplit);
            this.referenceTab.Location = new System.Drawing.Point(4, 22);
            this.referenceTab.Name = "referenceTab";
            this.referenceTab.Size = new System.Drawing.Size(792, 574);
            this.referenceTab.TabIndex = 1;
            this.referenceTab.Text = "Data Reference";
            this.referenceTab.UseVisualStyleBackColor = true;
            // 
            // refSplit
            // 
            this.refSplit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.refSplit.Location = new System.Drawing.Point(0, 0);
            this.refSplit.Name = "refSplit";
            // 
            // refSplit.Panel1
            // 
            this.refSplit.Panel1.Controls.Add(this.refTreeView);
            this.refSplit.Panel1.Controls.Add(this.refSearchBox);
            // 
            // refSplit.Panel2
            // 
            this.refSplit.Panel2.Controls.Add(this.refDetailBox);
            this.refSplit.Size = new System.Drawing.Size(792, 574);
            this.refSplit.SplitterDistance = 260;
            this.refSplit.SplitterWidth = 6;
            this.refSplit.TabIndex = 0;
            // 
            // refSearchBox
            // 
            this.refSearchBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.refSearchBox.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.refSearchBox.ForeColor = System.Drawing.Color.Gray;
            this.refSearchBox.Location = new System.Drawing.Point(0, 0);
            this.refSearchBox.Name = "refSearchBox";
            this.refSearchBox.Size = new System.Drawing.Size(260, 24);
            this.refSearchBox.TabIndex = 1;
            this.refSearchBox.Text = "Search...";
            // 
            // refTreeView
            // 
            this.refTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.refTreeView.Font = new System.Drawing.Font("Consolas", 9.5F);
            this.refTreeView.Location = new System.Drawing.Point(0, 24);
            this.refTreeView.Name = "refTreeView";
            this.refTreeView.Size = new System.Drawing.Size(260, 550);
            this.refTreeView.TabIndex = 0;
            this.refTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.OnRefTreeSelect);
            // 
            // refDetailBox
            // 
            this.refDetailBox.BackColor = System.Drawing.Color.White;
            this.refDetailBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.refDetailBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.refDetailBox.Font = new System.Drawing.Font("Consolas", 10F);
            this.refDetailBox.Location = new System.Drawing.Point(0, 0);
            this.refDetailBox.Name = "refDetailBox";
            this.refDetailBox.ReadOnly = true;
            this.refDetailBox.Size = new System.Drawing.Size(526, 574);
            this.refDetailBox.TabIndex = 0;
            this.refDetailBox.Text = "";
            this.refDetailBox.WordWrap = false;
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
            this.pkgRightPanel.Controls.Add(this.pkgListLabel);
            this.pkgRightPanel.Controls.Add(this.pkgSearchBox);
            this.pkgRightPanel.Controls.Add(this.packageListBox);
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
            this.packageListBox.Font = new System.Drawing.Font("Consolas", 9F);
            this.packageListBox.FormattingEnabled = true;
            this.packageListBox.Location = new System.Drawing.Point(10, 32);
            this.packageListBox.Name = "packageListBox";
            this.packageListBox.Size = new System.Drawing.Size(422, 532);
            this.packageListBox.TabIndex = 0;
            // 
            // pkgListLabel
            // 
            this.pkgListLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.pkgListLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.pkgListLabel.Location = new System.Drawing.Point(10, 10);
            this.pkgListLabel.Name = "pkgListLabel";
            this.pkgListLabel.Size = new System.Drawing.Size(422, 22);
            this.pkgListLabel.TabIndex = 1;
            this.pkgListLabel.Text = "Installed Packages:";
            // 
            // pkgSearchBox
            // 
            this.pkgSearchBox = new System.Windows.Forms.TextBox();
            this.pkgSearchBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.pkgSearchBox.Font = new System.Drawing.Font("Consolas", 9F);
            this.pkgSearchBox.Location = new System.Drawing.Point(10, 32);
            this.pkgSearchBox.Name = "pkgSearchBox";
            this.pkgSearchBox.Size = new System.Drawing.Size(422, 22);
            this.pkgSearchBox.TabIndex = 2;
            this.pkgSearchBox.TextChanged += new System.EventHandler(this.OnPkgSearchChanged);
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
            this.editorPanel.ResumeLayout(false);
            this.editorPanel.PerformLayout();
            this.editorMenuBar.ResumeLayout(false);
            this.editorMenuBar.PerformLayout();
            this.outputPanel.ResumeLayout(false);
            this.referenceTab.ResumeLayout(false);
            this.refSplit.Panel1.ResumeLayout(false);
            this.refSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.refSplit)).EndInit();
            this.refSplit.ResumeLayout(false);
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
        private System.Windows.Forms.Panel editorPanel;
        private DataScienceWorkbench.SquiggleRichTextBox pythonEditor;
        private DataScienceWorkbench.LineNumberPanel lineNumberPanel;
        private System.Windows.Forms.MenuStrip editorMenuBar;
        private System.Windows.Forms.ToolStripMenuItem insertSnippetBtn;
        private System.Windows.Forms.Panel outputPanel;
        private System.Windows.Forms.RichTextBox outputBox;
        private System.Windows.Forms.Label outputLabel;
        private System.Windows.Forms.TabPage referenceTab;
        private System.Windows.Forms.SplitContainer refSplit;
        private System.Windows.Forms.TreeView refTreeView;
        private System.Windows.Forms.TextBox refSearchBox;
        private System.Windows.Forms.RichTextBox refDetailBox;
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
        private System.Windows.Forms.TextBox pkgSearchBox;
    }
}
