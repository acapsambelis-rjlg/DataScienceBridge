using System;
using System.Windows.Forms;
using RJLG.IntelliSEM.UI.Controls.PythonDataScience;

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
            this.MainMenuStrip = menuBar;

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
