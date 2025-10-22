using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.Commands.Models;
using Clair.Common.RazorLib.Icons.Displays;

namespace Clair.Common.RazorLib.TreeViews.Models;

/// <summary>
/// This interface should always be directly tied to UI of a TreeView actively being rendered.
/// To maintain TreeView state beyond the lifecycle of the UI, implement the Dispose
/// and store the TreeView yourself however you want, in an optimized manner.
/// </summary>
public abstract class TreeViewContainer
{
    public TreeViewContainer(CommonService commonService)
    {
        CommonService = commonService;
    }
    
    public virtual CommonService CommonService { get; }

    public virtual int NextNodeValueKey { get; set; }
    
    /// <summary>
    /// TODO: Don't use the static int named 's_nextKey'.
    /// ...a single user theoretically could have a key collision,
    /// but even moreso if the app were to be ServerSide hosted
    /// the 's_nextKey' can more frequently hit the same int (and perhaps go to the same user/TreeViewContainer).
    /// 
    /// Also, using 0 to indicate "None"/"a null of sorts" is a bit odd given the "eventual" return of 0 when the int wraps around then returns every negative value.
    /// </summary>

    /// <summary>Unique identifier</summary>
    public virtual Key<TreeViewContainer> Key { get; init; }
    
    public virtual bool IsRootNodeHidden { get; set; }
    
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
    public virtual List<TreeViewNodeValue> NodeValueList { get; }
    
    /// <summary>The nodeValue with the highlighted background color</summary>
    public virtual int ActiveNodeValueIndex { get; }
    
    /// <summary>
    /// In your constructor:
    /// `ActiveNodeElementId = $"ci_node-{Key.Guid}";`
    ///
    /// This is used for scrolling the ActiveNodeValueIndex into view.
    /// </summary>
    public virtual string ActiveNodeElementId { get; }
    
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
    public virtual void RemoveRelatedFilesFromParent(List<TreeViewNodeValue> siblingsAndSelfTreeViews)
    {
        // The default implementation of this method is to do nothing.
        // Override this method to implement some functionality if desired.
    }
    
    /// <summary>
    /// When the UI expands a nodeValue, then this method is invoked foreach child of the expanded nodeValue.
    /// (virtualization is NOT accounted for. This gets invoked for every new child whether it is visible or not).
    /// </summary>
    public virtual void Saturate(int indexNodeValue)
    {
        // NOTE: Code behinds can be done easily because you only ever remove children....
        // ...thus you have this contiguous space in the List and a span over it.
        // You can decrease length and shift various children around.
    }
    
    /// <summary>
    /// When the UI collapses a nodeValue, then this method is invoked on every nodeValue which
    /// was a child of the collapsed nodeValue.
    /// </summary>
    public virtual void Dessicate(int indexNodeValue)
    {
        
    }
    
    public virtual string GetDisplayText(int indexNodeValue) => this.GetType().Name;
    /// <summary>
    /// Make sure to return null if you don't use this, in order to avoid the 'class' attribute for no reason.
    /// </summary>
    public virtual string? GetDisplayTextCssClass(int indexNodeValue) => null;
    /// <summary>
    /// Make sure to return null if you don't use this, in order to avoid the 'class' attribute for no reason.
    /// </summary>
    public virtual string? GetHoverText(int indexNodeValue) => null;
    public virtual IconKind IconKind => IconKind.None;
    
    public abstract Task LoadChildListAsync(int indexNodeValue);

    /// <summary>
    /// Sets foreach child: child.Parent = this;
    /// As well it sets the child.IndexAmongSiblings, and maintains expanded state.
    /// </summary>
    public virtual void LinkChildrenNoMap(int indexNodeValue, IEnumerable<TreeViewNodeValue> nextChildList)
    {
        /*
        // 2025-10-22 (rewrite TreeViews)
        LinkChildren(previousChildList: null, nextChildList, container);
        */
    }
    
    /// <summary>
    /// Sets foreach child: child.Parent = this;
    /// As well it sets the child.IndexAmongSiblings, and maintains expanded state.
    /// </summary>
    public virtual void LinkChildren(
        int indexNodeValue, 
        IEnumerable<TreeViewNodeValue>? previousChildList,
        IEnumerable<TreeViewNodeValue> nextChildList)
    {
        /*
        // 2025-10-22 (rewrite TreeViews)
        Dictionary<TreeViewNodeValue, TreeViewNodeValue>? previousChildMap;
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
        */
    }
    
