using Microsoft.AspNetCore.Components;

namespace Clair.TextEditor.RazorLib.TextEditors.Displays.Internals;

public partial class LabelWidgetInlineDisplay : ComponentBase
{
    [Parameter, EditorRequired]
    public string Text { get; set; } = null!;
}
