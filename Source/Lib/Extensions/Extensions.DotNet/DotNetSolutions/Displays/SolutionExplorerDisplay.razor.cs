using Microsoft.AspNetCore.Components;
using Clair.Common.RazorLib.Commands.Models;
using Clair.Common.RazorLib.Dialogs.Models;
using Clair.Common.RazorLib.Dropdowns.Models;
using Clair.Common.RazorLib.Dynamics.Models;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.Extensions.DotNet.DotNetSolutions.Displays.Internals;
using Clair.Extensions.DotNet.DotNetSolutions.Models;
using Clair.CompilerServices.DotNetSolution.Models;

namespace Clair.Extensions.DotNet.DotNetSolutions.Displays;

public sealed partial class SolutionExplorerDisplay : ComponentBase, IDisposable
{
    [Inject]
    private DotNetService DotNetService { get; set; } = null!;

    protected override void OnInitialized()
    {
        DotNetService.DotNetStateChanged += OnDotNetSolutionStateChanged;
    
        /*_treeViewContainerParameter = new(
            DotNetSolutionState.TreeViewSolutionExplorerStateKey,
            new SolutionExplorerTreeViewKeyboardEventHandler(DotNetService.IdeService),
            new SolutionExplorerTreeViewMouseEventHandler(DotNetService.IdeService),
            OnTreeViewContextMenuFunc);*/
    }
    
    protected override async Task OnInitializedAsync()
    {
        if (!DotNetService.CommonService.TryGetTreeViewContainer(DotNetSolutionState.TreeViewSolutionExplorerStateKey, out _))
            await DotNetService.Do_SetDotNetSolutionTreeView(Key<DotNetSolutionModel>.Empty/*this arg isn't used*/);
    }

    private void OpenNewDotNetSolutionDialog()
    {
        var dialogRecord = new DialogViewModel(
            Key<IDynamicViewModel>.NewKey(),
            "New .NET Solution",
            typeof(DotNetSolutionFormDisplay),
            null,
            null,
            true,
            null);

        DotNetService.IdeService.TextEditorService.CommonService.Dialog_ReduceRegisterAction(dialogRecord);
    }
    
    public async void OnDotNetSolutionStateChanged(DotNetStateChangedKind dotNetStateChangedKind)
    {
        if (dotNetStateChangedKind == DotNetStateChangedKind.SolutionStateChanged)
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        DotNetService.DotNetStateChanged -= OnDotNetSolutionStateChanged;
        DotNetService.CommonService.TreeView_DisposeContainerAction(DotNetSolutionState.TreeViewSolutionExplorerStateKey);
    }
}