namespace Clair.Extensions.DotNet.CommandLines.Models;

/// <summary>
/// Used in the method <see cref="ParseOutputEntireDotNetRun"/>
///
/// Need to store source text in TreeViewContainer then get the text of a node lazily.
/// </summary>
public struct DiagnosticLine()
{
    private string _textShort;

    // <summary>The entire line of text itself</summary>
    public int StartInclusiveIndex { get; init; }
    // <summary>The entire line of text itself</summary>
    public int EndExclusiveIndex { get; init; }
    // <summary>The entire line of text itself</summary>
    public string Text { get; init; }
    public DiagnosticLineKind DiagnosticLineKind { get; init; } = DiagnosticLineKind.Error;
    
    public DiagnosticTextSpan FilePathTextSpan { get; init; }
    public DiagnosticTextSpan LineAndColumnIndicesTextSpan { get; init; }
    public DiagnosticTextSpan DiagnosticKindTextSpan { get; init; }
    public DiagnosticTextSpan DiagnosticCodeTextSpan { get; init; }
    public DiagnosticTextSpan MessageTextSpan { get; init; }
    public DiagnosticTextSpan ProjectTextSpan { get; init; }
    
    public string TextShort => _textShort ??= Text
        .Replace(FilePathTextSpan.Text, string.Empty)
        .Replace(ProjectTextSpan.Text, string.Empty);
    
    public bool IsValid => 
        FilePathTextSpan.Kind != DiagnosticTextSpanKind.None &&
        LineAndColumnIndicesTextSpan.Kind != DiagnosticTextSpanKind.None &&
        DiagnosticKindTextSpan.Kind != DiagnosticTextSpanKind.None &&
        DiagnosticCodeTextSpan.Kind != DiagnosticTextSpanKind.None &&
        MessageTextSpan.Kind != DiagnosticTextSpanKind.None &&
        ProjectTextSpan.Kind != DiagnosticTextSpanKind.None;
}
