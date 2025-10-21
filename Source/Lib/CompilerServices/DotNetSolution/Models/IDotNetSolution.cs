using Clair.Common.RazorLib.FileSystems.Models;
using Clair.Common.RazorLib.Keys.Models;
using Clair.CompilerServices.DotNetSolution.Models.Project;

namespace Clair.CompilerServices.DotNetSolution.Models;

public interface IDotNetSolution
{
    public Key<DotNetSolutionModel> Key { get; init; }
    public AbsolutePath AbsolutePath { get; init; }
    public List<IDotNetProject> DotNetProjectList { get; }
    public List<SolutionFolder> SolutionFolderList { get; init; }
    /// <summary>Use when the solution is '.sln'</summary>
    public List<GuidNestedProjectEntry>? GuidNestedProjectEntryList { get; init; }
    /// <summary>Use when the solution is '.slnx'</summary>
    public List<StringNestedProjectEntry>? StringNestedProjectEntryList { get; init; }

    public string NamespaceString => string.Empty;
}
