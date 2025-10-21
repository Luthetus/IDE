using Clair.TextEditor.RazorLib.CompilerServices;
using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.Extensions.CompilerServices.Syntax;

namespace Clair.Ide.RazorLib.Terminals.Models;

public class TerminalCompilationUnit : ICompilationUnit
{
    public IReadOnlyList<SyntaxToken> SyntaxTokenList { get; set; } = new List<SyntaxToken>();
    public List<TextEditorTextSpan> ManualDecorationTextSpanList { get; } = new List<TextEditorTextSpan>();
    public List<Symbol> ManualSymbolList { get; } = new List<Symbol>();

    public IEnumerable<TextEditorTextSpan> GetDiagnosticTextSpans()
    {
        return Array.Empty<TextEditorTextSpan>();
    }
}
