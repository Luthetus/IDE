using Microsoft.AspNetCore.Components;

namespace Clair.Common.RazorLib.Options.Displays;

public sealed partial class InputAppFontFamily : ComponentBase, IDisposable
{
    [Inject]
    private CommonService CommonService { get; set; } = null!;

    public string FontFamily
    {
        get => CommonService.GetAppOptionsState().Options.FontFamily ?? "unset";
        set => CommonService.Options_SetFontFamily(value);
    }

    protected override void OnInitialized()
    {
        CommonService.CommonUiStateChanged += AppOptionsStateWrapOnStateChanged;
    }

    private async void AppOptionsStateWrapOnStateChanged(CommonUiEventKind commonUiEventKind)
    {
        if (commonUiEventKind == CommonUiEventKind.AppOptionsStateChanged)
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        CommonService.CommonUiStateChanged -= AppOptionsStateWrapOnStateChanged;
    }
}
