using Microsoft.AspNetCore.Components;
using Clair.Common.RazorLib.Keys.Models;
using Clair.TextEditor.RazorLib;
using Clair.TextEditor.RazorLib.TextEditors.Models.Internals;
using Clair.TextEditor.RazorLib.TextEditors.Displays.Internals;
using Clair.Extensions.DotNet;

namespace Clair.Extensions.Config.Installations.Displays;

public partial class EditorDisplay : ComponentBase, IDisposable
{
    [Inject]
    private DotNetService DotNetService { get; set; } = null!;
    
    private int _previousActiveViewModelKey = 0;
    
    private Key<TextEditorComponentData> _componentDataKey;
    private ViewModelDisplayOptions _viewModelDisplayOptions = null!;
    
    private static readonly List<HeaderButtonKind> TextEditorHeaderButtonKindsList =
        Enum.GetValues(typeof(HeaderButtonKind))
            .Cast<HeaderButtonKind>()
            .ToList();
    
    private string? _htmlId = null;
    private string HtmlId => _htmlId ??= $"ci_te_group_{Clair.TextEditor.RazorLib.TextEditorService.EditorTextEditorGroupKey.Guid}";
    
    protected override void OnInitialized()
    {
        _viewModelDisplayOptions = new()
        {
            TabIndex = 0,
            HeaderButtonKinds = TextEditorHeaderButtonKindsList,
            HeaderComponentType = typeof(TextEditorFileExtensionHeaderDisplay),
            TextEditorHtmlElementId = Guid.NewGuid(),
        };
        
        _componentDataKey = new Key<TextEditorComponentData>(_viewModelDisplayOptions.TextEditorHtmlElementId);
    
        DotNetService.TextEditorService.SecondaryChanged += TextEditorOptionsStateWrap_StateChanged;
    }
    
    private async void TextEditorOptionsStateWrap_StateChanged(SecondaryChangedKind secondaryChangedKind)
    {
        if (secondaryChangedKind == SecondaryChangedKind.Group_TextEditorGroupStateChanged)
        {
            var textEditorGroup = DotNetService.TextEditorService.Group_GetTextEditorGroupState().EditorTextEditorGroup;
                
            if (_previousActiveViewModelKey != textEditorGroup.ActiveViewModelKey)
            {
                _previousActiveViewModelKey = textEditorGroup.ActiveViewModelKey;
                DotNetService.TextEditorService.ViewModel_StopCursorBlinking();
            }
        
            await InvokeAsync(StateHasChanged);
        }
    }
    
    public void Dispose()
    {
        DotNetService.TextEditorService.SecondaryChanged -= TextEditorOptionsStateWrap_StateChanged;
    }
}