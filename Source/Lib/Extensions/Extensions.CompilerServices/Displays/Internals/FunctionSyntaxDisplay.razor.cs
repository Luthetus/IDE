using Microsoft.AspNetCore.Components;
using Clair.TextEditor.RazorLib;
using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.Extensions.CompilerServices.Syntax.Interfaces;

namespace Clair.Extensions.CompilerServices.Displays.Internals;

public partial class FunctionSyntaxDisplay : ComponentBase
{
    [Inject]
    private TextEditorService TextEditorService { get; set; } = null!;
    
    [Parameter, EditorRequired]
    public SyntaxViewModel SyntaxViewModel { get; set; } = default!;
    
    private string GetIdentifierText(ISyntaxNode node)
    {
        return SyntaxViewModel.GetIdentifierText(node);
    }
    
    private string GetTextFromTextSpan(TextEditorTextSpan textSpan)
    {
        return SyntaxViewModel.GetTextFromTextSpan(textSpan);
    }
}
