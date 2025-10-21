using Microsoft.AspNetCore.Components;
using Clair.TextEditor.RazorLib;
using Clair.Extensions.CompilerServices.Syntax.NodeValues;

namespace Clair.Extensions.CompilerServices.Displays.Internals;

public partial class TypeSyntaxDisplay : ComponentBase
{
    [Inject]
    private TextEditorService TextEditorService { get; set; } = null!;
    
    [Parameter, EditorRequired]
    public SyntaxViewModel SyntaxViewModel { get; set; } = default!;
    
    [Parameter]
    public TypeReferenceValue TypeReference { get; set; } = default;
}
