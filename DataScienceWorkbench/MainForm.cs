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
            if (menuBar is Telerik.WinControls.UI.RadMenu radMenu)
            {
                radMenu.Dock = DockStyle.Top;
                this.Controls.Add(radMenu);
            }

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
