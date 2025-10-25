using Microsoft.AspNetCore.Components;
using Clair.Common.RazorLib.Commands.Models;
using Clair.Common.RazorLib.Menus.Models;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.Dropdowns.Models;

namespace Clair.TextEditor.RazorLib.FindAlls.Displays;

public partial class FindAllContextMenu : ComponentBase
{
    [Parameter, EditorRequired]
    public TreeViewCommandArgs TreeViewCommandArgs { get; set; }

    public static readonly Key<DropdownRecord> ContextMenuEventDropdownKey = Key<DropdownRecord>.NewKey();

    private (TreeViewCommandArgs treeViewCommandArgs, MenuRecord menuRecord) _previousGetMenuRecordInvocation;
    
    private MenuRecord GetMenuRecord(TreeViewCommandArgs commandArgs)
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

        var menuRecordsList = new List<MenuOptionRecord>();

        if (!menuRecordsList.Any())
        {
            var menuRecord = new MenuRecord(MenuRecord.NoMenuOptionsExistList);
            _previousGetMenuRecordInvocation = (commandArgs, menuRecord);
            return menuRecord;
        }

        // Default case
        {
            var menuRecord = new MenuRecord(menuRecordsList);
            _previousGetMenuRecordInvocation = (commandArgs, menuRecord);
            return menuRecord;
        }
    }

    private MenuRecord GetMultiSelectionMenuRecord(TreeViewCommandArgs commandArgs)
    {
        var menuOptionRecordList = new List<MenuOptionRecord>();

        if (!menuOptionRecordList.Any())
        {
            var menuRecord = new MenuRecord(MenuRecord.NoMenuOptionsExistList);
            _previousGetMenuRecordInvocation = (commandArgs, menuRecord);
            return menuRecord;
        }

        // Default case
        {
            var menuRecord = new MenuRecord(menuOptionRecordList);
            _previousGetMenuRecordInvocation = (commandArgs, menuRecord);
            return menuRecord;
        }
    }
}
