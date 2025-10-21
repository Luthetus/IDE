using Clair.TextEditor.RazorLib.TextEditors.Models;

namespace Clair.CompilerServices.Json;

public struct JsonLexerOutput
{
    public JsonLexerOutput(TextEditorModel modelModifier)
    {
        ModelModifier = modelModifier;
    }
    
    public TextEditorModel ModelModifier { get; }
}
