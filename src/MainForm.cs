using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace DataScienceWorkbench
{
    public partial class MainForm : Form
    {
        private static readonly List<int> SampleMeasurements = new List<int>
        {
            42, 87, 15, 63, 91, 28, 74, 56, 33, 99,
            12, 68, 45, 80, 37, 54, 71, 19, 88, 26
        };

        public MainForm()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            InitializeComponent();
            SetupMenuAndEvents();

            dataScienceControl.RegisterInMemoryData("measurements", SampleMeasurements, "value");
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
