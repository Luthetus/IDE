using Microsoft.AspNetCore.Components;
using Clair.TextEditor.RazorLib;

namespace Clair.Extensions.CompilerServices.Displays.Internals;

public partial class VariableSyntaxDisplay : ComponentBase
{
    [Inject]
    private TextEditorService TextEditorService { get; set; } = null!;
    
    [Parameter, EditorRequired]
    public SyntaxViewModel SyntaxViewModel { get; set; } = default!;
}
