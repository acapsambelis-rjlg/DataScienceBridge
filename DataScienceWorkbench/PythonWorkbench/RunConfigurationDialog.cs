using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using RJLG.IntelliSEM.Data.PythonDataScience;

namespace RJLG.IntelliSEM.UI.Controls.PythonDataScience
{
    public class RunConfigurationDialog : Form
    {
        private ListBox configListBox;
        private TextBox nameBox;
        private RadioButton useCurrentRadio;
        private RadioButton useSpecificRadio;
        private TextBox scriptBox;
        private Button browseScriptBtn;
        private TextBox argsBox;
        private TextBox inputFileBox;
        private Button browseInputBtn;
        private Button clearInputBtn;
        private Button addBtn;
        private Button removeBtn;
        private Button duplicateBtn;
        private Button okBtn;
        private Button cancelBtn;
        private Panel detailPanel;
        private Label nameLabel;
        private Label scriptLabel;
        private Label argsLabel;
        private Label inputLabel;
        private Label inputHintLabel;

        private List<RunConfiguration> configs;
        private string scriptsDir;
        private bool suppressSelectionChange;

        public List<RunConfiguration> Configurations
        {
            get { return configs; }
        }

        public int SelectedIndex { get; private set; }

        public RunConfigurationDialog(List<RunConfiguration> existingConfigs, int selectedIndex, string scriptsDirectory)
        {
            scriptsDir = scriptsDirectory;
            configs = new List<RunConfiguration>();
            foreach (var c in existingConfigs)
                configs.Add(c.Clone());

            SelectedIndex = selectedIndex;
            InitializeDialogComponents();
            PopulateList();
            if (configs.Count > 0)
            {
                int idx = Math.Min(selectedIndex, configs.Count - 1);
                if (idx >= 0) configListBox.SelectedIndex = idx;
            }
            UpdateDetailPanel();
        }

        private void InitializeDialogComponents()
        {
            Text = "Run Configurations";
            Size = new Size(620, 440);
            MinimumSize = new Size(560, 380);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;

            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 180,
                FixedPanel = FixedPanel.Panel1,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(splitContainer);

            var listPanel = new Panel { Dock = DockStyle.Fill };
            splitContainer.Panel1.Controls.Add(listPanel);

            var listLabel = new Label
            {
                Text = "Configurations:",
                Dock = DockStyle.Top,
                Height = 22,
                Padding = new Padding(4, 4, 0, 0),
                Font = new Font(Font.FontFamily, 9f, FontStyle.Bold)
            };
            listPanel.Controls.Add(listLabel);

            configListBox = new ListBox
            {
                Dock = DockStyle.Fill,
                IntegralHeight = false,
                BorderStyle = BorderStyle.None
            };
            configListBox.SelectedIndexChanged += OnConfigSelected;
            listPanel.Controls.Add(configListBox);
            listPanel.Controls.SetChildIndex(listLabel, 1);

            var listBtnPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                Padding = new Padding(2)
            };
            listPanel.Controls.Add(listBtnPanel);

            addBtn = new Button { Text = "+", Width = 32, Height = 26, Location = new Point(2, 3), FlatStyle = FlatStyle.Flat };
            addBtn.Click += OnAddConfig;
            listBtnPanel.Controls.Add(addBtn);

            removeBtn = new Button { Text = "\u2212", Width = 32, Height = 26, Location = new Point(36, 3), FlatStyle = FlatStyle.Flat };
            removeBtn.Click += OnRemoveConfig;
            listBtnPanel.Controls.Add(removeBtn);

            duplicateBtn = new Button { Text = "Copy", Width = 46, Height = 26, Location = new Point(70, 3), FlatStyle = FlatStyle.Flat };
            duplicateBtn.Click += OnDuplicateConfig;
            listBtnPanel.Controls.Add(duplicateBtn);

            detailPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            splitContainer.Panel2.Controls.Add(detailPanel);

