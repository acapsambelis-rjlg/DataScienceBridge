using System.Collections.Generic;
using Telerik.WinForms.SyntaxEditor.Core.Tagging;
using Telerik.WinForms.SyntaxEditor.Core.Text;

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
            this.CallOnTagsChanged();
        }

        public void ClearDiagnostics()
        {
            if (diagnostics.Count == 0) return;
            diagnostics.Clear();
            this.CallOnTagsChanged();
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
            this.CallOnTagsChanged();
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
                this.CallOnTagsChanged();
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
            this.CallOnTagsChanged();
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
                this.CallOnTagsChanged();
        }

        public override IEnumerable<TagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (var diag in diagnostics)
            {
                var type = diag.Severity == DiagnosticSeverity.Error ? ErrorType : WarningType;
                var span = new Span(diag.StartIndex, diag.Length);
                yield return new TagSpan<ClassificationTag>(span, new ClassificationTag(type));
            }
        }

        public List<DiagnosticSpan> CurrentDiagnostics
        {
            get { return new List<DiagnosticSpan>(diagnostics); }
        }

        private void CallOnTagsChanged()
        {
            this.InvalidateTags();
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
