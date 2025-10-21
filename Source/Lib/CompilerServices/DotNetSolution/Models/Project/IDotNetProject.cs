using Clair.Common.RazorLib.FileSystems.Models;

namespace Clair.CompilerServices.DotNetSolution.Models.Project;

public interface IDotNetProject : ISolutionMember
{
    public string DisplayName { get; }
    public Guid ProjectTypeGuid { get; }
    public string RelativePathFromSolutionFileString { get; }
    public Guid ProjectIdGuid { get; }
    /// <summary>
    /// TODO: Remove the "set;" hack.
    /// </summary>
    public AbsolutePath AbsolutePath { get; set; }
    public DotNetProjectKind DotNetProjectKind { get; }
    public int ProjectReferencesOffset { get; set; }
    public int ProjectReferencesLength { get; set; }
}
