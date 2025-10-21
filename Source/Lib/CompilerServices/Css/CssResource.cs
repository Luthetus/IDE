using Clair.TextEditor.RazorLib.CompilerServices;
using Clair.TextEditor.RazorLib.Lexers.Models;

namespace Clair.CompilerServices.Css;

public class CssResource : CompilerServiceResource
{
    public CssResource(ResourceUri resourceUri, CssCompilerService textEditorCssCompilerService)
        : base(resourceUri, textEditorCssCompilerService)
    {
    }
}
