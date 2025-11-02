using Clair.TextEditor.RazorLib.TextEditors.Displays.Internals;

namespace Clair.TextEditor.RazorLib.Autocompletes.Models;

/// <summary>
/// Do not modify the AutocompleteMenuList.
/// 
/// IReadOnlyList would presumably add an extra layer of code when indexing.
/// A lot of autocompletes are shown on the screen at the same time and they are constantly changing.
/// Thus, just don't modify the List.
/// </summary>
public class AutocompleteContainer
{
    private static readonly List<AutocompleteValue> _empty = new();

    public AutocompleteContainer()
    {
        AutocompleteMenuList = _empty;
    }
    
    public AutocompleteContainer(List<AutocompleteValue> autocompleteMenuList)
    {
        AutocompleteMenuList = autocompleteMenuList;
    }

    public List<AutocompleteValue> AutocompleteMenuList { get; set; }
}
