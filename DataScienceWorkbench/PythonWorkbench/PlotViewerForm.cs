using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace DataScienceWorkbench.PythonWorkbench
{
    public class PlotViewerForm : Form
    {
        private PictureBox pictureBox;
        private Panel navPanel;
        private Button prevBtn;
        private Button nextBtn;
        private Button saveBtn;
        private Label indexLabel;
        private List<string> imagePaths;
        private int currentIndex;

        public PlotViewerForm(List<string> paths)
        {
            imagePaths = paths;
            currentIndex = 0;

            Text = "Plot Viewer";
            Size = new Size(900, 700);
            MinimumSize = new Size(400, 300);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(30, 30, 30);

            pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White
            };

            navPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.FromArgb(45, 45, 48)
            };

            prevBtn = new Button
            {
                Text = "< Previous",
                Location = new Point(10, 7),
                Size = new Size(90, 26),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(60, 60, 65)
            };
            prevBtn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 85);
            prevBtn.Click += (s, e) => Navigate(-1);

            nextBtn = new Button
            {
                Text = "Next >",
                Location = new Point(110, 7),
                Size = new Size(90, 26),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(60, 60, 65)
            };
            nextBtn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 85);
            nextBtn.Click += (s, e) => Navigate(1);

            indexLabel = new Label
            {
                Location = new Point(210, 11),
                Size = new Size(200, 20),
                ForeColor = Color.FromArgb(200, 200, 200),
                TextAlign = ContentAlignment.MiddleLeft
            };

            saveBtn = new Button
            {
                Text = "Save As...",
                Size = new Size(90, 26),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(60, 60, 65),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            saveBtn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 85);
            saveBtn.Location = new Point(navPanel.Width - 100, 7);
            saveBtn.Click += OnSaveAs;

            navPanel.Controls.Add(prevBtn);
            navPanel.Controls.Add(nextBtn);
            navPanel.Controls.Add(indexLabel);
            navPanel.Controls.Add(saveBtn);

            Controls.Add(pictureBox);
            Controls.Add(navPanel);

            if (imagePaths.Count <= 1)
            {
                prevBtn.Visible = false;
                nextBtn.Visible = false;
            }

            ShowImage();

            FormClosed += (s, e) => CleanupTempFiles();
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
