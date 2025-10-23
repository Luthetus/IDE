using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.JavaScriptObjects.Models;
using Clair.Common.RazorLib.Commands.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.Common.RazorLib.Dimensions.Models;
using Clair.Common.RazorLib.BackgroundTasks.Models;

namespace Clair.Common.RazorLib.TreeViews.Displays;

public sealed partial class TreeViewContainerDisplay : ComponentBase, IDisposable
{
    [Inject]
    private CommonService CommonService { get; set; } = null!;

    [Parameter, EditorRequired]
    public Key<TreeViewContainer> TreeViewContainerKey { get; set; }
    
    private int OffsetPerDepthInPixels => (int)Math.Ceiling(
        CommonService.GetAppOptionsState().Options.IconSizeInPixels * (2.0 / 3.0));
    
    private int ClairTreeViewIconWidth => CommonService.GetAppOptionsState().Options.IconSizeInPixels;
    
    /// <summary>Pixels</summary>
    private int LineHeight => CommonService.Options_LineHeight;
    
    private Guid _guidId = Guid.NewGuid();
    private string _htmlId = null!;
    
    /// <summary>
    /// UI thread only.
    ///
    /// This property is somewhat-awkwardly set from the '.razor'.
    /// All usage of this is expected to be on UI thread for that reason.
    /// If doing async work, be sure to NOT use '.ConfigureAwait(false)'
    /// if you intend to use this property after the task finishes.
    /// </summary>
    private int VirtualIndexActiveNode { get; set; }

    private TreeViewCommandArgs _treeViewContextMenuCommandArgs;
    private TreeViewContainer _treeViewContainer;
    private TreeViewMeasurements _treeViewMeasurements;
    private DotNetObjectReference<TreeViewContainerDisplay>? _dotNetHelper;
    
    private double _seenScrollLeft;
    private double _seenScrollTop;
    
    /// <summary>
    /// UI thread only.
    /// </summary>
    private readonly List<int> _indexVirtualizedNodeValueList = new();
    /// <summary>
    /// UI thread only.
    /// Contains the "used to be" targetNode, and the index that it left off at.
    /// </summary>
    private readonly Stack<(TreeViewNodeValue Node, int Index)> _nodeRecursionStack = new();
    
    private Key<TreeViewContainer> _previousTreeViewContainerKey = Key<TreeViewContainer>.Empty;
    private int _previousContainerVersion;
    private bool _shouldValidateScrollbarOnAfterRender;

    protected override void OnInitialized()
    {
        // TODO: Does the object used here matter? Should it be a "smaller" object or is this just reference?
        _dotNetHelper = DotNetObjectReference.Create(this);
    
        _htmlId = $"luth_common_treeview-{_guidId}";
        
        CommonService.CommonUiStateChanged += OnTreeViewStateChanged;
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Do not ConfigureAwait(false) so that the UI doesn't change out from under you
            // before you finish setting up the events?
            // (is this a thing, I'm just presuming this would be true).
            _treeViewMeasurements = await CommonService.JsRuntimeCommonApi.JsRuntime.InvokeAsync<TreeViewMeasurements>(
                "clairCommon.treeViewInitialize",
                _dotNetHelper,
                _htmlId);
        }
        
