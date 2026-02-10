using System;
using System.Drawing;
using System.Windows.Forms;

namespace DataScienceWorkbench
{
    public class MainForm : Form
    {
        private DataScienceControl dataScienceControl;
        private StatusStrip statusBar;
        private ToolStripStatusLabel statusLabel;

        public MainForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Data Science Workbench - Python + .NET";
            this.Size = new Size(1200, 800);
            this.MinimumSize = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            dataScienceControl = new DataScienceControl();
            dataScienceControl.StatusChanged += (s, msg) => SetStatus(msg);

            var menuBar = dataScienceControl.CreateMenuStrip();
            var exitItem = new ToolStripMenuItem("Exit", null, (s, e) => this.Close());
            var fileMenu = (ToolStripMenuItem)menuBar.Items[0];
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(exitItem);
            this.MainMenuStrip = menuBar;

            statusBar = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Ready");
            statusBar.Items.Add(statusLabel);

            this.Controls.Add(dataScienceControl);
            this.Controls.Add(statusBar);
            this.Controls.Add(menuBar);

            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (dataScienceControl.HandleKeyDown(e.KeyCode))
                    e.Handled = true;
            };

            SetStatus("Ready");
        }

        private void SetStatus(string msg)
        {
            statusLabel.Text = msg;
        }
    }
}
