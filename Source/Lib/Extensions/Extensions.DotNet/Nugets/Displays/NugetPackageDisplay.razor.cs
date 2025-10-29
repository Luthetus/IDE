using Microsoft.AspNetCore.Components;
using Clair.Common.RazorLib;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Ide.RazorLib.Terminals.Models;
using Clair.Extensions.DotNet.DotNetSolutions.Models;
using Clair.Extensions.DotNet.Nugets.Models;
using Clair.Extensions.DotNet.CommandLines.Models;

namespace Clair.Extensions.DotNet.Nugets.Displays;

public sealed partial class NugetPackageDisplay : ComponentBase, IDisposable
{
    [Inject]
    private DotNetService DotNetService { get; set; } = null!;

    [Parameter, EditorRequired]
    public NugetPackageRecord NugetPackageRecord { get; set; } = null!;

    private static readonly Key<TerminalCommandRequest> AddNugetPackageTerminalCommandRequestKey = Key<TerminalCommandRequest>.NewKey();

    private string _nugetPackageVersionString = string.Empty;

    private List<NugetPackageVersionRecord> _nugetPackageVersionsOrdered = new();
    private string? _previousNugetPackageId;

    protected override void OnInitialized()
    {
        DotNetService.DotNetStateChanged += OnNuGetPackageManagerStateChanged;
    }
    
    protected override void OnParametersSet()
    {
        if (_previousNugetPackageId is null || _previousNugetPackageId != NugetPackageRecord.Id)
        {
            _previousNugetPackageId = NugetPackageRecord.Id;

            _nugetPackageVersionsOrdered = NugetPackageRecord.Versions
                .OrderByDescending(x => x.Version)
                .ToList();

            // Also, non-hot paths don't matter much either. Like I said I wanna see where things go regardless.
            _nugetPackageVersionString = _nugetPackageVersionsOrdered.FirstOrDefault()
                ?.Version ?? string.Empty;
        }

        base.OnParametersSet();
    }

    private void SelectedNugetVersionChanged(ChangeEventArgs changeEventArgs)
    {
        _nugetPackageVersionString = changeEventArgs.Value?.ToString() ?? string.Empty;
    }

    private bool ValidateSolutionContainsSelectedProject(
        DotNetSolutionState dotNetSolutionState,
        NuGetPackageManagerState nuGetPackageManagerState)
    {
        if (dotNetSolutionState.DotNetSolutionModel is null || nuGetPackageManagerState.SelectedProjectToModify is null)
            return false;

        foreach (var x in dotNetSolutionState.DotNetSolutionModel.DotNetProjectList)
        {
            if (x.ProjectIdGuid == nuGetPackageManagerState.SelectedProjectToModify.ProjectIdGuid)
                return true;
        }
        return false;
    }

    private void AddNugetPackageReferenceOnClick(
        DotNetSolutionState dotNetSolutionState,
        NuGetPackageManagerState nuGetPackageManagerState)
    {
        var targetProject = nuGetPackageManagerState.SelectedProjectToModify;
        var targetNugetPackage = NugetPackageRecord;
        var targetNugetVersion = _nugetPackageVersionString;

        var isValid = ValidateSolutionContainsSelectedProject(dotNetSolutionState, nuGetPackageManagerState);

        if (!isValid || targetProject is null)
        {
            return;
        }

        var parentDirectory = targetProject.AbsolutePath.CreateSubstringParentDirectory();
        if (parentDirectory is null)
            return;

        var formattedCommandValue = DotNetCliCommandFormatter.FormatAddNugetPackageReferenceToProject(
            targetProject.AbsolutePath.Value,
            targetNugetPackage.Id,
            targetNugetVersion);

        var terminalCommandRequest = new TerminalCommandRequest(
            formattedCommandValue,
            parentDirectory,
            AddNugetPackageTerminalCommandRequestKey)
        {
            ContinueWithFunc = parsedCommand =>
            {
                CommonFacts.DispatchInformative("Add Nuget Package Reference", $"{targetNugetPackage.Title}, {targetNugetVersion} was added to {targetProject.DisplayName}", DotNetService.IdeService.CommonService, TimeSpan.FromSeconds(7));
                return Task.CompletedTask;
            }
        };
            
        DotNetService.IdeService.GetTerminalState().GeneralTerminal.EnqueueCommand(terminalCommandRequest);
    }
    
    private async void OnNuGetPackageManagerStateChanged(DotNetStateChangedKind dotNetStateChangedKind)
    {
        if (dotNetStateChangedKind == DotNetStateChangedKind.SolutionStateChanged ||
            dotNetStateChangedKind == DotNetStateChangedKind.NuGetPackageManagerStateChanged)
        {
            await InvokeAsync(StateHasChanged);
        }
    }
    
    public void Dispose()
    {
        DotNetService.DotNetStateChanged -= OnNuGetPackageManagerStateChanged;
    }
}
