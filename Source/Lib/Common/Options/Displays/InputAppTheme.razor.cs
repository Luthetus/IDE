using Microsoft.AspNetCore.Components;
using Clair.Common.RazorLib.Themes.Models;

namespace Clair.Common.RazorLib.Options.Displays;

public sealed partial class InputAppTheme : IDisposable
{
    [Inject]
    private CommonService CommonService { get; set; } = null!;

    protected override void OnInitialized()
    {
        CommonService.CommonUiStateChanged += OnAppOptionsStateChanged;
    }

    private void OnThemeSelectChanged(ChangeEventArgs changeEventArgs)
    {
        if (changeEventArgs.Value is null)
            return;

        var themeState = CommonService.GetThemeState();

        var intAsString = (string)changeEventArgs.Value;

        if (int.TryParse(intAsString, out var intValue))
        {
            var themesInScopeList = themeState.ThemeList.Where(x => x.IncludeScopeApp)
                .ToArray();

            var existingThemeRecord = themesInScopeList.FirstOrDefault(btr => btr.Key == intValue);

            if (!existingThemeRecord.IsDefault())
                CommonService.Options_SetActiveThemeRecordKey(existingThemeRecord.Key);
        }
    }

    private bool CheckIsActiveValid(ThemeRecord[] themeRecordList, int activeThemeKey)
    {
        return themeRecordList.Any(btr => btr.Key == activeThemeKey);
    }

    private bool CheckIsActiveSelection(int themeKey, int activeThemeKey)
    {
        return themeKey == activeThemeKey;
    }

    public async void OnAppOptionsStateChanged(CommonUiEventKind commonUiEventKind)
    {
        if (commonUiEventKind == CommonUiEventKind.AppOptionsStateChanged)
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        CommonService.CommonUiStateChanged -= OnAppOptionsStateChanged;
    }
}
