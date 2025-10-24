using System.Text;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components.Web;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.BackgroundTasks.Models;
using Clair.Common.RazorLib.Installations.Models;
using Clair.Common.RazorLib.Tabs.Models;
using Clair.Common.RazorLib.Commands.Models;
using Clair.Common.RazorLib.TreeViews.Models;

namespace Clair.Common.RazorLib;

public partial class CommonService
{
    private readonly ConcurrentQueue<CommonWorkArgs> _workQueue = new();
    
    public Key<IBackgroundTaskGroup> BackgroundTaskKey { get; } = Key<IBackgroundTaskGroup>.NewKey();
    
    public bool __TaskCompletionSourceWasCreated { get; set; }
    
    /// <summary>
    /// Clear() prior to use.
    /// Only use from "UI Thread".
    /// </summary>
    public StringBuilder UiStringBuilder { get; } = new();
    
    public ClairCommonConfig CommonConfig { get; }

    public void Enqueue(CommonWorkArgs commonWorkArgs)
    {
        _workQueue.Enqueue(commonWorkArgs);
        Continuous_Enqueue(this);
    }

    public async ValueTask Do_WriteToLocalStorage(string key, object value)
    {
        var valueJson = System.Text.Json.JsonSerializer.Serialize(value);
        await Storage_SetValue(key, valueJson).ConfigureAwait(false);
    }

    public async ValueTask Do_Tab_ManuallyPropagateOnContextMenu(
        Func<TabContextMenuEventArgs, Task> localHandleTabButtonOnContextMenu, TabContextMenuEventArgs tabContextMenuEventArgs)
    {
        await localHandleTabButtonOnContextMenu.Invoke(tabContextMenuEventArgs).ConfigureAwait(false);
    }

    public async ValueTask Do_TreeView_HandleTreeViewOnContextMenu(
        Func<TreeViewCommandArgs, Task>? onContextMenuFunc, TreeViewCommandArgs treeViewContextMenuCommandArgs)
    {
        if (onContextMenuFunc is not null)
        {
            await onContextMenuFunc
                .Invoke(treeViewContextMenuCommandArgs)
                .ConfigureAwait(false);
        }
    }

    public async ValueTask Do_TreeView_HandleExpansionChevronOnMouseDown(int indexNodeValue, TreeViewContainer treeViewContainer)
    {
        await treeViewContainer.LoadChildListAsync(indexNodeValue).ConfigureAwait(false);
        TreeView_ReRenderNodeAction();
    }

    public async ValueTask Do_TreeView_ManuallyPropagateOnContextMenu(Func<MouseEventArgs?, Key<TreeViewContainer>, TreeViewNodeValue, Task> handleTreeViewOnContextMenu, MouseEventArgs mouseEventArgs, Key<TreeViewContainer> key, TreeViewNodeValue treeViewNoType)
    {
        await handleTreeViewOnContextMenu.Invoke(
                mouseEventArgs,
                key,
                treeViewNoType)
            .ConfigureAwait(false);
    }

    public async ValueTask Do_TreeViewService_LoadChildList(Key<TreeViewContainer> containerKey, TreeViewNodeValue treeViewNoType)
    {
        /*
        // 2025-10-22 (rewrite TreeViews)
        try
        {
            var inState = GetTreeViewState();
    
            var indexContainer = inState.ContainerList.FindIndex(
                x => x.Key == containerKey);
    
            if (indexContainer == -1)
                return;
    
            var inContainer = inState.ContainerList[indexContainer];
        
            await treeViewNoType.LoadChildListAsync(inContainer).ConfigureAwait(false);

            TreeView_ReRenderNodeAction(
                containerKey,
                treeViewNoType,
                flatListChanged: true);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        */
    }
    
    public ValueTask HandleEvent()
    {
        if (!_workQueue.TryDequeue(out CommonWorkArgs workArgs))
            return ValueTask.CompletedTask;
            
        switch (workArgs.WorkKind)
        {
            case CommonWorkKind.WriteToLocalStorage:
                return Do_WriteToLocalStorage(workArgs.WriteToLocalStorage_Key, workArgs.WriteToLocalStorage_Value);
            case CommonWorkKind.Tab_ManuallyPropagateOnContextMenu:
                return Do_Tab_ManuallyPropagateOnContextMenu(workArgs.HandleTabButtonOnContextMenu, workArgs.TabContextMenuEventArgs);
            case CommonWorkKind.TreeView_HandleTreeViewOnContextMenu:
                return Do_TreeView_HandleTreeViewOnContextMenu(workArgs.OnContextMenuFunc, workArgs.TreeViewContextMenuCommandArgs);
            case CommonWorkKind.TreeView_HandleExpansionChevronOnMouseDown:
                return Do_TreeView_HandleExpansionChevronOnMouseDown(workArgs.IndexNodeValue, workArgs.TreeViewContainer);
            case CommonWorkKind.TreeView_ManuallyPropagateOnContextMenu:
                return Do_TreeView_ManuallyPropagateOnContextMenu(workArgs.HandleTreeViewOnContextMenu, workArgs.MouseEventArgs, workArgs.ContainerKey, workArgs.TreeViewNoType);
            case CommonWorkKind.TreeViewService_LoadChildList:
                return Do_TreeViewService_LoadChildList(workArgs.ContainerKey, workArgs.TreeViewNoType);
            default:
                Console.WriteLine($"{nameof(CommonService)} {nameof(HandleEvent)} default case");
                return ValueTask.CompletedTask;
        }
    }
}
