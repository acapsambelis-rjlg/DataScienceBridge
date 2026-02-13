using System;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;

namespace DataScienceWorkbench
{
    public class LineNumberPanel : Panel
    {
        private RichTextBox editor;
        private Font lineFont;

        public LineNumberPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            lineFont = new Font("Consolas", 9f);
        }

        public void UpdateFont(Font font)
        {
            if (lineFont != null) lineFont.Dispose();
            lineFont = font;
            this.Invalidate();
        }

        public void AttachEditor(RichTextBox editorBox)
        {
            editor = editorBox;
            editor.VScroll += (s, e) => this.Invalidate();
            editor.TextChanged += (s, e) => this.Invalidate();
            editor.Resize += (s, e) => this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (editor == null) return;

            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            e.Graphics.Clear(Color.FromArgb(240, 240, 240));

            int firstCharIndex = editor.GetCharIndexFromPosition(new Point(0, 0));
            int firstLine = editor.GetLineFromCharIndex(firstCharIndex);

            int totalLines = editor.Lines.Length;
            if (totalLines == 0) totalLines = 1;

            using (var brush = new SolidBrush(Color.FromArgb(110, 110, 110)))
            using (var sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Far;
                sf.LineAlignment = StringAlignment.Near;

                for (int i = firstLine; i < totalLines; i++)
                {
                    int charIdx = editor.GetFirstCharIndexFromLine(i);
                    if (charIdx < 0) break;

                    Point pos = editor.GetPositionFromCharIndex(charIdx);
                    int y = pos.Y;

                    if (y > editor.Height) break;

                    var rect = new RectangleF(0, y, this.Width - 6, lineFont.Height);
                    e.Graphics.DrawString((i + 1).ToString(), lineFont, brush, rect, sf);
                }
            }

            using (var pen = new Pen(Color.FromArgb(200, 200, 200)))
            {
                e.Graphics.DrawLine(pen, this.Width - 1, 0, this.Width - 1, this.Height);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && lineFont != null)
            {
                lineFont.Dispose();
                lineFont = null;
            }
            base.Dispose(disposing);
        }
    }
}
