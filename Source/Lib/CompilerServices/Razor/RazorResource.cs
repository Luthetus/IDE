using Clair.TextEditor.RazorLib.CompilerServices;
using Clair.TextEditor.RazorLib.Lexers.Models;

namespace Clair.CompilerServices.Razor;

public sealed class RazorResource : CompilerServiceResource
{
    public RazorResource(ResourceUri resourceUri, RazorCompilerService razorCompilerService)
        : base(resourceUri, razorCompilerService)
    {
    }
}
