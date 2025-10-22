using Clair.Common.RazorLib.Keys.Models;

namespace Clair.Common.RazorLib.TreeViews.Models;

/// <summary>
/// This interface should always be directly tied to UI of a TreeView actively being rendered.
/// To maintain TreeView state beyond the lifecycle of the UI, implement the Dispose
/// and store the TreeView yourself however you want, in an optimized manner.
/// </summary>
public interface ITreeViewContainer : IDisposable
{
    public int NextNodeValueKey { get; set; }
    
    /// <summary>
    /// TODO: Don't use the static int named 's_nextKey'.
    /// ...a single user theoretically could have a key collision,
    /// but even moreso if the app were to be ServerSide hosted
    /// the 's_nextKey' can more frequently hit the same int (and perhaps go to the same user/TreeViewContainer).
    /// 
    /// Also, using 0 to indicate "None"/"a null of sorts" is a bit odd given the "eventual" return of 0 when the int wraps around then returns every negative value.
    /// </summary>

    /// <summary>Unique identifier</summary>
    public Key<TreeViewContainer> Key { get; init; }
    
    /// <summary>
    /// WARNING: modification of this list from a non-`ITreeViewContainer` is extremely unsafe.
    /// ...it is the responsibility of the container to ensure any modifications it performs
    /// are thread safe.
    /// ...
    /// ... Making this an IReadOnlyList only serves to increase overhead when enumerating the NodeValue
    /// (and the enumeration of this is a hot path),
    /// just don't touch the list and there won't be any problems.
    ///
    /// A TreeViewNodeValue's children are enumerated by the span of
    /// the container's (inclusive) ChildList[TreeViewNoType.ChildListOffset] to
    /// (exclusive) ChildList[TreeViewNoType.ChildListOffset + TreeViewNoType.ChildListLength].
    /// </summary>
    public List<TreeViewNodeValue> NodeValueList { get; }
    
    /// <summary>The nodeValue with the highlighted background color</summary>
    public int ActiveNodeValueIndex { get; }
    
    /// <summary>
    /// In your constructor:
    /// `ActiveNodeElementId = $"ci_node-{Key.Guid}";`
    ///
    /// This is used for scrolling the ActiveNodeValueIndex into view.
    /// </summary>
    public string ActiveNodeElementId { get; }
    
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
    
    /// <summary>
    /// When the UI expands a nodeValue, then this method is invoked foreach child of the expanded nodeValue.
    /// (virtualization is NOT accounted for. This gets invoked for every new child whether it is visible or not).
    /// </summary>
    public void Saturate(ref TreeViewNodeValue nodeValue)
    {
        
    }
    
    /// <summary>
    /// When the UI collapses a nodeValue, then this method is invoked on every nodeValue which
    /// was a child of the collapsed nodeValue.
    /// </summary>
    public void Dessicate(ref TreeViewNodeValue nodeValue)
    {
        
    }
    
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
    public void LinkChildrenNoMap(IEnumerable<TreeViewNoType> nextChildList, ITreeViewContainer container)
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
        ITreeViewContainer container)
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
    
    public virtual IEnumerable<TreeViewNoType> GetChildList(TreeViewContainer container)
    {
        if (ChildListOffset >= container.ChildList.Count)
            return Enumerable.Empty<TreeViewNoType>();
    
        return container.ChildList.Skip(ChildListOffset).Take(ChildListLength);
    }
    
    /// <summary>
    /// This interface should always be directly tied to UI of a TreeView actively being rendered.
    /// To maintain TreeView state beyond the lifecycle of the UI, implement the Dispose
    /// and store the TreeView yourself however you want, in an optimized manner.
    /// </summary>
    public void Dispose()
    {
    }
}
