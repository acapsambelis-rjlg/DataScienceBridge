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
        private static readonly string SymbolFontFamily = "DejaVu Sans";

        public static Icon CreateEditorIcon()
        {
            return RenderSymbolIcon("\u27E8\u27E9", 7f, Color.FromArgb(60, 120, 216), FontStyle.Bold);
        }

        public static Icon CreateFilesIcon()
        {
            return RenderSymbolIcon("\u2636", 11f, Color.FromArgb(200, 160, 40));
        }

        public static Icon CreateOutputIcon()
        {
            return RenderIcon((g, r) =>
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(30, 30, 30)), 1, 1, 14, 14);
                var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(">_", new Font("DejaVu Sans Mono", 7f, FontStyle.Bold),
                    new SolidBrush(Color.FromArgb(78, 201, 176)), r, fmt);
            });
        }

        public static Icon CreateReferenceIcon()
        {
            return RenderSymbolIcon("\u25C8", 11f, Color.FromArgb(100, 100, 200));
        }

        public static Icon CreatePackageIcon()
        {
            return RenderSymbolIcon("\u2B21", 11f, Color.FromArgb(0, 122, 204));
        }

        public static Bitmap CreatePlayBitmap()
        {
            return RenderSymbolBitmap("\u25B6", 10f, Color.FromArgb(34, 139, 34));
        }

        public static Bitmap CreateCheckBitmap()
        {
            return RenderSymbolBitmap("\u2713", 11f, Color.FromArgb(0, 122, 204), FontStyle.Bold);
        }

        public static Bitmap CreateSaveBitmap()
        {
            return RenderSymbolBitmap("\u2B07", 11f, Color.FromArgb(80, 80, 80));
        }

        public static Bitmap CreateUndoBitmap()
        {
            return RenderSymbolBitmap("\u21B6", 11f, Color.FromArgb(80, 80, 80), FontStyle.Bold);
        }

        public static Bitmap CreateRedoBitmap()
        {
            return RenderSymbolBitmap("\u21B7", 11f, Color.FromArgb(80, 80, 80), FontStyle.Bold);
        }

        public static Bitmap CreateFindBitmap()
        {
            return RenderSymbolBitmap("\u2315", 11f, Color.FromArgb(80, 80, 80));
        }

        private static Bitmap RenderSymbolBitmap(string symbol, float size, Color color, FontStyle style = FontStyle.Regular)
        {
            return RenderBitmap((g, r) =>
            {
                var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using (var font = new Font(SymbolFontFamily, size, style))
                using (var brush = new SolidBrush(color))
                    g.DrawString(symbol, font, brush, r, fmt);
            });
        }

        private static Icon RenderSymbolIcon(string symbol, float size, Color color, FontStyle style = FontStyle.Regular)
        {
            return RenderIcon((g, r) =>
            {
                var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using (var font = new Font(SymbolFontFamily, size, style))
                using (var brush = new SolidBrush(color))
                    g.DrawString(symbol, font, brush, r, fmt);
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
