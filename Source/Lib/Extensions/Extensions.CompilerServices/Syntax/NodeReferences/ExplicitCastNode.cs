using Clair.Extensions.CompilerServices.Syntax.Interfaces;
using Clair.Extensions.CompilerServices.Syntax.NodeValues;

namespace Clair.Extensions.CompilerServices.Syntax.NodeReferences;

public sealed class ExplicitCastNode : IExpressionNode
{
    public ExplicitCastNode(
        SyntaxToken openParenthesisToken,
        TypeReferenceValue resultTypeReference,
        SyntaxToken closeParenthesisToken)
    {
        OpenParenthesisToken = openParenthesisToken;
        ResultTypeReference = resultTypeReference;
        CloseParenthesisToken = closeParenthesisToken;
    }

    public ExplicitCastNode(SyntaxToken openParenthesisToken, TypeReferenceValue resultTypeReference)
        : this(openParenthesisToken, resultTypeReference, default)
    {
    }

    public SyntaxToken OpenParenthesisToken { get; set; }
    public TypeReferenceValue ResultTypeReference { get; set; }
    public SyntaxToken CloseParenthesisToken { get; set; }

    public int ParentScopeSubIndex { get; set; }
    public bool _isFabricated;
    public bool IsFabricated
    {
        get => _isFabricated;
        init => _isFabricated = value;
    }
    public SyntaxKind SyntaxKind => SyntaxKind.ExplicitCastNode;
}
