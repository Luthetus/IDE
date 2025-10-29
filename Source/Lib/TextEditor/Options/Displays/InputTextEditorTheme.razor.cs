using Microsoft.AspNetCore.Components;
using Clair.Common.RazorLib;
using Clair.Common.RazorLib.Themes.Models;

namespace Clair.TextEditor.RazorLib.Options.Displays;

public sealed partial class InputTextEditorTheme : ComponentBase, IDisposable
{
    [Inject]
    private TextEditorService TextEditorService { get; set; } = null!;
    
    protected override void OnInitialized()
    {
        TextEditorService.SecondaryChanged += TextEditorOptionsStateWrapOnStateChanged;
    }
    
    private void SelectedThemeChanged(ChangeEventArgs changeEventArgs)
    {
        var themeList = TextEditorService.CommonService.GetThemeState().ThemeList;

        var chosenThemeKeyIntString = changeEventArgs.Value?.ToString() ?? string.Empty;

        if (int.TryParse(chosenThemeKeyIntString, out var chosenThemeKeyInt))
        {
            var foundTheme = default(ThemeRecord);
            foreach (var x in themeList)
            {
                if (x.Key == chosenThemeKeyInt)
                {
                    foundTheme = x;
                    break;
                }
            }
            if (!foundTheme.IsDefault())
                TextEditorService.Options_SetTheme(foundTheme);
        }
        else
        {
            TextEditorService.Options_SetTheme(CommonFacts.VisualStudioDarkThemeClone);
        }
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
