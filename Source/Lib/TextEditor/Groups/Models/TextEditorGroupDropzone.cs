using Clair.Common.RazorLib.Dimensions.Models;
using Clair.Common.RazorLib.Dynamics.Models;
using Clair.Common.RazorLib.JavaScriptObjects.Models;
using Clair.Common.RazorLib.Keys.Models;

namespace Clair.TextEditor.RazorLib.Groups.Models;

public class TextEditorGroupDropzone : IDropzone
{
    public TextEditorGroupDropzone(
        MeasuredHtmlElementDimensions measuredHtmlElementDimensions,
        Key<TextEditorGroup> textEditorGroupKey,
        ElementDimensions elementDimensions)
    {
        MeasuredHtmlElementDimensions = measuredHtmlElementDimensions;
        TextEditorGroupKey = textEditorGroupKey;
        ElementDimensions = elementDimensions;
    }

    public MeasuredHtmlElementDimensions MeasuredHtmlElementDimensions { get; }
    public Key<TextEditorGroup> TextEditorGroupKey { get; }
    public Key<IDropzone> DropzoneKey { get; }
    public ElementDimensions ElementDimensions { get; }
    public string CssClass { get; init; }
    public string CssStyle { get; }
}

