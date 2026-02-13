using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace DataScienceWorkbench
{
    public class LineNumberPanel : Panel
    {
        private RichTextBox editor;
        private Font lineFont;
        private HashSet<int> bookmarks = new HashSet<int>();
        private List<FoldInfo> foldRegions = new List<FoldInfo>();

        public event EventHandler<int> BookmarkToggled;
        public event EventHandler<int> FoldToggled;

        public class FoldInfo
        {
            public int StartLine;
            public int EndLine;
            public bool Collapsed;
        }

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

        public void SetBookmarks(HashSet<int> marks)
        {
            bookmarks = marks ?? new HashSet<int>();
            this.Invalidate();
        }

        public void SetFoldRegions(List<FoldInfo> regions)
        {
            foldRegions = regions ?? new List<FoldInfo>();
            this.Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (editor == null) return;

            int firstCharIndex = editor.GetCharIndexFromPosition(new Point(0, 0));
            int firstLine = editor.GetLineFromCharIndex(firstCharIndex);
            int totalLines = editor.Lines.Length;
            if (totalLines == 0) totalLines = 1;

            for (int i = firstLine; i < totalLines; i++)
            {
                int charIdx = editor.GetFirstCharIndexFromLine(i);
                if (charIdx < 0) break;
                Point pos = editor.GetPositionFromCharIndex(charIdx);
                int y = pos.Y;
                if (y > editor.Height) break;

                if (e.Y >= y && e.Y < y + lineFont.Height)
                {
                    if (e.X >= this.Width - 16)
                    {
                        var region = foldRegions.Find(r => r.StartLine == i);
                        if (region != null)
                        {
                            FoldToggled?.Invoke(this, i);
                            return;
                        }
                    }

                    BookmarkToggled?.Invoke(this, i);
                    return;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (editor == null) return;

            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Color.FromArgb(240, 240, 240));

            int firstCharIndex = editor.GetCharIndexFromPosition(new Point(0, 0));
            int firstLine = editor.GetLineFromCharIndex(firstCharIndex);

            int totalLines = editor.Lines.Length;
            if (totalLines == 0) totalLines = 1;

            using (var numBrush = new SolidBrush(Color.FromArgb(110, 110, 110)))
            using (var bookmarkBrush = new SolidBrush(Color.FromArgb(30, 120, 200)))
            using (var foldBrush = new SolidBrush(Color.FromArgb(100, 100, 100)))
            using (var foldPen = new Pen(Color.FromArgb(140, 140, 140), 1f))
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

                    if (bookmarks.Contains(i))
                    {
                        int circleSize = 8;
                        int cx = 4;
                        int cy = y + (lineFont.Height - circleSize) / 2;
                        e.Graphics.FillEllipse(bookmarkBrush, cx, cy, circleSize, circleSize);
                    }

                    var numRect = new RectangleF(0, y, this.Width - 18, lineFont.Height);
                    e.Graphics.DrawString((i + 1).ToString(), lineFont, numBrush, numRect, sf);

                    var foldRegion = foldRegions.Find(r => r.StartLine == i);
                    if (foldRegion != null)
                    {
                        int boxSize = 9;
                        int bx = this.Width - 14;
                        int by = y + (lineFont.Height - boxSize) / 2;
                        e.Graphics.DrawRectangle(foldPen, bx, by, boxSize, boxSize);

                        float midY = by + boxSize / 2f;
                        float midX = bx + boxSize / 2f;
                        e.Graphics.DrawLine(foldPen, bx + 2, midY, bx + boxSize - 2, midY);
                        if (foldRegion.Collapsed)
                        {
                            e.Graphics.DrawLine(foldPen, midX, by + 2, midX, by + boxSize - 2);
                        }
                    }
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
