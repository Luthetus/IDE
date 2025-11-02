using Clair.TextEditor.RazorLib;
using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.TextEditor.RazorLib.Autocompletes.Models;
using Clair.Common.RazorLib.Menus.Models;

namespace Clair.CompilerServices.CSharp.CompilerServiceCase;

public class CSharpAutocompleteContainer : AutocompleteContainer
{
    public CSharpAutocompleteContainer(
            TextEditorService textEditorService,
            AutocompleteValue[] autocompleteMenuList)
        : base(autocompleteMenuList)
    {
        TextEditorService = textEditorService;
    }
    
    public TextEditorService TextEditorService { get; }
    public ResourceUri ResourceUri { get; set; }
    public int TextEditorViewModelKey { get; set; }
    
    public override void OnClick(AutocompleteValue entry)
    {
        TextEditorService.WorkerArbitrary.PostUnique(editContext =>
        {
            var modelModifier = editContext.GetModelModifier(ResourceUri);
            var viewModelModifier = editContext.GetViewModelModifier(TextEditorViewModelKey);
            
            if (entry.AutocompleteEntryKind != AutocompleteEntryKind.Snippet)
            {
                modelModifier.Insert(
                    entry.DisplayName,
                    viewModelModifier);
            }
            else
            {
                if (entry.DisplayName == "prop")
                {
                    modelModifier.Insert(
                        "public TYPE NAME { get; set; }",
                        viewModelModifier);
                }
            }
            
            return ValueTask.CompletedTask;
        });
    }
}
