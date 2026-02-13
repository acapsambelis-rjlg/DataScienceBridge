using System;
using System.Windows.Forms;

namespace DataScienceWorkbench
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            InitializeComponent();
            SetupMenuAndEvents();
        }

        private void SetupMenuAndEvents()
        {
            dataScienceControl.StatusChanged += (s, msg) => SetStatus(msg);

            var menuBar = dataScienceControl.CreateMenuStrip();
            var exitItem = new ToolStripMenuItem("Exit", null, (s, e) => this.Close());
            var fileMenu = (ToolStripMenuItem)menuBar.Items[0];
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(exitItem);
            this.MainMenuStrip = menuBar;
            this.Controls.Add(menuBar);

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
