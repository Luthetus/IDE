/*
using Clair.Common.RazorLib;
using Clair.Common.RazorLib.Commands.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.Ide.RazorLib.FileSystems.Models;

namespace Clair.Ide.RazorLib.InputFiles.Models;

public class InputFileTreeViewKeyboardEventHandler : TreeViewKeyboardEventHandler
{
    private readonly IdeService _ideService;

    public InputFileTreeViewKeyboardEventHandler(IdeService ideService)
        : base(ideService.CommonService)
    {
        _ideService = ideService;
    }

    public override Task OnKeyDownAsync(TreeViewCommandArgs commandArgs)
    {
        base.OnKeyDownAsync(commandArgs);

        if (commandArgs.KeyboardEventArgs is null)
            return Task.CompletedTask;

        switch (commandArgs.KeyboardEventArgs.Code)
        {
            case CommonFacts.ENTER_CODE:
                SetSelectedTreeViewModel(commandArgs);
                return Task.CompletedTask;
            case CommonFacts.SPACE_CODE:
                SetSelectedTreeViewModel(commandArgs);
                return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }

    private void SetSelectedTreeViewModel(TreeViewCommandArgs commandArgs)
    {
        var activeNode = commandArgs.TreeViewContainer.ActiveNode;
        var treeViewAbsolutePath = activeNode as TreeViewAbsolutePath;

        if (treeViewAbsolutePath is null)
            return;

        _ideService.InputFile_SetSelectedTreeViewModel(treeViewAbsolutePath);
        return;
    }
}
*/
