using Microsoft.AspNetCore.Components;
using Clair.Common.RazorLib.Menus.Models;
using Clair.Common.RazorLib.Dropdowns.Models;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.Tabs.Models;

namespace Clair.Common.RazorLib.Tabs.Displays;

public partial class TabContextMenu : ComponentBase
{
    [Parameter, EditorRequired]
    public TabContextMenuEventArgs TabContextMenuEventArgs { get; set; } = null!;

    public static readonly Key<DropdownRecord> ContextMenuEventDropdownKey = Key<DropdownRecord>.NewKey();

    private (TabContextMenuEventArgs tabContextMenuEventArgs, MenuRecord menuRecord) _previousGetMenuRecordInvocation;

    private MenuRecord GetMenuRecord(TabContextMenuEventArgs tabContextMenuEventArgs)
    {
        if (_previousGetMenuRecordInvocation.tabContextMenuEventArgs == tabContextMenuEventArgs)
            return _previousGetMenuRecordInvocation.menuRecord;

        var menuOptionList = new List<MenuOptionRecord>();

        menuOptionList.Add(new MenuOptionRecord(
            "Close All",
            MenuOptionKind.Delete,
            _ => tabContextMenuEventArgs.Tab.TabGroup.CloseAllAsync()));

        menuOptionList.Add(new MenuOptionRecord(
            "Close Others",
            MenuOptionKind.Delete,
            _ => tabContextMenuEventArgs.Tab.TabGroup.CloseOthersAsync(tabContextMenuEventArgs.Tab)));

        if (menuOptionList.Count == 0)
        {
            var menuRecord = new MenuRecord(MenuRecord.NoMenuOptionsExistList);
            _previousGetMenuRecordInvocation = (tabContextMenuEventArgs, menuRecord);
            return menuRecord;
        }

        // Default case
        {
            var menuRecord = new MenuRecord(menuOptionList);
            _previousGetMenuRecordInvocation = (tabContextMenuEventArgs, menuRecord);
            return menuRecord;
        }
    }
}
