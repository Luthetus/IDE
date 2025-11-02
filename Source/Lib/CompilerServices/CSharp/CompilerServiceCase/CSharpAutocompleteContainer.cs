using Clair.TextEditor.RazorLib.Autocompletes.Models;

namespace Clair.CompilerServices.CSharp.CompilerServiceCase;

public class CSharpAutocompleteContainer : AutocompleteContainer
{
    public CSharpAutocompleteContainer(List<AutocompleteValue> autocompleteMenuList)
        : base(autocompleteMenuList)
    {
    }
    
    public List<AutocompleteMenu> AutocompleteMenuList { get; set; }
}
