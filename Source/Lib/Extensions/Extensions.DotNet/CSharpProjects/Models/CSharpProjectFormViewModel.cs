using Clair.Common.RazorLib;
using Clair.Common.RazorLib.FileSystems.Models;
using Clair.Common.RazorLib.Keys.Models;
using Clair.CompilerServices.DotNetSolution.Models;
using Clair.Ide.RazorLib.Terminals.Models;
using Clair.Extensions.DotNet.CommandLines.Models;

namespace Clair.Extensions.DotNet.CSharpProjects.Models;

public class CSharpProjectFormViewModel
{
    public readonly Key<TerminalCommandRequest> NewCSharpProjectTerminalCommandRequestKey = Key<TerminalCommandRequest>.NewKey();
    public readonly Key<TerminalCommandRequest> AddCSharpProjectToSolutionTerminalCommandRequestKey = Key<TerminalCommandRequest>.NewKey();
    public readonly Key<TerminalCommandRequest> LoadProjectTemplatesTerminalCommandRequestKey = Key<TerminalCommandRequest>.NewKey();
    public readonly CancellationTokenSource NewCSharpProjectCancellationTokenSource = new();

    public CSharpProjectFormViewModel(
        DotNetSolutionModel? dotNetSolutionModel,
        IFileSystemProvider fileSystemProvider)
    {
        DotNetSolutionModel = dotNetSolutionModel;
        FileSystemProvider = fileSystemProvider;
    }

    public DotNetSolutionModel? DotNetSolutionModel { get; set; }
    public IFileSystemProvider FileSystemProvider { get; }
    public bool IsReadingProjectTemplates { get; set; } = false;
    public string ProjectTemplateShortNameValue { get; set; } = string.Empty;
    public string CSharpProjectNameValue { get; set; } = string.Empty;
    public string OptionalParametersValue { get; set; } = string.Empty;
    public string ParentDirectoryNameValue { get; set; } = string.Empty;
    public List<ProjectTemplate> ProjectTemplateList { get; set; } = new List<ProjectTemplate>();
    public CSharpProjectFormPanelKind ActivePanelKind { get; set; } = CSharpProjectFormPanelKind.Graphical;
    public string SearchInput { get; set; } = string.Empty;
    public ProjectTemplate? SelectedProjectTemplate { get; set; } = null;

    public bool IsValid => DotNetSolutionModel is not null;

    public string ProjectTemplateShortNameDisplay => string.IsNullOrWhiteSpace(ProjectTemplateShortNameValue)
        ? "{enter Template name}"
        : ProjectTemplateShortNameValue;

    public string CSharpProjectNameDisplay => string.IsNullOrWhiteSpace(CSharpProjectNameValue)
        ? "{enter C# Project name}"
        : CSharpProjectNameValue;

    public string OptionalParametersDisplay => OptionalParametersValue;

    public string ParentDirectoryNameDisplay => string.IsNullOrWhiteSpace(ParentDirectoryNameValue)
        ? "{enter parent directory name}"
        : ParentDirectoryNameValue;

    public string FormattedNewCSharpProjectCommandValue => DotNetCliCommandFormatter.FormatDotnetNewCSharpProject(
        ProjectTemplateShortNameValue,
        CSharpProjectNameValue,
        OptionalParametersValue);

    public string FormattedAddExistingProjectToSolutionCommandValue => DotNetCliCommandFormatter.FormatAddExistingProjectToSolution(
        DotNetSolutionModel?.NamespaceString is null
            ? string.Empty
            : DotNetSolutionModel?.AbsolutePath.Value,
        $"{CSharpProjectNameValue}{FileSystemProvider.DirectorySeparatorChar}{CSharpProjectNameValue}.{CommonFacts.C_SHARP_PROJECT}");

    public bool TryTakeSnapshot(out CSharpProjectFormViewModelImmutable? viewModelImmutable)
    {
        var localDotNetSolutionModel = DotNetSolutionModel;

        if (localDotNetSolutionModel is null)
        {
            viewModelImmutable = null;
            return false;
        }

        viewModelImmutable = new CSharpProjectFormViewModelImmutable(
            localDotNetSolutionModel,
            FileSystemProvider,
            IsReadingProjectTemplates,
            ProjectTemplateShortNameValue,
            CSharpProjectNameValue,
            OptionalParametersValue,
            ParentDirectoryNameValue,
            ProjectTemplateList,
            ActivePanelKind,
            SearchInput,
            SelectedProjectTemplate,
            IsValid,
            ProjectTemplateShortNameDisplay,
            CSharpProjectNameDisplay,
            OptionalParametersDisplay,
            ParentDirectoryNameDisplay,
            FormattedNewCSharpProjectCommandValue,
            FormattedAddExistingProjectToSolutionCommandValue,
            NewCSharpProjectTerminalCommandRequestKey,
            AddCSharpProjectToSolutionTerminalCommandRequestKey,
            LoadProjectTemplatesTerminalCommandRequestKey,
            NewCSharpProjectCancellationTokenSource);

        return true;
    }
}
