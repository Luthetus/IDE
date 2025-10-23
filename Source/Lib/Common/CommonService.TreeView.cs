using Clair.Common.RazorLib.Dimensions.Models;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.BackgroundTasks.Models;
using Clair.Common.RazorLib.ListExtensions;
using Clair.Common.RazorLib.TreeViews.Models;

namespace Clair.Common.RazorLib;

public partial class CommonService
{
    private TreeViewState _treeViewState = new();
    
    public TreeViewState GetTreeViewState() => _treeViewState;
    
    public TreeViewContainer? GetTreeViewContainer(Key<TreeViewContainer> containerKey)    {
        foreach (var container in _treeViewState.ContainerList)        {
            if (container.Key == containerKey)                return container;        }
        return null;    }

    public bool TryGetTreeViewContainer(Key<TreeViewContainer> containerKey, out TreeViewContainer? container)
    {
        foreach (var c in GetTreeViewState().ContainerList)        {            if (c.Key == containerKey)            {                container = c;                return true;            }            }        container = null;
        return false;
    }

    public void TreeView_MoveRight(
        Key<TreeViewContainer> containerKey,
        bool addSelectedNodes,
        bool selectNodesBetweenCurrentAndNextActiveNode)
    {
        TreeView_MoveRightAction(
            containerKey,
            addSelectedNodes,
            selectNodesBetweenCurrentAndNextActiveNode,
            treeViewNoType =>
            {
                Enqueue(new CommonWorkArgs
                {
                    WorkKind = CommonWorkKind.TreeViewService_LoadChildList,
                    ContainerKey = containerKey,
                    TreeViewNoType = treeViewNoType,
                });
            });
    }

    public string TreeView_GetActiveNodeElementId(Key<TreeViewContainer> containerKey)
    {
        var inState = GetTreeViewState();        TreeViewContainer? inContainer = null;
        foreach (var c in inState.ContainerList)        {            if (c.Key == containerKey)            {                inContainer = c;                break;            }        }
        if (inContainer is not null)
            return inContainer.ActiveNodeElementId;
        
        return string.Empty;
    }
        
    public void TreeView_RegisterContainerAction(TreeViewContainer container, bool shouldFireStateChangedEvent = true)
    {
        if (container.Key == Key<TreeViewContainer>.Empty)
            throw new NotImplementedException("container.Key == Key<TreeViewContainer>.Empty; must not be true");
    
        var inState = GetTreeViewState();        TreeViewContainer? inContainer = null;
        foreach (var c in inState.ContainerList)        {            if (c.Key == container.Key)            {                inContainer = c;                break;            }        }

        if (inContainer is not null)
        {
            CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);
            return;
        }

        var outContainerList = new List<TreeViewContainer>(inState.ContainerList);
        outContainerList.Add(container);
        
        _treeViewState = inState with { ContainerList = outContainerList };
        
