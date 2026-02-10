using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace DataScienceWorkbench
{
    public class ErrorSquiggleOverlay : Control
    {
        private RichTextBox editor;
        private int errorLineNumber = -1;

        public ErrorSquiggleOverlay()
        {
            this.SetStyle(
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);
            this.BackColor = Color.Transparent;
        }

        public void AttachEditor(RichTextBox editorBox)
        {
            editor = editorBox;
            editor.VScroll += (s, e) => this.Invalidate();
            editor.Resize += (s, e) =>
            {
                this.Size = editor.ClientSize;
                this.Invalidate();
            };
            this.Size = editor.ClientSize;
            this.Location = new Point(0, 0);
        }

        public void SetErrorLine(int lineNumber)
        {
            if (errorLineNumber == lineNumber) return;
            errorLineNumber = lineNumber;
            this.Invalidate();
        }

        public void ClearError()
        {
            if (errorLineNumber == -1) return;
            errorLineNumber = -1;
            this.Invalidate();
        }

        public int ErrorLine { get { return errorLineNumber; } }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x20;
                return cp;
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (editor == null || errorLineNumber < 1) return;
            if (errorLineNumber > editor.Lines.Length) return;

            int lineIdx = errorLineNumber - 1;
            string lineText = editor.Lines[lineIdx];
            if (lineText.Length == 0) return;

            int startCharIdx = editor.GetFirstCharIndexFromLine(lineIdx);
            Point startPos = editor.GetPositionFromCharIndex(startCharIdx);

            int endCharIdx = startCharIdx + lineText.Length - 1;
            Point endPos = editor.GetPositionFromCharIndex(endCharIdx);

            Size charSize = TextRenderer.MeasureText("W", editor.Font);
            int lineRight = endPos.X + charSize.Width;

            int squiggleY = startPos.Y + editor.Font.Height - 1;

            if (squiggleY < 0 || squiggleY > editor.ClientSize.Height) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (var pen = new Pen(Color.FromArgb(255, 80, 80), 1.2f))
            {
                int x = startPos.X;
                int waveHeight = 3;
                int waveWidth = 4;
                var points = new System.Collections.Generic.List<Point>();

                bool up = true;
                while (x < lineRight)
                {
                    points.Add(new Point(x, squiggleY + (up ? 0 : waveHeight)));
                    x += waveWidth / 2;
                    up = !up;
                }

                if (points.Count > 1)
                {
                    e.Graphics.DrawLines(pen, points.ToArray());
                }
            }
        }
    }
}
