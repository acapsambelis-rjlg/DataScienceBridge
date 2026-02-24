namespace RJLG.IntelliSEM.UI.Controls.PythonDataScience
{
    partial class PlotViewerForm
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

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.navPanel = new System.Windows.Forms.Panel();
            this.prevBtn = new System.Windows.Forms.Button();
            this.nextBtn = new System.Windows.Forms.Button();
            this.indexLabel = new System.Windows.Forms.Label();
            this.saveBtn = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.navPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // pictureBox
            //
            this.pictureBox.BackColor = System.Drawing.Color.White;
            this.pictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox.Location = new System.Drawing.Point(0, 0);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(884, 621);
            this.pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox.TabIndex = 0;
            this.pictureBox.TabStop = false;
            //
            // navPanel
            //
            this.navPanel.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            this.navPanel.Controls.Add(this.saveBtn);
            this.navPanel.Controls.Add(this.indexLabel);
            this.navPanel.Controls.Add(this.nextBtn);
            this.navPanel.Controls.Add(this.prevBtn);
            this.navPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.navPanel.Location = new System.Drawing.Point(0, 621);
            this.navPanel.Name = "navPanel";
            this.navPanel.Size = new System.Drawing.Size(884, 40);
            this.navPanel.TabIndex = 1;
            //
            // prevBtn
            //
            this.prevBtn.BackColor = System.Drawing.Color.FromArgb(60, 60, 65);
            this.prevBtn.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(80, 80, 85);
            this.prevBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.prevBtn.ForeColor = System.Drawing.Color.White;
            this.prevBtn.Location = new System.Drawing.Point(10, 7);
            this.prevBtn.Name = "prevBtn";
            this.prevBtn.Size = new System.Drawing.Size(90, 26);
            this.prevBtn.TabIndex = 0;
            this.prevBtn.Text = "< Previous";
            this.prevBtn.UseVisualStyleBackColor = false;
            this.prevBtn.Click += new System.EventHandler(this.OnPrevClick);
            //
            // nextBtn
            //
            this.nextBtn.BackColor = System.Drawing.Color.FromArgb(60, 60, 65);
            this.nextBtn.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(80, 80, 85);
            this.nextBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.nextBtn.ForeColor = System.Drawing.Color.White;
            this.nextBtn.Location = new System.Drawing.Point(110, 7);
            this.nextBtn.Name = "nextBtn";
            this.nextBtn.Size = new System.Drawing.Size(90, 26);
            this.nextBtn.TabIndex = 1;
            this.nextBtn.Text = "Next >";
            this.nextBtn.UseVisualStyleBackColor = false;
            this.nextBtn.Click += new System.EventHandler(this.OnNextClick);
            //
            // indexLabel
            //
            this.indexLabel.ForeColor = System.Drawing.Color.FromArgb(200, 200, 200);
            this.indexLabel.Location = new System.Drawing.Point(210, 11);
            this.indexLabel.Name = "indexLabel";
            this.indexLabel.Size = new System.Drawing.Size(200, 20);
            this.indexLabel.TabIndex = 2;
            this.indexLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // saveBtn
            //
            this.saveBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.saveBtn.BackColor = System.Drawing.Color.FromArgb(60, 60, 65);
            this.saveBtn.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(80, 80, 85);
            this.saveBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.saveBtn.ForeColor = System.Drawing.Color.White;
            this.saveBtn.Location = new System.Drawing.Point(784, 7);
            this.saveBtn.Name = "saveBtn";
            this.saveBtn.Size = new System.Drawing.Size(90, 26);
            this.saveBtn.TabIndex = 3;
            this.saveBtn.Text = "Save As...";
            this.saveBtn.UseVisualStyleBackColor = false;
            this.saveBtn.Click += new System.EventHandler(this.OnSaveAs);
            //
            // PlotViewerForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.ClientSize = new System.Drawing.Size(884, 661);
            this.Controls.Add(this.pictureBox);
            this.Controls.Add(this.navPanel);
            this.MinimumSize = new System.Drawing.Size(400, 300);
            this.Name = "PlotViewerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Plot Viewer";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.OnFormClosed);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.navPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox;
        private System.Windows.Forms.Panel navPanel;
        private System.Windows.Forms.Button prevBtn;
        private System.Windows.Forms.Button nextBtn;
        private System.Windows.Forms.Label indexLabel;
        private System.Windows.Forms.Button saveBtn;
    }
}
