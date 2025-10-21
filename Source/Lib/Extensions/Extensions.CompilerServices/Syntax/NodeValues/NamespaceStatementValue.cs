using Clair.Extensions.CompilerServices.Syntax.NodeReferences;

namespace Clair.Extensions.CompilerServices.Syntax.NodeValues;

public struct NamespaceStatementValue
{
    public NamespaceStatementValue(NamespaceStatementNode namespaceStatementNode)
    {
        KeywordToken = namespaceStatementNode.KeywordToken;
        IdentifierToken = namespaceStatementNode.IdentifierToken;
        AbsolutePathId = namespaceStatementNode.AbsolutePathId;
        ParentScopeSubIndex = namespaceStatementNode.ParentScopeSubIndex;
        SelfScopeSubIndex = namespaceStatementNode.SelfScopeSubIndex;
    }

    public SyntaxToken KeywordToken { get; set; }
    public SyntaxToken IdentifierToken { get; set; }
    public int AbsolutePathId { get; set; }
    public int ParentScopeSubIndex { get; set; } = -1;
    public int SelfScopeSubIndex { get; set; } = -1;
}
