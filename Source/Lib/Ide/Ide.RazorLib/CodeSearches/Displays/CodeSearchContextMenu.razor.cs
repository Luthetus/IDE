using Microsoft.AspNetCore.Components;
using Clair.Common.RazorLib.Commands.Models;
using Clair.Common.RazorLib.Menus.Models;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.Dropdowns.Models;

namespace Clair.Ide.RazorLib.CodeSearches.Displays;

public partial class CodeSearchContextMenu : ComponentBase
{
    [Parameter, EditorRequired]
    public TreeViewCommandArgs TreeViewCommandArgs { get; set; }

    public static readonly Key<DropdownRecord> ContextMenuEventDropdownKey = Key<DropdownRecord>.NewKey();

    private (TreeViewCommandArgs treeViewCommandArgs, MenuContainer menuRecord) _previousGetMenuRecordInvocation;
    
    private MenuContainer GetMenuRecord(TreeViewCommandArgs commandArgs)
    {
        if (_previousGetMenuRecordInvocation.treeViewCommandArgs == commandArgs)
            return _previousGetMenuRecordInvocation.menuRecord;

        /*
        // 2025-10-22 (rewrite TreeViews)
        if (commandArgs.TreeViewContainer.SelectedNodeList.Count > 1)
        {
            return GetMultiSelectionMenuRecord(commandArgs);
        }
        */

        /*
        // 2025-10-22 (rewrite TreeViews)
        if (commandArgs.NodeThatReceivedMouseEvent is null)
        {
            var menuRecord = new MenuRecord(MenuRecord.NoMenuOptionsExistList);
            _previousGetMenuRecordInvocation = (commandArgs, menuRecord);
            return menuRecord;
        }
        */

        var menuRecordsList = new List<MenuOptionValue>();

        if (menuRecordsList.Count == 0)
        {
            var menuRecord = new MenuContainer();
            _previousGetMenuRecordInvocation = (commandArgs, menuRecord);
            return menuRecord;
        }

        // Default case
        {
            var menuRecord = new MenuContainer(menuRecordsList);
            _previousGetMenuRecordInvocation = (commandArgs, menuRecord);
            return menuRecord;
        }
    }

    private MenuContainer GetMultiSelectionMenuRecord(TreeViewCommandArgs commandArgs)
    {
        var menuOptionRecordList = new List<MenuOptionValue>();

        if (menuOptionRecordList.Count == 0)
        {
            var menuRecord = new MenuContainer(MenuContainer.NoMenuOptionsExistList);
            _previousGetMenuRecordInvocation = (commandArgs, menuRecord);
            return menuRecord;
        }

        // Default case
        {
            var menuRecord = new MenuContainer(menuOptionRecordList);
            _previousGetMenuRecordInvocation = (commandArgs, menuRecord);
            return menuRecord;
        }
    }
}
