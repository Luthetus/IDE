namespace Clair.Extensions.DotNet.CommandLines.Models;

public struct DiagnosticTextSpan
{
    public DiagnosticTextSpan(
        DiagnosticTextSpanKind kind,
        int startInclusiveIndex,
        int endExclusiveIndex,
        string sourceText)
    {
        Kind = kind;
        StartInclusiveIndex = startInclusiveIndex;
        EndExclusiveIndex = endExclusiveIndex;
        
        Text = sourceText.Substring(
            StartInclusiveIndex,
            EndExclusiveIndex - StartInclusiveIndex);
    }

    public DiagnosticTextSpanKind Kind { get; }
    public int StartInclusiveIndex { get; }
    public int EndExclusiveIndex { get; }
    public string Text { get; }
}
