using Microsoft.AspNetCore.Components;

namespace Clair.Common.RazorLib.Options.Displays;

public sealed partial class InputAppResizeHandleHeight : ComponentBase, IDisposable
{
    [Inject]
    private CommonService CommonService { get; set; } = null!;

    public int ResizeHandleHeightInPixels
    {
        get => CommonService.GetAppOptionsState().Options.ResizeHandleHeightInPixels;
        set => CommonService.Options_SetResizeHandleHeight(value);
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
