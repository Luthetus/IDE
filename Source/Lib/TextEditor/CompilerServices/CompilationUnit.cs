using Clair.TextEditor.RazorLib.Lexers.Models;

namespace Clair.TextEditor.RazorLib.CompilerServices;

public class CompilationUnit : ICompilationUnit
{
    public IReadOnlyList<TextEditorDiagnostic> DiagnosticList { get; init; } = Array.Empty<TextEditorDiagnostic>();

    public IEnumerable<TextEditorTextSpan> GetDiagnosticTextSpans()
    {
        return DiagnosticList.Select(x => x.TextSpan);
    }
}
