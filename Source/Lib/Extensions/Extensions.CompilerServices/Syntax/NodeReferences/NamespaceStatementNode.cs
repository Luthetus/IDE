using Clair.Extensions.CompilerServices.Syntax.Enums;
using Clair.Extensions.CompilerServices.Syntax.Interfaces;

namespace Clair.Extensions.CompilerServices.Syntax.NodeReferences;

public sealed class NamespaceStatementNode : ICodeBlockOwner
{
    public NamespaceStatementNode(
        SyntaxToken keywordToken,
        SyntaxToken identifierToken,
        int absolutePathId,
        TextSourceKind textSourceKind)
    {
        KeywordToken = keywordToken;
        IdentifierToken = identifierToken;
        AbsolutePathId = absolutePathId;
        TextSourceKind = textSourceKind;
    }

    public SyntaxToken KeywordToken { get; set; }
    public SyntaxToken IdentifierToken { get; set; }
    public int AbsolutePathId { get; set; }
    public TextSourceKind TextSourceKind { get; set; }

    public int ParentScopeSubIndex { get; set; } = -1;
    public int SelfScopeSubIndex { get; set; } = -1;

    public bool _isFabricated;
    public bool IsFabricated
    {
        get => _isFabricated;
        init => _isFabricated = value;
    }
    public SyntaxKind SyntaxKind => SyntaxKind.NamespaceStatementNode;
}
