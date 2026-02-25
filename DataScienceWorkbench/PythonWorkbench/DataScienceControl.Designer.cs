using CodeEditor;
using Telerik.WinControls;

namespace RJLG.IntelliSEM.UI.Controls.PythonDataScience
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
            this.dockPanel = new WeifenLuo.WinFormsUI.Docking.DockPanel();
            this.fileListPanel = new Telerik.WinControls.UI.RadPanel();
            this.fileTreeView = new Telerik.WinControls.UI.RadTreeView();
            this.fileListLabel = new Telerik.WinControls.UI.RadLabel();
            this.fileListButtonPanel = new Telerik.WinControls.UI.RadPanel();
            this.fileNewBtn = new Telerik.WinControls.UI.RadButton();
            this.fileOpenBtn = new Telerik.WinControls.UI.RadButton();
            this.fileCloseBtn = new Telerik.WinControls.UI.RadButton();
            this.editorMenuBar = new Telerik.WinControls.UI.RadMenu();
            this.insertSnippetBtn = new Telerik.WinControls.UI.RadMenuItem();
            this.editorToolBar = new System.Windows.Forms.ToolStrip();
            this.runToolBtn = new System.Windows.Forms.ToolStripButton();
            this.syntaxCheckToolBtn = new System.Windows.Forms.ToolStripButton();
            this.saveToolBtn = new System.Windows.Forms.ToolStripButton();
            this.undoToolBtn = new System.Windows.Forms.ToolStripButton();
            this.redoToolBtn = new System.Windows.Forms.ToolStripButton();
            this.findToolBtn = new System.Windows.Forms.ToolStripButton();
            this.outputPanel = new Telerik.WinControls.UI.RadPanel();
            this.outputBox = new System.Windows.Forms.RichTextBox();
            this.outputLabel = new Telerik.WinControls.UI.RadLabel();
            this.refPanel = new Telerik.WinControls.UI.RadPanel();
            this.refSplit = new Telerik.WinControls.UI.RadSplitContainer();
            this.refSplitTreePanel = new Telerik.WinControls.UI.SplitPanel();
            this.refSplitDetailPanel = new Telerik.WinControls.UI.SplitPanel();
            this.refTreeView = new Telerik.WinControls.UI.RadTreeView();
            this.refSearchBox = new Telerik.WinControls.UI.RadTextBox();
            this.refDetailBox = new System.Windows.Forms.RichTextBox();
            this.pkgPanel = new Telerik.WinControls.UI.RadPanel();
            this.packageListBox = new System.Windows.Forms.ListBox();
            this.pkgListLabel = new Telerik.WinControls.UI.RadLabel();
            this.pkgSearchBox = new Telerik.WinControls.UI.RadTextBox();
            this.refreshBtn = new Telerik.WinControls.UI.RadButton();
            this.installGroup = new Telerik.WinControls.UI.RadGroupBox();
            this.pkgBtnPanel = new Telerik.WinControls.UI.RadPanel();
            this.quickInstallBtn = new Telerik.WinControls.UI.RadButton();
            this.quickCombo = new Telerik.WinControls.UI.RadDropDownList();
            this.quickInstallLabel = new Telerik.WinControls.UI.RadLabel();
            this.uninstallBtn = new Telerik.WinControls.UI.RadButton();
            this.installBtn = new Telerik.WinControls.UI.RadButton();
            this.packageNameBox = new Telerik.WinControls.UI.RadTextBox();
            this.pkgLabel = new Telerik.WinControls.UI.RadLabel();
            this.fileListPanel.SuspendLayout();
            this.fileListButtonPanel.SuspendLayout();
            this.outputPanel.SuspendLayout();
            this.refPanel.SuspendLayout();
            this.pkgPanel.SuspendLayout();
            this.pkgBtnPanel.SuspendLayout();
            this.installGroup.SuspendLayout();
            this.SuspendLayout();
            //
            // dockPanel
            //
            this.dockPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dockPanel.DocumentStyle = WeifenLuo.WinFormsUI.Docking.DocumentStyle.DockingWindow;
            this.dockPanel.Location = new System.Drawing.Point(0, 0);
            this.dockPanel.Name = "dockPanel";
            this.dockPanel.Size = new System.Drawing.Size(800, 600);
            this.dockPanel.TabIndex = 0;
            //
            // fileListPanel
            //
            this.fileListPanel.Controls.Add(this.fileTreeView);
            this.fileListPanel.Controls.Add(this.fileListButtonPanel);
            this.fileListPanel.Controls.Add(this.fileListLabel);
            this.fileListPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fileListPanel.Name = "fileListPanel";
            this.fileListPanel.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            //
            // fileListLabel
            //
            this.fileListLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.fileListLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.fileListLabel.Name = "fileListLabel";
            this.fileListLabel.Padding = new System.Windows.Forms.Padding(6, 4, 0, 0);
            this.fileListLabel.Size = new System.Drawing.Size(200, 24);
            this.fileListLabel.Text = "Files";
            //
            // fileTreeView
            //
            this.fileTreeView.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            this.fileTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fileTreeView.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.fileTreeView.FullRowSelect = true;
            this.fileTreeView.ShowLines = true;
            this.fileTreeView.AllowEdit = false;
            this.fileTreeView.TreeIndent = 16;
            this.fileTreeView.Name = "fileTreeView";
            //
            // fileListButtonPanel
            //
            this.fileListButtonPanel.Controls.Add(this.fileNewBtn);
            this.fileListButtonPanel.Controls.Add(this.fileOpenBtn);
            this.fileListButtonPanel.Controls.Add(this.fileCloseBtn);
            this.fileListButtonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.fileListButtonPanel.Name = "fileListButtonPanel";
            this.fileListButtonPanel.Size = new System.Drawing.Size(200, 28);
            //
            // fileNewBtn
            //
            this.fileNewBtn.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.fileNewBtn.Location = new System.Drawing.Point(4, 2);
            this.fileNewBtn.Name = "fileNewBtn";
            this.fileNewBtn.Size = new System.Drawing.Size(44, 24);
            this.fileNewBtn.Text = "+";
            //
            // fileOpenBtn
            //
            this.fileOpenBtn.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.fileOpenBtn.Location = new System.Drawing.Point(52, 2);
            this.fileOpenBtn.Name = "fileOpenBtn";
            this.fileOpenBtn.Size = new System.Drawing.Size(55, 24);
            this.fileOpenBtn.Text = "Open";
            //
            // fileCloseBtn
            //
            this.fileCloseBtn.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.fileCloseBtn.Location = new System.Drawing.Point(111, 2);
            this.fileCloseBtn.Name = "fileCloseBtn";
            this.fileCloseBtn.Size = new System.Drawing.Size(44, 24);
            this.fileCloseBtn.Text = "\u00d7";
            //
            // editorMenuBar
            //
            this.editorMenuBar.Items.AddRange(new[] { this.insertSnippetBtn });
            this.editorMenuBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.editorMenuBar.Name = "editorMenuBar";
            this.editorMenuBar.Size = new System.Drawing.Size(792, 24);
            //
            // insertSnippetBtn
            //
            this.insertSnippetBtn.Name = "insertSnippetBtn";
            this.insertSnippetBtn.Size = new System.Drawing.Size(95, 20);
            this.insertSnippetBtn.Text = "Insert Snippet";
            //
            // editorToolBar
            //
            this.editorToolBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.runToolBtn,
            this.syntaxCheckToolBtn,
            new System.Windows.Forms.ToolStripSeparator(),
            this.saveToolBtn,
            new System.Windows.Forms.ToolStripSeparator(),
            this.undoToolBtn,
            this.redoToolBtn,
            new System.Windows.Forms.ToolStripSeparator(),
            this.findToolBtn});
            this.editorToolBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.editorToolBar.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.editorToolBar.Name = "editorToolBar";
            this.editorToolBar.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.editorToolBar.BackColor = System.Drawing.SystemColors.Control;
            //
            // runToolBtn
            //
            this.runToolBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.runToolBtn.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.runToolBtn.ForeColor = System.Drawing.Color.FromArgb(34, 139, 34);
            this.runToolBtn.Name = "runToolBtn";
            this.runToolBtn.Text = "\u25B6 Run";
            this.runToolBtn.ToolTipText = "Execute Script (F5)";
            this.runToolBtn.Click += new System.EventHandler(this.OnRunScript);
            //
            // syntaxCheckToolBtn
            //
            this.syntaxCheckToolBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.syntaxCheckToolBtn.Name = "syntaxCheckToolBtn";
            this.syntaxCheckToolBtn.Text = "Check Syntax";
            this.syntaxCheckToolBtn.ToolTipText = "Check Python syntax for errors";
            this.syntaxCheckToolBtn.Click += new System.EventHandler(this.OnCheckSyntax);
            //
            // saveToolBtn
            //
            this.saveToolBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.saveToolBtn.Name = "saveToolBtn";
            this.saveToolBtn.Text = "Save";
            this.saveToolBtn.ToolTipText = "Save (Ctrl+S)";
            this.saveToolBtn.Click += new System.EventHandler(this.OnSaveFile);
            //
            // undoToolBtn
            //
            this.undoToolBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.undoToolBtn.Name = "undoToolBtn";
            this.undoToolBtn.Text = "Undo";
            this.undoToolBtn.ToolTipText = "Undo (Ctrl+Z)";
            this.undoToolBtn.Click += new System.EventHandler(this.OnToolbarUndo);
            //
            // redoToolBtn
            //
            this.redoToolBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.redoToolBtn.Name = "redoToolBtn";
            this.redoToolBtn.Text = "Redo";
            this.redoToolBtn.ToolTipText = "Redo (Ctrl+Y)";
            this.redoToolBtn.Click += new System.EventHandler(this.OnToolbarRedo);
            //
            // findToolBtn
            //
            this.findToolBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.findToolBtn.Name = "findToolBtn";
            this.findToolBtn.Text = "Find";
            this.findToolBtn.ToolTipText = "Find && Replace (Ctrl+F)";
            this.findToolBtn.Click += new System.EventHandler(this.OnToolbarFind);
            //
            // outputPanel
            //
            this.outputPanel.Controls.Add(this.outputBox);
            this.outputPanel.Controls.Add(this.outputLabel);
            this.outputPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.outputPanel.Name = "outputPanel";
            //
            // outputBox
            //
            this.outputBox.BackColor = System.Drawing.Color.White;
            this.outputBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.outputBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.outputBox.Font = new System.Drawing.Font("Consolas", 9.5F);
            this.outputBox.ForeColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.outputBox.Name = "outputBox";
            this.outputBox.ReadOnly = true;
            this.outputBox.Text = "";
            this.outputBox.WordWrap = false;
            //
            // outputLabel
            //
            this.outputLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.outputLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.outputLabel.Name = "outputLabel";
            this.outputLabel.Padding = new System.Windows.Forms.Padding(4, 4, 0, 0);
            this.outputLabel.Size = new System.Drawing.Size(792, 22);
            this.outputLabel.Text = "Output:";
            //
            // refPanel
            //
            this.refPanel.Controls.Add(this.refSplit);
            this.refPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.refPanel.Name = "refPanel";
            //
            // refSplit
            //
            this.refSplit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.refSplit.Name = "refSplit";
            //
            // refSplitTreePanel
            //
            this.refSplitTreePanel.Controls.Add(this.refTreeView);
            this.refSplitTreePanel.Controls.Add(this.refSearchBox);
            this.refSplitTreePanel.SizeInfo.SizeMode = Telerik.WinControls.UI.Docking.SplitPanelSizeMode.Absolute;
            this.refSplitTreePanel.SizeInfo.AbsoluteSize = new System.Drawing.Size(260, 574);
            this.refSplitTreePanel.Name = "refSplitTreePanel";
            //
            // refSplitDetailPanel
            //
            this.refSplitDetailPanel.Controls.Add(this.refDetailBox);
            this.refSplitDetailPanel.Name = "refSplitDetailPanel";
            //
            // refSplit.SplitPanels
            //
            this.refSplit.SplitPanels.AddRange(new Telerik.WinControls.UI.SplitPanel[] { this.refSplitTreePanel, this.refSplitDetailPanel });
            //
            // refSearchBox
            //
            this.refSearchBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.refSearchBox.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.refSearchBox.ForeColor = System.Drawing.Color.Gray;
            this.refSearchBox.Name = "refSearchBox";
            this.refSearchBox.Size = new System.Drawing.Size(260, 24);
            this.refSearchBox.Text = "Search...";
            //
            // refTreeView
            //
            this.refTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.refTreeView.Font = new System.Drawing.Font("Consolas", 9.5F);
            this.refTreeView.Name = "refTreeView";
            this.refTreeView.SelectedNodeChanged += new Telerik.WinControls.UI.RadTreeView.RadTreeViewEventHandler(this.OnRefTreeSelect);
            //
            // refDetailBox
            //
            this.refDetailBox.BackColor = System.Drawing.Color.White;
            this.refDetailBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.refDetailBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.refDetailBox.Font = new System.Drawing.Font("Consolas", 10F);
            this.refDetailBox.Name = "refDetailBox";
            this.refDetailBox.ReadOnly = true;
            this.refDetailBox.Text = "";
            this.refDetailBox.WordWrap = false;
            //
            // pkgPanel
            //
            this.pkgPanel.Controls.Add(this.packageListBox);
            this.pkgPanel.Controls.Add(this.pkgSearchBox);
            this.pkgPanel.Controls.Add(this.pkgListLabel);
            this.pkgPanel.Controls.Add(this.refreshBtn);
            this.pkgPanel.Controls.Add(this.installGroup);
            this.pkgPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pkgPanel.Name = "pkgPanel";
            this.pkgPanel.Padding = new System.Windows.Forms.Padding(8);
            //
            // packageListBox
            //
            this.packageListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.packageListBox.Font = new System.Drawing.Font("Consolas", 9F);
            this.packageListBox.FormattingEnabled = true;
            this.packageListBox.Name = "packageListBox";
            //
            // pkgSearchBox
            //
            this.pkgSearchBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.pkgSearchBox.Font = new System.Drawing.Font("Consolas", 9F);
            this.pkgSearchBox.Name = "pkgSearchBox";
            this.pkgSearchBox.TextChanged += new System.EventHandler(this.OnPkgSearchChanged);
            //
            // pkgListLabel
            //
            this.pkgListLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.pkgListLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.pkgListLabel.Name = "pkgListLabel";
            this.pkgListLabel.Size = new System.Drawing.Size(200, 22);
            this.pkgListLabel.Text = "Installed Packages:";
            //
            // refreshBtn
            //
            this.refreshBtn.Dock = System.Windows.Forms.DockStyle.Top;
            this.refreshBtn.Name = "refreshBtn";
            this.refreshBtn.Size = new System.Drawing.Size(200, 28);
            this.refreshBtn.Text = "Refresh";
            this.refreshBtn.Click += new System.EventHandler(this.OnRefreshPackages);
            //
            // installGroup
            //
            this.installGroup.Controls.Add(this.quickInstallBtn);
            this.installGroup.Controls.Add(this.quickCombo);
            this.installGroup.Controls.Add(this.quickInstallLabel);
            this.installGroup.Controls.Add(this.pkgBtnPanel);
            this.installGroup.Controls.Add(this.packageNameBox);
            this.installGroup.Controls.Add(this.pkgLabel);
            this.installGroup.Dock = System.Windows.Forms.DockStyle.Top;
            this.installGroup.Name = "installGroup";
            this.installGroup.Padding = new System.Windows.Forms.Padding(8, 20, 8, 4);
            this.installGroup.Size = new System.Drawing.Size(200, 175);
            this.installGroup.Text = "Install / Uninstall";
            //
            // pkgLabel
            //
            this.pkgLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.pkgLabel.Name = "pkgLabel";
            this.pkgLabel.Size = new System.Drawing.Size(184, 20);
            this.pkgLabel.Text = "Package name:";
            //
            // packageNameBox
            //
            this.packageNameBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.packageNameBox.Name = "packageNameBox";
            //
            // pkgBtnPanel
            //
            this.pkgBtnPanel.Controls.Add(this.uninstallBtn);
            this.pkgBtnPanel.Controls.Add(this.installBtn);
            this.pkgBtnPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.pkgBtnPanel.Name = "pkgBtnPanel";
            this.pkgBtnPanel.Size = new System.Drawing.Size(184, 28);
            //
            // installBtn
            //
            this.installBtn.Dock = System.Windows.Forms.DockStyle.Left;
            this.installBtn.Name = "installBtn";
            this.installBtn.Size = new System.Drawing.Size(90, 28);
            this.installBtn.Text = "Install";
            this.installBtn.Click += new System.EventHandler(this.OnInstallPackage);
            //
            // uninstallBtn
            //
            this.uninstallBtn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uninstallBtn.Name = "uninstallBtn";
            this.uninstallBtn.Size = new System.Drawing.Size(94, 28);
            this.uninstallBtn.Text = "Uninstall";
            this.uninstallBtn.Click += new System.EventHandler(this.OnUninstallPackage);
            //
            // quickInstallLabel
            //
            this.quickInstallLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.quickInstallLabel.Name = "quickInstallLabel";
            this.quickInstallLabel.Size = new System.Drawing.Size(184, 20);
            this.quickInstallLabel.Text = "Quick install:";
            //
            // quickCombo
            //
            this.quickCombo.Dock = System.Windows.Forms.DockStyle.Top;
            this.quickCombo.DropDownStyle = RadDropDownStyle.DropDownList;
            this.quickCombo.Items.Add("scipy");
            this.quickCombo.Items.Add("scikit-learn");
            this.quickCombo.Items.Add("seaborn");
            this.quickCombo.Items.Add("statsmodels");
            this.quickCombo.Items.Add("plotly");
            this.quickCombo.Items.Add("bokeh");
            this.quickCombo.Items.Add("pillow");
            this.quickCombo.Items.Add("openpyxl");
            this.quickCombo.Items.Add("requests");
            this.quickCombo.Items.Add("beautifulsoup4");
            this.quickCombo.Items.Add("sympy");
            this.quickCombo.Items.Add("networkx");
            this.quickCombo.Name = "quickCombo";
            //
            // quickInstallBtn
            //
            this.quickInstallBtn.Dock = System.Windows.Forms.DockStyle.Top;
            this.quickInstallBtn.Name = "quickInstallBtn";
            this.quickInstallBtn.Size = new System.Drawing.Size(184, 28);
            this.quickInstallBtn.Text = "Quick Install";
            this.quickInstallBtn.Click += new System.EventHandler(this.OnQuickInstall);
            //
            // DataScienceControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dockPanel);
            this.Controls.Add(this.editorToolBar);
            this.Controls.Add(this.editorMenuBar);
            this.Name = "DataScienceControl";
            this.Size = new System.Drawing.Size(800, 600);
            this.fileListButtonPanel.ResumeLayout(false);
            this.fileListPanel.ResumeLayout(false);
            this.outputPanel.ResumeLayout(false);
            this.refPanel.ResumeLayout(false);
            this.pkgPanel.ResumeLayout(false);
            this.pkgBtnPanel.ResumeLayout(false);
            this.installGroup.ResumeLayout(false);
            this.installGroup.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private WeifenLuo.WinFormsUI.Docking.DockPanel dockPanel;
        private Telerik.WinControls.UI.RadMenu editorMenuBar;
        private Telerik.WinControls.UI.RadMenuItem insertSnippetBtn;
        private System.Windows.Forms.ToolStrip editorToolBar;
        private System.Windows.Forms.ToolStripButton runToolBtn;
        private System.Windows.Forms.ToolStripButton syntaxCheckToolBtn;
        private System.Windows.Forms.ToolStripButton saveToolBtn;
        private System.Windows.Forms.ToolStripButton undoToolBtn;
        private System.Windows.Forms.ToolStripButton redoToolBtn;
        private System.Windows.Forms.ToolStripButton findToolBtn;
        private Telerik.WinControls.UI.RadPanel outputPanel;
        private System.Windows.Forms.RichTextBox outputBox;
        private Telerik.WinControls.UI.RadLabel outputLabel;
        private Telerik.WinControls.UI.RadPanel refPanel;
        private Telerik.WinControls.UI.RadSplitContainer refSplit;
        private Telerik.WinControls.UI.SplitPanel refSplitTreePanel;
        private Telerik.WinControls.UI.SplitPanel refSplitDetailPanel;
        private Telerik.WinControls.UI.RadTreeView refTreeView;
        private Telerik.WinControls.UI.RadTextBox refSearchBox;
        private System.Windows.Forms.RichTextBox refDetailBox;
        private Telerik.WinControls.UI.RadPanel pkgPanel;
        private Telerik.WinControls.UI.RadPanel pkgBtnPanel;
        private System.Windows.Forms.ListBox packageListBox;
        private Telerik.WinControls.UI.RadLabel pkgListLabel;
        private Telerik.WinControls.UI.RadButton refreshBtn;
        private Telerik.WinControls.UI.RadGroupBox installGroup;
        private Telerik.WinControls.UI.RadButton quickInstallBtn;
        private Telerik.WinControls.UI.RadDropDownList quickCombo;
        private Telerik.WinControls.UI.RadLabel quickInstallLabel;
        private Telerik.WinControls.UI.RadButton uninstallBtn;
        private Telerik.WinControls.UI.RadButton installBtn;
        private Telerik.WinControls.UI.RadTextBox packageNameBox;
        private Telerik.WinControls.UI.RadLabel pkgLabel;
        private Telerik.WinControls.UI.RadTextBox pkgSearchBox;
        private Telerik.WinControls.UI.RadPanel fileListPanel;
        private Telerik.WinControls.UI.RadTreeView fileTreeView;
        private Telerik.WinControls.UI.RadLabel fileListLabel;
        private Telerik.WinControls.UI.RadPanel fileListButtonPanel;
        private Telerik.WinControls.UI.RadButton fileNewBtn;
        private Telerik.WinControls.UI.RadButton fileOpenBtn;
        private Telerik.WinControls.UI.RadButton fileCloseBtn;
    }
}
