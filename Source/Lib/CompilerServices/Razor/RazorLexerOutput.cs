using Clair.TextEditor.RazorLib.TextEditors.Models;

namespace Clair.CompilerServices.Razor;

public struct RazorLexerOutput
{
    public RazorLexerOutput(TextEditorModel modelModifier)
    {
        ModelModifier = modelModifier;
    }
    
    public TextEditorModel ModelModifier { get; }
}
