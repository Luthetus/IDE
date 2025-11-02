using Clair.TextEditor.RazorLib.TextEditors.Displays.Internals;

namespace Clair.TextEditor.RazorLib.Autocompletes.Models;

/// <summary>
/// Do not modify the AutocompleteMenuList.
/// 
/// IReadOnlyList would presumably add an extra layer of code when indexing.
/// A lot of autocompletes are shown on the screen at the same time and they are constantly changing.
/// Thus, just don't modify the List.
///
/// I considered making this an array but then you have to preallocate vs Add'ing to the List.
/// I'm not sure I think I'm gonna change it to an array and just do preallocation.
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
