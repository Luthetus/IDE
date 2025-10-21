using Clair.TextEditor.RazorLib.Decorations.Models;

namespace Clair.TextEditor.RazorLib.CompilerServices;

public class CompilerServiceDiagnosticDecorationMapper : IDecorationMapper
{
    public string Map(byte decorationByte)
    {
        var decoration = (CompilerServiceDiagnosticDecorationKind)decorationByte;

        return decoration switch
        {
            CompilerServiceDiagnosticDecorationKind.None => string.Empty,
            CompilerServiceDiagnosticDecorationKind.DiagnosticError => "border-bottom: 2px solid var(--ci_te_semantic-diagnostic-error-background-color);",
            CompilerServiceDiagnosticDecorationKind.DiagnosticHint => "border-bottom: 2px solid var(--ci_te_semantic-diagnostic-hint-background-color);",
            CompilerServiceDiagnosticDecorationKind.DiagnosticSuggestion => "ci_te_semantic-diagnostic-suggestion",
            CompilerServiceDiagnosticDecorationKind.DiagnosticWarning => "ci_te_semantic-diagnostic-warning",
            CompilerServiceDiagnosticDecorationKind.DiagnosticOther => "ci_te_semantic-diagnostic-other",
            _ => string.Empty,
        };
    }
}
