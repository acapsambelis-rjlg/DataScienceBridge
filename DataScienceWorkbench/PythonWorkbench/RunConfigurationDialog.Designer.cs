namespace RJLG.IntelliSEM.UI.Controls.PythonDataScience
{
    partial class RunConfigurationDialog
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

        private void InitializeComponent()
        {
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.listPanel = new System.Windows.Forms.Panel();
            this.configListBox = new System.Windows.Forms.ListBox();
            this.listLabel = new System.Windows.Forms.Label();
            this.listBtnPanel = new System.Windows.Forms.Panel();
            this.addBtn = new System.Windows.Forms.Button();
            this.removeBtn = new System.Windows.Forms.Button();
            this.duplicateBtn = new System.Windows.Forms.Button();
            this.detailPanel = new System.Windows.Forms.Panel();
            this.nameLabel = new System.Windows.Forms.Label();
            this.nameBox = new System.Windows.Forms.TextBox();
            this.useCurrentRadio = new System.Windows.Forms.RadioButton();
            this.useSpecificRadio = new System.Windows.Forms.RadioButton();
            this.scriptLabel = new System.Windows.Forms.Label();
            this.scriptBox = new System.Windows.Forms.TextBox();
            this.browseScriptBtn = new System.Windows.Forms.Button();
            this.argsLabel = new System.Windows.Forms.Label();
            this.argsBox = new System.Windows.Forms.TextBox();
            this.inputLabel = new System.Windows.Forms.Label();
            this.inputFileBox = new System.Windows.Forms.TextBox();
            this.browseInputBtn = new System.Windows.Forms.Button();
            this.clearInputBtn = new System.Windows.Forms.Button();
            this.inputHintLabel = new System.Windows.Forms.Label();
            this.bottomPanel = new System.Windows.Forms.Panel();
            this.okBtn = new System.Windows.Forms.Button();
            this.cancelBtn = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.listPanel.SuspendLayout();
            this.listBtnPanel.SuspendLayout();
            this.detailPanel.SuspendLayout();
            this.bottomPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // splitContainer
            //
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer.Location = new System.Drawing.Point(0, 0);
            this.splitContainer.Name = "splitContainer";
            this.splitContainer.SplitterDistance = 180;
            this.splitContainer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            //
            // splitContainer.Panel1
            //
            this.splitContainer.Panel1.Controls.Add(this.listPanel);
            //
            // splitContainer.Panel2
            //
            this.splitContainer.Panel2.Controls.Add(this.detailPanel);
            //
            // listPanel
            //
            this.listPanel.Controls.Add(this.configListBox);
            this.listPanel.Controls.Add(this.listLabel);
            this.listPanel.Controls.Add(this.listBtnPanel);
            this.listPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listPanel.Name = "listPanel";
            //
            // configListBox
            //
            this.configListBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.configListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.configListBox.IntegralHeight = false;
            this.configListBox.Name = "configListBox";
            this.configListBox.SelectedIndexChanged += new System.EventHandler(this.OnConfigSelected);
            //
            // listLabel
            //
            this.listLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.listLabel.Font = new System.Drawing.Font(this.Font.FontFamily, 9F, System.Drawing.FontStyle.Bold);
            this.listLabel.Height = 22;
            this.listLabel.Name = "listLabel";
            this.listLabel.Padding = new System.Windows.Forms.Padding(4, 4, 0, 0);
            this.listLabel.Text = "Configurations:";
            //
            // listBtnPanel
            //
            this.listBtnPanel.Controls.Add(this.addBtn);
            this.listBtnPanel.Controls.Add(this.removeBtn);
            this.listBtnPanel.Controls.Add(this.duplicateBtn);
            this.listBtnPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.listBtnPanel.Height = 32;
            this.listBtnPanel.Name = "listBtnPanel";
            this.listBtnPanel.Padding = new System.Windows.Forms.Padding(2);
            //
            // addBtn
            //
            this.addBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.addBtn.Location = new System.Drawing.Point(2, 3);
            this.addBtn.Name = "addBtn";
            this.addBtn.Size = new System.Drawing.Size(32, 26);
            this.addBtn.Text = "+";
            this.addBtn.Click += new System.EventHandler(this.OnAddConfig);
            //
            // removeBtn
            //
            this.removeBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.removeBtn.Location = new System.Drawing.Point(36, 3);
            this.removeBtn.Name = "removeBtn";
            this.removeBtn.Size = new System.Drawing.Size(32, 26);
            this.removeBtn.Text = "\u2212";
            this.removeBtn.Click += new System.EventHandler(this.OnRemoveConfig);
            //
            // duplicateBtn
            //
            this.duplicateBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.duplicateBtn.Location = new System.Drawing.Point(70, 3);
            this.duplicateBtn.Name = "duplicateBtn";
            this.duplicateBtn.Size = new System.Drawing.Size(46, 26);
            this.duplicateBtn.Text = "Copy";
            this.duplicateBtn.Click += new System.EventHandler(this.OnDuplicateConfig);
            //
            // detailPanel
            //
            this.detailPanel.Controls.Add(this.inputHintLabel);
            this.detailPanel.Controls.Add(this.clearInputBtn);
            this.detailPanel.Controls.Add(this.browseInputBtn);
            this.detailPanel.Controls.Add(this.inputFileBox);
            this.detailPanel.Controls.Add(this.inputLabel);
            this.detailPanel.Controls.Add(this.argsBox);
            this.detailPanel.Controls.Add(this.argsLabel);
            this.detailPanel.Controls.Add(this.browseScriptBtn);
            this.detailPanel.Controls.Add(this.scriptBox);
            this.detailPanel.Controls.Add(this.scriptLabel);
            this.detailPanel.Controls.Add(this.useSpecificRadio);
            this.detailPanel.Controls.Add(this.useCurrentRadio);
            this.detailPanel.Controls.Add(this.nameBox);
            this.detailPanel.Controls.Add(this.nameLabel);
            this.detailPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.detailPanel.Name = "detailPanel";
            this.detailPanel.Padding = new System.Windows.Forms.Padding(8);
            //
            // nameLabel
            //
            this.nameLabel.AutoSize = true;
            this.nameLabel.Location = new System.Drawing.Point(8, 8);
            this.nameLabel.Name = "nameLabel";
            this.nameLabel.Text = "Name:";
            //
            // nameBox
            //
            this.nameBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.nameBox.Location = new System.Drawing.Point(8, 28);
            this.nameBox.Name = "nameBox";
            this.nameBox.TextChanged += new System.EventHandler(this.OnNameChanged);
            //
            // useCurrentRadio
            //
            this.useCurrentRadio.AutoSize = true;
            this.useCurrentRadio.Checked = true;
            this.useCurrentRadio.Location = new System.Drawing.Point(8, 58);
            this.useCurrentRadio.Name = "useCurrentRadio";
            this.useCurrentRadio.TabStop = true;
            this.useCurrentRadio.Text = "Run current file";
            this.useCurrentRadio.CheckedChanged += new System.EventHandler(this.OnRadioChanged);
            //
            // useSpecificRadio
            //
            this.useSpecificRadio.AutoSize = true;
            this.useSpecificRadio.Location = new System.Drawing.Point(8, 82);
            this.useSpecificRadio.Name = "useSpecificRadio";
            this.useSpecificRadio.Text = "Run specific script:";
            this.useSpecificRadio.CheckedChanged += new System.EventHandler(this.OnRadioChanged);
            //
            // scriptLabel
            //
            this.scriptLabel.AutoSize = true;
            this.scriptLabel.Location = new System.Drawing.Point(8, 106);
            this.scriptLabel.Name = "scriptLabel";
            this.scriptLabel.Text = "Script:";
            //
            // scriptBox
            //
            this.scriptBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.scriptBox.Location = new System.Drawing.Point(8, 124);
            this.scriptBox.Name = "scriptBox";
            this.scriptBox.TextChanged += new System.EventHandler(this.OnFieldChanged);
            //
            // browseScriptBtn
            //
            this.browseScriptBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.browseScriptBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.browseScriptBtn.Location = new System.Drawing.Point(0, 124);
            this.browseScriptBtn.Name = "browseScriptBtn";
            this.browseScriptBtn.Size = new System.Drawing.Size(30, 22);
            this.browseScriptBtn.Text = "...";
            this.browseScriptBtn.Click += new System.EventHandler(this.OnBrowseScript);
            //
            // argsLabel
            //
            this.argsLabel.AutoSize = true;
            this.argsLabel.Location = new System.Drawing.Point(8, 154);
            this.argsLabel.Name = "argsLabel";
            this.argsLabel.Text = "Arguments:";
            //
            // argsBox
            //
            this.argsBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.argsBox.Location = new System.Drawing.Point(8, 172);
            this.argsBox.Name = "argsBox";
            this.argsBox.TextChanged += new System.EventHandler(this.OnFieldChanged);
            //
            // inputLabel
            //
            this.inputLabel.AutoSize = true;
            this.inputLabel.Location = new System.Drawing.Point(8, 202);
            this.inputLabel.Name = "inputLabel";
            this.inputLabel.Text = "Input file (stdin):";
            //
            // inputFileBox
            //
            this.inputFileBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.inputFileBox.BackColor = System.Drawing.SystemColors.Window;
            this.inputFileBox.Location = new System.Drawing.Point(8, 220);
            this.inputFileBox.Name = "inputFileBox";
            this.inputFileBox.ReadOnly = true;
            //
            // browseInputBtn
            //
            this.browseInputBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.browseInputBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.browseInputBtn.Location = new System.Drawing.Point(0, 220);
            this.browseInputBtn.Name = "browseInputBtn";
            this.browseInputBtn.Size = new System.Drawing.Size(30, 22);
            this.browseInputBtn.Text = "...";
            this.browseInputBtn.Click += new System.EventHandler(this.OnBrowseInput);
            //
            // clearInputBtn
            //
            this.clearInputBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.clearInputBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.clearInputBtn.Location = new System.Drawing.Point(0, 220);
            this.clearInputBtn.Name = "clearInputBtn";
            this.clearInputBtn.Size = new System.Drawing.Size(24, 22);
            this.clearInputBtn.Text = "\u00d7";
            this.clearInputBtn.Click += new System.EventHandler(this.OnClearInput);
            //
            // inputHintLabel
            //
            this.inputHintLabel.AutoSize = true;
            this.inputHintLabel.Font = new System.Drawing.Font(this.Font.FontFamily, 7.5F);
            this.inputHintLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            this.inputHintLabel.Location = new System.Drawing.Point(8, 244);
            this.inputHintLabel.Name = "inputHintLabel";
            this.inputHintLabel.Text = "Lines from this file are fed to input() calls sequentially.";
            //
            // bottomPanel
            //
            this.bottomPanel.Controls.Add(this.okBtn);
            this.bottomPanel.Controls.Add(this.cancelBtn);
            this.bottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.bottomPanel.Height = 40;
            this.bottomPanel.Name = "bottomPanel";
            //
            // okBtn
            //
            this.okBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okBtn.Name = "okBtn";
            this.okBtn.Size = new System.Drawing.Size(80, 28);
            this.okBtn.Text = "OK";
            this.okBtn.Click += new System.EventHandler(this.OnOkClick);
            //
            // cancelBtn
            //
            this.cancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelBtn.Name = "cancelBtn";
            this.cancelBtn.Size = new System.Drawing.Size(80, 28);
            this.cancelBtn.Text = "Cancel";
            //
            // RunConfigurationDialog
            //
            this.AcceptButton = this.okBtn;
            this.CancelButton = this.cancelBtn;
            this.ClientSize = new System.Drawing.Size(604, 401);
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.bottomPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(560, 380);
            this.Name = "RunConfigurationDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Run Configurations";
            this.Load += new System.EventHandler(this.RunConfigurationDialog_Load);
            this.Resize += new System.EventHandler(this.RunConfigurationDialog_Resize);
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.listPanel.ResumeLayout(false);
            this.listBtnPanel.ResumeLayout(false);
            this.detailPanel.ResumeLayout(false);
            this.detailPanel.PerformLayout();
            this.bottomPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.Panel listPanel;
        private System.Windows.Forms.ListBox configListBox;
        private System.Windows.Forms.Label listLabel;
        private System.Windows.Forms.Panel listBtnPanel;
        private System.Windows.Forms.Button addBtn;
        private System.Windows.Forms.Button removeBtn;
        private System.Windows.Forms.Button duplicateBtn;
        private System.Windows.Forms.Panel detailPanel;
        private System.Windows.Forms.Label nameLabel;
        private System.Windows.Forms.TextBox nameBox;
        private System.Windows.Forms.RadioButton useCurrentRadio;
        private System.Windows.Forms.RadioButton useSpecificRadio;
        private System.Windows.Forms.Label scriptLabel;
        private System.Windows.Forms.TextBox scriptBox;
        private System.Windows.Forms.Button browseScriptBtn;
        private System.Windows.Forms.Label argsLabel;
        private System.Windows.Forms.TextBox argsBox;
        private System.Windows.Forms.Label inputLabel;
        private System.Windows.Forms.TextBox inputFileBox;
        private System.Windows.Forms.Button browseInputBtn;
        private System.Windows.Forms.Button clearInputBtn;
        private System.Windows.Forms.Label inputHintLabel;
        private System.Windows.Forms.Panel bottomPanel;
        private System.Windows.Forms.Button okBtn;
        private System.Windows.Forms.Button cancelBtn;
    }
}
