using Clair.Common.RazorLib.Commands.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models;
using Clair.Ide.RazorLib;
using Clair.Extensions.DotNet.Namespaces.Models;

namespace Clair.Extensions.DotNet.DotNetSolutions.Models;

public class SolutionExplorerTreeViewMouseEventHandler : TreeViewMouseEventHandler
{
    private readonly IdeService _ideService;

    public SolutionExplorerTreeViewMouseEventHandler(
            IdeService ideService)
        : base(ideService.CommonService)
    {
        _ideService = ideService;
    }

    public override Task OnDoubleClickAsync(TreeViewCommandArgs commandArgs)
    {
        base.OnDoubleClickAsync(commandArgs);

        if (commandArgs.NodeThatReceivedMouseEvent is not TreeViewNamespacePath treeViewNamespacePath)
            return Task.CompletedTask;
        
        _ideService.TextEditorService.WorkerArbitrary.PostUnique(async editContext =>
        {
            await _ideService.TextEditorService.OpenInEditorAsync(
                editContext,
                treeViewNamespacePath.Item.Value,
                true,
                null,
                new Category("main"),
                editContext.TextEditorService.NewViewModelKey());
        });
        return Task.CompletedTask;
    }
}
