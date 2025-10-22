using Microsoft.AspNetCore.Components;
using Clair.Common.RazorLib.Dropdowns.Models;
using Clair.Common.RazorLib.Commands.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.Ide.RazorLib.FolderExplorers.Models;

namespace Clair.Ide.RazorLib.FolderExplorers.Displays;

public sealed partial class FolderExplorerDisplay : ComponentBase, IDisposable
{
    [Inject]
    private IdeService IdeService { get; set; } = null!;

    private TreeViewContainerParameter _treeViewContainerParameter;

    protected override void OnInitialized()
    {
        IdeService.IdeStateChanged += OnFolderExplorerStateChanged;
    }

    private Task OnTreeViewContextMenuFunc(TreeViewCommandArgs treeViewCommandArgs)
    {
        var dropdownRecord = new DropdownRecord(
            FolderExplorerContextMenu.ContextMenuEventDropdownKey,
            treeViewCommandArgs.ContextMenuFixedPosition.LeftPositionInPixels,
            treeViewCommandArgs.ContextMenuFixedPosition.TopPositionInPixels,
            typeof(FolderExplorerContextMenu),
            new Dictionary<string, object?>
            {
                {
                    nameof(FolderExplorerContextMenu.TreeViewCommandArgs),
                    treeViewCommandArgs
                }
            },
            restoreFocusOnClose: null);

        IdeService.CommonService.Dropdown_ReduceRegisterAction(dropdownRecord);
        return Task.CompletedTask;
    }
    
    private async void OnFolderExplorerStateChanged(IdeStateChangedKind ideStateChangedKind)
    {
        if (ideStateChangedKind == IdeStateChangedKind.FolderExplorerStateChanged)
        {
            await InvokeAsync(StateHasChanged);
        }
    }
    
    public void Dispose()
    {
        IdeService.IdeStateChanged -= OnFolderExplorerStateChanged;
    }
}
