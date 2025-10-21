using Clair.TextEditor.RazorLib.CompilerServices;
using Clair.TextEditor.RazorLib.Lexers.Models;

namespace Clair.CompilerServices.CSharpProject.CompilerServiceCase;

public class CSharpProjectResource : CompilerServiceResource
{
    public CSharpProjectResource(ResourceUri resourceUri, CSharpProjectCompilerService cSharpProjectCompilerService)
        : base(resourceUri, cSharpProjectCompilerService)
    {
    }
}
