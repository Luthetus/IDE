using Clair.Common.RazorLib.FileSystems.Models;

namespace Clair.CompilerServices.DotNetSolution.Models.Project;

public class CSharpProjectNugetPackageReferences
{
    public CSharpProjectNugetPackageReferences(AbsolutePath cSharpProjectAbsolutePath)
    {
        CSharpProjectAbsolutePath = cSharpProjectAbsolutePath;
    }

    public AbsolutePath CSharpProjectAbsolutePath { get; }
}
