using System.Collections.Generic;
using Telerik.WinForms.SyntaxEditor.Core.Editor;
using Telerik.WinForms.SyntaxEditor.Core.Tagging;
using Telerik.WinForms.SyntaxEditor.Core.Text;

namespace RJLG.IntelliSEM.UI.Controls.PythonDataScience
{
    // FIX: Inherits TaggerBase<ClassificationTag> for diagnostic underline tagging.
    //       Uses ClassificationType instances registered with TextFormatDefinitions
    //       that include UnderlineInfo for visual error/warning indicators.
    public class DiagnosticTagger : TaggerBase<ClassificationTag>
    {
        public static readonly ClassificationType ErrorType = new ClassificationType("SyntaxError");
        public static readonly ClassificationType WarningType = new ClassificationType("ColumnWarning");

        private readonly List<DiagnosticSpan> diagnostics = new List<DiagnosticSpan>();

        public DiagnosticTagger(ITextDocumentEditor editor) : base(editor)
        {
        }

        public void SetDiagnostics(List<DiagnosticSpan> newDiagnostics)
        {
            diagnostics.Clear();
            if (newDiagnostics != null)
                diagnostics.AddRange(newDiagnostics);
            // FIX: InvalidateTags() does not exist on TaggerBase.
            //       Use base.CallOnTagsChanged(Span) to notify the editor that tags have changed.
            //       this.Document.CurrentSnapshot.Span returns the full document Span.
            this.CallOnTagsChanged(this.Document.CurrentSnapshot.Span);
        }

        public void ClearDiagnostics()
        {
            if (diagnostics.Count == 0) return;
            diagnostics.Clear();
            // FIX: Use base.CallOnTagsChanged(Span) instead of InvalidateTags()
            this.CallOnTagsChanged(this.Document.CurrentSnapshot.Span);
        }

        public void SetErrorLine(int lineNumber, string message, TextDocument document)
        {
            diagnostics.Clear();

            if (lineNumber >= 1 && document != null)
            {
                var snapshot = document.CurrentSnapshot;
                int lineIdx = lineNumber - 1;
                if (lineIdx < snapshot.LineCount)
                {
                    var line = snapshot.GetLineFromLineNumber(lineIdx);
                    string lineText = line.GetText();
                    if (lineText.Length > 0)
                    {
                        diagnostics.Add(new DiagnosticSpan
                        {
                            // FIX: TextSnapshotLine does NOT have a .Start property.
                            //       Use .Span.Start to get the absolute character offset of the line.
                            //       If .Span.Start also fails, try .StartPosition as an alternative.
                            StartIndex = line.Span.Start,
                            Length = lineText.Length,
                            Message = message ?? "Syntax error on line " + lineNumber,
                            Severity = DiagnosticSeverity.Error
                        });
                    }
                }
            }
            // FIX: Use base.CallOnTagsChanged(Span) instead of InvalidateTags()
            this.CallOnTagsChanged(this.Document.CurrentSnapshot.Span);
        }

        public void ClearErrorLine()
        {
            bool hadErrors = false;
            for (int i = diagnostics.Count - 1; i >= 0; i--)
            {
                if (diagnostics[i].Severity == DiagnosticSeverity.Error)
                {
                    diagnostics.RemoveAt(i);
                    hadErrors = true;
                }
            }
            if (hadErrors)
                // FIX: Use base.CallOnTagsChanged(Span) instead of InvalidateTags()
                this.CallOnTagsChanged(this.Document.CurrentSnapshot.Span);
        }

        public void SetSymbolErrors(List<SymbolError> symbolErrors)
        {
            for (int i = diagnostics.Count - 1; i >= 0; i--)
            {
                if (diagnostics[i].Severity == DiagnosticSeverity.Warning)
                    diagnostics.RemoveAt(i);
            }

            if (symbolErrors != null)
            {
                foreach (var err in symbolErrors)
                {
                    diagnostics.Add(new DiagnosticSpan
                    {
                        StartIndex = err.StartIndex,
                        Length = err.Length,
                        Message = err.Message,
                        Severity = DiagnosticSeverity.Warning
                    });
                }
            }
            // FIX: Use base.CallOnTagsChanged(Span) instead of InvalidateTags()
            this.CallOnTagsChanged(this.Document.CurrentSnapshot.Span);
        }

        public void ClearSymbolErrors()
        {
            bool hadWarnings = false;
            for (int i = diagnostics.Count - 1; i >= 0; i--)
            {
                if (diagnostics[i].Severity == DiagnosticSeverity.Warning)
                {
                    diagnostics.RemoveAt(i);
                    hadWarnings = true;
                }
            }
            if (hadWarnings)
                // FIX: Use base.CallOnTagsChanged(Span) instead of InvalidateTags()
                this.CallOnTagsChanged(this.Document.CurrentSnapshot.Span);
        }

        // FIX: GetTags must yield TagSpan<ClassificationTag> with TextSnapshotSpan, not raw Span.
        //       TagSpan<T> constructor requires TextSnapshotSpan(TextSnapshot, Span).
        public override IEnumerable<TagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans == null || spans.Count == 0)
                yield break;

            // FIX: Need the snapshot to create TextSnapshotSpan for each TagSpan
            var snapshot = spans[0].Snapshot;

            foreach (var diag in diagnostics)
            {
                var type = diag.Severity == DiagnosticSeverity.Error ? ErrorType : WarningType;
                // FIX: TagSpan<T> requires TextSnapshotSpan, not raw Span.
                //       Wrap Span in TextSnapshotSpan with snapshot reference.
                var snapshotSpan = new TextSnapshotSpan(snapshot, new Span(diag.StartIndex, diag.Length));
                yield return new TagSpan<ClassificationTag>(snapshotSpan, new ClassificationTag(type));
            }
        }

        public List<DiagnosticSpan> CurrentDiagnostics
        {
            get { return new List<DiagnosticSpan>(diagnostics); }
        }
    }

    public class DiagnosticSpan
    {
        public int StartIndex { get; set; }
        public int Length { get; set; }
        public string Message { get; set; }
        public DiagnosticSeverity Severity { get; set; }
    }

    public enum DiagnosticSeverity
    {
        Error,
        Warning
    }
}
