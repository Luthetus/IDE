using Clair.Common.RazorLib;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.TreeViews.Models;

namespace Clair.TextEditor.RazorLib.FindAlls.Models;

public class FindAllTreeViewContainer : TreeViewContainer
{
    public FindAllTreeViewContainer(CommonService commonService)
        : base(commonService)
    {
        Key = Key<TreeViewContainer>.NewKey();
        NodeValueList = new();
    }
    
    public override Key<TreeViewContainer> Key { get; init; }
    public override List<TreeViewNodeValue> NodeValueList { get; }

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
}
