using System;
using System.Drawing;
using System.IO;
using Telerik.WinControls.UI;
using Telerik.WinForms.SyntaxEditor.Core.Text;

namespace RJLG.IntelliSEM.UI.Controls.PythonDataScience
{
    internal static class SyntaxEditorHelper
    {
        // FIX: snapshot.GetText(snapshot.Span) — explicit Span overload required, parameterless GetText() doesn't exist
        public static string GetText(this RadSyntaxEditor editor)
        {
            var snapshot = editor.Document.CurrentSnapshot;
            return snapshot.GetText(snapshot.Span);
        }

        // FIX: TextDocument constructor takes a StringReader, used to replace entire document content
        public static void SetText(this RadSyntaxEditor editor, string text)
        {
            editor.Document = new TextDocument(new StringReader(text ?? ""));
        }

        // FIX: CaretPosition.Index returns column within line, not absolute position.
        //       Must compute absolute offset = (start of current line) + column.
        //       TextSnapshotLine does NOT have a .Start property; use .Span.Start instead.
        //       If .Span.Start also fails, try .StartPosition or calculate offset manually by iterating lines.
        public static int GetCaretIndex(this RadSyntaxEditor editor)
        {
            var caret = editor.SyntaxEditorElement.CaretPosition;
            var snapshot = editor.Document.CurrentSnapshot;
            int lineNumber = caret.LineNumber;
            if (lineNumber < 0 || lineNumber >= snapshot.LineCount) return 0;
            var lineInfo = snapshot.GetLineFromLineNumber(lineNumber);
            // FIX: TextSnapshotLine.Start doesn't exist; use TextSnapshotLine.Span.Start
            //       which gets the character offset of the line start within the document
            return lineInfo.Span.Start + caret.Index;
        }

        // FIX: Same .Span.Start fix for converting absolute position to line+column
        public static void SetCaretIndex(this RadSyntaxEditor editor, int index)
        {
            var snapshot = editor.Document.CurrentSnapshot;
            if (index < 0) index = 0;
            int textLen = snapshot.Length;
            if (index > textLen) index = textLen;

            var lineInfo = snapshot.GetLineFromPosition(index);
            int lineNumber = lineInfo.LineNumber;
            // FIX: Use .Span.Start to get the absolute start offset of this line
            int column = index - lineInfo.Span.Start;
            editor.SyntaxEditorElement.CaretPosition.MoveToPosition(new CaretPosition(lineNumber, column));
        }

        // FIX: Selection.GetSelectedSpans() may not exist in WinForms Telerik.
        //       Use GetSelectedText().Length instead — confirmed API that works.
        public static int GetSelectionLength(this RadSyntaxEditor editor)
        {
            var sel = editor.SyntaxEditorElement.Selection;
            if (sel.IsEmpty) return 0;
            // FIX: GetSelectedSpans() is not available on WinForms Selection;
            //       using GetSelectedText().Length as a reliable alternative
            string selectedText = sel.GetSelectedText();
            return selectedText != null ? selectedText.Length : 0;
        }

        // FIX: Selection.GetSelectedText() is the confirmed WinForms API
        public static string GetSelectedText(this RadSyntaxEditor editor)
        {
            var sel = editor.SyntaxEditorElement.Selection;
            if (sel.IsEmpty) return "";
            return sel.GetSelectedText();
        }

        // FIX: TextDocument.Insert(int, string) inserts text at absolute position.
        //       If selection exists, delete it first via DeleteCommand then insert.
        public static void InsertAtCaret(this RadSyntaxEditor editor, string text)
        {
            var sel = editor.SyntaxEditorElement.Selection;
            if (!sel.IsEmpty)
            {
                // FIX: Commands.Execute requires an object parameter — pass null
                editor.Commands.DeleteCommand.Execute(null);
            }
            int pos = GetCaretIndex(editor);
            // FIX: TextDocument.Insert(int, string) is a valid API for inserting text
            editor.Document.Insert(pos, text);
        }

        // FIX: ReplaceSelection delegates to InsertAtCaret which handles selection deletion
        public static void ReplaceSelection(this RadSyntaxEditor editor, string text)
        {
            InsertAtCaret(editor, text);
        }

        // FIX: TextSnapshotLine.GetText() returns line content without line break
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

        // FIX: GetLineFromPosition(charIndex).LineNumber returns 0-based line index
        public static int GetLineFromCharIndex(this RadSyntaxEditor editor, int charIndex)
        {
            var snapshot = editor.Document.CurrentSnapshot;
            if (charIndex < 0) charIndex = 0;
            if (charIndex > snapshot.Length) charIndex = snapshot.Length;
            return snapshot.GetLineFromPosition(charIndex).LineNumber;
        }

        // FIX: Use .Span.Start to get the absolute start offset of a line
        //       TextSnapshotLine does NOT have a .Start property directly
        public static int GetFirstCharIndexFromLine(this RadSyntaxEditor editor, int lineIndex)
        {
            var snapshot = editor.Document.CurrentSnapshot;
            if (lineIndex < 0) lineIndex = 0;
            if (lineIndex >= snapshot.LineCount) lineIndex = snapshot.LineCount - 1;
            if (lineIndex < 0) return 0;
            // FIX: .Span.Start instead of .Start
            return snapshot.GetLineFromLineNumber(lineIndex).Span.Start;
        }

