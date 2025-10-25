using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.Commands.Models;

namespace Clair.Common.RazorLib.TreeViews.Models;

public struct TreeViewContainerParameter
{
    public TreeViewContainerParameter(
        Key<TreeViewContainer> treeViewContainerKey,
        Func<TreeViewCommandArgs, Task>? onContextMenuFunc)
    {
        TreeViewContainerKey = treeViewContainerKey;
        OnContextMenuFunc = onContextMenuFunc;
    }
    
    public Key<TreeViewContainer> TreeViewContainerKey { get; set; } = Key<TreeViewContainer>.Empty;
    public Func<TreeViewCommandArgs, Task>? OnContextMenuFunc { get; set; }
}
