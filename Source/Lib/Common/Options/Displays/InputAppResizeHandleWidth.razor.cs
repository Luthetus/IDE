using Microsoft.AspNetCore.Components;

namespace Clair.Common.RazorLib.Options.Displays;

public sealed partial class InputAppResizeHandleWidth : ComponentBase, IDisposable
{
    [Inject]
    private CommonService CommonService { get; set; } = null!;

    public int ResizeHandleWidthInPixels
    {
        get => CommonService.GetAppOptionsState().Options.ResizeHandleWidthInPixels;
        set => CommonService.Options_SetResizeHandleWidth(value);
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
