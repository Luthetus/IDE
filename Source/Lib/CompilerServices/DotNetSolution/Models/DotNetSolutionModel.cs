using Clair.Common.RazorLib.FileSystems.Models;
using Clair.Common.RazorLib.Keys.Models;
using Clair.CompilerServices.DotNetSolution.Models.Project;

namespace Clair.CompilerServices.DotNetSolution.Models;

public record DotNetSolutionModel : IDotNetSolution
{
    public DotNetSolutionModel(
        AbsolutePath absolutePath,
        List<IDotNetProject> dotNetProjectList,
        List<SolutionFolder> solutionFolderList,
        List<GuidNestedProjectEntry>? guidNestedProjectEntryList,
        List<StringNestedProjectEntry>? stringNestedProjectEntryList)
    {
        AbsolutePath = absolutePath;
        DotNetProjectList = dotNetProjectList;
        SolutionFolderList = solutionFolderList;
        GuidNestedProjectEntryList = guidNestedProjectEntryList;
        StringNestedProjectEntryList = stringNestedProjectEntryList;
    }

    public Key<DotNetSolutionModel> Key { get; init; }
    public AbsolutePath AbsolutePath { get; init; }
    /// <summary>
    /// This "long term" must not contain the solution folders.
    /// TODO: It may or may not contain the solution folders temporarily
    /// when initially parsing the .sln I'm not sure and I need to find time to verify.
    /// </summary>
    public List<IDotNetProject> DotNetProjectList { get; set; }
    public List<SolutionFolder> SolutionFolderList { get; init; }
    public List<GuidNestedProjectEntry> GuidNestedProjectEntryList { get; init; }
    public List<StringNestedProjectEntry> StringNestedProjectEntryList { get; init; }

    public List<string> ProjectReferencesList { get; set; } = new();

    public string NamespaceString => string.Empty;
}
