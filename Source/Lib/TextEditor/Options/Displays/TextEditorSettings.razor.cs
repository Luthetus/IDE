using Microsoft.AspNetCore.Components;

namespace Clair.TextEditor.RazorLib.Options.Displays;

public partial class TextEditorSettings : ComponentBase
{
    [Parameter]
    public string InputElementCssClass { get; set; } = string.Empty;
}
