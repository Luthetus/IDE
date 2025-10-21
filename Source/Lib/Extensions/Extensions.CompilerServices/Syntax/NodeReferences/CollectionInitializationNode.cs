using Clair.Extensions.CompilerServices.Syntax.Interfaces;
using Clair.Extensions.CompilerServices.Syntax.NodeValues;

namespace Clair.Extensions.CompilerServices.Syntax.NodeReferences;

public sealed class CollectionInitializationNode : IExpressionNode
{
    public int ParentScopeSubIndex { get; set; }
    public bool IsFabricated { get; init; }
    public SyntaxKind SyntaxKind => SyntaxKind.CollectionInitializationNode;
    
    public TypeReferenceValue ResultTypeReference { get; }
    
    public bool IsClosed { get; set; }
}
