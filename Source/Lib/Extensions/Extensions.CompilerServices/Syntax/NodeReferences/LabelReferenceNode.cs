using Clair.Extensions.CompilerServices.Syntax.Interfaces;
using Clair.Extensions.CompilerServices.Syntax.NodeValues;

namespace Clair.Extensions.CompilerServices.Syntax.NodeReferences;

public sealed class LabelReferenceNode : IExpressionNode
{
    public LabelReferenceNode(SyntaxToken identifierToken)
    {
        IdentifierToken = identifierToken;
    }

    public SyntaxToken IdentifierToken { get; }

    TypeReferenceValue IExpressionNode.ResultTypeReference => TypeFacts.Pseudo.ToTypeReference();

    public int ParentScopeSubIndex { get; set; }
    public bool IsFabricated { get; init; }
    public SyntaxKind SyntaxKind => SyntaxKind.LabelReferenceNode;
}


