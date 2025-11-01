using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models;

namespace Clair.CompilerServices.Xml;

public struct XmlLexerOutput
{
    public XmlLexerOutput(TextEditorModel modelModifier, List<TextEditorTextSpan>? textSpanList)
    {
        ModelModifier = modelModifier;
        TextSpanList = textSpanList;
    }
    
    public TextEditorModel? ModelModifier { get; }
    public List<TextEditorTextSpan>? TextSpanList { get; }

    public void AddTextSpan(TextEditorTextSpan textSpan)
    {
        TextSpanList?.Add(textSpan);
        ModelModifier?.__SetDecorationByteRange(
            textSpan.StartInclusiveIndex,
            textSpan.EndExclusiveIndex,
            textSpan.DecorationByte);
    }
}
