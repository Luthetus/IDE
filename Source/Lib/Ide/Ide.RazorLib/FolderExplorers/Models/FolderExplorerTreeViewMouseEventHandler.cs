using Clair.Common.RazorLib.Commands.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models;
using Clair.Ide.RazorLib.FileSystems.Models;

namespace Clair.Ide.RazorLib.FolderExplorers.Models;

public class FolderExplorerTreeViewMouseEventHandler : TreeViewMouseEventHandler
{
    private readonly IdeService _ideService;

    public FolderExplorerTreeViewMouseEventHandler(
            IdeService ideService)
        : base(ideService.CommonService)
    {
        _ideService = ideService;
    }

    public override async Task OnDoubleClickAsync(TreeViewCommandArgs commandArgs)
    {
        await base.OnDoubleClickAsync(commandArgs).ConfigureAwait(false);

        if (commandArgs.NodeThatReceivedMouseEvent is not TreeViewAbsolutePath treeViewAbsolutePath)
            return;

        _ideService.TextEditorService.WorkerArbitrary.PostUnique(async editContext =>
        {
            await _ideService.TextEditorService.OpenInEditorAsync(
                editContext,
                treeViewAbsolutePath.Item.Value,
                true,
                cursorPositionIndex: null,
                new Category("main"),
                editContext.TextEditorService.NewViewModelKey());
        });
    }
}
