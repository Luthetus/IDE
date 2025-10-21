using Microsoft.AspNetCore.Components;
using Clair.Common.RazorLib.Keys.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models.Internals;

namespace Clair.TextEditor.RazorLib.TextEditors.Displays;

public partial class TextEditorViewModelDisplay : ComponentBase
{
    [Parameter, EditorRequired]
    public int TextEditorViewModelKey { get; set; } = 0;
    
    [Parameter]
    public ViewModelDisplayOptions ViewModelDisplayOptions { get; set; } = new();

    private Key<TextEditorComponentData> _componentDataKey;
    
    private static string DictionaryKey => nameof(TextEditorComponentData.ComponentDataKey);

    public Dictionary<string, object?> DependentComponentParameters { get; set; } = new Dictionary<string, object?>
    {
        {
            DictionaryKey,
            null
        }
    };
    
    protected override void OnInitialized()
    {
        if (ViewModelDisplayOptions.TextEditorHtmlElementId == Guid.Empty)
            ViewModelDisplayOptions.TextEditorHtmlElementId = Guid.NewGuid();
            
        _componentDataKey = new Key<TextEditorComponentData>(ViewModelDisplayOptions.TextEditorHtmlElementId);
        DependentComponentParameters[DictionaryKey] = _componentDataKey;
    }
}
