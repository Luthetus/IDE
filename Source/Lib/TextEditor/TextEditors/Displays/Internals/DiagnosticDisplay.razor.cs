using Clair.TextEditor.RazorLib.CompilerServices;
using Microsoft.AspNetCore.Components;

namespace Clair.TextEditor.RazorLib.TextEditors.Displays.Internals;

public partial class DiagnosticDisplay : ComponentBase
{
    [Parameter, EditorRequired]
    public TextEditorDiagnostic Diagnostic { get; set; } = default!;
}
