using Microsoft.AspNetCore.Components;
using Clair.Extensions.CompilerServices.Syntax.NodeValues;

namespace Clair.Extensions.CompilerServices.Displays.Internals;

public partial class GenericSyntaxDisplay : ComponentBase
{
    [Parameter, EditorRequired]
    public SyntaxViewModel SyntaxViewModel { get; set; } = default!;
    
    [Parameter]
    public TypeReferenceValue TypeReference { get; set; } = default;
}
