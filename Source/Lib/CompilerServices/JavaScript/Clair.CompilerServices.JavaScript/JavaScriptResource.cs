using Clair.TextEditor.RazorLib.CompilerServices;
using Clair.TextEditor.RazorLib.Lexers.Models;

namespace Clair.CompilerServices.JavaScript;

public class JavaScriptResource : CompilerServiceResource
{
    public JavaScriptResource(ResourceUri resourceUri, JavaScriptCompilerService javaScriptCompilerService)
        : base(resourceUri, javaScriptCompilerService)
    {
    }
}
