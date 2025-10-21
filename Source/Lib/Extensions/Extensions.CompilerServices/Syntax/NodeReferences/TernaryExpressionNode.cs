using Clair.Extensions.CompilerServices.Syntax.Interfaces;
using Clair.Extensions.CompilerServices.Syntax.NodeValues;

namespace Clair.Extensions.CompilerServices.Syntax.NodeReferences;

public class TernaryExpressionNode : IExpressionNode
{
    public TernaryExpressionNode(TypeReferenceValue resultTypeReference)
    {
        ResultTypeReference = resultTypeReference;
    }

    public bool GotType { get; set; }
    public bool SeenSecondTime { get; set; }
    public bool HasCompleted { get; set; }

    // take type of expression between '?' and ':'
    public TypeReferenceValue ResultTypeReference { get; set; }

    public int ParentScopeSubIndex { get; set; }
    public bool IsFabricated { get; init; }
    
    public SyntaxKind SyntaxKind => SyntaxKind.TernaryExpressionNode;
}
