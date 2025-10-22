using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Clair.Common.RazorLib;
using Clair.Common.RazorLib.Commands.Models;
using Clair.Common.RazorLib.Dropdowns.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.Common.RazorLib.Dimensions.Models;
using Clair.Common.RazorLib.Resizes.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models.Internals;
using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.Ide.RazorLib.CodeSearches.Models;

namespace Clair.Ide.RazorLib.CodeSearches.Displays;

public sealed partial class CodeSearchDisplay : ComponentBase, IDisposable
{
    [Inject]
    private IdeService IdeService { get; set; } = null!;
    
    private readonly ViewModelDisplayOptions _textEditorViewModelDisplayOptions = new()
    {
        HeaderComponentType = null,
    };
    
    private TreeViewContainerParameter _treeViewContainerParameter;
    
    private Func<ElementDimensions, ElementDimensions, (MouseEventArgs firstMouseEventArgs, MouseEventArgs secondMouseEventArgs), Task>? _dragEventHandler;
    private MouseEventArgs? _previousDragMouseEventArgs;

    private string InputValue
    {
        get => IdeService.GetCodeSearchState().Query;
        set
        {
            if (value is null)
                value = string.Empty;

            IdeService.CodeSearch_With(inState => inState with
            {
                Query = value,
            });

            IdeService.CodeSearch_HandleSearchEffect();
        }
    }
    
    protected override void OnInitialized()
    {
        IdeService.IdeStateChanged += OnCodeSearchStateChanged;
        IdeService.CommonService.CommonUiStateChanged += OnCommonUiStateChanged;
    }
    
    protected override void OnAfterRender(bool firstRender)
    {
        IdeService.CodeSearch_updateContentThrottle.Run(_ => IdeService.CodeSearch_UpdateContent(ResourceUri.Empty));
    }
    
    private Task OnTreeViewContextMenuFunc(TreeViewCommandArgs treeViewCommandArgs)
    {
        var dropdownRecord = new DropdownRecord(
            CodeSearchContextMenu.ContextMenuEventDropdownKey,
            treeViewCommandArgs.ContextMenuFixedPosition.LeftPositionInPixels,
            treeViewCommandArgs.ContextMenuFixedPosition.TopPositionInPixels,
            typeof(CodeSearchContextMenu),
            new Dictionary<string, object?>
            {
                {
                    nameof(CodeSearchContextMenu.TreeViewCommandArgs),
                    treeViewCommandArgs
                }
            },
            null);

        IdeService.CommonService.Dropdown_ReduceRegisterAction(dropdownRecord);
        return Task.CompletedTask;
    }
    
    public async void OnCommonUiStateChanged(CommonUiEventKind commonUiEventKind)
    {
        if (commonUiEventKind == CommonUiEventKind.TreeViewStateChanged)
        {
            await InvokeAsync(StateHasChanged);
        }
        else if (commonUiEventKind == CommonUiEventKind.DragStateChanged)
        {
            if (_dragEventHandler is not null)
            {
                var codeSearchState = IdeService.GetCodeSearchState();
        
                await ResizableRow.Do(
                    IdeService.CommonService,
                    codeSearchState.TopContentElementDimensions,
                    codeSearchState.BottomContentElementDimensions,
                    _dragEventHandler,
                    _previousDragMouseEventArgs,
                    x => _dragEventHandler = x,
                    x => _previousDragMouseEventArgs = x);
                
                await InvokeAsync(StateHasChanged);
            }
        }
    }
    
    public async void OnCodeSearchStateChanged(IdeStateChangedKind ideStateChangedKind)
    {
        if (ideStateChangedKind == IdeStateChangedKind.CodeSearchStateChanged)
        {
            await InvokeAsync(StateHasChanged);
        }
    }
    
    public void SubscribeToDragEvent()
    {
        _dragEventHandler = ResizableRow.DragEventHandlerResizeHandleAsync;
        IdeService.CommonService.Drag_ShouldDisplayAndMouseEventArgsSetAction(true, null);
    }
    
    public void Dispose()
    {
        IdeService.IdeStateChanged -= OnCodeSearchStateChanged;
        IdeService.CommonService.CommonUiStateChanged -= OnCommonUiStateChanged;
    }
}