        /*
        if (_treeViewContainer is not null)
        {
            var didValidateScrollbar = false;
            if (_shouldValidateScrollbarOnAfterRender)
            {
                _shouldValidateScrollbarOnAfterRender = false;
                
                _treeViewMeasurements = await CommonService.JsRuntimeCommonApi.JsRuntime.InvokeAsync<TreeViewMeasurements>(
                    "clairCommon.focusAndMeasureTreeView",
                    _htmlId,
                    /*preventScroll:*//* false);
            
                didValidateScrollbar = true;
                ValidateScrollbar();
            }
        
            // It is thought that you shouldn't '.ConfigureAwait(false)' for the scrolling JS Interop,
            // because this could provide a "natural throttle for the scrolling"
            // since more ITextEditorService edit contexts might have time to be calculated
            // and thus not every single one of them need be scrolled to.
            // This idea has not been proven yet.
            //
            // (the same is true for rendering the UI, it might avoid some renders
            //  because the most recent should render took time to get executed).
            
            // WARNING: It is only thread safe to read, then assign `_componentData.ScrollLeftChanged` or `_componentData.ScrollTopChanged`...
            // ...if this method is running synchronously, i.e.: there hasn't been an await.
            // |
            // `if (firstRender)` is the only current scenario where an await comes prior to this read and assign.
            //
            // ScrollLeft is most likely to shortcircuit, thus it is being put first.
            var scroll_LeftChanged = _seenScrollLeft != _treeViewMeasurements.ScrollLeft;
            var scroll_TopChanged = _seenScrollTop != _treeViewMeasurements.ScrollTop;
            
            if (scroll_LeftChanged && scroll_TopChanged)
            {
                _seenScrollLeft = _treeViewMeasurements.ScrollLeft;
                _seenScrollTop = _treeViewMeasurements.ScrollTop;
                
                await CommonService.JsRuntimeCommonApi.JsRuntime.InvokeVoidAsync(
                        "clairCommon.treeViewSetScrollPositionBoth",
                        _htmlId,
                        _treeViewMeasurements.ScrollLeft,
                        _treeViewMeasurements.ScrollTop)
                    .ConfigureAwait(false);
            }
            else if (scroll_TopChanged) // ScrollTop is most likely to come next
            {
                if (_treeViewMeasurements.ScrollTop < 0)
                    _treeViewMeasurements.ScrollTop = 0;
            
                _seenScrollTop = _treeViewMeasurements.ScrollTop;
                
                await CommonService.JsRuntimeCommonApi.JsRuntime.InvokeVoidAsync(
                        "clairCommon.treeViewSetScrollPositionTop",
                        _htmlId,
                        _treeViewMeasurements.ScrollTop)
                    .ConfigureAwait(false);
            }
            else if (scroll_LeftChanged)
            {
                if (_treeViewMeasurements.ScrollLeft < 0)
                    _treeViewMeasurements.ScrollLeft = 0;
            
                _seenScrollLeft = _treeViewMeasurements.ScrollLeft;
                
                await CommonService.JsRuntimeCommonApi.JsRuntime.InvokeVoidAsync(
                        "clairCommon.treeViewSetScrollPositionLeft",
                        _htmlId,
                        _treeViewMeasurements.ScrollLeft)
                    .ConfigureAwait(false);
            }
            
            if (didValidateScrollbar)
            {
                await InvokeAsync(StateHasChanged);
            }
        }
        */
    }
    
    [JSInvokable]
    public async Task ReceiveOnWheel(TreeViewEventArgsMouseDown eventArgsKeyDown)
    {
        if (_treeViewContainer is null)
            return;
        
        _treeViewMeasurements = _treeViewMeasurements with
        {
            // The JS will scroll then provide the updated ScrollTop.
            ScrollTop = eventArgsKeyDown.ScrollTop,
            ViewWidth = eventArgsKeyDown.ViewWidth,
            ViewHeight = eventArgsKeyDown.ViewHeight,
            ScrollWidth = eventArgsKeyDown.ScrollWidth,
            ScrollHeight = eventArgsKeyDown.ScrollHeight,
        };
        //Console.WriteLine(eventArgsKeyDown.ScrollTop);
        //ValidateScrollbar();
        StateHasChanged();
    }
    
    [JSInvokable]
    public async Task ReceiveOnKeyDown(TreeViewEventArgsKeyDown eventArgsKeyDown)
    {
        if (_treeViewContainer is null)
            return;

        switch (eventArgsKeyDown.Key)
        {
            case "ContextMenu":
            {
                var mouseEventArgs = new MouseEventArgs { Button = -1 };
                
                ReceiveOnContextMenu(
                    new TreeViewEventArgsMouseDown(
                        Buttons: 0,
                        Button: -1,
                        X: 0,
                        Y: 0,
                        ShiftKey: false,
                        eventArgsKeyDown.ScrollLeft,
                        eventArgsKeyDown.ScrollTop,
                        eventArgsKeyDown.ScrollWidth,
                        eventArgsKeyDown.ScrollHeight,
                        eventArgsKeyDown.ViewWidth,
                        eventArgsKeyDown.ViewHeight,
                        eventArgsKeyDown.BoundingClientRectLeft,
                        eventArgsKeyDown.BoundingClientRectTop));
                return;
            }
            case ".":
            {
                if (eventArgsKeyDown.CtrlKey)
                {
                    ReceiveOnContextMenu(
                        new TreeViewEventArgsMouseDown(
                            Buttons: 0,
                            Button: -1,
                            X: 0,
                            Y: 0,
                            ShiftKey: false,
                            eventArgsKeyDown.ScrollLeft,
                            eventArgsKeyDown.ScrollTop,
                            eventArgsKeyDown.ScrollWidth,
                            eventArgsKeyDown.ScrollHeight,
                            eventArgsKeyDown.ViewWidth,
                            eventArgsKeyDown.ViewHeight,
                            eventArgsKeyDown.BoundingClientRectLeft,
                            eventArgsKeyDown.BoundingClientRectTop));
                }
                return;
            }
            case "F10":
            {
                if (eventArgsKeyDown.ShiftKey)
                {
                    ReceiveOnContextMenu(
                        new TreeViewEventArgsMouseDown(
                            Buttons: 0,
                            Button: -1,
                            X: 0,
                            Y: 0,
                            ShiftKey: false,
                            eventArgsKeyDown.ScrollLeft,
                            eventArgsKeyDown.ScrollTop,
                            eventArgsKeyDown.ScrollWidth,
                            eventArgsKeyDown.ScrollHeight,
                            eventArgsKeyDown.ViewWidth,
                            eventArgsKeyDown.ViewHeight,
                            eventArgsKeyDown.BoundingClientRectLeft,
                            eventArgsKeyDown.BoundingClientRectTop));
                }
                return;
            }
        }
        
        var treeViewCommandArgs = new TreeViewCommandArgs(
            _treeViewContainer,
            default,
            async () =>
            {
                _treeViewMeasurements = await CommonService.JsRuntimeCommonApi.JsRuntime.InvokeAsync<TreeViewMeasurements>(
                    "clairCommon.focusAndMeasureTreeView",
                    _htmlId,
                    /*preventScroll:*/ false);
            },
            null,
            null,
            new KeyboardEventArgs
            {
                Key = eventArgsKeyDown.Key,
                Code = eventArgsKeyDown.Code,
            });

        // Do not ConfigureAwait(false) here, the _flatNodeList is made on the UI thread
        // and after this await we need to read the _flatNodeList to scroll the newly active node into view.
        await _treeViewContainer.OnKeyDownAsync(treeViewCommandArgs);
        
        var treeViewContainerLocal = CommonService.GetTreeViewContainer(TreeViewContainerKey);
        if (treeViewContainerLocal is null)
            return;
        
        _treeViewMeasurements = _treeViewMeasurements with
        {
            ScrollHeight = eventArgsKeyDown.ScrollHeight,
            ScrollWidth = eventArgsKeyDown.ScrollWidth
        };
        
        /*
        for (int i = 0; i < _flatNodeList.Count; i++)
        {
            var node = _flatNodeList[i];
            if (node == treeViewContainerLocal.ActiveNode)
            {
                var top = LineHeight * i;
                
                if (top < eventArgsKeyDown.ScrollTop)
                {
                    _treeViewMeasurements = _treeViewMeasurements with
                    {
                        ScrollTop = _treeViewMeasurements.ScrollTop + (top - eventArgsKeyDown.ScrollTop)
                    };
                }
                else if (top + (2 * LineHeight) > eventArgsKeyDown.ScrollTop + eventArgsKeyDown.ViewHeight)
                {
                    _treeViewMeasurements = _treeViewMeasurements with
                    {
                        ScrollTop = _treeViewMeasurements.ScrollTop + (top - (eventArgsKeyDown.ScrollTop + eventArgsKeyDown.ViewHeight) + (2 * LineHeight))
                    };
                }
                
                ValidateScrollbar();
                StateHasChanged();
                break;
            }
        }
        */
    }
    
    [JSInvokable]
    public void ReceiveOnContextMenu(TreeViewEventArgsMouseDown eventArgsMouseDown)
    {
        /*_treeViewMeasurements = new TreeViewMeasurements(
            eventArgsMouseDown.ViewWidth,
            eventArgsMouseDown.ViewHeight,
            eventArgsMouseDown.BoundingClientRectLeft,
            eventArgsMouseDown.BoundingClientRectTop,
            eventArgsMouseDown.ScrollLeft,
            eventArgsMouseDown.ScrollTop,
            eventArgsMouseDown.ScrollWidth,
            eventArgsMouseDown.ScrollHeight);
        
        if (TreeViewContainerParameter.OnContextMenuFunc is null)
            return;
        
        ContextMenuFixedPosition contextMenuFixedPosition;
        
        TreeViewNoType? contextMenuTarget;
        
        if (eventArgsMouseDown.Button == -1)
        {
            contextMenuTarget = _flatNodeList[IndexActiveNode];
            
            contextMenuFixedPosition = new ContextMenuFixedPosition(
                OccurredDueToMouseEvent: false,
                LeftPositionInPixels: eventArgsMouseDown.BoundingClientRectLeft,
                TopPositionInPixels: eventArgsMouseDown.BoundingClientRectTop + LineHeight + (IndexActiveNode * LineHeight) - eventArgsMouseDown.ScrollTop);
        }
        else if (eventArgsMouseDown.Button == 2)
        {
            var relativeY = eventArgsMouseDown.Y - _treeViewMeasurements.BoundingClientRectTop + eventArgsMouseDown.ScrollTop;
            relativeY = Math.Max(0, relativeY);
            
            var indexLocal = (int)(relativeY / LineHeight);
            
            IndexActiveNode = IndexBasicValidation(indexLocal);
            contextMenuTarget = _flatNodeList[IndexActiveNode];
            
            contextMenuFixedPosition = new ContextMenuFixedPosition(
                OccurredDueToMouseEvent: true,
                LeftPositionInPixels: eventArgsMouseDown.X,
                TopPositionInPixels: eventArgsMouseDown.Y);
        }
        else
        {
            return;
        }
        
        _treeViewContextMenuCommandArgs = new TreeViewCommandArgs(
            _treeViewContainer,
            _flatNodeList[IndexActiveNode],
            async () =>
            {
                _treeViewMeasurements = await CommonService.JsRuntimeCommonApi.JsRuntime.InvokeAsync<TreeViewMeasurements>(
                    "clairCommon.focusAndMeasureTreeView",
                    _htmlId,
                    /*preventScroll:*//* false);
            },
            contextMenuFixedPosition,
            new MouseEventArgs
            {
                ClientX = eventArgsMouseDown.X,
                ClientY = eventArgsMouseDown.Y,
            },
            keyboardEventArgs: null);
    
        CommonService.Enqueue(new CommonWorkArgs
        {
            WorkKind = CommonWorkKind.TreeView_HandleTreeViewOnContextMenu,
            OnContextMenuFunc = TreeViewContainerParameter.OnContextMenuFunc,
            TreeViewContextMenuCommandArgs = _treeViewContextMenuCommandArgs,
        });*/
    }
    
    [JSInvokable]
    public void ReceiveContentOnMouseDown(TreeViewEventArgsMouseDown eventArgsMouseDown)
    {
        _treeViewMeasurements = new TreeViewMeasurements(
            eventArgsMouseDown.ViewWidth,
            eventArgsMouseDown.ViewHeight,
            eventArgsMouseDown.BoundingClientRectLeft,
            eventArgsMouseDown.BoundingClientRectTop,
            eventArgsMouseDown.ScrollLeft,
            eventArgsMouseDown.ScrollTop,
            eventArgsMouseDown.ScrollWidth,
            eventArgsMouseDown.ScrollHeight);

        var relativeY = eventArgsMouseDown.Y - _treeViewMeasurements.BoundingClientRectTop;// + eventArgsMouseDown.ScrollTop;
        relativeY = Math.Max(0, relativeY);
        
        var indexLocal = (int)(relativeY / LineHeight);
        
        VirtualIndexActiveNode = VirtualIndexBasicValidation(indexLocal);
        
        var relativeX = eventArgsMouseDown.X - _treeViewMeasurements.BoundingClientRectLeft + eventArgsMouseDown.ScrollLeft;
        relativeX = Math.Max(0, relativeX);
        
        if (relativeX >= 0 &&
            relativeX <= ClairTreeViewIconWidth)
        {
            HandleChevronOnClick(eventArgsMouseDown);
        }

        /*
        // TODO: Determine why my math is wrong...
        // ...I need to subtract 1.1 for lower bound and subtract 1 for upper bound.
        // So the question is, "Why do I need to add this arbitrary subtractions,
        // and are the arbitrary subtractions different depending on display settings / font sizes / etc...".
        // Given my setup, these arbitrary subtractions make the hitbox "feel" pixel perfect.
        //
        if (relativeX >= (_flatNodeList[IndexActiveNode].Depth * OffsetPerDepthInPixels - 1.1) &&
            relativeX <= (_flatNodeList[IndexActiveNode].Depth * OffsetPerDepthInPixels + ClairTreeViewIconWidth - 1))
        {
            HandleChevronOnClick(eventArgsMouseDown);
        }
        */

        if (_indexVirtualizedNodeValueList[VirtualIndexActiveNode] >= _treeViewContainer.NodeValueList.Count)
            return;

        CommonService.TreeView_SetActiveNodeAction(
            _treeViewContainer.Key,
            _indexVirtualizedNodeValueList[VirtualIndexActiveNode],
            addSelectedNodes: false,
            selectNodesBetweenCurrentAndNextActiveNode: false);
    }
    
    [JSInvokable]
    public async Task ReceiveOnClick(TreeViewEventArgsMouseDown eventArgsMouseDown)
    {
        _treeViewMeasurements = new TreeViewMeasurements(
            eventArgsMouseDown.ViewWidth,
            eventArgsMouseDown.ViewHeight,
            eventArgsMouseDown.BoundingClientRectLeft,
            eventArgsMouseDown.BoundingClientRectTop,
            eventArgsMouseDown.ScrollLeft,
            eventArgsMouseDown.ScrollTop,
            eventArgsMouseDown.ScrollWidth,
            eventArgsMouseDown.ScrollHeight);
    
        var relativeY = eventArgsMouseDown.Y - _treeViewMeasurements.BoundingClientRectTop + eventArgsMouseDown.ScrollTop;
        relativeY = Math.Max(0, relativeY);
        
        var indexLocal = (int)(relativeY / LineHeight);
        
        VirtualIndexActiveNode = VirtualIndexBasicValidation(indexLocal);

        if (_indexVirtualizedNodeValueList[VirtualIndexActiveNode] >= _treeViewContainer.NodeValueList.Count)
            return;

        await _treeViewContainer.OnClickAsync(new TreeViewCommandArgs(
            _treeViewContainer,
            _treeViewContainer.NodeValueList[_indexVirtualizedNodeValueList[VirtualIndexActiveNode]],
            async () =>
            {
                _treeViewMeasurements = await CommonService.JsRuntimeCommonApi.JsRuntime.InvokeAsync<TreeViewMeasurements>(
                    "clairCommon.focusAndMeasureTreeView",
                    _htmlId,
                    /*preventScroll:*/ false);
            },
            contextMenuFixedPosition: null,
            new MouseEventArgs
            {
                ClientX = eventArgsMouseDown.X,
                ClientY = eventArgsMouseDown.Y,
            },
            keyboardEventArgs: null));
    }
    
    [JSInvokable]
    public async Task ReceiveOnDoubleClick(TreeViewEventArgsMouseDown eventArgsMouseDown)
    {
        _treeViewMeasurements = new TreeViewMeasurements(
            eventArgsMouseDown.ViewWidth,
            eventArgsMouseDown.ViewHeight,
            eventArgsMouseDown.BoundingClientRectLeft,
            eventArgsMouseDown.BoundingClientRectTop,
            eventArgsMouseDown.ScrollLeft,
            eventArgsMouseDown.ScrollTop,
            eventArgsMouseDown.ScrollWidth,
            eventArgsMouseDown.ScrollHeight);
    
        var relativeY = eventArgsMouseDown.Y - _treeViewMeasurements.BoundingClientRectTop + eventArgsMouseDown.ScrollTop;
        relativeY = Math.Max(0, relativeY);
        
        var indexLocal = (int)(relativeY / LineHeight);
        
        VirtualIndexActiveNode = VirtualIndexBasicValidation(indexLocal);
        
        if (_indexVirtualizedNodeValueList[VirtualIndexActiveNode] >= _treeViewContainer.NodeValueList.Count)
            return;

        await _treeViewContainer.OnDoubleClickAsync(new TreeViewCommandArgs(
            _treeViewContainer,
            _treeViewContainer.NodeValueList[_indexVirtualizedNodeValueList[VirtualIndexActiveNode]],
            async () =>
            {
                _treeViewMeasurements = await CommonService.JsRuntimeCommonApi.JsRuntime.InvokeAsync<TreeViewMeasurements>(
                    "clairCommon.focusAndMeasureTreeView",
                    _htmlId,
                    /*preventScroll:*/ false);
            },
            contextMenuFixedPosition: null,
            new MouseEventArgs
            {
                ClientX = eventArgsMouseDown.X,
                ClientY = eventArgsMouseDown.Y,
            },
            keyboardEventArgs: null));
    }
    
    private int VirtualIndexBasicValidation(int virtualIndexLocal)
    {
        if (virtualIndexLocal < 0)
            return 0;
        else if (virtualIndexLocal >= _indexVirtualizedNodeValueList.Count)
            return _indexVirtualizedNodeValueList.Count - 1;
        
        return virtualIndexLocal;
    }
    
    private Task HandleChevronOnClick(TreeViewEventArgsMouseDown eventArgsMouseDown)
    {
        return _treeViewContainer.ToggleExpansion(VirtualIndexActiveNode);
        /*var localTreeViewNoType = _flatNodeList[IndexActiveNode];
        
        if (!localTreeViewNoType.IsExpandable)
            return;

        localTreeViewNoType.IsExpanded = !localTreeViewNoType.IsExpanded;

        if (localTreeViewNoType.IsExpanded)
        {
            CommonService.Enqueue(new CommonWorkArgs
            {
                WorkKind = CommonWorkKind.TreeView_HandleExpansionChevronOnMouseDown,
                IndexNodeValue = eventArgsMouseDown.localTreeViewNoType,
                TreeViewContainer = _treeViewContainer
            });
        }
        else
        {
            CommonService.TreeView_ReRenderNodeAction(_treeViewContainer.Key, localTreeViewNoType, flatListChanged: true);
        }*/
    }

    private string GetHasActiveNodeCssClass(TreeViewContainer? treeViewContainer)
    {
        /*
        // 2025-10-22 (rewrite TreeViews)
        if (treeViewContainer?.ActiveNode is null)
            return string.Empty;

        return "ci_active";
        */
        return string.Empty;
    }
    
    public async void OnTreeViewStateChanged(CommonUiEventKind commonUiEventKind)
    {
        if (commonUiEventKind == CommonUiEventKind.TreeViewStateChanged)
        {
            await InvokeAsync(StateHasChanged);
        }
    }
    
    /// <summary>
    /// This method should only be invoked from the "UI thread" due to the usage of `CommonBackgroundTaskApi.UiStringBuilder`.
    /// </summary>
    private string GetContainerElementCssClass(TreeViewContainer treeViewContainer)
    {
        CommonService.UiStringBuilder.Clear();
        CommonService.UiStringBuilder.Append("ci_tree-view-state ci_unselectable ");
        CommonService.UiStringBuilder.Append(GetHasActiveNodeCssClass(treeViewContainer));
        
        return CommonService.UiStringBuilder.ToString();
    }

    /// <summary>
    /// This method should only be invoked from the "UI thread" due to the usage of `CommonBackgroundTaskApi.UiStringBuilder`.
    /// </summary>
    private string GetNodeElementCssStyle(TreeViewNodeValue node, int depth)
    {
        if (!CommonService.IntToCssValueCache.ContainsKey(depth * OffsetPerDepthInPixels))
            CommonService.IntToCssValueCache.Add(depth * OffsetPerDepthInPixels, (depth * OffsetPerDepthInPixels).ToCssValue());
    
        CommonService.UiStringBuilder.Clear();
        CommonService.UiStringBuilder.Append("height: ");
        CommonService.UiStringBuilder.Append(CommonService.Options_LineHeight_CssStyle);
        CommonService.UiStringBuilder.Append("px; ");

        CommonService.UiStringBuilder.Append("padding-left: ");
        CommonService.UiStringBuilder.Append(CommonService.IntToCssValueCache[depth * OffsetPerDepthInPixels]);
        CommonService.UiStringBuilder.Append("px; ");
        CommonService.UiStringBuilder.Append(CommonService.Options_LineHeight_CssStyle);
        
        return CommonService.UiStringBuilder.ToString();
    }
    
    private void ValidateScrollbar()
    {
        if (_treeViewMeasurements.ScrollLeft + _treeViewMeasurements.ViewWidth > _treeViewMeasurements.ScrollWidth)
        {
            _treeViewMeasurements = _treeViewMeasurements with
            {
                ScrollLeft = _treeViewMeasurements.ScrollWidth - _treeViewMeasurements.ViewWidth
            };
        }
        if (_treeViewMeasurements.ScrollTop + _treeViewMeasurements.ViewHeight > _treeViewMeasurements.ScrollHeight)
        {
            _treeViewMeasurements = _treeViewMeasurements with
            {
                ScrollTop = _treeViewMeasurements.ScrollHeight - _treeViewMeasurements.ViewHeight
            };
        }
    
        if (_treeViewMeasurements.ScrollLeft < 0)
        {
            _treeViewMeasurements = _treeViewMeasurements with
            {
                ScrollLeft = 0
            };
        }
        if (_treeViewMeasurements.ScrollTop < 0)
        {
            _treeViewMeasurements = _treeViewMeasurements with
            {
                ScrollTop = 0
            };
        }
    }
    
    public void Dispose()
    {
        CommonService.CommonUiStateChanged -= OnTreeViewStateChanged;
        _dotNetHelper?.Dispose();
    }
}
