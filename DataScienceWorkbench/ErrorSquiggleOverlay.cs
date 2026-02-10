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
        private const int WM_PAINT = 0x000F;

        private static readonly Color CurrentLineColor = Color.FromArgb(20, 255, 255, 255);
        private static readonly Color BracketHighlightColor = Color.FromArgb(80, 128, 128, 128);
        private static readonly Color WarningSquiggleColor = Color.FromArgb(220, 180, 50);
        private int matchedBracketPos1 = -1;
        private int matchedBracketPos2 = -1;
        private List<SymbolError> symbolErrors = new List<SymbolError>();

        private static readonly Dictionary<char, char> OpenBrackets = new Dictionary<char, char>
        {
            { '(', ')' }, { '[', ']' }, { '{', '}' }
        };
        private static readonly Dictionary<char, char> CloseBrackets = new Dictionary<char, char>
        {
            { ')', '(' }, { ']', '[' }, { '}', '{' }
        };

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

        public void SetSymbolErrors(List<SymbolError> errors)
        {
            symbolErrors = errors ?? new List<SymbolError>();
            this.Invalidate();
        }

        public void ClearSymbolErrors()
        {
            if (symbolErrors.Count == 0) return;
            symbolErrors.Clear();
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
            using (var pen = new Pen(Color.FromArgb(120, 180, 180, 180)))
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
    }
}
