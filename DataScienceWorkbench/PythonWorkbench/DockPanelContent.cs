using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace RJLG.IntelliSEM.UI.Controls.PythonDataScience
{
    internal class ToolDockContent : DockContent
    {
        public ToolDockContent()
        {
            DockAreas = DockAreas.DockLeft | DockAreas.DockRight |
                        DockAreas.DockTop | DockAreas.DockBottom |
                        DockAreas.Float;
            HideOnClose = true;
            CloseButtonVisible = false;
        }

        protected override string GetPersistString()
        {
            return Text;
        }
    }

    internal class DocumentDockContent : DockContent
    {
        public DocumentDockContent()
        {
            DockAreas = DockAreas.Document | DockAreas.Float;
            CloseButtonVisible = false;
        }

        protected override string GetPersistString()
        {
            return "Document";
        }
    }

    internal class FileDockContent : DockContent
    {
        public bool AllowClose { get; set; } = false;
        public event EventHandler CloseRequested;

        public FileDockContent()
        {
            DockAreas = DockAreas.Document | DockAreas.Float;
            CloseButton = true;
            CloseButtonVisible = true;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!AllowClose)
            {
                e.Cancel = true;
                CloseRequested?.Invoke(this, EventArgs.Empty);
                return;
            }
            base.OnFormClosing(e);
        }

        protected override string GetPersistString()
        {
            return "File:" + Text;
        }
    }

    internal static class DockIcons
    {
        public static Icon CreateEditorIcon()
        {
            return RenderIcon((g, r) =>
            {
                var pen = new Pen(Color.FromArgb(60, 120, 216), 1.2f);
                g.DrawString("</>", new Font("Arial", 6f, FontStyle.Bold), new SolidBrush(Color.FromArgb(60, 120, 216)),
                    r, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                pen.Dispose();
            });
        }

        public static Icon CreateFilesIcon()
        {
            return RenderIcon((g, r) =>
            {
                var brush = new SolidBrush(Color.FromArgb(220, 180, 60));
                var pen = new Pen(Color.FromArgb(180, 140, 30), 1f);
                g.FillRectangle(brush, 2, 5, 11, 8);
                g.DrawRectangle(pen, 2, 5, 11, 8);
                var tabBrush = new SolidBrush(Color.FromArgb(240, 200, 80));
                g.FillRectangle(tabBrush, 2, 3, 5, 3);
                g.DrawRectangle(pen, 2, 3, 5, 3);
                brush.Dispose(); pen.Dispose(); tabBrush.Dispose();
            });
        }

        public static Icon CreateOutputIcon()
        {
            return RenderIcon((g, r) =>
            {
                var bgBrush = new SolidBrush(Color.FromArgb(30, 30, 30));
                g.FillRectangle(bgBrush, 1, 1, 14, 14);
                var textBrush = new SolidBrush(Color.FromArgb(78, 201, 176));
                g.DrawString(">_", new Font("Consolas", 7f, FontStyle.Bold), textBrush,
                    r, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                bgBrush.Dispose(); textBrush.Dispose();
            });
        }

        public static Icon CreateReferenceIcon()
        {
            return RenderIcon((g, r) =>
            {
                var pen = new Pen(Color.FromArgb(100, 100, 200), 1.2f);
                g.DrawEllipse(pen, 3, 2, 5, 5);
                g.DrawEllipse(pen, 8, 8, 5, 5);
                g.DrawLine(pen, 7, 6, 9, 9);
                g.DrawEllipse(pen, 1, 9, 4, 4);
                g.DrawLine(pen, 4, 6, 3, 9);
                pen.Dispose();
            });
        }

        public static Icon CreatePackageIcon()
        {
            return RenderIcon((g, r) =>
            {
                var pen = new Pen(Color.FromArgb(0, 122, 204), 1.2f);
                var brush = new SolidBrush(Color.FromArgb(200, 220, 245));
                Point[] box = { new Point(8, 1), new Point(14, 4), new Point(14, 11), new Point(8, 14), new Point(2, 11), new Point(2, 4) };
                g.FillPolygon(brush, box);
                g.DrawPolygon(pen, box);
                g.DrawLine(pen, 2, 4, 8, 7);
                g.DrawLine(pen, 14, 4, 8, 7);
                g.DrawLine(pen, 8, 7, 8, 14);
                brush.Dispose(); pen.Dispose();
            });
        }

        public static Bitmap CreatePlayBitmap()
        {
            return RenderBitmap((g, r) =>
            {
                var brush = new SolidBrush(Color.FromArgb(34, 139, 34));
                Point[] tri = { new Point(4, 2), new Point(13, 8), new Point(4, 14) };
                g.FillPolygon(brush, tri);
                brush.Dispose();
            });
        }

        public static Bitmap CreateCheckBitmap()
        {
            return RenderBitmap((g, r) =>
            {
                var pen = new Pen(Color.FromArgb(0, 122, 204), 2f);
                g.DrawLine(pen, 3, 8, 6, 12);
                g.DrawLine(pen, 6, 12, 13, 3);
                pen.Dispose();
            });
        }

        public static Bitmap CreateSaveBitmap()
        {
            return RenderBitmap((g, r) =>
            {
                var pen = new Pen(Color.FromArgb(80, 80, 80), 1f);
                var brush = new SolidBrush(Color.FromArgb(100, 160, 220));
                g.FillRectangle(brush, 2, 1, 12, 14);
                g.DrawRectangle(pen, 2, 1, 12, 14);
                var labelBrush = new SolidBrush(Color.FromArgb(60, 60, 60));
                g.FillRectangle(labelBrush, 4, 1, 8, 5);
                var diskBrush = new SolidBrush(Color.FromArgb(240, 240, 240));
                g.FillRectangle(diskBrush, 5, 9, 6, 6);
                brush.Dispose(); pen.Dispose(); labelBrush.Dispose(); diskBrush.Dispose();
            });
        }

        public static Bitmap CreateUndoBitmap()
        {
            return RenderBitmap((g, r) =>
            {
                var pen = new Pen(Color.FromArgb(80, 80, 80), 1.5f);
                g.DrawArc(pen, 4, 4, 9, 9, 180, 230);
                var brush = new SolidBrush(Color.FromArgb(80, 80, 80));
                Point[] arrow = { new Point(3, 3), new Point(7, 3), new Point(5, 7) };
                g.FillPolygon(brush, arrow);
                pen.Dispose(); brush.Dispose();
            });
        }

        public static Bitmap CreateRedoBitmap()
        {
            return RenderBitmap((g, r) =>
            {
                var pen = new Pen(Color.FromArgb(80, 80, 80), 1.5f);
                g.DrawArc(pen, 3, 4, 9, 9, 180, -230);
                var brush = new SolidBrush(Color.FromArgb(80, 80, 80));
                Point[] arrow = { new Point(13, 3), new Point(9, 3), new Point(11, 7) };
                g.FillPolygon(brush, arrow);
                pen.Dispose(); brush.Dispose();
            });
        }

        public static Bitmap CreateFindBitmap()
        {
            return RenderBitmap((g, r) =>
            {
                var pen = new Pen(Color.FromArgb(80, 80, 80), 1.5f);
                g.DrawEllipse(pen, 2, 1, 8, 8);
                var handlePen = new Pen(Color.FromArgb(80, 80, 80), 2.5f);
                g.DrawLine(handlePen, 9, 9, 14, 14);
                pen.Dispose(); handlePen.Dispose();
            });
        }

        private static Bitmap RenderBitmap(Action<Graphics, Rectangle> draw)
        {
            var bmp = new Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                draw(g, new Rectangle(0, 0, 16, 16));
            }
            return bmp;
        }

        private static Icon RenderIcon(Action<Graphics, Rectangle> draw)
        {
            using (var bmp = new Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    draw(g, new Rectangle(0, 0, 16, 16));
                }
                return BitmapToIcon(bmp);
            }
        }

        private static Icon BitmapToIcon(Bitmap bmp)
        {
            int w = bmp.Width;
            int h = bmp.Height;
            int stride = w * 4;
            byte[] xorData = new byte[h * stride];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = bmp.GetPixel(x, h - 1 - y);
                    int offset = y * stride + x * 4;
                    xorData[offset + 0] = c.B;
                    xorData[offset + 1] = c.G;
                    xorData[offset + 2] = c.R;
                    xorData[offset + 3] = c.A;
                }
            }

            int andStride = ((w + 31) / 32) * 4;
            byte[] andData = new byte[h * andStride];

            int bmpInfoSize = 40;
            int imageSize = xorData.Length + andData.Length;
            int dataOffset = 22;

            using (var ms = new System.IO.MemoryStream())
            {
                var bw = new System.IO.BinaryWriter(ms);
                bw.Write((short)0);
                bw.Write((short)1);
                bw.Write((short)1);
                bw.Write((byte)w);
                bw.Write((byte)h);
                bw.Write((byte)0);
                bw.Write((byte)0);
                bw.Write((short)1);
                bw.Write((short)32);
                bw.Write(bmpInfoSize + imageSize);
                bw.Write(dataOffset);

                bw.Write(bmpInfoSize);
                bw.Write(w);
                bw.Write(h * 2);
                bw.Write((short)1);
                bw.Write((short)32);
                bw.Write(0);
                bw.Write(imageSize);
                bw.Write(0);
                bw.Write(0);
                bw.Write(0);
                bw.Write(0);

                bw.Write(xorData);
                bw.Write(andData);

                ms.Position = 0;
                return new Icon(ms);
            }
        }
    }
}
