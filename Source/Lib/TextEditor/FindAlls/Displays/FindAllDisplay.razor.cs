using Microsoft.AspNetCore.Components;
using Clair.Common.RazorLib.Commands.Models;
using Clair.Common.RazorLib.Dropdowns.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.TextEditor.RazorLib.FindAlls.Models;

namespace Clair.TextEditor.RazorLib.FindAlls.Displays;

public sealed partial class FindAllDisplay : ComponentBase, IDisposable
{
    [Inject]
    private TextEditorService TextEditorService { get; set; } = null!;
    
    private TreeViewContainerParameter _treeViewContainerParameter;

    private string SearchQuery
    {
        get => TextEditorService.GetFindAllState().SearchQuery;
        set
        {
            if (value is not null)
                TextEditorService.SetSearchQuery(value);
        }
    }

    private string StartingDirectoryPath
    {
        get => TextEditorService.GetFindAllState().StartingDirectoryPath;
        set
        {
            if (value is not null)
                TextEditorService.SetStartingDirectoryPath(value);
        }
    }
    
    protected override void OnInitialized()
    {
        TextEditorService.SecondaryChanged += OnFindAllStateChanged;
        
        _treeViewContainerParameter = new(
            TextEditorService.TextEditorFindAllState.TreeViewFindAllContainerKey,
            new FindAllTreeViewKeyboardEventHandler(TextEditorService),
            new FindAllTreeViewMouseEventHandler(TextEditorService),
            OnTreeViewContextMenuFunc);
    }
    
    private Task OnTreeViewContextMenuFunc(TreeViewCommandArgs treeViewCommandArgs)
    {
        var dropdownRecord = new DropdownRecord(
            FindAllContextMenu.ContextMenuEventDropdownKey,
            treeViewCommandArgs.ContextMenuFixedPosition.LeftPositionInPixels,
            treeViewCommandArgs.ContextMenuFixedPosition.TopPositionInPixels,
            typeof(FindAllContextMenu),
            new Dictionary<string, object?>
            {
                {
                    nameof(FindAllContextMenu.TreeViewCommandArgs),
                    treeViewCommandArgs
                }
            },
            null);

        TextEditorService.CommonService.Dropdown_ReduceRegisterAction(dropdownRecord);
        return Task.CompletedTask;
    }

    private void DoSearchOnClick()
    {
        TextEditorService.HandleStartSearchAction();
    }

    private void CancelSearchOnClick()
    {
        TextEditorService.CancelSearch();
    }
    
    public async void OnFindAllStateChanged(SecondaryChangedKind secondaryChangedKind)
    {
        if (secondaryChangedKind == SecondaryChangedKind.FindAllStateChanged)
        {
            await InvokeAsync(StateHasChanged);
        }
    }
    
    public void Dispose()
    {
        TextEditorService.SecondaryChanged -= OnFindAllStateChanged;
    }
}
