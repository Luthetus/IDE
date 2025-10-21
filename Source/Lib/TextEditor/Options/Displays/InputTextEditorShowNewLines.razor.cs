using Microsoft.AspNetCore.Components;

namespace Clair.TextEditor.RazorLib.Options.Displays;

public sealed partial class InputTextEditorShowNewLines : ComponentBase, IDisposable
{
    [Inject]
    private TextEditorService TextEditorService { get; set; } = null!;

    public bool GlobalShowNewlines
    {
        get => TextEditorService.Options_GetTextEditorOptionsState().Options.ShowNewlines;
        set => TextEditorService.Options_SetShowNewlines(value);
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
