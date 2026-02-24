using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace RJLG.IntelliSEM.UI.Controls.PythonDataScience
{
    public partial class PlotViewerForm : Form
    {
        private List<string> imagePaths;
        private int currentIndex;

        public PlotViewerForm(List<string> paths)
        {
            imagePaths = paths;
            currentIndex = 0;

            InitializeComponent();

            if (imagePaths.Count <= 1)
            {
                prevBtn.Visible = false;
                nextBtn.Visible = false;
            }

            ShowImage();
        }

        private void OnPrevClick(object sender, EventArgs e)
        {
            Navigate(-1);
        }

        private void OnNextClick(object sender, EventArgs e)
        {
            Navigate(1);
        }

        private void OnFormClosed(object sender, FormClosedEventArgs e)
        {
            CleanupTempFiles();
        }

        private void Navigate(int direction)
        {
            currentIndex += direction;
            if (currentIndex < 0) currentIndex = imagePaths.Count - 1;
            if (currentIndex >= imagePaths.Count) currentIndex = 0;
            ShowImage();
        }

        private void ShowImage()
        {
            if (currentIndex < 0 || currentIndex >= imagePaths.Count) return;

            try
            {
                if (pictureBox.Image != null)
                {
                    pictureBox.Image.Dispose();
                    pictureBox.Image = null;
                }

                using (var stream = new FileStream(imagePaths[currentIndex], FileMode.Open, FileAccess.Read))
                {
                    pictureBox.Image = Image.FromStream(stream);
                }

                Text = "Plot Viewer - " + Path.GetFileName(imagePaths[currentIndex]);
                indexLabel.Text = "Plot " + (currentIndex + 1) + " of " + imagePaths.Count;
            }
            catch (Exception ex)
            {
                indexLabel.Text = "Error loading image: " + ex.Message;
            }
        }

        private void OnSaveAs(object sender, EventArgs e)
        {
            if (currentIndex < 0 || currentIndex >= imagePaths.Count) return;

            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "PNG Image|*.png|JPEG Image|*.jpg|All Files|*.*";
                dlg.FileName = "plot_" + (currentIndex + 1) + ".png";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.Copy(imagePaths[currentIndex], dlg.FileName, true);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error saving: " + ex.Message, "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void CleanupTempFiles()
        {
            if (pictureBox.Image != null)
            {
                pictureBox.Image.Dispose();
                pictureBox.Image = null;
            }

            foreach (var path in imagePaths)
            {
                try { File.Delete(path); } catch { }
            }

            if (imagePaths.Count > 0)
            {
                try
                {
                    string dir = Path.GetDirectoryName(imagePaths[0]);
                    if (dir != null && dir.Contains("dsw_plots_"))
                        Directory.Delete(dir, false);
                }
                catch { }
            }
        }
    }
}
