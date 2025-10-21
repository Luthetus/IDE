using Clair.TextEditor.RazorLib.TextEditors.Models;

namespace Clair.CompilerServices.JavaScript;

public struct JavaScriptLexerOutput
{
    public JavaScriptLexerOutput(TextEditorModel modelModifier)
    {
        ModelModifier = modelModifier;
    }
    
    public TextEditorModel ModelModifier { get; }
}
