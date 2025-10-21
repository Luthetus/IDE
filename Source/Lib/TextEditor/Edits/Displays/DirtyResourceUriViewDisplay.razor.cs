using Microsoft.AspNetCore.Components;
using Clair.TextEditor.RazorLib.TextEditors.Models;

namespace Clair.TextEditor.RazorLib.Edits.Displays;

public sealed partial class DirtyResourceUriViewDisplay : ComponentBase, IDisposable
{
    [Inject]
    private TextEditorService TextEditorService { get; set; } = null!;
    
    protected override void OnInitialized()
    {
        TextEditorService.SecondaryChanged += OnDirtyResourceUriStateChanged;
    }

    private Task OpenInEditorOnClick(string filePath)
    {
        TextEditorService.WorkerArbitrary.PostUnique(async editContext =>
        {
            await TextEditorService.OpenInEditorAsync(
                editContext,
                filePath,
                true,
                null,
                new Category("main"),
                editContext.TextEditorService.NewViewModelKey());
        });
        return Task.CompletedTask;
    }
    
    public async void OnDirtyResourceUriStateChanged(SecondaryChangedKind secondaryChangedKind)
    {
        if (secondaryChangedKind == SecondaryChangedKind.DirtyResourceUriStateChanged)
        {
            await InvokeAsync(StateHasChanged);
        }
    }
    
    public void Dispose()
    {
        TextEditorService.SecondaryChanged -= OnDirtyResourceUriStateChanged;
    }
}
