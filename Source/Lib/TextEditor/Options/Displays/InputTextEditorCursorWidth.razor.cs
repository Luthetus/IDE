using Microsoft.AspNetCore.Components;

namespace Clair.TextEditor.RazorLib.Options.Displays;

public sealed partial class InputTextEditorCursorWidth : ComponentBase, IDisposable
{
    [Inject]
    private TextEditorService TextEditorService { get; set; } = null!;

    private double TextEditorCursorWidth
    {
        get => TextEditorService.Options_GetTextEditorOptionsState().Options.CursorWidthInPixels;
        set
        {
            TextEditorService.Options_SetCursorWidth(value);
        }
    }
    
    protected override void OnInitialized()
    {
        TextEditorService.SecondaryChanged += OnOptionStaticStateChanged;
    }
    
    private async void OnOptionStaticStateChanged(SecondaryChangedKind secondaryChangedKind)
    {
        if (secondaryChangedKind == SecondaryChangedKind.StaticStateChanged)
        {
            await InvokeAsync(StateHasChanged);
        }
    }
    
    public void Dispose()
    {
        TextEditorService.SecondaryChanged -= OnOptionStaticStateChanged;
    }
}
