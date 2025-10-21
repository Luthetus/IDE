using Clair.Extensions.CompilerServices.Syntax.Interfaces;
using Clair.Extensions.CompilerServices.Syntax.NodeValues;

namespace Clair.Extensions.CompilerServices.Syntax.NodeReferences;

public sealed class LiteralExpressionNode : IExpressionNode
{
    public LiteralExpressionNode(SyntaxToken literalSyntaxToken, TypeReferenceValue typeReference)
    {
        LiteralSyntaxToken = literalSyntaxToken;
        ResultTypeReference = typeReference;
    }
    
    public bool _isFabricated;

    public SyntaxToken LiteralSyntaxToken { get; set; }
    public TypeReferenceValue ResultTypeReference { get; set; }

    public int ParentScopeSubIndex { get; set; }
    public bool IsFabricated
    {
        get
        {
            return _isFabricated;
        }
        init
        {
            _isFabricated = value;
        }
    }
    public SyntaxKind SyntaxKind => SyntaxKind.LiteralExpressionNode;
}
