using System.Collections.Generic;
using Telerik.WinForms.Documents.Model.Code;

namespace RJLG.IntelliSEM.UI.Controls.PythonDataScience
{
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
            NotifyTagsChanged();
        }

        public void ClearDiagnostics()
        {
            if (diagnostics.Count == 0) return;
            diagnostics.Clear();
            NotifyTagsChanged();
        }

        public void SetErrorLine(int lineNumber, string message, ITextDocument document)
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
                            StartIndex = line.Start,
                            Length = lineText.Length,
                            Message = message ?? "Syntax error on line " + lineNumber,
                            Severity = DiagnosticSeverity.Error
                        });
                    }
                }
            }
            NotifyTagsChanged();
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
                NotifyTagsChanged();
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
            NotifyTagsChanged();
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
                NotifyTagsChanged();
        }

        public override IEnumerable<TagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans == null || spans.Count == 0)
                yield break;

            var snapshot = spans[0].Snapshot;

            foreach (var diag in diagnostics)
            {
                if (diag.StartIndex + diag.Length > snapshot.Length)
                    continue;

                var type = diag.Severity == DiagnosticSeverity.Error ? ErrorType : WarningType;
                var snapshotSpan = new TextSnapshotSpan(snapshot, new Span(diag.StartIndex, diag.Length));
                yield return new TagSpan<ClassificationTag>(snapshotSpan, new ClassificationTag(type));
            }
        }

        public List<DiagnosticSpan> CurrentDiagnostics
        {
            get { return new List<DiagnosticSpan>(diagnostics); }
        }

        private void NotifyTagsChanged()
        {
            if (this.Document != null && this.Document.CurrentSnapshot != null)
                this.CallOnTagsChanged(this.Document.CurrentSnapshot.Span);
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
