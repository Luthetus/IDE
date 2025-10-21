using Microsoft.AspNetCore.Components;
using Clair.Common.RazorLib;
using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models.Internals;

namespace Clair.TextEditor.RazorLib.Options.Displays;

public partial class TextEditorSettingsPreview : ComponentBase
{
    [Inject]
    private TextEditorService TextEditorService { get; set; } = null!;

    [Parameter]
    public string TopLevelDivElementCssClassString { get; set; } = string.Empty;
    [Parameter]
    public string InputElementCssClassString { get; set; } = string.Empty;
    [Parameter]
    public string LabelElementCssClassString { get; set; } = string.Empty;
    [Parameter]
    public string PreviewElementCssClassString { get; set; } = string.Empty;

    public int SettingsPreviewTextEditorViewModelKey { get; private set; }
    
    private readonly ViewModelDisplayOptions _viewModelDisplayOptions = new()
    {
        HeaderComponentType = null,
        FooterComponentType = null,
        AfterOnKeyDownAsync = (_, _, _, _, _) => Task.CompletedTask,
    };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            SettingsPreviewTextEditorViewModelKey = TextEditorService.NewViewModelKey();

            TextEditorService.WorkerArbitrary.PostUnique(async editContext =>
            {
                TextEditorService.Model_RegisterTemplated(
                    editContext,
                    CommonFacts.TXT,
                    ResourceUriFacts.SettingsPreviewTextEditorResourceUri,
                    DateTime.UtcNow,
                    "Preview settings here",
                    "Settings Preview");
    
                TextEditorService.ViewModel_Register(
                    editContext,
                    SettingsPreviewTextEditorViewModelKey,
                    ResourceUriFacts.SettingsPreviewTextEditorResourceUri,
                    new Category(nameof(TextEditorSettingsPreview)));
                    
                await InvokeAsync(StateHasChanged);
            });
        }
    }
}