    public virtual IEnumerable<TreeViewNodeValue> GetChildList(int indexNodeValue)
    {
        /*
        // 2025-10-22 (rewrite TreeViews)
        if (ChildListOffset >= container.ChildList.Count)
            return Enumerable.Empty<TreeViewNoType>();
    
        return container.ChildList.Skip(ChildListOffset).Take(ChildListLength);
        */
        return Enumerable.Empty<TreeViewNodeValue>();
    }
    
    /// <summary>
    /// Invoked, and awaited, as part of the async UI event handler for 'onkeydownwithpreventscroll' events.<br/><br/>
    /// 
    /// The synchronous version: '<see cref="OnKeyDown(TreeViewCommandArgs)"/>' will be invoked
    /// immediately from within this method, to allow the synchronous code to block the UI purposefully.
    /// 
    /// Any overrides of this method are intended to have 'base.MethodBeingOverridden()' prior to their code.<br/><br/>
    /// </summary>
    public virtual Task OnKeyDownAsync(TreeViewCommandArgs commandArgs)
    {
        // Run the synchronous code first
        OnKeyDown(commandArgs);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked, and awaited, as part of the synchronous UI event handler for 'onkeydownwithpreventscroll' events.<br/><br/>
    /// 
    /// This method is invoked by the async version: '<see cref="OnKeyDownAsync(TreeViewCommandArgs)"/>'.<br/><br/>
    /// 
    /// Any overrides of this method are intended to have 'base.MethodBeingOverridden()' prior to their code.<br/><br/>
    /// </summary>
    protected virtual void OnKeyDown(TreeViewCommandArgs commandArgs)
    {
        if (commandArgs.KeyboardEventArgs is null)
            return;

        switch (commandArgs.KeyboardEventArgs.Key)
        {
            case CommonFacts.ARROW_LEFT_KEY:
                CommonService.TreeView_MoveLeftAction(
                    commandArgs.TreeViewContainer.Key,
                    commandArgs.KeyboardEventArgs.ShiftKey,
                    false);
                break;
            case CommonFacts.ARROW_DOWN_KEY:
                CommonService.TreeView_MoveDownAction(
                    commandArgs.TreeViewContainer.Key,
                    commandArgs.KeyboardEventArgs.ShiftKey,
                    false);
                break;
            case CommonFacts.ARROW_UP_KEY:
                CommonService.TreeView_MoveUpAction(
                    commandArgs.TreeViewContainer.Key,
                    commandArgs.KeyboardEventArgs.ShiftKey,
                    false);
                break;
            case CommonFacts.ARROW_RIGHT_KEY:
                CommonService.TreeView_MoveRight(
                    commandArgs.TreeViewContainer.Key,
                    commandArgs.KeyboardEventArgs.ShiftKey,
                    false);
                break;
            case CommonFacts.HOME_KEY:
                CommonService.TreeView_MoveHomeAction(
                    commandArgs.TreeViewContainer.Key,
                    commandArgs.KeyboardEventArgs.ShiftKey,
                    false);
                break;
            case CommonFacts.END_KEY:
                CommonService.TreeView_MoveEndAction(
                    commandArgs.TreeViewContainer.Key,
                    commandArgs.KeyboardEventArgs.ShiftKey,
                    false);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Invoked, and awaited, as part of the async UI event handler for 'onclick' events.<br/><br/>
    /// 
    /// The synchronous version: '<see cref="OnClick(TreeViewCommandArgs)"/>' will be invoked
    /// immediately from within this method, to allow the synchronous code to block the UI purposefully.
    /// 
    /// Any overrides of this method are intended to have 'base.MethodBeingOverridden()' prior to their code.<br/><br/>
    /// </summary>
    public virtual Task OnClickAsync(TreeViewCommandArgs commandArgs)
    {
        // Run the synchronous code first to maintain the UI's synchronization context
        OnClick(commandArgs);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked, and awaited, as part of the async UI event handler for 'ondblclick' events.<br/><br/>
    /// 
    /// The synchronous version: '<see cref="OnDoubleClick(TreeViewCommandArgs)"/>' will be invoked
    /// immediately from within this method, to allow the synchronous code to block the UI purposefully.
    /// 
    /// Any overrides of this method are intended to have 'base.MethodBeingOverridden()' prior to their code.<br/><br/>
    /// </summary>
    public virtual Task OnDoubleClickAsync(TreeViewCommandArgs commandArgs)
    {
        // Run the synchronous code first
        OnDoubleClick(commandArgs);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked, and awaited, as part of the async UI event handler for 'onmousedown' events.<br/><br/>
    /// 
    /// The synchronous version: '<see cref="OnMouseDown(TreeViewCommandArgs)"/>' will be invoked
    /// immediately from within this method, to allow the synchronous code to block the UI purposefully.<br/><br/>
    /// 
    /// Any overrides of this method are intended to have 'base.MethodBeingOverridden()' prior to their code.<br/><br/>
    /// </summary>
    public virtual Task OnMouseDownAsync(TreeViewCommandArgs commandArgs)
    {
        // Run the synchronous code first
        OnMouseDown(commandArgs);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked, and awaited, as part of the synchronous UI event handler for 'onclick' events.<br/><br/>
    /// 
    /// This method is invoked by the async version: '<see cref="OnMouseDownAsync(TreeViewCommandArgs)"/>'.<br/><br/>
    /// 
    /// Any overrides of this method are intended to have 'base.MethodBeingOverridden()' prior to their code,
    /// but for this method it makes no difference if one puts it after their code.<br/><br/>
    /// </summary>
    protected virtual void OnClick(TreeViewCommandArgs commandArgs)
    {
        return;
    }

    /// <summary>
    /// Invoked, and awaited, as part of the synchronous UI event handler for 'ondblclick' events.<br/><br/>
    ///
    /// This method is invoked by the async version: '<see cref="OnDoubleClickAsync(TreeViewCommandArgs)"/>'.<br/><br/>
    /// 
    /// Any overrides of this method are intended to have 'base.MethodBeingOverridden()' prior to their code,
    /// but for this method it makes no difference if one puts it after their code.<br/><br/>
    /// </summary>
    protected virtual void OnDoubleClick(TreeViewCommandArgs commandArgs)
    {
        return;
    }

    /// <summary>
    /// Invoked, and awaited, as part of the synchronous UI event handler for 'onmousedown' events.<br/><br/>
    /// 
    /// This method is invoked by the async version: '<see cref="OnMouseDownAsync(TreeViewCommandArgs)"/>'.<br/><br/>
    /// 
    /// Any overrides of this method are intended to have 'base.MethodBeingOverridden()' prior to their code.<br/><br/>
    /// </summary>
    protected virtual void OnMouseDown(TreeViewCommandArgs commandArgs)
    {
        /*
        // 2025-10-22 (rewrite TreeViews)
        if (commandArgs.NodeThatReceivedMouseEvent is null || commandArgs.MouseEventArgs is null)
            return;

        if ((commandArgs.MouseEventArgs.Buttons & 1) == 1) // Left Click
        {
            // This boolean asks: Should I ADD this TargetNode to the list of selected nodes,
            //                    OR
            //                    Should I CLEAR the list of selected nodes and make this TargetNode the only entry.
            var addSelectedNodes = commandArgs.MouseEventArgs.CtrlKey || commandArgs.MouseEventArgs.ShiftKey;

            // This boolean asks: Should I ALSO SELECT the nodes between the currentNode and the targetNode.
            var selectNodesBetweenCurrentAndNextActiveNode = commandArgs.MouseEventArgs.ShiftKey;

            CommonService.TreeView_SetActiveNodeAction(
                commandArgs.TreeViewContainer.Key,
                commandArgs.NodeThatReceivedMouseEvent,
                addSelectedNodes,
                selectNodesBetweenCurrentAndNextActiveNode);
        }
        else // Presume Right Click or Context Menu
        {
            if (commandArgs.MouseEventArgs.CtrlKey)
            {
                // Open context menu, but do not move the active node, regardless who the TargetNode is
            }
            else
            {
                var targetNodeAlreadySelected = commandArgs.TreeViewContainer.SelectedNodeList.Any(x => x.Key == commandArgs.NodeThatReceivedMouseEvent.Key);
                
                if (targetNodeAlreadySelected)
                {
                    // Open context menu, but do not move the active node, regardless who the TargetNode is
                }
                else
                {
                    // Move the active node, and open context menu
                    CommonService.TreeView_SetActiveNodeAction(
                        commandArgs.TreeViewContainer.Key,
                        commandArgs.NodeThatReceivedMouseEvent,
                        false,
                        false);
                }
            }
        }
        */
    }
    
    /// <summary>
    /// This interface should always be directly tied to UI of a TreeView actively being rendered.
    /// To maintain TreeView state beyond the lifecycle of the UI, implement the Dispose
    /// and store the TreeView yourself however you want, in an optimized manner.
    /// </summary>
    public virtual void DisposeContainer()
    {
        
    }
}
