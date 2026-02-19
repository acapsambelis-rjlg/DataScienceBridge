using System;
using System.Drawing;
using System.IO;
using Telerik.WinControls.UI;

namespace RJLG.IntelliSEM.UI.Controls.PythonDataScience
{
    internal static class SyntaxEditorHelper
    {
        public static string GetText(this RadSyntaxEditor editor)
        {
            var snapshot = editor.Document.CurrentSnapshot;
            return snapshot.GetText(snapshot.Span);
        }

        public static void SetText(this RadSyntaxEditor editor, string text)
        {
            editor.Document = new TextDocument(new StringReader(text ?? ""));
        }

        public static int GetCaretIndex(this RadSyntaxEditor editor)
        {
            var caret = editor.SyntaxEditorElement.CaretPosition;
            var snapshot = editor.Document.CurrentSnapshot;
            int lineNumber = caret.LineNumber;
            if (lineNumber < 0 || lineNumber >= snapshot.LineCount) return 0;
            var lineInfo = snapshot.GetLineFromLineNumber(lineNumber);
            return lineInfo.Start + caret.Index;
        }

        public static void SetCaretIndex(this RadSyntaxEditor editor, int index)
        {
            var snapshot = editor.Document.CurrentSnapshot;
            if (index < 0) index = 0;
            int textLen = snapshot.Length;
            if (index > textLen) index = textLen;

            var lineInfo = snapshot.GetLineFromPosition(index);
            int lineNumber = lineInfo.LineNumber;
            int column = index - lineInfo.Start;
            editor.SyntaxEditorElement.CaretPosition.MoveToPosition(new CaretPosition(lineNumber, column));
        }

        public static int GetSelectionLength(this RadSyntaxEditor editor)
        {
            var sel = editor.SyntaxEditorElement.Selection;
            if (sel.IsEmpty) return 0;
            var spans = sel.GetSelectedSpans();
            if (spans == null || spans.Count == 0) return 0;
            int total = 0;
            foreach (var s in spans)
                total += s.Length;
            return total;
        }

        public static string GetSelectedText(this RadSyntaxEditor editor)
        {
            var sel = editor.SyntaxEditorElement.Selection;
            if (sel.IsEmpty) return "";
            return sel.GetSelectedText();
        }

        public static void InsertAtCaret(this RadSyntaxEditor editor, string text)
        {
            var sel = editor.SyntaxEditorElement.Selection;
            if (!sel.IsEmpty)
            {
                editor.Commands.DeleteCommand.Execute(null);
            }
            int pos = GetCaretIndex(editor);
            editor.Document.Insert(pos, text);
        }

        public static void ReplaceSelection(this RadSyntaxEditor editor, string text)
        {
            InsertAtCaret(editor, text);
        }

        public static string[] GetLines(this RadSyntaxEditor editor)
        {
            var snapshot = editor.Document.CurrentSnapshot;
            int lineCount = snapshot.LineCount;
            var lines = new string[lineCount];
            for (int i = 0; i < lineCount; i++)
                lines[i] = snapshot.GetLineFromLineNumber(i).GetText();
            return lines;
        }

        public static int GetLineCount(this RadSyntaxEditor editor)
        {
            return editor.Document.CurrentSnapshot.LineCount;
        }

        public static int GetLineFromCharIndex(this RadSyntaxEditor editor, int charIndex)
        {
            var snapshot = editor.Document.CurrentSnapshot;
            if (charIndex < 0) charIndex = 0;
            if (charIndex > snapshot.Length) charIndex = snapshot.Length;
            return snapshot.GetLineFromPosition(charIndex).LineNumber;
        }

        public static int GetFirstCharIndexFromLine(this RadSyntaxEditor editor, int lineIndex)
        {
            var snapshot = editor.Document.CurrentSnapshot;
            if (lineIndex < 0) lineIndex = 0;
            if (lineIndex >= snapshot.LineCount) lineIndex = snapshot.LineCount - 1;
            if (lineIndex < 0) return 0;
            return snapshot.GetLineFromLineNumber(lineIndex).Start;
        }

        public static string GetLineText(this RadSyntaxEditor editor, int lineIndex)
        {
            var snapshot = editor.Document.CurrentSnapshot;
            if (lineIndex < 0 || lineIndex >= snapshot.LineCount) return "";
            return snapshot.GetLineFromLineNumber(lineIndex).GetText();
        }

        public static void SelectRange(this RadSyntaxEditor editor, int start, int length)
        {
            var snapshot = editor.Document.CurrentSnapshot;
            int textLen = snapshot.Length;

            if (start < 0) start = 0;
            if (start > textLen) start = textLen;
            int end = start + length;
            if (end > textLen) end = textLen;
            if (end < start) end = start;

            var startLine = snapshot.GetLineFromPosition(start);
            var endLine = snapshot.GetLineFromPosition(end);

            var startCaret = new CaretPosition(startLine.LineNumber, start - startLine.Start);
            var endCaret = new CaretPosition(endLine.LineNumber, end - endLine.Start);

            editor.SyntaxEditorElement.Selection.Select(startCaret, endCaret);
        }

        public static void ClearSelection(this RadSyntaxEditor editor)
        {
            editor.SyntaxEditorElement.Selection.Clear();
        }

        public static void ScrollToCaretPosition(this RadSyntaxEditor editor)
        {
            editor.MoveCurrentLineToTop();
        }

        public static void PerformCut(this RadSyntaxEditor editor)
        {
            editor.Commands.CutCommand.Execute(null);
        }

        public static void PerformCopy(this RadSyntaxEditor editor)
        {
            editor.Copy();
        }

        public static void PerformPaste(this RadSyntaxEditor editor)
        {
            editor.Commands.PasteCommand.Execute(null);
        }

        public static void PerformSelectAll(this RadSyntaxEditor editor)
        {
            editor.SelectAll();
        }

        public static void PerformUndo(this RadSyntaxEditor editor)
        {
            editor.Commands.UndoCommand.Execute(null);
        }

        public static void PerformRedo(this RadSyntaxEditor editor)
        {
            editor.Commands.RedoCommand.Execute(null);
        }

        public static void DeleteSelection(this RadSyntaxEditor editor)
        {
            var sel = editor.SyntaxEditorElement.Selection;
            if (!sel.IsEmpty)
            {
                editor.Commands.DeleteCommand.Execute(null);
            }
        }

        public static void RemoveRange(this RadSyntaxEditor editor, int start, int length)
        {
            editor.Document.Remove(new Span(start, length));
        }

        public static void InsertAt(this RadSyntaxEditor editor, int position, string text)
        {
            editor.Document.Insert(position, text);
        }

        public static Point GetPositionFromCharIndex(this RadSyntaxEditor editor, int charIndex)
        {
            var snapshot = editor.Document.CurrentSnapshot;
            if (charIndex < 0) charIndex = 0;
            if (charIndex > snapshot.Length) charIndex = snapshot.Length;

            var lineInfo = snapshot.GetLineFromPosition(charIndex);
            int lineNumber = lineInfo.LineNumber;
            int column = charIndex - lineInfo.Start;

            float fontSize = editor.EditorFontSize;
            float lineHeight = fontSize * 1.6f;
            float charWidth = fontSize * 0.6f;

            int lineMarginWidth = editor.ShowLineNumbers ? 55 : 0;
            int x = (int)(column * charWidth) + lineMarginWidth;
            int y = (int)(lineNumber * lineHeight);

            return new Point(x, y);
        }
    }
}
