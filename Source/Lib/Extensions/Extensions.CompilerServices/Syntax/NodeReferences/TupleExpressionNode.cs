using Clair.Extensions.CompilerServices.Syntax.Interfaces;
using Clair.Extensions.CompilerServices.Syntax.NodeValues;

namespace Clair.Extensions.CompilerServices.Syntax.NodeReferences;

public sealed class TupleExpressionNode : IExpressionNode
{
    public TypeReferenceValue ResultTypeReference { get; } = TypeFacts.Empty.ToTypeReference();

    // public List<IExpressionNode> InnerExpressionList { get; } = new();

    public int ParentScopeSubIndex { get; set; }
    public bool IsFabricated { get; init; }
    public SyntaxKind SyntaxKind => SyntaxKind.TupleExpressionNode;
}