            int y = 8;
            nameLabel = new Label { Text = "Name:", Location = new Point(8, y), AutoSize = true };
            detailPanel.Controls.Add(nameLabel);
            y += 20;
            nameBox = new TextBox { Location = new Point(8, y), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            nameBox.TextChanged += OnNameChanged;
            detailPanel.Controls.Add(nameBox);
            y += 30;

            useCurrentRadio = new RadioButton { Text = "Run current file", Location = new Point(8, y), AutoSize = true, Checked = true };
            useCurrentRadio.CheckedChanged += OnRadioChanged;
            detailPanel.Controls.Add(useCurrentRadio);
            y += 24;

            useSpecificRadio = new RadioButton { Text = "Run specific script:", Location = new Point(8, y), AutoSize = true };
            useSpecificRadio.CheckedChanged += OnRadioChanged;
            detailPanel.Controls.Add(useSpecificRadio);
            y += 24;

            scriptLabel = new Label { Text = "Script:", Location = new Point(8, y), AutoSize = true };
            detailPanel.Controls.Add(scriptLabel);
            y += 18;
            scriptBox = new TextBox { Location = new Point(8, y), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            scriptBox.TextChanged += OnFieldChanged;
            detailPanel.Controls.Add(scriptBox);
            browseScriptBtn = new Button { Text = "...", Width = 30, Height = 22, Location = new Point(0, y), Anchor = AnchorStyles.Top | AnchorStyles.Right, FlatStyle = FlatStyle.Flat };
            browseScriptBtn.Click += OnBrowseScript;
            detailPanel.Controls.Add(browseScriptBtn);
            y += 30;

            argsLabel = new Label { Text = "Arguments:", Location = new Point(8, y), AutoSize = true };
            detailPanel.Controls.Add(argsLabel);
            y += 18;
            argsBox = new TextBox { Location = new Point(8, y), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            argsBox.TextChanged += OnFieldChanged;
            detailPanel.Controls.Add(argsBox);
            y += 30;

            inputLabel = new Label { Text = "Input file (stdin):", Location = new Point(8, y), AutoSize = true };
            detailPanel.Controls.Add(inputLabel);
            y += 18;
            inputFileBox = new TextBox { Location = new Point(8, y), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, ReadOnly = true, BackColor = SystemColors.Window };
            detailPanel.Controls.Add(inputFileBox);
            browseInputBtn = new Button { Text = "...", Width = 30, Height = 22, Location = new Point(0, y), Anchor = AnchorStyles.Top | AnchorStyles.Right, FlatStyle = FlatStyle.Flat };
            browseInputBtn.Click += OnBrowseInput;
            detailPanel.Controls.Add(browseInputBtn);
            clearInputBtn = new Button { Text = "\u00d7", Width = 24, Height = 22, Location = new Point(0, y), Anchor = AnchorStyles.Top | AnchorStyles.Right, FlatStyle = FlatStyle.Flat };
            clearInputBtn.Click += (s, e) => { inputFileBox.Text = ""; OnFieldChanged(s, e); };
            detailPanel.Controls.Add(clearInputBtn);
            y += 24;
            inputHintLabel = new Label
            {
                Text = "Lines from this file are fed to input() calls sequentially.",
                Location = new Point(8, y),
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Font = new Font(Font.FontFamily, 7.5f)
            };
            detailPanel.Controls.Add(inputHintLabel);

            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40
            };
            Controls.Add(bottomPanel);
            Controls.SetChildIndex(bottomPanel, 0);

            okBtn = new Button { Text = "OK", Width = 80, Height = 28, Anchor = AnchorStyles.Bottom | AnchorStyles.Right, DialogResult = DialogResult.OK };
            okBtn.Click += (s, e) => { SaveCurrent(); };
            bottomPanel.Controls.Add(okBtn);

            cancelBtn = new Button { Text = "Cancel", Width = 80, Height = 28, Anchor = AnchorStyles.Bottom | AnchorStyles.Right, DialogResult = DialogResult.Cancel };
            bottomPanel.Controls.Add(cancelBtn);

            AcceptButton = okBtn;
            CancelButton = cancelBtn;

            Resize += (s, e) => LayoutDetailControls();
            Load += (s, e) => LayoutDetailControls();
        }

        private void LayoutDetailControls()
        {
            int w = detailPanel.ClientSize.Width - 16;
            int browseW = 30;
            int clearW = 24;
            int gap = 2;

            nameBox.Width = w;
            scriptBox.Width = w - browseW - gap;
            browseScriptBtn.Left = scriptBox.Right + gap;
            argsBox.Width = w;
            inputFileBox.Width = w - browseW - clearW - gap * 2;
            browseInputBtn.Left = inputFileBox.Right + gap;
            clearInputBtn.Left = browseInputBtn.Right + gap;

            okBtn.Location = new Point(okBtn.Parent.ClientSize.Width - 170, 6);
            cancelBtn.Location = new Point(okBtn.Parent.ClientSize.Width - 85, 6);
        }

        private void PopulateList()
        {
            suppressSelectionChange = true;
            configListBox.Items.Clear();
            foreach (var c in configs)
                configListBox.Items.Add(c.Name);
            suppressSelectionChange = false;
        }

        private void UpdateDetailPanel()
        {
            bool hasSelection = configListBox.SelectedIndex >= 0;
            detailPanel.Enabled = hasSelection;
            if (!hasSelection)
            {
                nameBox.Text = "";
                scriptBox.Text = "";
                argsBox.Text = "";
                inputFileBox.Text = "";
                useCurrentRadio.Checked = true;
                return;
            }

            var c = configs[configListBox.SelectedIndex];
            suppressSelectionChange = true;
            nameBox.Text = c.Name;
            scriptBox.Text = c.ScriptPath ?? "";
            argsBox.Text = c.Arguments ?? "";
            inputFileBox.Text = c.InputFilePath ?? "";
            useCurrentRadio.Checked = c.UseCurrentFile;
            useSpecificRadio.Checked = !c.UseCurrentFile;
            scriptBox.Enabled = !c.UseCurrentFile;
            browseScriptBtn.Enabled = !c.UseCurrentFile;
            suppressSelectionChange = false;
        }

        private void SaveCurrent()
        {
            if (configListBox.SelectedIndex < 0) return;
            var c = configs[configListBox.SelectedIndex];
            c.Name = nameBox.Text.Trim();
            c.UseCurrentFile = useCurrentRadio.Checked;
            c.ScriptPath = scriptBox.Text.Trim();
            c.Arguments = argsBox.Text.Trim();
            c.InputFilePath = inputFileBox.Text.Trim();
            SelectedIndex = configListBox.SelectedIndex;
        }

        private void OnConfigSelected(object sender, EventArgs e)
        {
            if (suppressSelectionChange) return;
            SaveCurrentFromFields();
            UpdateDetailPanel();
        }

        private void SaveCurrentFromFields()
        {
            for (int i = 0; i < configs.Count; i++)
            {
                if (i < configListBox.Items.Count && configListBox.Items[i].ToString() != configs[i].Name)
                {
                    configs[i].Name = configListBox.Items[i].ToString();
                }
            }
            if (configListBox.SelectedIndex >= 0)
            {
                var c = configs[configListBox.SelectedIndex];
                c.Name = nameBox.Text.Trim();
                c.UseCurrentFile = useCurrentRadio.Checked;
                c.ScriptPath = scriptBox.Text.Trim();
                c.Arguments = argsBox.Text.Trim();
                c.InputFilePath = inputFileBox.Text.Trim();
            }
        }

        private void OnNameChanged(object sender, EventArgs e)
        {
            if (suppressSelectionChange) return;
            if (configListBox.SelectedIndex < 0) return;
            configs[configListBox.SelectedIndex].Name = nameBox.Text.Trim();
            int idx = configListBox.SelectedIndex;
            suppressSelectionChange = true;
            configListBox.Items[idx] = nameBox.Text.Trim();
            configListBox.SelectedIndex = idx;
            suppressSelectionChange = false;
        }

        private void OnFieldChanged(object sender, EventArgs e)
        {
            if (suppressSelectionChange) return;
            if (configListBox.SelectedIndex < 0) return;
            var c = configs[configListBox.SelectedIndex];
            c.ScriptPath = scriptBox.Text.Trim();
            c.Arguments = argsBox.Text.Trim();
            c.InputFilePath = inputFileBox.Text.Trim();
        }

        private void OnRadioChanged(object sender, EventArgs e)
        {
            if (suppressSelectionChange) return;
            scriptBox.Enabled = useSpecificRadio.Checked;
            browseScriptBtn.Enabled = useSpecificRadio.Checked;
            if (configListBox.SelectedIndex >= 0)
                configs[configListBox.SelectedIndex].UseCurrentFile = useCurrentRadio.Checked;
        }

        private void OnAddConfig(object sender, EventArgs e)
        {
            SaveCurrentFromFields();
            var newConfig = new RunConfiguration
            {
                Name = GenerateUniqueName("New Configuration"),
                UseCurrentFile = true
            };
            configs.Add(newConfig);
            PopulateList();
            configListBox.SelectedIndex = configs.Count - 1;
            UpdateDetailPanel();
            nameBox.Focus();
            nameBox.SelectAll();
        }

        private void OnRemoveConfig(object sender, EventArgs e)
        {
            if (configListBox.SelectedIndex < 0) return;
            int idx = configListBox.SelectedIndex;
            configs.RemoveAt(idx);
            PopulateList();
            if (configs.Count > 0)
                configListBox.SelectedIndex = Math.Min(idx, configs.Count - 1);
            UpdateDetailPanel();
        }

        private void OnDuplicateConfig(object sender, EventArgs e)
        {
            if (configListBox.SelectedIndex < 0) return;
            SaveCurrentFromFields();
            var original = configs[configListBox.SelectedIndex];
            var copy = original.Clone();
            copy.Name = GenerateUniqueName(original.Name + " (Copy)");
            configs.Add(copy);
            PopulateList();
            configListBox.SelectedIndex = configs.Count - 1;
            UpdateDetailPanel();
        }

        private void OnBrowseScript(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Select Python Script";
                ofd.Filter = "Python files (*.py)|*.py|All files (*.*)|*.*";
                if (!string.IsNullOrEmpty(scriptsDir) && Directory.Exists(scriptsDir))
                    ofd.InitialDirectory = scriptsDir;
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    string relPath = GetRelativePath(ofd.FileName);
                    scriptBox.Text = relPath;
                }
            }
        }

        private void OnBrowseInput(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Select Input File";
                ofd.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                if (!string.IsNullOrEmpty(scriptsDir) && Directory.Exists(scriptsDir))
                    ofd.InitialDirectory = scriptsDir;
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    string relPath = GetRelativePath(ofd.FileName);
                    inputFileBox.Text = relPath;
                }
            }
        }

        private string GetRelativePath(string fullPath)
        {
            if (string.IsNullOrEmpty(scriptsDir)) return fullPath;
            string parentDir = Path.GetDirectoryName(scriptsDir);
            if (parentDir == null) return fullPath;
            string pythonDir = parentDir;
            if (fullPath.StartsWith(pythonDir + Path.DirectorySeparatorChar) ||
                fullPath.StartsWith(pythonDir + "/"))
            {
                return fullPath.Substring(pythonDir.Length + 1);
            }
            return fullPath;
        }

        private string GenerateUniqueName(string baseName)
        {
            string name = baseName;
            int counter = 2;
            while (configs.Exists(c => c.Name == name))
            {
                name = baseName + " " + counter;
                counter++;
            }
            return name;
        }
    }
}
