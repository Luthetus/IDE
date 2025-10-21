using Microsoft.AspNetCore.Components;
using Clair.Common.RazorLib.Keys.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models.Internals;

namespace Clair.TextEditor.RazorLib.TextEditors.Displays.Internals;

public sealed partial class TextEditorFileExtensionHeaderDisplay : ComponentBase, IDisposable
{
    [Inject]
    private TextEditorService TextEditorService { get; set; } = null!;

    [Parameter, EditorRequired]
    public int TextEditorViewModelKey { get; set; }
    [Parameter, EditorRequired]
    public Key<TextEditorComponentData> ComponentDataKey { get; set; }
    
    private Dictionary<string, object?> _componentInnerParameters = null!;
    
    private string _fileExtensionCurrent = string.Empty;
    
    private string DictionaryKey => nameof(ITextEditorDependentComponent.ComponentDataKey);
    
    protected override void OnInitialized()
    {
        _componentInnerParameters = new()
        {
            {
                DictionaryKey,
                ComponentDataKey
            }
        };

        // ShouldRender does not invoke on the initial render.
        _ = ShouldRender();
        
        TextEditorService.SecondaryChanged += OnTextEditorWrapperCssStateChanged;
    }
    
    protected override bool ShouldRender()
    {
        TextEditorViewModel? viewModel;
        TextEditorModel? model;
        
        if (TextEditorService.TextEditorState._viewModelMap.TryGetValue(
                TextEditorViewModelKey,
                out viewModel))
        {
            _ = TextEditorService.TextEditorState._modelMap.TryGetValue(
                    viewModel.PersistentState.ResourceUri,
                    out model);
        }
        else
        {
            model = null;
        }
        
        var fileExtensionLocal = model is null
            ? string.Empty
            : model.PersistentState.FileExtension;
            
        if (_fileExtensionCurrent != fileExtensionLocal)
            _fileExtensionCurrent = fileExtensionLocal;
            
        return true;
    }
    
    private async void OnTextEditorWrapperCssStateChanged(SecondaryChangedKind secondaryChangedKind)
    {
        if (secondaryChangedKind == SecondaryChangedKind.TextEditorWrapperCssStateChanged)
        {
            await InvokeAsync(StateHasChanged);
        }
    }
    
    public void Dispose()
    {
        TextEditorService.SecondaryChanged -= OnTextEditorWrapperCssStateChanged;
    }
}
