using Microsoft.AspNetCore.Components;
using Clair.Common.RazorLib.Commands.Models;
using Clair.Common.RazorLib.Dropdowns.Models;
using Clair.Common.RazorLib.Dimensions.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.TextEditor.RazorLib;
using Clair.Extensions.DotNet.TestExplorers.Models;

namespace Clair.Extensions.DotNet.TestExplorers.Displays.Internals;

public partial class TestExplorerTreeViewDisplay : ComponentBase
{
    [Inject]
    private TextEditorService TextEditorService { get; set; } = null!;

    [CascadingParameter]
    public TestExplorerRenderBatchValidated RenderBatch { get; set; } = null!;

    [Parameter, EditorRequired]
    public ElementDimensions ElementDimensions { get; set; } = null!;

    private TreeViewContainerParameter _treeViewContainerParameter;

    protected override void OnInitialized()
    {
        _treeViewContainerParameter = new(
            TestExplorerState.TreeViewTestExplorerKey,
            new TestExplorerTreeViewKeyboardEventHandler(TextEditorService),
            new TestExplorerTreeViewMouseEventHandler(TextEditorService),
            OnTreeViewContextMenuFunc);
    }

    private Task OnTreeViewContextMenuFunc(TreeViewCommandArgs treeViewCommandArgs)
    {
        var dropdownRecord = new DropdownRecord(
            TestExplorerContextMenu.ContextMenuEventDropdownKey,
            treeViewCommandArgs.ContextMenuFixedPosition.LeftPositionInPixels,
            treeViewCommandArgs.ContextMenuFixedPosition.TopPositionInPixels,
            typeof(TestExplorerContextMenu),
            new Dictionary<string, object?>
            {
                {
                    nameof(TestExplorerContextMenu.TreeViewCommandArgs),
                    treeViewCommandArgs
                }
            },
            restoreFocusOnClose: null);

        TextEditorService.CommonService.Dropdown_ReduceRegisterAction(dropdownRecord);
        return Task.CompletedTask;
    }
}
