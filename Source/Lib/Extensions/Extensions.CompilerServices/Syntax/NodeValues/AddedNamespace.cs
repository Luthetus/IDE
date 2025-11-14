using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.Extensions.CompilerServices.Syntax.Enums;

namespace Clair.Extensions.CompilerServices.Syntax.NodeValues;

/// <summary>
/// TODO: 'Ctrl A' then 'Ctrl V' => handled exception???...
/// ...I had to paste without doing 'Ctrl A' first.
/// </summary>
public struct AddedNamespace
{
    public AddedNamespace(TextEditorTextSpan textSpan)
    {
        TextSpan = textSpan;
    }

    public TextEditorTextSpan TextSpan { get; }
}

