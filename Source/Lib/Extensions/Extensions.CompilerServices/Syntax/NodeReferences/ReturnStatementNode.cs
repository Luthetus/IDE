using Clair.Extensions.CompilerServices.Syntax.Interfaces;
using Clair.Extensions.CompilerServices.Syntax.NodeValues;

namespace Clair.Extensions.CompilerServices.Syntax.NodeReferences;

public sealed class ReturnStatementNode : IExpressionNode
{
    public ReturnStatementNode(SyntaxToken keywordToken, IExpressionNode expressionNode)
    {
        KeywordToken = keywordToken;
        // ExpressionNode = expressionNode;
        
        ResultTypeReference = expressionNode.ResultTypeReference;
    }

    public SyntaxToken KeywordToken { get; }
    // public IExpressionNode ExpressionNode { get; set; }
    public TypeReferenceValue ResultTypeReference { get; }

    public int ParentScopeSubIndex { get; set; }
    public bool IsFabricated { get; init; }
    public SyntaxKind SyntaxKind => SyntaxKind.ReturnStatementNode;
}
