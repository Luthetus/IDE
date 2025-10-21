using Clair.TextEditor.RazorLib.Lexers.Models;

namespace Clair.Extensions.CompilerServices.Syntax;

public struct SyntaxToken
{
    public SyntaxToken(SyntaxKind syntaxKind, TextEditorTextSpan textSpan)
    {
        SyntaxKind = syntaxKind;
        TextSpan = textSpan;
    }

    public SyntaxKind SyntaxKind { get; }

    /// <summary>TODO: Remove the setter.</summary>
    public TextEditorTextSpan TextSpan { get; set; }

    public bool IsFabricated { get; init; }
    public bool ConstructorWasInvoked => TextSpan.ConstructorWasInvoked;
}
