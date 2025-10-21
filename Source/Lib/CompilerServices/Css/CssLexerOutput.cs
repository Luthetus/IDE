using Clair.TextEditor.RazorLib.TextEditors.Models;

namespace Clair.CompilerServices.Css;

public struct CssLexerOutput
{
    public CssLexerOutput(TextEditorModel modelModifier)
    {
        ModelModifier = modelModifier;
    }
    
    public TextEditorModel ModelModifier { get; }
}
