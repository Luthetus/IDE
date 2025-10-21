using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.TextEditor.RazorLib.CompilerServices;

namespace Clair.CompilerServices.DotNetSolution;

public class DotNetSolutionResource : CompilerServiceResource
{
    public DotNetSolutionResource(ResourceUri resourceUri, DotNetSolutionCompilerService dotNetSolutionCompilerService)
        : base(resourceUri, dotNetSolutionCompilerService)
    {
    }
}
