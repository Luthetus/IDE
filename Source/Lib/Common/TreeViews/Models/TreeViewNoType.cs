using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.Icons.Displays;

namespace Clair.Common.RazorLib.TreeViews.Models;

/// <summary>
/// Without this datatype one cannot for example hold all their <see cref="TreeViewWithType{T}"/> in a <see cref="List{T}"/> unless
/// all implementation instances share the same generic argument type.
/// </summary>
public abstract class TreeViewNoType
{
    public abstract object UntypedItem { get; }
    public abstract Type ItemType { get; }

    /*
    # Primary concern TreeViewNoType currently stores:
    - their own List instance for the ChildList
    - reference to Parent (this point is far less of a concern than the first)
    ...from a garbage collection perspective this is extremely costly.
    
    # Secondary concern is the Key being a Guid, this seems a bit overly expensive when an int or etc... would likely suffice.
    
    # Tertiary concern is that LinkChildren requires a previous child list, thus even if you're clearing the ChildList,
      you must new() an empty List in order to invoke LinkChildren.
    
    ============================================================================================================================
    
    # Primary.A: their own List instance for the ChildList
    ...
    
    ------------------------------------------------------
    
    # Primary.B: reference to Parent (this point is far less of a concern than the first)
    ...
    
    -------------------------------------------------------------------------------------
    
    # Secondary: A secondary concern is the Key being a Guid, this seems a bit overly expensive when an int or etc... would likely suffice.
    ...
    
    This step is probably quite simple. I'm kinda dead on the inside so I think I want something easy to do. Then I can
    ramp up my motivation from there and do the more difficult tasks.
    
    The cost of these Guids versus an int is actually extremely impactful.
    
    TreeViewNodes currently can occur in large numbers due to lacking optimizations steps.
    Thus in the heap there exists some TreeViewNode, and the instance includes this relatively "massive" GUID.
    
    So, you then have the garbage collector go on to defragment the heap and it has to move many
    of these relatively "massive" GUIDs versus an int which is 1/4th the size.
    
    There is a concern of conflict, it shouldn't be an issue though you just have to think for a moment.
    
    ---------------------------------------------------------------------------------------------------------------------------------------
    
    # Tertiary: LinkChildren requires a previous child list, thus even if you're clearing the ChildList,
                you must new() an empty List in order to invoke LinkChildren.
    
    The method performs two steps.
    
    First step is:
    ```csharp
    // there is an extra step here (outside the 'for' loop) for the "second step" to work.
    
    for (int i = 0; i < nextChildList.Count; i++)
    {
        var nextChild = nextChildList[i];

        nextChild.Parent = this;
        nextChild.IndexAmongSiblings = i;

        // second step is commented out
    }
    ```
    
    Second step is:
    ```csharp
    var previousChildMap = previousChildList.ToDictionary(child => child);
    
    for (int i = 0; i < nextChildList.Count; i++)
    {
        // first step is commented out

        if (previousChildMap.TryGetValue(nextChild, out var previousChild))
        {
            nextChild.IsExpanded = previousChild.IsExpanded;
            nextChild.IsExpandable = previousChild.IsExpandable;
            nextChild.IsHidden = previousChild.IsHidden;
            nextChild.Key = previousChild.Key;
            nextChild.ChildList = previousChild.ChildList;

            foreach (var innerChild in nextChild.ChildList)
            {
                innerChild.Parent = nextChild;
            }
        }
    }
    ```
    
    Necessary changes:
    ```csharp
    Dictionary<TreeViewNoType, TreeViewNoType>? previousChildMap;
    if (previousChildList is not null)
        previousChildMap = previousChildList.ToDictionary(child => child)
    else
        previousChildMap = null;
        
    for (int i = 0; i < nextChildList.Count; i++)
    {
        // first step is commented out

        if (previousChildMap is not null && previousChildMap.TryGetValue(nextChild, out var previousChild))
        {
            nextChild.IsExpanded = previousChild.IsExpanded;
            nextChild.IsExpandable = previousChild.IsExpandable;
            nextChild.IsHidden = previousChild.IsHidden;
            nextChild.Key = previousChild.Key;
            nextChild.ChildList = previousChild.ChildList;

            foreach (var innerChild in nextChild.ChildList)
            {
                innerChild.Parent = nextChild;
            }
        }
    }
    ```
    
    I'm not overly concerned with whether a method such as 'LinkChildren' is necessary,
    or if it is written "properly".
    
    The reason I'm not concerned is that there are more important things to do.
    
    But, the wasteful garbage collection overhead of the previousChildList does bother me a bit. So I'd like to fix that.
    
    (side note: why do I at times LinkChildren and pass in the same List as the previous and the next?)
    
    I see why, it is actually "3 steps" currently, and the final step is erroneously included within the "2nd step".
    You were talking about sharing nodes but you can't share them if you change the child to point to the most recently made parent.
    
    ... that step is only necessary when restoring the previousChild. That is the reason it is within the "2nd step" because
    if you don't run the second step then there's no need to change the childrens' parent reference.
    
    ----------------------------------------------------------------------------------------------------
    
    The above concerns are ordered based on how dangerous they are with respect to a user gesture.
    The user can end up creating a great deal of TreeView nodes, especially in scenarios where the TreeView's lifetime is not tied to the UI.
    
    Even if you want to keep alive a TreeView, you likely can store this information in a specialized value type manner.
    And then recreate the nodes if you decide to continue using the 'TreeViewNoType' when the UI becomes shown again.
    (as well you might decide to only remember the parents and then load the children of each parent so you only need to persist the parent nodes in memory,
     which is presumably far less nodes than if you stored the entire tree).
     
    ========================================================================
     
     On an unrelated note, the TextEditor's "virtualization span list" (I think that's what is called)
     was changed to create the next list with the initial capacity equal to the size of the previous list.
     
     Maybe it is better to just always use the previous capacity, i.e.: use the largest capacity seen during the duration
     of that view model.
     
     The worry is with an outlier scenario and then you keep creating this massive list each virtualization result...
     
     ----------------------------------------------------------------------------------------------------------------
     
     If you were to determine an "average overlap" in the capacity to avoid frequent 1 off scenarios
     (i.e.: the next virtualization result needs to store 1 further value than the previous one
            and thus needs to reallocate the list to fit the final entry...
            then maybe you can do a +someCount for the capacity to avoid these 1 offs (if they're even frequent)).
    */
    public TreeViewNoType? Parent { get; set; }
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
