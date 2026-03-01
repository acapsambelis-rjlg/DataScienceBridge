using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using RJLG.IntelliSEM.Data.PythonDataScience;

namespace RJLG.IntelliSEM.UI.Controls.PythonDataScience
{
    public partial class RunConfigurationDialog : Form
    {
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
            InitializeComponent();
            PopulateList();
            if (configs.Count > 0)
            {
                int idx = Math.Min(selectedIndex, configs.Count - 1);
                if (idx >= 0) configListBox.SelectedIndex = idx;
            }
            UpdateDetailPanel();
        }

        private void RunConfigurationDialog_Load(object sender, EventArgs e)
        {
            LayoutDetailControls();
        }

        private void RunConfigurationDialog_Resize(object sender, EventArgs e)
        {
            LayoutDetailControls();
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

        private void OnConfigSelected(object sender, EventArgs e)
        {
            if (suppressSelectionChange) return;
            SaveCurrentFromFields();
            UpdateDetailPanel();
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

        private void OnOkClick(object sender, EventArgs e)
        {
            SaveCurrent();
        }

        private void OnClearInput(object sender, EventArgs e)
        {
            inputFileBox.Text = "";
            OnFieldChanged(sender, e);
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
