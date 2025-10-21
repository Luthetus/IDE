using Clair.Extensions.CompilerServices.Syntax.Interfaces;
using Clair.Extensions.CompilerServices.Syntax.NodeValues;

namespace Clair.Extensions.CompilerServices.Syntax.NodeReferences;

public sealed class WithExpressionNode : IExpressionNode
{
    public WithExpressionNode(VariableReferenceValue variableReference)
    {
        VariableReference = variableReference;
        ResultTypeReference = variableReference.ResultTypeReference;
    }

    public VariableReferenceValue VariableReference { get; }
    public TypeReferenceValue ResultTypeReference { get; }

    public int ParentScopeSubIndex { get; set; }
    public bool IsFabricated { get; init; }
    public SyntaxKind SyntaxKind => SyntaxKind.WithExpressionNode;
}
