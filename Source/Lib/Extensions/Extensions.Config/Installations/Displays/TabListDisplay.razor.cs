using Microsoft.AspNetCore.Components;
using Clair.Common.RazorLib.Dropdowns.Models;
using Clair.Common.RazorLib.Dynamics.Models;
using Clair.Common.RazorLib.Tabs.Models;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.Tabs.Displays;
using Clair.TextEditor.RazorLib;
using Clair.Extensions.DotNet;

namespace Clair.Extensions.Config.Installations.Displays;

public partial class TabListDisplay : ComponentBase, IDisposable
{
    [Inject]
    private DotNetService DotNetService { get; set; } = null!;
    
    private TabCascadingValueBatch _tabCascadingValueBatch = new();
    
    protected override void OnInitialized()
    {
        DotNetService.TextEditorService.SecondaryChanged += TextEditorOptionsStateWrap_StateChanged;
    }

    private Task HandleTabButtonOnContextMenu(TabContextMenuEventArgs tabContextMenuEventArgs)
    {
        var dropdownRecord = new DropdownRecord(
            TabContextMenu.ContextMenuEventDropdownKey,
            tabContextMenuEventArgs.MouseEventArgs.ClientX,
            tabContextMenuEventArgs.MouseEventArgs.ClientY,
            typeof(TabContextMenu),
            new Dictionary<string, object?>
            {
                {
                    nameof(TabContextMenu.TabContextMenuEventArgs),
                    tabContextMenuEventArgs
                }
            },
            restoreFocusOnClose: null);

        DotNetService.CommonService.Dropdown_ReduceRegisterAction(dropdownRecord);
        return Task.CompletedTask;
    }
    
    #region TabDisplay
    private bool _thinksLeftMouseButtonIsDown;

    private Key<IDynamicViewModel> _dynamicViewModelKeyPrevious;

    private ElementReference? _tabButtonElementReference;
    
    private string GetIsActiveCssClass(ITab localTabViewModel) => (localTabViewModel.TabGroup?.GetIsActive(localTabViewModel) ?? false)
        ? "ci_active"
        : string.Empty;
    
    private void HandleOnMouseUp()
    {
        _thinksLeftMouseButtonIsDown = false;
    }
    
    public void SubscribeToDragEventForScrolling(IDrag draggable)
    {
        DotNetService.CommonService.Drag_ShouldDisplayAndMouseEventArgsAndDragSetAction(true, null, draggable);
    }

    /// <summary>
    /// This method can only be invoked from the "UI thread" due to the shared `UiStringBuilder` usage.
    /// </summary>
    private string GetCssClass(ITab localTabViewModel)
    {
        var uiStringBuilder = DotNetService.CommonService.UiStringBuilder;
        
        uiStringBuilder.Clear();
        uiStringBuilder.Append("ci_dynamic-tab ci_button ci_unselectable ");
        uiStringBuilder.Append(GetIsActiveCssClass(localTabViewModel));
        uiStringBuilder.Append(" ");
        uiStringBuilder.Append(localTabViewModel.TabGroup?.GetDynamicCss(localTabViewModel));
    
        return uiStringBuilder.ToString();
    }
    #endregion
    
    private async void TextEditorOptionsStateWrap_StateChanged(SecondaryChangedKind secondaryChangedKind)
    {
        if (secondaryChangedKind == SecondaryChangedKind.DirtyResourceUriStateChanged)
        {
            await InvokeAsync(StateHasChanged);
        }
        else if (secondaryChangedKind == SecondaryChangedKind.Group_TextEditorGroupStateChanged)
        {
            await InvokeAsync(StateHasChanged);
        }
    }
    
    public void Dispose()
    {
        DotNetService.TextEditorService.SecondaryChanged -= TextEditorOptionsStateWrap_StateChanged;
    }
}
