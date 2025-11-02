using Microsoft.AspNetCore.Components;
using Clair.Common.RazorLib.Commands.Models;
using Clair.Common.RazorLib.Menus.Models;
using Clair.Common.RazorLib.Dropdowns.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.Common.RazorLib.Keys.Models;

namespace Clair.Ide.RazorLib.FolderExplorers.Displays;

public partial class FolderExplorerContextMenu : ComponentBase
{
    [Inject]
    private IdeService IdeService { get; set; } = null!;

    [Parameter, EditorRequired]
    public TreeViewCommandArgs TreeViewCommandArgs { get; set; }

    public static readonly Key<DropdownRecord> ContextMenuEventDropdownKey = Key<DropdownRecord>.NewKey();

    private (TreeViewCommandArgs treeViewCommandArgs, MenuContainer menuRecord) _previousGetMenuRecordInvocation;

    private MenuContainer GetMenuRecord(TreeViewCommandArgs treeViewCommandArgs)
    {
        if (_previousGetMenuRecordInvocation.treeViewCommandArgs == treeViewCommandArgs)
            return _previousGetMenuRecordInvocation.menuRecord;

        // ----------------------------------------------------------------------
        var menuRecord = new MenuContainer(MenuContainer.NoMenuOptionsExistList);
            _previousGetMenuRecordInvocation = (treeViewCommandArgs, menuRecord);
            return menuRecord;
        // ---------------------------------------------------------------------- 

        /*
        // 2025-10-22 (rewrite TreeViews)
        if (treeViewCommandArgs.NodeThatReceivedMouseEvent is null)
        {
            var menuRecord = new MenuRecord(MenuRecord.NoMenuOptionsExistList);
            _previousGetMenuRecordInvocation = (treeViewCommandArgs, menuRecord);
            return menuRecord;
        }
        *//*// 2025-10-22 (rewrite TreeViews)

        var menuRecordsList = new List<MenuOptionRecord>();

        var treeViewModel = treeViewCommandArgs.NodeThatReceivedMouseEvent;
        var parentTreeViewModel = treeViewModel.Parent;

        var parentTreeViewAbsolutePath = parentTreeViewModel as TreeViewAbsolutePath;

        if (treeViewModel is not TreeViewAbsolutePath treeViewAbsolutePath)
        {
            var menuRecord = new MenuRecord(MenuRecord.NoMenuOptionsExistList);
            _previousGetMenuRecordInvocation = (treeViewCommandArgs, menuRecord);
            return menuRecord;
        }

        if (treeViewAbsolutePath.Item.IsDirectory)
        {
            menuRecordsList.AddRange(GetFileMenuOptions(treeViewAbsolutePath, parentTreeViewAbsolutePath)
                .Union(GetDirectoryMenuOptions(treeViewAbsolutePath))
                .Union(GetDebugMenuOptions(treeViewAbsolutePath)));
        }
        else
        {
            menuRecordsList.AddRange(GetFileMenuOptions(treeViewAbsolutePath, parentTreeViewAbsolutePath)
                .Union(GetDebugMenuOptions(treeViewAbsolutePath)));
        }

        // Default case
        {
            var menuRecord = new MenuRecord(menuRecordsList);
            _previousGetMenuRecordInvocation = (treeViewCommandArgs, menuRecord);
            return menuRecord;
        }*/
    }

    private MenuOptionValue[] GetDirectoryMenuOptions(TreeViewNodeValue treeViewModel)
    {
        return new MenuOptionValue[]
        {
            /*
            // 2025-10-22 (rewrite TreeViews)
            IdeService.NewEmptyFile(
                treeViewModel.Item,
                async () => await ReloadTreeViewModel(treeViewModel).ConfigureAwait(false)),
            IdeService.NewDirectory(
                treeViewModel.Item,
                async () => await ReloadTreeViewModel(treeViewModel).ConfigureAwait(false)),
            IdeService.PasteClipboard(
                treeViewModel.Item,
                async () => 
                {
                    var localParentOfCutFile = IdeService.CommonService.ParentOfCutFile;
                    IdeService.CommonService.ParentOfCutFile = null;

                    if (localParentOfCutFile is TreeViewAbsolutePath parentTreeViewAbsolutePath)
                        await ReloadTreeViewModel(parentTreeViewAbsolutePath).ConfigureAwait(false);

                    await ReloadTreeViewModel(treeViewModel).ConfigureAwait(false);
                }),
            */
        };
    }

    private MenuOptionValue[] GetFileMenuOptions(
        TreeViewNodeValue treeViewModel,
        TreeViewNodeValue parentTreeViewModel)
    {
        return new MenuOptionValue[]
        {
            /*
            // 2025-10-22 (rewrite TreeViews)
            IdeService.CopyFile(
                treeViewModel.Item,
                (Func<Task>)(() => {
                    CommonFacts.DispatchInformative("Copy Action", $"Copied: {treeViewModel.Item.Name}", IdeService.CommonService, TimeSpan.FromSeconds(7));
                    return Task.CompletedTask;
                })),
            IdeService.CutFile(
                treeViewModel.Item,
                (Func<Task>)(() => {
                    CommonFacts.DispatchInformative("Cut Action", $"Cut: {treeViewModel.Item.Name}", IdeService.CommonService, TimeSpan.FromSeconds(7));
                    IdeService.CommonService.ParentOfCutFile = parentTreeViewModel;
                    return Task.CompletedTask;
                })),
            IdeService.DeleteFile(
                treeViewModel.Item,
                async () => await ReloadTreeViewModel(parentTreeViewModel).ConfigureAwait(false)),
            IdeService.RenameFile(
                treeViewModel.Item,
                IdeService.CommonService,
                async ()  => await ReloadTreeViewModel(parentTreeViewModel).ConfigureAwait(false))
            */
        };
    }

    private MenuOptionValue[] GetDebugMenuOptions(TreeViewNodeValue treeViewModel)
    {
        return new MenuOptionValue[]
        {
            // new MenuOptionRecord(
            //     $"namespace: {treeViewModel.Item.Namespace}",
            //     MenuOptionKind.Read)
        };
    }

    /// <summary>
    /// This method I believe is causing bugs
    /// <br/><br/>
    /// For example, when removing a C# Project the
    /// solution is reloaded and a new root is made.
    /// <br/><br/>
    /// Then there is a timing issue where the new root is made and set
    /// as the root. But this method erroneously reloads the old root.
    /// </summary>
    /// <param name="treeViewModel"></param>
    private async Task ReloadTreeViewModel(TreeViewNodeValue treeViewModel)
    {
        /*
        // 2025-10-22 (rewrite TreeViews)
        if (treeViewModel is null)
            return;

        if (!IdeService.CommonService.TryGetTreeViewContainer(FolderExplorerState.TreeViewContentStateKey, out var treeViewContainer))
            return;
        
        await treeViewModel.LoadChildListAsync(treeViewContainer).ConfigureAwait(false);

        IdeService.CommonService.TreeView_MoveUpAction(
            FolderExplorerState.TreeViewContentStateKey,
            false,
            false);
        
        IdeService.CommonService.TreeView_ReRenderNodeAction(
            FolderExplorerState.TreeViewContentStateKey,
            treeViewModel,
            flatListChanged: true);
        */
    }
}
