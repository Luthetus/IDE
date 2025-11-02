using Clair.TextEditor.RazorLib.Autocompletes.Models;

namespace Clair.CompilerServices.CSharp.CompilerServiceCase;

public class CSharpAutocompleteContainer : AutocompleteContainer
{
    public CSharpAutocompleteContainer(AutocompleteValue[] autocompleteMenuList)
        : base(autocompleteMenuList)
    {
    }
    
    public List<AutocompleteValue> AutocompleteMenuList { get; set; }
}
