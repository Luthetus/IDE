using Clair.Common.RazorLib.FileSystems.Models;

namespace Clair.CompilerServices.DotNetSolution.Models.Project;

public class CSharpProjectToProjectReferences
{
    public CSharpProjectToProjectReferences(AbsolutePath cSharpProjectAbsolutePath)
    {
        CSharpProjectAbsolutePath = cSharpProjectAbsolutePath;
    }

    public AbsolutePath CSharpProjectAbsolutePath { get; }
}
