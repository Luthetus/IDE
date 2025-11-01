namespace Clair.Extensions.DotNet.CommandLines.Models;

public class DotNetRunParseResult
{
    public DotNetRunParseResult(
        string message,
        List<DiagnosticLine> allDiagnosticLineList,
        int errorCount,
        int warningCount,
        int otherCount)
    {
        Message = message;
        AllDiagnosticLineList = allDiagnosticLineList;
        ErrorCount = errorCount;
        WarningCount = warningCount;
        OtherCount = otherCount;
    }

    /// <summary>Use this to determine if the UI is up to date.</summary>
    public Guid Id { get; } = Guid.NewGuid();
    
    public string Message { get; }
    public List<DiagnosticLine> AllDiagnosticLineList { get; }
    public int ErrorCount { get; }
    public int WarningCount { get; }
    public int OtherCount { get; }
}
