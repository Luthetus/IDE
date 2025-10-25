/*
// 2025-10-22 (rewrite TreeViews)
using Clair.Common.RazorLib;
using Clair.Common.RazorLib.Commands.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models;

namespace Clair.TextEditor.RazorLib.FindAlls.Models;

public class FindAllTreeViewKeyboardEventHandler : TreeViewKeyboardEventHandler
{
    private readonly TextEditorService _textEditorService;

    public FindAllTreeViewKeyboardEventHandler(TextEditorService textEditorService)
        : base(textEditorService.CommonService)
    {
        _textEditorService = textEditorService;
    }

    public override Task OnKeyDownAsync(TreeViewCommandArgs commandArgs)
    {
        if (commandArgs.KeyboardEventArgs is null)
            return Task.CompletedTask;

        base.OnKeyDownAsync(commandArgs);

        switch (commandArgs.KeyboardEventArgs.Code)
        {
            case CommonFacts.ENTER_CODE:
                return InvokeOpenInEditor(commandArgs, true);
            case CommonFacts.SPACE_CODE:
                return InvokeOpenInEditor(commandArgs, false);
        }
        
        return Task.CompletedTask;
    }

    private Task InvokeOpenInEditor(TreeViewCommandArgs commandArgs, bool shouldSetFocusToEditor)
    {
        var activeNode = commandArgs.TreeViewContainer.ActiveNode;

        if (activeNode is not TreeViewFindAllTextSpan treeViewFindAllTextSpan)
            return Task.CompletedTask;

        _textEditorService.WorkerArbitrary.PostUnique(async editContext =>
        {
            await _textEditorService.OpenInEditorAsync(
                editContext,
                treeViewFindAllTextSpan.AbsolutePath.Value,
                shouldSetFocusToEditor,
                treeViewFindAllTextSpan.Item.TextSpan.StartInclusiveIndex,
                new Category("main"),
                editContext.TextEditorService.NewViewModelKey());
        });
        return Task.CompletedTask;
    }
}
*/
