using Microsoft.AspNetCore.Components;

namespace Clair.TextEditor.RazorLib.Options.Displays;

public sealed partial class InputTextEditorTabWidth : ComponentBase, IDisposable
{
    [Inject]
    private TextEditorService TextEditorService { get; set; } = null!;

    public int TabWidth
    {
        get => TextEditorService.Options_GetTextEditorOptionsState().Options.TabWidth;
        set => TextEditorService.Options_SetTabWidth(value);
    }
    
    protected override void OnInitialized()
    {
        TextEditorService.SecondaryChanged += TextEditorOptionsStateWrapOnStateChanged;
    }
    
    private async void TextEditorOptionsStateWrapOnStateChanged(SecondaryChangedKind secondaryChangedKind)
    {
        if (secondaryChangedKind == SecondaryChangedKind.StaticStateChanged)
        {
            await InvokeAsync(StateHasChanged);
        }
    }
    
    public void Dispose()
    {
        TextEditorService.SecondaryChanged -= TextEditorOptionsStateWrapOnStateChanged;
    }
}
