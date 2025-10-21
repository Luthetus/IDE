using Clair.TextEditor.RazorLib.CompilerServices;
using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.Extensions.CompilerServices.Syntax;

namespace Clair.Extensions.CompilerServices;

public class ExtendedCompilationUnit : ICompilationUnit
{
    public IReadOnlyList<SyntaxToken> TokenList { get; init; } = Array.Empty<SyntaxToken>();
    public IReadOnlyList<TextEditorDiagnostic> DiagnosticList { get; init; } = Array.Empty<TextEditorDiagnostic>();

    public IEnumerable<TextEditorTextSpan> GetDiagnosticTextSpans()
    {
        return DiagnosticList.Select(x => x.TextSpan);
    }
}
