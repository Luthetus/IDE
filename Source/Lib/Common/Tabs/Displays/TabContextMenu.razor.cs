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

    private (TabContextMenuEventArgs tabContextMenuEventArgs, MenuContainer menuRecord) _previousGetMenuRecordInvocation;

    private MenuContainer GetMenuRecord(TabContextMenuEventArgs tabContextMenuEventArgs)
    {
        if (_previousGetMenuRecordInvocation.tabContextMenuEventArgs == tabContextMenuEventArgs)
            return _previousGetMenuRecordInvocation.menuRecord;

        var menuOptionList = new List<MenuOptionValue>();

        menuOptionList.Add(new MenuOptionValue(
            "Close All",
            MenuOptionKind.Delete,
            _ => tabContextMenuEventArgs.Tab.TabGroup.CloseAllAsync()));

        menuOptionList.Add(new MenuOptionValue(
            "Close Others",
            MenuOptionKind.Delete,
            _ => tabContextMenuEventArgs.Tab.TabGroup.CloseOthersAsync(tabContextMenuEventArgs.Tab)));

        if (menuOptionList.Count == 0)
        {
            var menuRecord = new MenuContainer();
            _previousGetMenuRecordInvocation = (tabContextMenuEventArgs, menuRecord);
            return menuRecord;
        }

        // Default case
        {
            var menuRecord = new MenuContainer(menuOptionList);
            _previousGetMenuRecordInvocation = (tabContextMenuEventArgs, menuRecord);
            return menuRecord;
        }
    }
}
