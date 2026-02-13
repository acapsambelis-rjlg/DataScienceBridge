using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DataScienceWorkbench
{
    public class SquiggleRichTextBox : RichTextBox
    {
        private int errorLineNumber = -1;
        private string errorMessage = "";
        private const int WM_PAINT = 0x000F;

        private static readonly Color CurrentLineColor = Color.FromArgb(25, 0, 0, 0);
        private static readonly Color BracketHighlightColor = Color.FromArgb(60, 0, 120, 215);
        private static readonly Color WarningSquiggleColor = Color.FromArgb(220, 180, 50);
        private int matchedBracketPos1 = -1;
        private int matchedBracketPos2 = -1;
        private List<SymbolError> symbolErrors = new List<SymbolError>();
        private ToolTip errorToolTip;
        private string lastTooltipText = "";
        private int lastTooltipCharIndex = -1;

        private static readonly Dictionary<char, char> OpenBrackets = new Dictionary<char, char>
        {
            { '(', ')' }, { '[', ']' }, { '{', '}' }
        };
        private static readonly Dictionary<char, char> CloseBrackets = new Dictionary<char, char>
        {
            { ')', '(' }, { ']', '[' }, { '}', '{' }
        };

        public SquiggleRichTextBox()
        {
            errorToolTip = new ToolTip();
            errorToolTip.InitialDelay = 300;
            errorToolTip.ReshowDelay = 100;
            errorToolTip.AutoPopDelay = 15000;
            errorToolTip.UseFading = true;
            errorToolTip.UseAnimation = true;
            errorToolTip.BackColor = Color.FromArgb(45, 45, 48);
            errorToolTip.ForeColor = Color.FromArgb(240, 240, 240);
            errorToolTip.OwnerDraw = true;
            errorToolTip.Draw += ErrorToolTip_Draw;
            errorToolTip.Popup += ErrorToolTip_Popup;
        }

        private void ErrorToolTip_Popup(object sender, PopupEventArgs e)
        {
            string text = lastTooltipText;
            if (string.IsNullOrEmpty(text)) return;
            using (var g = Graphics.FromHwnd(IntPtr.Zero))
            {
                var size = TextRenderer.MeasureText(g, text, new Font("Segoe UI", 9f), new Size(600, 0), TextFormatFlags.Left | TextFormatFlags.WordBreak);
                e.ToolTipSize = new Size(size.Width + 16, size.Height + 10);
            }
        }

        private void ErrorToolTip_Draw(object sender, DrawToolTipEventArgs e)
        {
            using (var bgBrush = new SolidBrush(Color.FromArgb(45, 45, 48)))
            {
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
            }
            using (var borderPen = new Pen(Color.FromArgb(80, 80, 85)))
            {
                e.Graphics.DrawRectangle(borderPen, new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1));
            }
            using (var textBrush = new SolidBrush(Color.FromArgb(240, 240, 240)))
            using (var font = new Font("Segoe UI", 9f))
            {
                var textRect = new Rectangle(e.Bounds.X + 8, e.Bounds.Y + 5, e.Bounds.Width - 16, e.Bounds.Height - 10);
                e.Graphics.DrawString(e.ToolTipText, font, textBrush, textRect);
            }
        }

        public void SetErrorLine(int lineNumber, string message = "")
        {
            errorMessage = message ?? "";
            if (errorLineNumber == lineNumber && !string.IsNullOrEmpty(errorMessage)) { this.Invalidate(); return; }
            errorLineNumber = lineNumber;
            this.Invalidate();
        }

        public void ClearError()
        {
            if (errorLineNumber == -1 && string.IsNullOrEmpty(errorMessage)) return;
            errorLineNumber = -1;
            errorMessage = "";
            HideTooltip();
            this.Invalidate();
        }

        public int ErrorLine { get { return errorLineNumber; } }
        public string ErrorMessage { get { return errorMessage; } }

        public void SetSymbolErrors(List<SymbolError> errors)
        {
            symbolErrors = errors ?? new List<SymbolError>();
            HideTooltip();
            this.Invalidate();
        }

        public void ClearSymbolErrors()
        {
            if (symbolErrors.Count == 0) return;
            symbolErrors.Clear();
            HideTooltip();
            this.Invalidate();
        }

        public List<SymbolError> SymbolErrors { get { return symbolErrors; } }

        public void UpdateBracketMatching()
        {
            int oldPos1 = matchedBracketPos1;
            int oldPos2 = matchedBracketPos2;

            matchedBracketPos1 = -1;
            matchedBracketPos2 = -1;

            string text = this.Text;
            int pos = this.SelectionStart;
            if (this.SelectionLength > 0 || pos < 0 || text.Length == 0)
            {
                if (oldPos1 != -1) this.Invalidate();
                return;
            }

            char charAtCursor = pos < text.Length ? text[pos] : '\0';
            char charBefore = pos > 0 ? text[pos - 1] : '\0';

            if (OpenBrackets.ContainsKey(charAtCursor))
            {
                matchedBracketPos1 = pos;
                matchedBracketPos2 = FindMatchingForward(text, pos, charAtCursor, OpenBrackets[charAtCursor]);
            }
            else if (CloseBrackets.ContainsKey(charAtCursor))
            {
                matchedBracketPos1 = pos;
                matchedBracketPos2 = FindMatchingBackward(text, pos, charAtCursor, CloseBrackets[charAtCursor]);
            }
            else if (OpenBrackets.ContainsKey(charBefore))
            {
                matchedBracketPos1 = pos - 1;
                matchedBracketPos2 = FindMatchingForward(text, pos - 1, charBefore, OpenBrackets[charBefore]);
            }
            else if (CloseBrackets.ContainsKey(charBefore))
            {
                matchedBracketPos1 = pos - 1;
                matchedBracketPos2 = FindMatchingBackward(text, pos - 1, charBefore, CloseBrackets[charBefore]);
            }

            if (oldPos1 != matchedBracketPos1 || oldPos2 != matchedBracketPos2)
                this.Invalidate();
        }

        private int FindMatchingForward(string text, int pos, char open, char close)
        {
            int depth = 1;
            for (int i = pos + 1; i < text.Length; i++)
            {
                if (text[i] == open) depth++;
                else if (text[i] == close) { depth--; if (depth == 0) return i; }
            }
            return -1;
        }

        private int FindMatchingBackward(string text, int pos, char close, char open)
        {
            int depth = 1;
            for (int i = pos - 1; i >= 0; i--)
            {
                if (text[i] == close) depth++;
                else if (text[i] == open) { depth--; if (depth == 0) return i; }
            }
            return -1;
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_PAINT)
            {
                DrawOverlays();
            }
        }

        private void DrawOverlays()
        {
            try
            {
                using (var g = this.CreateGraphics())
                {
                    g.SetClip(this.ClientRectangle);
                    DrawCurrentLineHighlight(g);
                    DrawBracketHighlights(g);
                    if (errorLineNumber >= 1)
                        DrawSquiggle(g);
                    DrawSymbolErrorSquiggles(g);
                }
            }
            catch { }
        }

        private void DrawCurrentLineHighlight(Graphics g)
        {
            if (this.SelectionLength > 0) return;

            int currentLine = this.GetLineFromCharIndex(this.SelectionStart);
            int charIdx = this.GetFirstCharIndexFromLine(currentLine);
            if (charIdx < 0) return;

            Point linePos = this.GetPositionFromCharIndex(charIdx);
            int lineHeight = this.Font.Height;

            Rectangle lineRect = new Rectangle(0, linePos.Y, this.ClientSize.Width, lineHeight);

            if (lineRect.Bottom < 0 || lineRect.Top > this.ClientSize.Height) return;

            using (var brush = new SolidBrush(CurrentLineColor))
            {
                g.FillRectangle(brush, lineRect);
            }
        }

        private void DrawBracketHighlights(Graphics g)
        {
            if (matchedBracketPos1 < 0 || matchedBracketPos2 < 0) return;

            DrawBracketRect(g, matchedBracketPos1);
            DrawBracketRect(g, matchedBracketPos2);
        }

        private void DrawBracketRect(Graphics g, int charIndex)
        {
            if (charIndex < 0 || charIndex >= this.Text.Length) return;

            Point pos = this.GetPositionFromCharIndex(charIndex);
            if (pos.Y < 0 || pos.Y > this.ClientSize.Height) return;

            int charWidth;
            if (charIndex + 1 < this.Text.Length && this.Text[charIndex] != '\n')
            {
                Point nextPos = this.GetPositionFromCharIndex(charIndex + 1);
                if (nextPos.Y == pos.Y && nextPos.X > pos.X)
                    charWidth = nextPos.X - pos.X;
                else
                    charWidth = TextRenderer.MeasureText(this.Text[charIndex].ToString(), this.Font, Size.Empty, TextFormatFlags.NoPadding).Width - 4;
            }
            else
            {
                charWidth = TextRenderer.MeasureText(this.Text[charIndex].ToString(), this.Font, Size.Empty, TextFormatFlags.NoPadding).Width - 4;
            }
            if (charWidth < 4) charWidth = 4;

            Rectangle rect = new Rectangle(pos.X, pos.Y, charWidth, this.Font.Height);

            using (var brush = new SolidBrush(BracketHighlightColor))
            {
                g.FillRectangle(brush, rect);
            }
            using (var pen = new Pen(Color.FromArgb(120, 0, 80, 180)))
            {
                g.DrawRectangle(pen, rect);
            }
        }

        private void DrawSymbolErrorSquiggles(Graphics g)
        {
            if (symbolErrors.Count == 0) return;

            string text = this.Text;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var pen = new Pen(WarningSquiggleColor, 1.0f))
            {
                foreach (var err in symbolErrors)
                {
                    if (err.StartIndex < 0 || err.StartIndex >= text.Length) continue;
                    if (err.StartIndex + err.Length > text.Length) continue;

                    Point startPos = this.GetPositionFromCharIndex(err.StartIndex);
                    int endIdx = err.StartIndex + err.Length - 1;
                    Point endPos = this.GetPositionFromCharIndex(endIdx);

                    if (startPos.Y != endPos.Y) continue;

                    int charWidth;
                    if (endIdx + 1 < text.Length && text[endIdx] != '\n')
                    {
                        Point nextPos = this.GetPositionFromCharIndex(endIdx + 1);
                        if (nextPos.Y == endPos.Y && nextPos.X > endPos.X)
                            charWidth = nextPos.X - endPos.X;
                        else
                            charWidth = 8;
                    }
                    else
                    {
                        charWidth = 8;
                    }

                    int rightEdge = endPos.X + charWidth;
                    int squiggleY = startPos.Y + this.Font.Height - 1;

                    if (squiggleY < 0 || squiggleY > this.ClientSize.Height) continue;
                    if (startPos.X >= rightEdge) continue;

                    int waveHeight = 2;
                    int waveWidth = 4;
                    var points = new List<Point>();
                    int x = startPos.X;
                    bool up = true;
                    while (x < rightEdge)
                    {
                        points.Add(new Point(x, squiggleY + (up ? 0 : waveHeight)));
                        x += waveWidth / 2;
                        up = !up;
                    }

                    if (points.Count > 1)
                        g.DrawLines(pen, points.ToArray());
                }
            }
        }

        private void DrawSquiggle(Graphics g)
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

            g.SmoothingMode = SmoothingMode.AntiAlias;

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

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int charIndex = this.GetCharIndexFromPosition(e.Location);
            if (charIndex < 0 || charIndex >= this.Text.Length)
            {
                HideTooltip();
                return;
            }

            string tooltip = GetErrorAtPosition(charIndex);
            if (!string.IsNullOrEmpty(tooltip))
            {
                if (tooltip != lastTooltipText || charIndex != lastTooltipCharIndex)
                {
                    lastTooltipText = tooltip;
                    lastTooltipCharIndex = charIndex;
                    errorToolTip.Hide(this);
                    Point pos = e.Location;
                    pos.Y -= 40;
                    if (pos.Y < 0) pos.Y = e.Location.Y + 20;
                    errorToolTip.Show(tooltip, this, pos);
                }
            }
            else
            {
                HideTooltip();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            HideTooltip();
        }

        private void HideTooltip()
        {
            if (!string.IsNullOrEmpty(lastTooltipText))
            {
                lastTooltipText = "";
                lastTooltipCharIndex = -1;
                errorToolTip.Hide(this);
            }
        }

        private string GetErrorAtPosition(int charIndex)
        {
            foreach (var err in symbolErrors)
            {
                if (charIndex >= err.StartIndex && charIndex < err.StartIndex + err.Length)
                    return err.Message;
            }

            if (errorLineNumber >= 1 && errorLineNumber <= this.Lines.Length)
            {
                int lineIdx = errorLineNumber - 1;
                int lineStart = this.GetFirstCharIndexFromLine(lineIdx);
                int lineLen = this.Lines[lineIdx].Length;
                if (charIndex >= lineStart && charIndex < lineStart + lineLen)
                {
                    if (!string.IsNullOrEmpty(errorMessage))
                        return errorMessage;
                    return "Syntax error on line " + errorLineNumber;
                }
            }

            return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && errorToolTip != null)
            {
                errorToolTip.Dispose();
                errorToolTip = null;
            }
            base.Dispose(disposing);
        }
    }
}
