using Clair.TextEditor.RazorLib.Lexers.Models;

namespace Clair.Extensions.CompilerServices.Syntax.NodeValues;

/// <summary>
/// The textspan points to the text that identifies the namespace group that was contributed to.
/// </summary>
public struct NamespaceContribution
{
    public NamespaceContribution(TextEditorTextSpan textSpan)
    {
        TextSpan = textSpan;
    }

    public TextEditorTextSpan TextSpan { get; }
}
