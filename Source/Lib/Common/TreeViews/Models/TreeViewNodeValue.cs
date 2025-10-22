using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.Icons.Displays;

namespace Clair.Common.RazorLib.TreeViews.Models;

public struct TreeViewNodeValue
{
    /// <summary>The corresponding TreeViewContainer will have this</summary>
    public int ParentIndex { get; set; }
    /// <summary>
    /// A TreeViewNoType's children are enumerated by the span of
    /// the container's (inclusive) ChildList[TreeViewNoType.ChildListOffset] to
    /// (exclusive) ChildList[TreeViewNoType.ChildListOffset + TreeViewNoType.ChildListLength].
    ///
    /// When modifying ChildListOffset, it is safest to set ChildListLength to 0 before doing so.
    /// </summary>
    public int ChildListOffset { get; set; }
    /// <summary>
    /// A TreeViewNoType's children are enumerated by the span of
    /// the container's (inclusive) ChildList[TreeViewNoType.ChildListOffset] to
    /// (exclusive) ChildList[TreeViewNoType.ChildListOffset + TreeViewNoType.ChildListLength].
    /// </summary>
    public int ChildListLength { get; set; }
    public TreeViewNodeValueKind TreeViewNodeValueKind { get; set; }
    
    public IEnumerable<TreeViewNoType> GetChildList(TreeViewContainer container)
    {
        if (ChildListOffset >= container.ChildList.Count)
            return Enumerable.Empty<TreeViewNoType>();
    
        return container.ChildList.Skip(ChildListOffset).Take(ChildListLength);
    }

    /// <summary>
    /// <see cref="IndexAmongSiblings"/> refers to the index which this <see cref="TreeViewNoType"/>
    /// is found at within their <see cref="Parent"/>'s <see cref="ChildList"/>
    /// </summary>
    public int IndexAmongSiblings { get; set; }
    public bool IsRoot { get; set; }
    public bool IsHidden { get; set; }
    public bool IsExpandable { get; set; }
    public bool IsExpanded { get; set; }
    
    /// <summary>
    /// Used by the UI
    /// </summary>
    public int Depth { get; set; }    /// <summary>    /// TODO: Don't use the static int named 's_nextKey'.    /// ...a single user theoretically could have a key collision,    /// but even moreso if the app were to be ServerSide hosted    /// the 's_nextKey' can more frequently hit the same int (and perhaps go to the same user/TreeViewContainer).    ///     /// Also, using 0 to indicate "None"/"a null of sorts" is a bit odd given the "eventual" return of 0 when the int wraps around then returns every negative value.    /// </summary>
    private static int s_nextKey = 1;
    /// <summary>    /// TODO: Don't use the static int named 's_nextKey'.    /// ...a single user theoretically could have a key collision,    /// but even moreso if the app were to be ServerSide hosted    /// the 's_nextKey' can more frequently hit the same int (and perhaps go to the same user/TreeViewContainer).    ///     /// Also, using 0 to indicate "None"/"a null of sorts" is a bit odd given the "eventual" return of 0 when the int wraps around then returns every negative value.    /// </summary>
    public int Key { get; set; } = s_nextKey++;

    public virtual string GetDisplayText() => this.GetType().Name;
    /// <summary>
    /// Make sure to return null if you don't use this, in order to avoid the 'class' attribute for no reason.
    /// </summary>
    public virtual string? GetDisplayTextCssClass() => null;
    /// <summary>
    /// Make sure to return null if you don't use this, in order to avoid the 'class' attribute for no reason.
    /// </summary>
    public virtual string? GetHoverText() => null;
    public virtual IconKind IconKind => IconKind.None;
    
    public abstract Task LoadChildListAsync(TreeViewContainer container);

    /// <summary>
    /// Sets foreach child: child.Parent = this;
    /// As well it sets the child.IndexAmongSiblings, and maintains expanded state.
    /// </summary>
    public void LinkChildrenNoMap(IEnumerable<TreeViewNoType> nextChildList, TreeViewContainer container)
    {
        LinkChildren(previousChildList: null, nextChildList, container);
    }
    
    /// <summary>
    /// Sets foreach child: child.Parent = this;
    /// As well it sets the child.IndexAmongSiblings, and maintains expanded state.
    /// </summary>
    public virtual void LinkChildren(
        IEnumerable<TreeViewNoType>? previousChildList,
        IEnumerable<TreeViewNoType> nextChildList,
        TreeViewContainer container)
    {
        Dictionary<TreeViewNoType, TreeViewNoType>? previousChildMap;
        if (previousChildList is not null)
            previousChildMap = previousChildList.ToDictionary(child => child);
        else
            previousChildMap = null;
            
        var indexAmongSiblings = 0;
        foreach (var nextChild in nextChildList)
        {
            nextChild.Parent = this;
            nextChild.IndexAmongSiblings = indexAmongSiblings++;
    
            if (previousChildMap is not null && previousChildMap.TryGetValue(nextChild, out var previousChild))
            {
                nextChild.IsExpanded = previousChild.IsExpanded;
                nextChild.IsExpandable = previousChild.IsExpandable;
                nextChild.IsHidden = previousChild.IsHidden;
                nextChild.Key = previousChild.Key;
                nextChild.ChildListOffset = previousChild.ChildListOffset;
                nextChild.ChildListLength = previousChild.ChildListLength;
                
                // This step is only necessary when restoring the previousChild.
                foreach (var innerChild in nextChild.GetChildList(container))
                {
                    innerChild.Parent = nextChild;
                }
            }
        }
    }

    /// <summary>
    /// <see cref="RemoveRelatedFilesFromParent"/> is used for showing codebehinds such that a file on
    /// the filesystem can be displayed as having children in the TreeView.<br/><br/>
    /// In the case of a directory loading its children. After the directory loads all its children it
    /// will loop through the children invoking <see cref="RemoveRelatedFilesFromParent"/> on each of
    /// the children.<br/><br/>
    /// For example: if a directory has the children { 'Component.razor', 'Component.razor.cs' }  then
    /// 'Component.razor' will remove 'Component.razor.cs' from the parent directories children and
    /// mark itself as expandable as it saw a related file in its parent.
    /// </summary>
    public virtual void RemoveRelatedFilesFromParent(List<TreeViewNoType> siblingsAndSelfTreeViews)
    {
        // The default implementation of this method is to do nothing.
        // Override this method to implement some functionality if desired.
    }
}
