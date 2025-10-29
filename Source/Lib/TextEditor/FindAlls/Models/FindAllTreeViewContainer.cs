using Clair.Common.RazorLib;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.Commands.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models;

namespace Clair.TextEditor.RazorLib.FindAlls.Models;

public class FindAllTreeViewContainer : TreeViewContainer
{
    public FindAllTreeViewContainer(
            TextEditorService textEditorService,
            List<(ResourceUri ResourceUri, TextEditorTextSpan TextSpan)> searchResultList,
            int nodeValueListInitialCapacity,
            List<(string ProjectAbsolutePath, int ChildListOffset, int ChildListLength)> projectRespectedList)
        : base(textEditorService.CommonService)
    {
        Key = TextEditorService.TextEditorFindAllState.TreeViewFindAllContainerKey;
        NodeValueList = new(capacity: nodeValueListInitialCapacity);
        SearchResultList = searchResultList;
        TextEditorService = textEditorService;
        ProjectRespectedList = projectRespectedList;
    }
    
    public override Key<TreeViewContainer> Key { get; init; }
    public override List<TreeViewNodeValue> NodeValueList { get; }
    
    public TextEditorService TextEditorService { get; set; }
    public List<(ResourceUri ResourceUri, TextEditorTextSpan TextSpan)> SearchResultList { get; set; }
    public List<(string ProjectAbsolutePath, int ChildListOffset, int ChildListLength)> ProjectRespectedList { get; set; }

    public override Task LoadChildListAsync(int indexNodeValue)
    {
        return Task.CompletedTask;
    }

    public override Task OnContextMenuAsync(
        int indexNodeValue,
        bool occurredDueToMouseEvent,
        double leftPositionInPixels,
        double topPositionInPixels)
    {
        return Task.CompletedTask;
    }
    
    public override string GetDisplayText(int indexNodeValue)
    {
        var nodeValue = NodeValueList[indexNodeValue];
        switch (nodeValue.ByteKind)
        {
            case FindAllTreeViewContainer.ByteKind_Aaa:
                return nameof(ByteKind_Aaa);
            case FindAllTreeViewContainer.ByteKind_ProjectGroup:
                return ProjectRespectedList[nodeValue.TraitsIndex].ProjectAbsolutePath;
            case FindAllTreeViewContainer.ByteKind_FileGroup:
                return SearchResultList[nodeValue.TraitsIndex].ResourceUri.Value;
            case FindAllTreeViewContainer.ByteKind_SearchResult:
                return SearchResultList[nodeValue.TraitsIndex].TextSpan.StartInclusiveIndex.ToString();
            default:
                return nameof(nodeValue.ByteKind) + nodeValue.ByteKind.ToString();
        }
    }
    
    public override Task OnDoubleClickAsync(TreeViewCommandArgs commandArgs, int indexNodeValue)
    {
        base.OnDoubleClickAsync(commandArgs, indexNodeValue);
        
        string? path = null;

        var nodeValue = NodeValueList[indexNodeValue];
        switch (nodeValue.ByteKind)
        {
            case FindAllTreeViewContainer.ByteKind_Aaa:
                return Task.CompletedTask;
            case FindAllTreeViewContainer.ByteKind_SearchResult:
                path = SearchResultList[nodeValue.TraitsIndex].ResourceUri.Value;
                break;
            default:
                return Task.CompletedTask;
        }
        
        if (path is null)
            return Task.CompletedTask;

        TextEditorService.WorkerArbitrary.PostUnique(async editContext =>
        {
            await TextEditorService.OpenInEditorAsync(
                editContext,
                path,
                shouldSetFocusToEditor: true,
                cursorPositionIndex: SearchResultList[nodeValue.TraitsIndex].TextSpan.StartInclusiveIndex,
                new Category("main"),
                editContext.TextEditorService.NewViewModelKey());
        });
        return Task.CompletedTask;
    }
    
    public override Task OnKeyDownAsync(TreeViewCommandArgs commandArgs, int indexNodeValue)
    {
        if (commandArgs.KeyboardEventArgs is null)
            return Task.CompletedTask;

        base.OnKeyDownAsync(commandArgs, indexNodeValue);

        switch (commandArgs.KeyboardEventArgs.Code)
        {
            case CommonFacts.ENTER_CODE:
            {
                string? path = null;

                var nodeValue = NodeValueList[indexNodeValue];
                switch (nodeValue.ByteKind)
                {
                    case FindAllTreeViewContainer.ByteKind_Aaa:
                        return Task.CompletedTask;
                    case FindAllTreeViewContainer.ByteKind_SearchResult:
                        path = SearchResultList[nodeValue.TraitsIndex].ResourceUri.Value;
                        break;
                    default:
                        return Task.CompletedTask;
                }
                
                if (path is null)
                    return Task.CompletedTask;
        
                TextEditorService.WorkerArbitrary.PostUnique(async editContext =>
                {
                    await TextEditorService.OpenInEditorAsync(
                        editContext,
                        path,
                        shouldSetFocusToEditor: true,
                        cursorPositionIndex: SearchResultList[nodeValue.TraitsIndex].TextSpan.StartInclusiveIndex,
                        new Category("main"),
                        editContext.TextEditorService.NewViewModelKey());
                });
                return Task.CompletedTask;
            }
            case CommonFacts.SPACE_CODE:
            {
                string? path = null;

                var nodeValue = NodeValueList[indexNodeValue];
                switch (nodeValue.ByteKind)
                {
                    case FindAllTreeViewContainer.ByteKind_Aaa:
                        return Task.CompletedTask;
                    case FindAllTreeViewContainer.ByteKind_SearchResult:
                        path = SearchResultList[nodeValue.TraitsIndex].ResourceUri.Value;
                        break;
                    default:
                        return Task.CompletedTask;
                }
                
                if (path is null)
                    return Task.CompletedTask;
        
                TextEditorService.WorkerArbitrary.PostUnique(async editContext =>
                {
                    await TextEditorService.OpenInEditorAsync(
                        editContext,
                        path,
                        shouldSetFocusToEditor: false,
                        cursorPositionIndex: SearchResultList[nodeValue.TraitsIndex].TextSpan.StartInclusiveIndex,
                        new Category("main"),
                        editContext.TextEditorService.NewViewModelKey());
                });
                return Task.CompletedTask;
            }
        }

        return Task.CompletedTask;
    }
    
    public const byte ByteKind_Aaa = 1;
    public const byte ByteKind_SearchResult = 2;
    public const byte ByteKind_FileGroup = 3;
    public const byte ByteKind_ProjectGroup = 4;
}
