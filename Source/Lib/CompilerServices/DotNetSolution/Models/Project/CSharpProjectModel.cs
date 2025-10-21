using Clair.Common.RazorLib.FileSystems.Models;

namespace Clair.CompilerServices.DotNetSolution.Models.Project;

public class CSharpProjectModel : IDotNetProject
{
    public CSharpProjectModel(
        string displayName,
        Guid projectTypeGuid,
        string relativePathFromSolutionFileString,
        Guid projectIdGuid,
        AbsolutePath absolutePath)
    {
        DisplayName = displayName;
        ProjectTypeGuid = projectTypeGuid;
        RelativePathFromSolutionFileString = relativePathFromSolutionFileString;
        ProjectIdGuid = projectIdGuid;
        AbsolutePath = absolutePath;
    }

    public string DisplayName { get; }
    public Guid ProjectTypeGuid { get; }
    public string RelativePathFromSolutionFileString { get; }
    public Guid ProjectIdGuid { get; }
    public AbsolutePath AbsolutePath { get; set; }
    public DotNetProjectKind DotNetProjectKind => DotNetProjectKind.CSharpProject;
    public int ProjectReferencesOffset { get; set; }
    public int ProjectReferencesLength { get; set; }
    
    public SolutionMemberKind SolutionMemberKind => SolutionMemberKind.Project;
}
