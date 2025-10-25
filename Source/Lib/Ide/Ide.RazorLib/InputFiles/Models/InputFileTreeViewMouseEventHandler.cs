/*
using Clair.Common.RazorLib.Commands.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.Ide.RazorLib.FileSystems.Models;

namespace Clair.Ide.RazorLib.InputFiles.Models;

public class InputFileTreeViewMouseEventHandler : TreeViewMouseEventHandler
{
    private readonly IdeService _ideService;

    public InputFileTreeViewMouseEventHandler(IdeService ideService)
        : base(ideService.CommonService)
    {
        _ideService = ideService;
    }

    protected override void OnClick(TreeViewCommandArgs commandArgs)
    {
        base.OnClick(commandArgs);

        if (commandArgs.NodeThatReceivedMouseEvent is not TreeViewAbsolutePath treeViewAbsolutePath)
            return;

        _ideService.InputFile_SetSelectedTreeViewModel(treeViewAbsolutePath);
    }

    public override Task OnDoubleClickAsync(TreeViewCommandArgs commandArgs)
    {
        base.OnDoubleClickAsync(commandArgs);

        if (commandArgs.NodeThatReceivedMouseEvent is not TreeViewAbsolutePath treeViewAbsolutePath)
            return Task.CompletedTask;

        return Task.CompletedTask;
    }
}
*/
