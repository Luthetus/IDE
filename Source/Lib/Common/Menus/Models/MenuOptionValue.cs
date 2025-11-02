namespace Clair.Common.RazorLib.Menus.Models;

public struct MenuOptionValue
{
    public MenuOptionValue(
        string displayName,
        MenuOptionKind menuOptionKind,
        Func<MenuOptionOnClickArgs, Task>? onClickFunc = null)
    {
        DisplayName = displayName;
        MenuOptionKind = menuOptionKind;
        OnClickFunc = onClickFunc;
    }
    
    public string DisplayName { get; init; }
    public MenuOptionKind MenuOptionKind { get; init; }
    public Func<MenuOptionOnClickArgs, Task>? OnClickFunc { get; init; }
    public AutocompleteEntryKind IconKind { get; set; }
}
