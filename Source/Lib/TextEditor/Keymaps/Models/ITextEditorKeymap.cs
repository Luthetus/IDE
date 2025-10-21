using Clair.TextEditor.RazorLib.Options.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models.Internals;

namespace Clair.TextEditor.RazorLib.Keymaps.Models;

/// <summary>
/// Are you not just writing the name of the keymap?
/// (or some unique identifier).
/// into local storage?
/// </summary>
public interface ITextEditorKeymap
{
    public string DisplayName { get; }

    public int GetLayer(bool hasSelection);

    public string GetCursorCssClassString();

    public string GetCursorCssStyleString(
        TextEditorModel textEditorModel,
        TextEditorViewModel textEditorViewModel,
        TextEditorOptions textEditorOptions);
    
    public ValueTask HandleEvent(
        TextEditorComponentData componentData,
        int viewModelKey,
        string key,
        string code,
        bool ctrlKey,
        bool shiftKey,
        bool altKey,
        bool metaKey);
}
