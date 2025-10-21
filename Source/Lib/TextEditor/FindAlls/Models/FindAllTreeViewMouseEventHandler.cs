using Clair.Common.RazorLib.Commands.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models;

namespace Clair.TextEditor.RazorLib.FindAlls.Models;

public class FindAllTreeViewMouseEventHandler : TreeViewMouseEventHandler
{
    private readonly TextEditorService _textEditorService;

    public FindAllTreeViewMouseEventHandler(TextEditorService textEditorService)
        : base(textEditorService.CommonService)
    {
        _textEditorService = textEditorService;
    }

    public override Task OnDoubleClickAsync(TreeViewCommandArgs commandArgs)
    {
        base.OnDoubleClickAsync(commandArgs);

        if (commandArgs.NodeThatReceivedMouseEvent is not TreeViewFindAllTextSpan treeViewFindAllTextSpan)
            return Task.CompletedTask;

        _textEditorService.WorkerArbitrary.PostUnique(async editContext =>
        {
            await _textEditorService.OpenInEditorAsync(
                editContext,
                treeViewFindAllTextSpan.AbsolutePath.Value,
                true,
                treeViewFindAllTextSpan.Item.TextSpan.StartInclusiveIndex,
                new Category("main"),
                editContext.TextEditorService.NewViewModelKey());
        });
        return Task.CompletedTask;
    }
}
