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
            this.splitContainer = new Telerik.WinControls.UI.RadSplitContainer();
            this.listSplitPanel = new Telerik.WinControls.UI.SplitPanel();
            this.detailSplitPanel = new Telerik.WinControls.UI.SplitPanel();
            this.listPanel = new Telerik.WinControls.UI.RadPanel();
            this.configListBox = new Telerik.WinControls.UI.RadListControl();
            this.listLabel = new Telerik.WinControls.UI.RadLabel();
            this.listBtnPanel = new Telerik.WinControls.UI.RadPanel();
            this.addBtn = new Telerik.WinControls.UI.RadButton();
            this.removeBtn = new Telerik.WinControls.UI.RadButton();
            this.duplicateBtn = new Telerik.WinControls.UI.RadButton();
            this.detailPanel = new Telerik.WinControls.UI.RadPanel();
            this.nameLabel = new Telerik.WinControls.UI.RadLabel();
            this.nameBox = new Telerik.WinControls.UI.RadTextBox();
            this.useCurrentRadio = new Telerik.WinControls.UI.RadRadioButton();
            this.useSpecificRadio = new Telerik.WinControls.UI.RadRadioButton();
            this.scriptLabel = new Telerik.WinControls.UI.RadLabel();
            this.scriptBox = new Telerik.WinControls.UI.RadTextBox();
            this.browseScriptBtn = new Telerik.WinControls.UI.RadButton();
            this.argsLabel = new Telerik.WinControls.UI.RadLabel();
            this.argsBox = new Telerik.WinControls.UI.RadTextBox();
            this.inputLabel = new Telerik.WinControls.UI.RadLabel();
            this.inputFileBox = new Telerik.WinControls.UI.RadTextBox();
            this.browseInputBtn = new Telerik.WinControls.UI.RadButton();
            this.clearInputBtn = new Telerik.WinControls.UI.RadButton();
            this.inputHintLabel = new Telerik.WinControls.UI.RadLabel();
            this.bottomPanel = new Telerik.WinControls.UI.RadPanel();
            this.okBtn = new Telerik.WinControls.UI.RadButton();
            this.cancelBtn = new Telerik.WinControls.UI.RadButton();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.listPanel)).BeginInit();
            this.listPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.configListBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.listLabel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.listBtnPanel)).BeginInit();
            this.listBtnPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.addBtn)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.removeBtn)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.duplicateBtn)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.detailPanel)).BeginInit();
            this.detailPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nameLabel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nameBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.useCurrentRadio)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.useSpecificRadio)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.scriptLabel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.scriptBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.browseScriptBtn)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.argsLabel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.argsBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.inputLabel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.inputFileBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.browseInputBtn)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.clearInputBtn)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.inputHintLabel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bottomPanel)).BeginInit();
            this.bottomPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.okBtn)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cancelBtn)).BeginInit();
            this.SuspendLayout();
            //
            // bottomPanel
            //
            this.bottomPanel.Controls.Add(this.okBtn);
            this.bottomPanel.Controls.Add(this.cancelBtn);
            this.bottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.bottomPanel.Location = new System.Drawing.Point(0, 361);
            this.bottomPanel.Name = "bottomPanel";
            this.bottomPanel.Size = new System.Drawing.Size(604, 40);
            this.bottomPanel.TabIndex = 0;
            //
            // okBtn
            //
            this.okBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okBtn.Location = new System.Drawing.Point(434, 6);
            this.okBtn.Name = "okBtn";
            this.okBtn.Size = new System.Drawing.Size(80, 28);
            this.okBtn.TabIndex = 0;
            this.okBtn.Text = "OK";
            this.okBtn.Click += new System.EventHandler(this.OnOkClick);
            //
            // cancelBtn
            //
            this.cancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelBtn.Location = new System.Drawing.Point(519, 6);
            this.cancelBtn.Name = "cancelBtn";
            this.cancelBtn.Size = new System.Drawing.Size(80, 28);
            this.cancelBtn.TabIndex = 1;
            this.cancelBtn.Text = "Cancel";
            //
            // splitContainer
            //
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.Location = new System.Drawing.Point(0, 0);
            this.splitContainer.Name = "splitContainer";
            this.splitContainer.Size = new System.Drawing.Size(604, 361);
            this.splitContainer.TabIndex = 1;
            //
            // listSplitPanel
            //
            this.listSplitPanel.Controls.Add(this.listPanel);
            this.listSplitPanel.Name = "listSplitPanel";
            this.listSplitPanel.SizeInfo.SizeMode = Telerik.WinControls.UI.Docking.SplitPanelSizeMode.Absolute;
            this.listSplitPanel.SizeInfo.AbsoluteSize = new System.Drawing.Size(180, 361);
            //
            // detailSplitPanel
            //
            this.detailSplitPanel.Controls.Add(this.detailPanel);
            this.detailSplitPanel.Name = "detailSplitPanel";
            //
            // splitContainer.SplitPanels
            //
            this.splitContainer.SplitPanels.AddRange(new Telerik.WinControls.UI.SplitPanel[] { this.listSplitPanel, this.detailSplitPanel });
            //
            // listPanel
            //
            this.listPanel.Controls.Add(this.configListBox);
            this.listPanel.Controls.Add(this.listBtnPanel);
            this.listPanel.Controls.Add(this.listLabel);
            this.listPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listPanel.Location = new System.Drawing.Point(0, 0);
            this.listPanel.Name = "listPanel";
            this.listPanel.Size = new System.Drawing.Size(178, 359);
            this.listPanel.TabIndex = 0;
            //
            // listLabel
            //
            this.listLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.listLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.listLabel.Location = new System.Drawing.Point(0, 0);
            this.listLabel.Name = "listLabel";
            this.listLabel.Padding = new System.Windows.Forms.Padding(4, 4, 0, 0);
            this.listLabel.Size = new System.Drawing.Size(178, 22);
            this.listLabel.TabIndex = 2;
            this.listLabel.Text = "Configurations:";
            //
            // listBtnPanel
            //
            this.listBtnPanel.Controls.Add(this.addBtn);
            this.listBtnPanel.Controls.Add(this.removeBtn);
            this.listBtnPanel.Controls.Add(this.duplicateBtn);
            this.listBtnPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.listBtnPanel.Location = new System.Drawing.Point(0, 327);
            this.listBtnPanel.Name = "listBtnPanel";
            this.listBtnPanel.Padding = new System.Windows.Forms.Padding(2);
            this.listBtnPanel.Size = new System.Drawing.Size(178, 32);
            this.listBtnPanel.TabIndex = 1;
            //
            // configListBox
            //
            this.configListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.configListBox.Location = new System.Drawing.Point(0, 22);
            this.configListBox.Name = "configListBox";
            this.configListBox.Size = new System.Drawing.Size(178, 305);
            this.configListBox.TabIndex = 0;
            this.configListBox.SelectedIndexChanged += new Telerik.WinControls.UI.Data.PositionChangedEventHandler(this.OnConfigSelected);
            //
            // addBtn
            //
            this.addBtn.Location = new System.Drawing.Point(2, 3);
            this.addBtn.Name = "addBtn";
            this.addBtn.Size = new System.Drawing.Size(32, 26);
            this.addBtn.TabIndex = 0;
            this.addBtn.Text = "+";
            this.addBtn.Click += new System.EventHandler(this.OnAddConfig);
            //
            // removeBtn
            //
            this.removeBtn.Location = new System.Drawing.Point(36, 3);
            this.removeBtn.Name = "removeBtn";
            this.removeBtn.Size = new System.Drawing.Size(32, 26);
            this.removeBtn.TabIndex = 1;
            this.removeBtn.Text = "\u2212";
            this.removeBtn.Click += new System.EventHandler(this.OnRemoveConfig);
            //
            // duplicateBtn
            //
            this.duplicateBtn.Location = new System.Drawing.Point(70, 3);
            this.duplicateBtn.Name = "duplicateBtn";
            this.duplicateBtn.Size = new System.Drawing.Size(46, 26);
            this.duplicateBtn.TabIndex = 2;
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
            this.detailPanel.Location = new System.Drawing.Point(0, 0);
            this.detailPanel.Name = "detailPanel";
            this.detailPanel.Padding = new System.Windows.Forms.Padding(8);
            this.detailPanel.Size = new System.Drawing.Size(418, 359);
            this.detailPanel.TabIndex = 0;
            //
            // nameLabel
            //
            this.nameLabel.AutoSize = false;
            this.nameLabel.Location = new System.Drawing.Point(8, 8);
            this.nameLabel.Name = "nameLabel";
            this.nameLabel.Size = new System.Drawing.Size(42, 18);
            this.nameLabel.TabIndex = 0;
            this.nameLabel.Text = "Name:";
            //
            // nameBox
            //
            this.nameBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.nameBox.Location = new System.Drawing.Point(8, 28);
            this.nameBox.Name = "nameBox";
            this.nameBox.Size = new System.Drawing.Size(402, 24);
            this.nameBox.TabIndex = 1;
            this.nameBox.TextChanged += new System.EventHandler(this.OnNameChanged);
            //
            // useCurrentRadio
            //
            this.useCurrentRadio.Location = new System.Drawing.Point(8, 58);
            this.useCurrentRadio.Name = "useCurrentRadio";
            this.useCurrentRadio.Size = new System.Drawing.Size(120, 18);
            this.useCurrentRadio.TabIndex = 2;
            this.useCurrentRadio.Text = "Run current file";
            this.useCurrentRadio.ToggleState = Telerik.WinControls.Enumerations.ToggleState.On;
            this.useCurrentRadio.ToggleStateChanged += new Telerik.WinControls.UI.StateChangedEventHandler(this.OnRadioChanged);
            //
            // useSpecificRadio
            //
            this.useSpecificRadio.Location = new System.Drawing.Point(8, 82);
            this.useSpecificRadio.Name = "useSpecificRadio";
            this.useSpecificRadio.Size = new System.Drawing.Size(140, 18);
            this.useSpecificRadio.TabIndex = 3;
            this.useSpecificRadio.Text = "Run specific script:";
            this.useSpecificRadio.ToggleStateChanged += new Telerik.WinControls.UI.StateChangedEventHandler(this.OnRadioChanged);
            //
            // scriptLabel
            //
            this.scriptLabel.AutoSize = false;
            this.scriptLabel.Location = new System.Drawing.Point(8, 106);
            this.scriptLabel.Name = "scriptLabel";
            this.scriptLabel.Size = new System.Drawing.Size(42, 18);
            this.scriptLabel.TabIndex = 4;
            this.scriptLabel.Text = "Script:";
            //
            // scriptBox
            //
            this.scriptBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.scriptBox.Location = new System.Drawing.Point(8, 124);
            this.scriptBox.Name = "scriptBox";
            this.scriptBox.Size = new System.Drawing.Size(370, 24);
            this.scriptBox.TabIndex = 5;
            this.scriptBox.TextChanged += new System.EventHandler(this.OnFieldChanged);
            //
            // browseScriptBtn
            //
            this.browseScriptBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.browseScriptBtn.Location = new System.Drawing.Point(380, 124);
            this.browseScriptBtn.Name = "browseScriptBtn";
            this.browseScriptBtn.Size = new System.Drawing.Size(30, 24);
            this.browseScriptBtn.TabIndex = 6;
            this.browseScriptBtn.Text = "...";
            this.browseScriptBtn.Click += new System.EventHandler(this.OnBrowseScript);
            //
            // argsLabel
            //
            this.argsLabel.AutoSize = false;
            this.argsLabel.Location = new System.Drawing.Point(8, 156);
            this.argsLabel.Name = "argsLabel";
            this.argsLabel.Size = new System.Drawing.Size(70, 18);
            this.argsLabel.TabIndex = 7;
            this.argsLabel.Text = "Arguments:";
            //
            // argsBox
            //
            this.argsBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.argsBox.Location = new System.Drawing.Point(8, 176);
            this.argsBox.Name = "argsBox";
            this.argsBox.Size = new System.Drawing.Size(402, 24);
            this.argsBox.TabIndex = 8;
            this.argsBox.TextChanged += new System.EventHandler(this.OnFieldChanged);
            //
            // inputLabel
            //
            this.inputLabel.AutoSize = false;
            this.inputLabel.Location = new System.Drawing.Point(8, 208);
            this.inputLabel.Name = "inputLabel";
            this.inputLabel.Size = new System.Drawing.Size(100, 18);
            this.inputLabel.TabIndex = 9;
            this.inputLabel.Text = "Input file (stdin):";
            //
            // inputFileBox
            //
            this.inputFileBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.inputFileBox.Location = new System.Drawing.Point(8, 228);
            this.inputFileBox.Name = "inputFileBox";
            this.inputFileBox.ReadOnly = true;
            this.inputFileBox.Size = new System.Drawing.Size(346, 24);
            this.inputFileBox.TabIndex = 10;
            //
            // browseInputBtn
            //
            this.browseInputBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.browseInputBtn.Location = new System.Drawing.Point(356, 228);
            this.browseInputBtn.Name = "browseInputBtn";
            this.browseInputBtn.Size = new System.Drawing.Size(30, 24);
            this.browseInputBtn.TabIndex = 11;
            this.browseInputBtn.Text = "...";
            this.browseInputBtn.Click += new System.EventHandler(this.OnBrowseInput);
            //
            // clearInputBtn
            //
            this.clearInputBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.clearInputBtn.Location = new System.Drawing.Point(388, 228);
            this.clearInputBtn.Name = "clearInputBtn";
            this.clearInputBtn.Size = new System.Drawing.Size(24, 24);
            this.clearInputBtn.TabIndex = 12;
            this.clearInputBtn.Text = "\u00d7";
            this.clearInputBtn.Click += new System.EventHandler(this.OnClearInput);
            //
            // inputHintLabel
            //
            this.inputHintLabel.Font = new System.Drawing.Font("Segoe UI", 7.5F);
            this.inputHintLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            this.inputHintLabel.Location = new System.Drawing.Point(8, 254);
            this.inputHintLabel.Name = "inputHintLabel";
            this.inputHintLabel.Size = new System.Drawing.Size(310, 18);
            this.inputHintLabel.TabIndex = 13;
            this.inputHintLabel.Text = "Lines from this file are fed to input() calls sequentially.";
            //
            // RunConfigurationDialog
            //
            this.AcceptButton = this.okBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
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
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.listPanel)).EndInit();
            this.listPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.configListBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.listLabel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.listBtnPanel)).EndInit();
            this.listBtnPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.addBtn)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.removeBtn)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.duplicateBtn)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.detailPanel)).EndInit();
            this.detailPanel.ResumeLayout(false);
            this.detailPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nameLabel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nameBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.useCurrentRadio)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.useSpecificRadio)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.scriptLabel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.scriptBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.browseScriptBtn)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.argsLabel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.argsBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.inputLabel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.inputFileBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.browseInputBtn)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.clearInputBtn)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.inputHintLabel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bottomPanel)).EndInit();
            this.bottomPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.okBtn)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cancelBtn)).EndInit();
            this.ResumeLayout(false);
        }

        private Telerik.WinControls.UI.RadSplitContainer splitContainer;
        private Telerik.WinControls.UI.SplitPanel listSplitPanel;
        private Telerik.WinControls.UI.SplitPanel detailSplitPanel;
        private Telerik.WinControls.UI.RadPanel listPanel;
        private Telerik.WinControls.UI.RadListControl configListBox;
        private Telerik.WinControls.UI.RadLabel listLabel;
        private Telerik.WinControls.UI.RadPanel listBtnPanel;
        private Telerik.WinControls.UI.RadButton addBtn;
        private Telerik.WinControls.UI.RadButton removeBtn;
        private Telerik.WinControls.UI.RadButton duplicateBtn;
        private Telerik.WinControls.UI.RadPanel detailPanel;
        private Telerik.WinControls.UI.RadLabel nameLabel;
        private Telerik.WinControls.UI.RadTextBox nameBox;
        private Telerik.WinControls.UI.RadRadioButton useCurrentRadio;
        private Telerik.WinControls.UI.RadRadioButton useSpecificRadio;
        private Telerik.WinControls.UI.RadLabel scriptLabel;
        private Telerik.WinControls.UI.RadTextBox scriptBox;
        private Telerik.WinControls.UI.RadButton browseScriptBtn;
        private Telerik.WinControls.UI.RadLabel argsLabel;
        private Telerik.WinControls.UI.RadTextBox argsBox;
        private Telerik.WinControls.UI.RadLabel inputLabel;
        private Telerik.WinControls.UI.RadTextBox inputFileBox;
        private Telerik.WinControls.UI.RadButton browseInputBtn;
        private Telerik.WinControls.UI.RadButton clearInputBtn;
        private Telerik.WinControls.UI.RadLabel inputHintLabel;
        private Telerik.WinControls.UI.RadPanel bottomPanel;
        private Telerik.WinControls.UI.RadButton okBtn;
        private Telerik.WinControls.UI.RadButton cancelBtn;
    }
}
