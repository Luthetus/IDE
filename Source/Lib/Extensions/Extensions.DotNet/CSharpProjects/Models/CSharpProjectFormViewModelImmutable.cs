using Clair.Common.RazorLib.FileSystems.Models;
using Clair.Common.RazorLib.Keys.Models;
using Clair.CompilerServices.DotNetSolution.Models;
using Clair.Ide.RazorLib.Terminals.Models;

namespace Clair.Extensions.DotNet.CSharpProjects.Models;

public record CSharpProjectFormViewModelImmutable(
    DotNetSolutionModel DotNetSolutionModel,
    IFileSystemProvider FileSystemProvider,
    bool IsReadingProjectTemplates,
    string ProjectTemplateShortNameValue,
    string CSharpProjectNameValue,
    string OptionalParametersValue,
    string ParentDirectoryNameValue,
    List<ProjectTemplate> ProjectTemplateList,
    CSharpProjectFormPanelKind ActivePanelKind,
    string SearchInput,
    ProjectTemplate? SelectedProjectTemplate,
    bool IsValid,
    string ProjectTemplateShortNameDisplay,
    string CSharpProjectNameDisplay,
    string OptionalParametersDisplay,
    string ParentDirectoryNameDisplay,
    string FormattedNewCSharpProjectCommandValue,
    string FormattedAddExistingProjectToSolutionCommandValue,
    Key<TerminalCommandRequest> NewCSharpProjectTerminalCommandRequestKey,
    Key<TerminalCommandRequest> AddCSharpProjectToSolutionTerminalCommandRequestKey,
    Key<TerminalCommandRequest> LoadProjectTemplatesTerminalCommandRequestKey,
    CancellationTokenSource NewCSharpProjectCancellationTokenSource);
