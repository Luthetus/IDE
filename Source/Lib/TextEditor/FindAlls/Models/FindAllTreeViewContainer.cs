using Clair.Common.RazorLib;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.TextEditor.RazorLib.Lexers.Models;

namespace Clair.TextEditor.RazorLib.FindAlls.Models;

public class FindAllTreeViewContainer : TreeViewContainer
{
    public FindAllTreeViewContainer(TextEditorService textEditorService, List<(ResourceUri ResourceUri, TextEditorTextSpan TextSpan)> searchResultList)
        : base(textEditorService.CommonService)
    {
        Key = TextEditorService.TextEditorFindAllState.TreeViewFindAllContainerKey;
        NodeValueList = new();
        SearchResultList = searchResultList;
    }
    
    public override Key<TreeViewContainer> Key { get; init; }
    public override List<TreeViewNodeValue> NodeValueList { get; }
    
    public List<(ResourceUri ResourceUri, TextEditorTextSpan TextSpan)> SearchResultList { get; set; }

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
            case FindAllTreeViewContainer.ByteKind_SearchResult:
                return SearchResultList[nodeValue.TraitsIndex].ResourceUri.Value;
            default:
                return "asdfg";
        }
    }
    
    public const byte ByteKind_Aaa = 1;
    public const byte ByteKind_SearchResult = 2;
}
