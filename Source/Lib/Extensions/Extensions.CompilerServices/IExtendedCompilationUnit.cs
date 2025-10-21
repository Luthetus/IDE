using Clair.TextEditor.RazorLib.CompilerServices;

namespace Clair.Extensions.CompilerServices;

public interface IExtendedCompilationUnit : ICompilationUnit
{
    public int ScopeOffset { get; set; }
    public int ScopeLength { get; set; }
    
    public int NodeOffset { get; set; }
    public int NodeLength { get; set; }
}
