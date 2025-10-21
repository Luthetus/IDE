using Microsoft.AspNetCore.Components;

namespace Clair.TextEditor.RazorLib.Options.Displays;

public sealed partial class InputTextEditorTabKeyBehavior : ComponentBase, IDisposable
{
    [Inject]
    private TextEditorService TextEditorService { get; set; } = null!;

    public bool TabKeyBehavior
    {
        get => TextEditorService.Options_GetTextEditorOptionsState().Options.TabKeyBehavior;
        set => TextEditorService.Options_SetTabKeyBehavior(value);
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
