namespace Clair.Extensions.DotNet.CommandLines.Models;

public enum DiagnosticTextSpanKind
{
    None,
    FilePath,
    LineAndColumnIndices,
    DiagnosticKind,
    DiagnosticCode,
    Message,
    Project
}