        // FIX: GetText() on TextSnapshotLine returns the line text
        public static string GetLineText(this RadSyntaxEditor editor, int lineIndex)
        {
            var snapshot = editor.Document.CurrentSnapshot;
            if (lineIndex < 0 || lineIndex >= snapshot.LineCount) return "";
            return snapshot.GetLineFromLineNumber(lineIndex).GetText();
        }

        // FIX: Selection.Select(CaretPosition, CaretPosition) is the confirmed WinForms API.
        //       Uses .Span.Start to convert absolute positions to line+column CaretPositions.
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

            // FIX: Use .Span.Start to get line start offset for column calculation
            var startCaret = new CaretPosition(startLine.LineNumber, start - startLine.Span.Start);
            var endCaret = new CaretPosition(endLine.LineNumber, end - endLine.Span.Start);

            editor.SyntaxEditorElement.Selection.Select(startCaret, endCaret);
        }

        // FIX: Selection.Clear() clears the current selection
        public static void ClearSelection(this RadSyntaxEditor editor)
        {
            editor.SyntaxEditorElement.Selection.Clear();
        }

        // FIX: MoveCurrentLineToTop() scrolls the editor to show the caret line at top
        public static void ScrollToCaretPosition(this RadSyntaxEditor editor)
        {
            editor.MoveCurrentLineToTop();
        }

        // FIX: Commands.CutCommand.Execute(null) — the Execute method requires an object parameter.
        //       SyntaxEditorCommandBase.Execute(object) is the signature; pass null for no parameter.
        public static void PerformCut(this RadSyntaxEditor editor)
        {
            editor.Commands.CutCommand.Execute(null);
        }

        // FIX: RadSyntaxEditor.Copy() is a direct method, no parameter needed
        public static void PerformCopy(this RadSyntaxEditor editor)
        {
            editor.Copy();
        }

        // FIX: PasteCommand.Execute(null) — Execute requires object parameter
        public static void PerformPaste(this RadSyntaxEditor editor)
        {
            editor.Commands.PasteCommand.Execute(null);
        }

        // FIX: RadSyntaxEditor.SelectAll() is a direct method
        public static void PerformSelectAll(this RadSyntaxEditor editor)
        {
            editor.SelectAll();
        }

        // FIX: UndoCommand.Execute(null) — Execute requires object parameter
        public static void PerformUndo(this RadSyntaxEditor editor)
        {
            editor.Commands.UndoCommand.Execute(null);
        }

        // FIX: RedoCommand.Execute(null) — Execute requires object parameter
        public static void PerformRedo(this RadSyntaxEditor editor)
        {
            editor.Commands.RedoCommand.Execute(null);
        }

        // FIX: DeleteCommand.Execute(null) deletes selected text (or char after caret if no selection)
        public static void DeleteSelection(this RadSyntaxEditor editor)
        {
            var sel = editor.SyntaxEditorElement.Selection;
            if (!sel.IsEmpty)
            {
                editor.Commands.DeleteCommand.Execute(null);
            }
        }

        // FIX: TextDocument does NOT have a .Remove() or .Delete() method.
        //       Instead, select the range and use DeleteCommand to remove it.
        //       This approach also integrates with undo/redo tracking.
        public static void RemoveRange(this RadSyntaxEditor editor, int start, int length)
        {
            // FIX: Select the range first, then delete via command
            //       TextDocument.Remove(Span) does not exist in Telerik WinForms
            SelectRange(editor, start, length);
            editor.Commands.DeleteCommand.Execute(null);
        }

        // FIX: TextDocument.Insert(int, string) inserts text at the specified position
        public static void InsertAt(this RadSyntaxEditor editor, int position, string text)
        {
            editor.Document.Insert(position, text);
        }

        // FIX: Estimates pixel position from character index for autocomplete popup positioning.
        //       Uses .Span.Start for line start offset, ShowLineNumbers (not IsLineNumberMarginVisible),
        //       and SyntaxEditorElement.EditorFontSize (not editor.EditorFontSize or editor.Font).
        public static Point GetPositionFromCharIndex(this RadSyntaxEditor editor, int charIndex)
        {
            var snapshot = editor.Document.CurrentSnapshot;
            if (charIndex < 0) charIndex = 0;
            if (charIndex > snapshot.Length) charIndex = snapshot.Length;

            var lineInfo = snapshot.GetLineFromPosition(charIndex);
            int lineNumber = lineInfo.LineNumber;
            // FIX: Use .Span.Start instead of .Start
            int column = charIndex - lineInfo.Span.Start;

            // FIX: EditorFontSize lives on SyntaxEditorElement, not on RadSyntaxEditor directly
            float fontSize = editor.SyntaxEditorElement.EditorFontSize;
            float lineHeight = fontSize * 1.6f;
            float charWidth = fontSize * 0.6f;

            // FIX: ShowLineNumbers is the correct property (R1 2021 SP2+), not IsLineNumberMarginVisible
            int lineMarginWidth = editor.ShowLineNumbers ? 55 : 0;
            int x = (int)(column * charWidth) + lineMarginWidth;
            int y = (int)(lineNumber * lineHeight);

            return new Point(x, y);
        }
    }
}
