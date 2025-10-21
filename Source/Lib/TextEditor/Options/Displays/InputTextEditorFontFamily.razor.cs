using Microsoft.AspNetCore.Components;

namespace Clair.TextEditor.RazorLib.Options.Displays;

public sealed partial class InputTextEditorFontFamily : ComponentBase, IDisposable
{
    [Inject]
    private TextEditorService TextEditorService { get; set; } = null!;

    public string FontFamily
    {
        get => TextEditorService.Options_GetTextEditorOptionsState().Options.CommonOptions.FontFamily ?? "unset";
        set
        {
            TextEditorService.Options_SetFontFamily(value);
        }
    }

    protected override void OnInitialized()
    {
        TextEditorService.SecondaryChanged += OptionsWrapOnStateChanged;
    }

    private async void OptionsWrapOnStateChanged(SecondaryChangedKind secondaryChangedKind)
    {
        if (secondaryChangedKind == SecondaryChangedKind.StaticStateChanged)
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        TextEditorService.SecondaryChanged -= OptionsWrapOnStateChanged;
    }
}
