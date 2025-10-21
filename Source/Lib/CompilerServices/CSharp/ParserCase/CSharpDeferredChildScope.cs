using Clair.Extensions.CompilerServices.Syntax;

namespace Clair.CompilerServices.CSharp.ParserCase;

public struct CSharpDeferredChildScope
{
    public CSharpDeferredChildScope(
        int scopeSubIndex,
        int openTokenIndex,
        SyntaxToken openToken,
        int closeTokenIndex,
        SyntaxToken closeToken)
    {
        ScopeSubIndex = scopeSubIndex;
        OpenTokenIndex = openTokenIndex;
        OpenToken = openToken;
        CloseTokenIndex = closeTokenIndex;
        CloseToken = closeToken;
    }
    
    public int ScopeSubIndex { get; }
    public int OpenTokenIndex { get; }
    public SyntaxToken OpenToken { get; }
    public int CloseTokenIndex { get; }
    public SyntaxToken CloseToken { get; }
    
    public readonly void PrepareMainParserLoop(int restoreTokenIndex, SyntaxToken restoreToken, ref CSharpParserState parserModel)
    {
        // Console.WriteLine($"set_{ScopeSubIndex} => (restore_{restoreTokenIndex}, restore_{restoreToken.SyntaxKind}, ...)");
        
        parserModel.ScopeCurrentSubIndex = ScopeSubIndex;
        parserModel.SetCurrentScope_PermitCodeBlockParsing(true);
        
        parserModel.TokenWalker.DeferredParsing(
            OpenTokenIndex,
            OpenToken,
            CloseTokenIndex,
            CloseToken,
            restoreTokenIndex,
            restoreToken);
    }
}
