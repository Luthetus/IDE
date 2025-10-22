using Clair.Common.RazorLib;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.TreeViews.Models;

namespace Clair.Extensions.DotNet.DotNetSolutions.Models;

public class SolutionExplorerTreeViewContainer : TreeViewContainer
{
    // DotNetSolutionState.TreeViewSolutionExplorerStateKey
    
    public SolutionExplorerTreeViewContainer(CommonService commonService)
        : base(commonService)
    {
        Key = DotNetSolutionState.TreeViewSolutionExplorerStateKey;
        NodeValueList = new();
    }
    
    public override Key<TreeViewContainer> Key { get; init; }
    public override List<TreeViewNodeValue> NodeValueList { get; }

    public override Task LoadChildListAsync(int indexNodeValue)
    {
        return Task.CompletedTask;
    }
    
    public override string GetDisplayText(int indexNodeValue)
    {
        var nodeValue = NodeValueList[NodeValueList];
        if (nodeValue.)
        {
        }
        return string.Empty;
    }
}
