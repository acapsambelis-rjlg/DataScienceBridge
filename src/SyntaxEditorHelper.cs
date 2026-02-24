using System;
using System.Drawing;
using CodeEditor;

namespace RJLG.IntelliSEM.UI.Controls.PythonDataScience
{
    internal static class SyntaxEditorHelper
    {
        public static string GetText(this CodeTextBox editor)
        {
            return editor.Text;
        }

        public static void SetText(this CodeTextBox editor, string text)
        {
            editor.Text = text ?? "";
        }

        public static int GetCaretIndex(this CodeTextBox editor)
        {
            return editor.GetAbsoluteCaretPosition();
        }

        public static void SetCaretIndex(this CodeTextBox editor, int index)
        {
            editor.SetAbsoluteCaretPosition(index);
        }

        public static void InsertAtCaret(this CodeTextBox editor, string text)
        {
            editor.InsertTextAtCaret(text);
        }

        public static void ReplaceSelection(this CodeTextBox editor, string text)
        {
            editor.InsertTextAtCaret(text);
        }

        public static void SelectRange(this CodeTextBox editor, int start, int length)
        {
            editor.SelectRange(start, length);
        }

        public static int GetSelectionLength(this CodeTextBox editor)
        {
            return editor.SelectionLength;
        }

        public static string GetSelectedText(this CodeTextBox editor)
        {
            return editor.SelectedText;
        }

        public static void ClearSelection(this CodeTextBox editor)
        {
            editor.ClearSelectionRange();
        }

        public static string[] GetLines(this CodeTextBox editor)
        {
            var doc = editor.Document;
            int count = doc.LineCount;
            var lines = new string[count];
            for (int i = 0; i < count; i++)
                lines[i] = doc.GetLine(i);
            return lines;
        }

        public static int GetLineCount(this CodeTextBox editor)
        {
            return editor.LineCount;
        }

        public static string GetLineText(this CodeTextBox editor, int lineIndex)
        {
            return editor.Document.GetLine(lineIndex);
        }

        public static int GetLineFromCharIndex(this CodeTextBox editor, int charIndex)
        {
            return editor.GetLineFromCharIndex(charIndex);
        }

        public static int GetFirstCharIndexFromLine(this CodeTextBox editor, int lineIndex)
        {
            return editor.GetFirstCharIndexFromLine(lineIndex);
        }

        public static Point GetPositionFromCharIndex(this CodeTextBox editor, int charIndex)
        {
            return editor.GetPositionFromCharIndex(charIndex);
        }

        public static void PerformUndo(this CodeTextBox editor)
        {
            editor.PerformUndo();
        }

        public static void PerformRedo(this CodeTextBox editor)
        {
            editor.PerformRedo();
        }

        public static void PerformCopy(this CodeTextBox editor)
        {
            editor.Copy();
        }

        public static void PerformCut(this CodeTextBox editor)
        {
            editor.Cut();
        }

        public static void PerformPaste(this CodeTextBox editor)
        {
            editor.Paste();
        }

        public static void PerformSelectAll(this CodeTextBox editor)
        {
            editor.SelectAll();
        }

        public static void DeleteSelection(this CodeTextBox editor)
        {
            editor.DeleteSelectionText();
        }

        public static void RemoveRange(this CodeTextBox editor, int start, int length)
        {
            int startLine, startCol;
            OffsetToPosition(editor, start, out startLine, out startCol);
            int endLine, endCol;
            OffsetToPosition(editor, start + length, out endLine, out endCol);
            editor.Document.Delete(new TextPosition(startLine, startCol), new TextPosition(endLine, endCol));
        }

        public static void InsertAt(this CodeTextBox editor, int position, string text)
        {
            int line, col;
            OffsetToPosition(editor, position, out line, out col);
            editor.Document.Insert(new TextPosition(line, col), text);
        }

        public static void ScrollToCaretPosition(this CodeTextBox editor)
        {
            editor.SetCaretPosition(editor.CaretPosition);
        }

        private static void OffsetToPosition(CodeTextBox editor, int offset, out int line, out int col)
        {
            var doc = editor.Document;
            if (offset <= 0) { line = 0; col = 0; return; }
            int remaining = offset;
            for (int i = 0; i < doc.LineCount; i++)
            {
                int lineLen = doc.GetLineLength(i);
                if (remaining <= lineLen)
                {
                    line = i;
                    col = remaining;
                    return;
                }
                remaining -= lineLen + 1;
            }
            line = doc.LineCount - 1;
            col = doc.GetLineLength(line);
        }
    }
}
