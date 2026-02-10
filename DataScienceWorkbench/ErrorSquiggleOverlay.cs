using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DataScienceWorkbench
{
    public class SquiggleRichTextBox : RichTextBox
    {
        private int errorLineNumber = -1;
        private const int WM_PAINT = 0x000F;

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

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

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_PAINT && errorLineNumber >= 1)
            {
                DrawSquiggle();
            }
        }

        private void DrawSquiggle()
        {
            if (errorLineNumber < 1 || errorLineNumber > this.Lines.Length) return;

            int lineIdx = errorLineNumber - 1;
            string lineText = this.Lines[lineIdx];
            if (lineText.Length == 0) return;

            int startCharIdx = this.GetFirstCharIndexFromLine(lineIdx);
            Point startPos = this.GetPositionFromCharIndex(startCharIdx);

            int endCharIdx = startCharIdx + lineText.Length - 1;
            Point endPos = this.GetPositionFromCharIndex(endCharIdx);

            Size charSize = TextRenderer.MeasureText("W", this.Font);
            int lineRight = endPos.X + charSize.Width;

            int squiggleY = startPos.Y + this.Font.Height - 1;

            if (squiggleY < 0 || squiggleY > this.ClientSize.Height) return;
            if (startPos.X >= lineRight) return;

            IntPtr hdc = GetDC(this.Handle);
            try
            {
                using (var g = Graphics.FromHdc(hdc))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.SetClip(this.ClientRectangle);

                    using (var pen = new Pen(Color.FromArgb(255, 60, 60), 1.0f))
                    {
                        int waveHeight = 2;
                        int waveWidth = 4;
                        var points = new List<Point>();

                        int x = startPos.X;
                        bool up = true;
                        while (x < lineRight)
                        {
                            points.Add(new Point(x, squiggleY + (up ? 0 : waveHeight)));
                            x += waveWidth / 2;
                            up = !up;
                        }

                        if (points.Count > 1)
                        {
                            g.DrawLines(pen, points.ToArray());
                        }
                    }
                }
            }
            finally
            {
                ReleaseDC(this.Handle, hdc);
            }
        }
    }
}