        if (shouldFireStateChangedEvent)
            CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);
    }

    public void TreeView_DisposeContainerAction(Key<TreeViewContainer> containerKey, bool shouldFireStateChangedEvent = true)
    {
        var inState = GetTreeViewState();
    
        var indexContainer = inState.ContainerList.FindIndex(
            x => x.Key == containerKey);

        if (indexContainer == -1)
        {
            return;
        }
        
        var outContainerList = new List<TreeViewContainer>(inState.ContainerList);
        outContainerList.RemoveAt(indexContainer);
        
        _treeViewState = inState with { ContainerList = outContainerList };
        
        if (shouldFireStateChangedEvent)
            CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);
    }

    public void TreeView_WithRootNodeAction(Key<TreeViewContainer> containerKey, TreeViewNodeValue node, bool shouldFireStateChangedEvent = true)
    {
        /*
        // 2025-10-22 (rewrite TreeViews)
        var inState = GetTreeViewState();
    
        var indexContainer = inState.ContainerList.FindIndex(
            x => x.Key == containerKey);

        if (indexContainer == -1)
        {
            CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);
            return;
        }
        
        var inContainer = inState.ContainerList[indexContainer];
        
        var outContainer = inContainer with
        {
            RootNode = node,
            SelectedNodeList = new List<TreeViewNoType>() { node }
        };

        var outContainerList = new List<TreeViewContainer>(inState.ContainerList);
        outContainerList[indexContainer] = outContainer;
        
        _treeViewState = inState with { ContainerList = outContainerList };
        
        if (shouldFireStateChangedEvent)
            CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);
        */
    }

    public void TreeView_AddChildNodeAction(Key<TreeViewContainer> containerKey, TreeViewNodeValue parentNode, TreeViewNodeValue childNode)
    {
        /*
        // 2025-10-22 (rewrite TreeViews)
        var inState = GetTreeViewState();        TreeViewContainer? inContainer = null;
        foreach (var c in inState.ContainerList)        {            if (c.Key == containerKey)            {                inContainer = c;                break;            }        }
        if (inContainer is null)
        {
            CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);
            return;
        }

        var parent = parentNode;
        var child = childNode;

        child.Parent = parent;
        child.IndexAmongSiblings = parent.ChildListLength;
        
        inContainer.ChildList.Add(child);
        ++child.ChildListLength;

        TreeView_ReRenderNodeAction(containerKey, parent);
        return;
        */
    }

    public void TreeView_ReRenderNodeAction()    {        CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);    }

    public void TreeView_ReRenderNodeAction(Key<TreeViewContainer> containerKey, TreeViewNodeValue node, bool flatListChanged = false)
    {
        /*
        // 2025-10-22 (rewrite TreeViews)
        var inState = GetTreeViewState();
    
        var indexContainer = inState.ContainerList.FindIndex(
            x => x.Key == containerKey);
        
        if (indexContainer == -1)
        {
            CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);
            return;
        }

        var inContainer = inState.ContainerList[indexContainer];
        
        var outContainer = PerformReRenderNode(inContainer, containerKey, node);
        if (flatListChanged)
            ++outContainer.FlatListVersion;

        var outContainerList = new List<TreeViewContainer>(inState.ContainerList);
        outContainerList[indexContainer] = outContainer;
        
        _treeViewState = inState with { ContainerList = outContainerList };
        CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);
        */
    }

    public void TreeView_SetActiveNodeAction(
        Key<TreeViewContainer> containerKey,
        int indexActiveNode,
        bool addSelectedNodes,
        bool selectNodesBetweenCurrentAndNextActiveNode,
        bool shouldFireStateChangedEvent = true)
    {
        var inState = GetTreeViewState();
    
        var indexContainer = inState.ContainerList.FindIndex(
            x => x.Key == containerKey);

        if (indexContainer == -1)
        {
            CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);
            return;
        }

        var inContainer = inState.ContainerList[indexContainer];

        inContainer.ActiveNodeValueIndex = indexActiveNode;

        if (shouldFireStateChangedEvent)
            CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);        /*var outContainer = PerformSetActiveNode(
            inContainer, containerKey, nextActiveNode, addSelectedNodes, selectNodesBetweenCurrentAndNextActiveNode);

        var outContainerList = new List<TreeViewContainer>(inState.ContainerList);
        outContainerList[indexContainer] = outContainer;

        _treeViewState = inState with { ContainerList = outContainerList };*/    }

    public void TreeView_RemoveSelectedNodeAction(
        Key<TreeViewContainer> containerKey,
        int keyOfNodeToRemove)
    {
        var inState = GetTreeViewState();
    
        var indexContainer = inState.ContainerList.FindIndex(
            x => x.Key == containerKey);

        if (indexContainer == -1)
        {
            CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);
            return;
        }

        var inContainer = inState.ContainerList[indexContainer];
        
        var outContainer = PerformRemoveSelectedNode(inContainer, containerKey, keyOfNodeToRemove);

        var outContainerList = new List<TreeViewContainer>(inState.ContainerList);
            
        outContainerList[indexContainer] = outContainer;

        _treeViewState = inState with { ContainerList = outContainerList };
        CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);
        return;
    }

    public void TreeView_MoveLeftAction(
        Key<TreeViewContainer> containerKey,
        bool addSelectedNodes,
        bool selectNodesBetweenCurrentAndNextActiveNode)
    {
        var inState = GetTreeViewState();
    
        var indexContainer = inState.ContainerList.FindIndex(
            x => x.Key == containerKey);

        if (indexContainer == -1)
        {
            CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);
            return;
        }
            
        var inContainer = inState.ContainerList[indexContainer];
        var activeNodeValueIndex = inContainer.ActiveNodeValueIndex;
        if (inContainer is null ||
            activeNodeValueIndex == -1 ||
            activeNodeValueIndex > inContainer.NodeValueList.Count)
        {
            CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);
            return;
        }
        
        var nodeValue = inContainer.NodeValueList[activeNodeValueIndex];
        if (nodeValue.IsExpanded)
        {
            inContainer.NodeValueList[activeNodeValueIndex] = nodeValue with
            {
                IsExpanded = false
            };
            TreeView_ReRenderNodeAction();
        }
        
        /*var outContainer = PerformMoveLeft(inContainer, containerKey, addSelectedNodes, selectNodesBetweenCurrentAndNextActiveNode);

        var outContainerList = new List<TreeViewContainer>(inState.ContainerList);
        outContainerList[indexContainer] = outContainer;

        _treeViewState = inState with { ContainerList = outContainerList };*/
    }

    public void TreeView_MoveDownAction(
        Key<TreeViewContainer> containerKey,
        bool addSelectedNodes,
        bool selectNodesBetweenCurrentAndNextActiveNode)
    {
        var inState = GetTreeViewState();        var indexContainer = inState.ContainerList.FindIndex(
            x => x.Key == containerKey);

        if (indexContainer == -1)
        {
            CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);
            return;
        }

        var inContainer = inState.ContainerList[indexContainer];
        if (inContainer is null)
        {
            CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);
            return;
        }

        ++inContainer.ActiveNodeValueIndex;

        CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);

        /*
        // 2025-10-22 (rewrite TreeViews)
        var outContainer = PerformMoveDown(inContainer, containerKey, addSelectedNodes, selectNodesBetweenCurrentAndNextActiveNode);

        var outContainerList = new List<TreeViewContainer>(inState.ContainerList);
        outContainerList[indexContainer] = outContainer;
        
        _treeViewState = inState with { ContainerList = outContainerList };
        CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);
        return;
        */
    }

    public void TreeView_MoveUpAction(
        Key<TreeViewContainer> containerKey,
        bool addSelectedNodes,
        bool selectNodesBetweenCurrentAndNextActiveNode)
    {
        /*
        // 2025-10-22 (rewrite TreeViews)
        var inState = GetTreeViewState();
    
        var indexContainer = inState.ContainerList.FindIndex(
            x => x.Key == containerKey);

        if (indexContainer == -1)
        {
            CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);
            return;
        }

        var inContainer = inState.ContainerList[indexContainer];
        
        var outContainer = PerformMoveUp(inContainer, containerKey, addSelectedNodes, selectNodesBetweenCurrentAndNextActiveNode);

        var outContainerList = new List<TreeViewContainer>(inState.ContainerList);
        outContainerList[indexContainer] = outContainer;
        
        _treeViewState = inState with { ContainerList = outContainerList };
        CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);
        return;
        */
    }

    public void TreeView_MoveRightAction(
        Key<TreeViewContainer> containerKey,
        bool addSelectedNodes,
        bool selectNodesBetweenCurrentAndNextActiveNode,
        Action<TreeViewNodeValue> loadChildListAction)
    {
        /*
        // 2025-10-22 (rewrite TreeViews)
        var inState = GetTreeViewState();
    
        var indexContainer = inState.ContainerList.FindIndex(
            x => x.Key == containerKey);
        
        if (indexContainer == -1)
        {
            CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);
            return;
        }
        
        var inContainer = inState.ContainerList[indexContainer];
            
        if (inContainer?.ActiveNode is null)
        {
            CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);
            return;
        }

        var outContainer = PerformMoveRight(inContainer, containerKey, addSelectedNodes, selectNodesBetweenCurrentAndNextActiveNode, loadChildListAction);

        var outContainerList = new List<TreeViewContainer>(inState.ContainerList);
        outContainerList[indexContainer] = outContainer;
        
        _treeViewState = inState with { ContainerList = outContainerList };
        CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);
        return;
        */
    }

    public void TreeView_MoveHomeAction(
        Key<TreeViewContainer> containerKey,
        bool addSelectedNodes,
        bool selectNodesBetweenCurrentAndNextActiveNode)
    {
        /*
        // 2025-10-22 (rewrite TreeViews)
        var inState = GetTreeViewState();
    
        var indexContainer = inState.ContainerList.FindIndex(
            x => x.Key == containerKey);
            
        if (indexContainer == -1)
        {
            CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);
            return;
        }
            
        var inContainer = inState.ContainerList[indexContainer];
        if (inContainer?.ActiveNode is null)
        {
            CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);
            return;
        }

        var outContainer = PerformMoveHome(inContainer, containerKey, addSelectedNodes, selectNodesBetweenCurrentAndNextActiveNode);

        var outContainerList = new List<TreeViewContainer>(inState.ContainerList);
        outContainerList[indexContainer] = outContainer;
        
        _treeViewState = inState with { ContainerList = outContainerList };
        CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);
        return;
        */
    }

    public void TreeView_MoveEndAction(
        Key<TreeViewContainer> containerKey,
        bool addSelectedNodes,
        bool selectNodesBetweenCurrentAndNextActiveNode)
    {
        /*
        // 2025-10-22 (rewrite TreeViews)
        var inState = GetTreeViewState();
    
        var indexContainer = inState.ContainerList.FindIndex(
            x => x.Key == containerKey);
        
        if (indexContainer == -1)
        {
            CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);
            return;
        }
        
        var inContainer = inState.ContainerList[indexContainer];
        if (inContainer?.ActiveNode is null)
        {
            CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);
            return;
        }

        var outContainer = PerformMoveEnd(inContainer, containerKey, addSelectedNodes, selectNodesBetweenCurrentAndNextActiveNode);

        var outContainerList = new List<TreeViewContainer>(inState.ContainerList);
        outContainerList[indexContainer] = outContainer;
        
        _treeViewState = inState with { ContainerList = outContainerList };
        CommonUiStateChanged?.Invoke(CommonUiEventKind.TreeViewStateChanged);
        return;
        */
    }
    
    public int TreeView_GetNextFlatListVersion(Key<TreeViewContainer> containerKey)
    {
        /*
        // 2025-10-22 (rewrite TreeViews)
        var inState = GetTreeViewState();
    
        var indexContainer = inState.ContainerList.FindIndex(
            x => x.Key == containerKey);
        
        if (indexContainer == -1)
        {
            // default(int) + 1
            return 1;
        }
        
        return inState.ContainerList[indexContainer].FlatListVersion + 1;
        */
        return 1;
    }

    private TreeViewContainer PerformReRenderNode(
        TreeViewContainer inContainer,
        Key<TreeViewContainer> containerKey,
        TreeViewNodeValue node)
    {
        /*
        // 2025-10-22 (rewrite TreeViews)
        return inContainer with { StateId = Guid.NewGuid() };
        */
        return inContainer;
    }

    private TreeViewContainer PerformSetActiveNode(
        TreeViewContainer inContainer,
        Key<TreeViewContainer> containerKey,
        TreeViewNodeValue nextActiveNode,
        bool addSelectedNodes,
        bool selectNodesBetweenCurrentAndNextActiveNode)
    {
        /*
        // 2025-10-22 (rewrite TreeViews)
        var inSelectedNodeList = inContainer.SelectedNodeList;
        var selectedNodeListWasCleared = false;

        TreeViewContainer outContainer;

        // TODO: I'm adding multi-select. I'd like to single out the...
        // ...SelectNodesBetweenCurrentAndNextActiveNode case for now...
        // ...and DRY the code after. (2024-01-13) 
        if (selectNodesBetweenCurrentAndNextActiveNode)
        {
            outContainer = inContainer;
            int direction;

            // Step 1: Determine the selection's direction.
            //
            // That is to say, on the UI, which node appears
            // vertically closer to the root node.
            //
            // Process: -Discover the closest common ancestor node.
            //          -Then backtrack one node depth.
            //          -One now has the two nodes at which
            //              they differ.
            //          -Compare the 'TreeViewNoType.IndexAmongSiblings'
            //          -Next.IndexAmongSiblings - Current.IndexAmongSiblings
            //              if (difference > 0)
            //                  then: direction is towards end
            //              if (difference < 0)
            //                  then: direction is towards home (AKA root)
            {
                var currentTarget = inContainer.ActiveNode;
                var nextTarget = nextActiveNode;

                while (currentTarget.Parent != nextTarget.Parent)
                {
                    if (currentTarget.Parent is null || nextTarget.Parent is null)
                        break;

                    currentTarget = currentTarget.Parent;
                    nextTarget = nextTarget.Parent;
                }
                
                direction = nextTarget.IndexAmongSiblings - currentTarget.IndexAmongSiblings;
            }

            if (direction > 0)
            {
                // Move down

                var previousNode = outContainer.ActiveNode;

                while (true)
                {
                    outContainer = PerformMoveDown(
                        outContainer,
                        containerKey,
                        true,
                        false);

                    if (previousNode.Key == outContainer.ActiveNode.Key)
                    {
                        // No change occurred, avoid an infinite loop and break
                        break;
                    }
                    else
                    {
                        previousNode = outContainer.ActiveNode;
                    }

                    if (nextActiveNode.Key == outContainer.ActiveNode.Key)
                    {
                        // Target acquired
                        break;
                    }
                }
            }
            else if (direction < 0)
            {
                // Move up

                var previousNode = outContainer.ActiveNode;

                while (true)
                {
                    outContainer = PerformMoveUp(
                        outContainer,
                        containerKey,
                        true,
                        false);

                    if (previousNode.Key == outContainer.ActiveNode.Key)
                    {
                        // No change occurred, avoid an infinite loop and break
                        break;
                    }
                    else
                    {
                        previousNode = outContainer.ActiveNode;
                    }

                    if (nextActiveNode.Key == outContainer.ActiveNode.Key)
                    {
                        // Target acquired
                        break;
                    }
                }
            }
            else
            {
                // The next target is the same as the current target.
                return outContainer;
            }
        }
        else
        {
            if (nextActiveNode is null)
            {
                selectedNodeListWasCleared = true;

                outContainer = inContainer with
                {
                    SelectedNodeList = Array.Empty<TreeViewNoType>()
                };
            }
            else if (!addSelectedNodes)
            {
                selectedNodeListWasCleared = true;

                outContainer = inContainer with
                {
                    SelectedNodeList = new List<TreeViewNoType>()
                    {
                        nextActiveNode
                    }
                };
            }
            else
            {
                var alreadyExistingIndex = inContainer.SelectedNodeList.FindIndex(
                    x => nextActiveNode.Equals(x));
                
                if (alreadyExistingIndex != -1)
                {
                    var outSelectedNodeList = new List<TreeViewNoType>(inContainer.SelectedNodeList);
                    outSelectedNodeList.RemoveAt(alreadyExistingIndex);
                
                    inContainer = inContainer with
                    {
                        SelectedNodeList = outSelectedNodeList
                    };
                }

                // Variable name collision on 'outSelectedNodeLists'.
                {
                    var outSelectedNodeList = new List<TreeViewNoType>(inContainer.SelectedNodeList);
                    outSelectedNodeList.Insert(0, nextActiveNode);
                    
                    outContainer = inContainer with
                    {
                        SelectedNodeList = outSelectedNodeList
                    };
                }
            }
        }
        
        return outContainer;
        */
        return inContainer;
    }
    
    private TreeViewContainer PerformRemoveSelectedNode(
        TreeViewContainer inContainer,
        Key<TreeViewContainer> containerKey,
        int keyOfNodeToRemove)
    {
        /*
        // 2025-10-22 (rewrite TreeViews)
        var indexOfNodeToRemove = inContainer.SelectedNodeList.FindIndex(
            x => x.Key == keyOfNodeToRemove);

        var outSelectedNodeList = new List<TreeViewNoType>(inContainer.SelectedNodeList);
        outSelectedNodeList.RemoveAt(indexOfNodeToRemove);

        return inContainer with
        {
            SelectedNodeList = outSelectedNodeList
        };
        */
        return inContainer;
    }
    
    private TreeViewContainer PerformMoveLeft(
        TreeViewContainer inContainer,
        Key<TreeViewContainer> containerKey,
        bool addSelectedNodes,
        bool selectNodesBetweenCurrentAndNextActiveNode)
    {
        /*
        // 2025-10-22 (rewrite TreeViews)
        var outContainer = inContainer;

        if (addSelectedNodes)
            return outContainer;

        if (outContainer.ActiveNode is null)
            return outContainer;

        if (outContainer.ActiveNode.IsExpanded &&
            outContainer.ActiveNode.IsExpandable)
        {
            outContainer.ActiveNode.IsExpanded = false;
            ++outContainer.FlatListVersion;
            return PerformReRenderNode(outContainer, outContainer.Key, outContainer.ActiveNode);
        }

        if (outContainer.ActiveNode.Parent is not null)
        {
            outContainer = PerformSetActiveNode(
                outContainer,
                outContainer.Key,
                outContainer.ActiveNode.Parent,
                false,
                false);
            // Do not increment 'outContainer.FlatListVersion' here, it creates a janky visual bug
            // when this case is hit.
            //
            // There already exists code that will scroll the active node into view in this scenario.
            // I believe they are competing for the scroll position and it looks quite bad.
        }

        return outContainer;
        */
        return inContainer;
    }

    private TreeViewContainer PerformMoveDown(
        TreeViewContainer inContainer,
        Key<TreeViewContainer> containerKey,
        bool addSelectedNodes,
        bool selectNodesBetweenCurrentAndNextActiveNode)
    {
        /*
        // 2025-10-22 (rewrite TreeViews)
        var outContainer = inContainer;

        if (outContainer.ActiveNode.IsExpanded &&
            outContainer.ActiveNode.ChildListLength > 0)
        {
            var nextActiveNode = outContainer.ChildList[outContainer.ActiveNode.ChildListOffset];

            outContainer = PerformSetActiveNode(
                outContainer,
                outContainer.Key,
                nextActiveNode,
                addSelectedNodes,
                selectNodesBetweenCurrentAndNextActiveNode);
        }
        else
        {
            var target = outContainer.ActiveNode;

            while (target.Parent is not null &&
                   target.IndexAmongSiblings == target.Parent.ChildListLength - 1)
            {
                target = target.Parent;
            }

            if (target.Parent is null ||
                target.IndexAmongSiblings == target.Parent.ChildListLength - 1)
            {
                return outContainer;
            }

            var nextActiveNode = outContainer.ChildList[target.Parent.ChildListOffset + target.IndexAmongSiblings + 1];

            outContainer = PerformSetActiveNode(
                outContainer,
                outContainer.Key,
                nextActiveNode,
                addSelectedNodes,
                selectNodesBetweenCurrentAndNextActiveNode);
        }

        return outContainer;
        */
        return inContainer;
    }

    private TreeViewContainer PerformMoveUp(
        TreeViewContainer inContainer,
        Key<TreeViewContainer> containerKey,
        bool addSelectedNodes,
        bool selectNodesBetweenCurrentAndNextActiveNode)
    {
        /*
        // 2025-10-22 (rewrite TreeViews)
        var outContainer = inContainer;

        if (outContainer?.ActiveNode?.Parent is null)
            return outContainer;

        if (outContainer.ActiveNode.IndexAmongSiblings == 0)
        {
            outContainer = PerformSetActiveNode(
                outContainer,
                outContainer.Key,
                outContainer.ActiveNode!.Parent,
                addSelectedNodes,
                selectNodesBetweenCurrentAndNextActiveNode);
        }
        else
        {
            var target = outContainer.ChildList[outContainer.ActiveNode.Parent.ChildListOffset + outContainer.ActiveNode.IndexAmongSiblings - 1];

            while (true)
            {
                if (target.IsExpanded &&
                    target.ChildListLength > 0)
                {
                    target = outContainer.ChildList[target.ChildListOffset + target.ChildListLength - 1];
                }
                else
                {
                    break;
                }
            }

            outContainer = PerformSetActiveNode(
                outContainer,
                outContainer.Key,
                target,
                addSelectedNodes,
                selectNodesBetweenCurrentAndNextActiveNode);
        }

        return outContainer;
        */
        return inContainer;
    }

    private TreeViewContainer PerformMoveRight(
        TreeViewContainer inContainer,
        Key<TreeViewContainer> containerKey,
        bool addSelectedNodes,
        bool selectNodesBetweenCurrentAndNextActiveNode,
        Action<TreeViewNodeValue> loadChildListAction)
    {
        /*
        // 2025-10-22 (rewrite TreeViews)
        var outContainer = inContainer;

        if (outContainer is null || outContainer.ActiveNode is null)
            return outContainer;

        if (addSelectedNodes)
            return outContainer;

        if (outContainer.ActiveNode is null)
            return outContainer;

        if (outContainer.ActiveNode.IsExpanded)
        {
            if (outContainer.ActiveNode.ChildListLength > 0)
            {
                outContainer = PerformSetActiveNode(
                    outContainer,
                    outContainer.Key,
                    outContainer.ChildList[outContainer.ActiveNode.ChildListOffset],
                    addSelectedNodes,
                    selectNodesBetweenCurrentAndNextActiveNode);
            }
        }
        else if (outContainer.ActiveNode.IsExpandable)
        {
            outContainer.ActiveNode.IsExpanded = true;

            loadChildListAction.Invoke(outContainer.ActiveNode);
        }

        return outContainer;
        */
        return inContainer;
    }

    private TreeViewContainer PerformMoveHome(
        TreeViewContainer inContainer,
        Key<TreeViewContainer> containerKey,
        bool addSelectedNodes,
        bool selectNodesBetweenCurrentAndNextActiveNode)
    {
        /*
        // 2025-10-22 (rewrite TreeViews)
        var outContainer = inContainer;

        TreeViewNoType target;

        if (outContainer.RootNode is TreeViewAdhoc)
        {
            if (outContainer.RootNode.ChildListLength > 0)
                target = outContainer.ChildList[outContainer.RootNode.ChildListOffset];
            else
                target = outContainer.RootNode;
        }
        else
        {
            target = outContainer.RootNode;
        }

        return PerformSetActiveNode(
            outContainer,
            outContainer.Key,
            target,
            addSelectedNodes,
            selectNodesBetweenCurrentAndNextActiveNode);
        */
        return inContainer;
    }

    private TreeViewContainer PerformMoveEnd(
        TreeViewContainer inContainer,
        Key<TreeViewContainer> containerKey,
        bool addSelectedNodes,
        bool selectNodesBetweenCurrentAndNextActiveNode)
    {
        /*
        // 2025-10-22 (rewrite TreeViews)
        var outContainer = inContainer;

        var target = outContainer.RootNode;

        while (target.IsExpanded && target.ChildListLength > 0)
        {
            target = inContainer.ChildList[target.ChildListOffset + target.ChildListLength - 1];
        }

        return PerformSetActiveNode(
            outContainer,
            outContainer.Key,
            target,
            addSelectedNodes,
            selectNodesBetweenCurrentAndNextActiveNode);
        */
        return inContainer;
    }
    
    // TODO: Clearing logic
    public Dictionary<int, string> IntToCssValueCache = new();
    
    /// <summary>This method should only be invoked by the "UI thread"</summary>
    public string TreeView_GetNodeTextStyle(int clairTreeViewIconWidth)
    {
        if (!IntToCssValueCache.ContainsKey(clairTreeViewIconWidth))
            IntToCssValueCache.Add(clairTreeViewIconWidth, clairTreeViewIconWidth.ToCssValue());
        
        UiStringBuilder.Clear();
        UiStringBuilder.Append("width: calc(100% - ");
        UiStringBuilder.Append(IntToCssValueCache[clairTreeViewIconWidth]);
        UiStringBuilder.Append("px); height:  100%;");
        
        return UiStringBuilder.ToString();
    }
    
    /// <summary>This method should only be invoked by the "UI thread"</summary>
    public string TreeView_GetNodeBorderStyle(int offsetInPixels, int clairTreeViewIconWidth)
    {
        var result = offsetInPixels + clairTreeViewIconWidth / 2;
        
        if (!IntToCssValueCache.ContainsKey(result))
            IntToCssValueCache.Add(result, result.ToCssValue());
    
        UiStringBuilder.Clear();
        UiStringBuilder.Append("margin-left: ");
        UiStringBuilder.Append(result);
        UiStringBuilder.Append("px;");
        
        return UiStringBuilder.ToString();
    }
}
