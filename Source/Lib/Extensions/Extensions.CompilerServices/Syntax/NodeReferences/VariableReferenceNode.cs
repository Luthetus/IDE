using Clair.Extensions.CompilerServices.Syntax.Interfaces;
using Clair.Extensions.CompilerServices.Syntax.NodeValues;

namespace Clair.Extensions.CompilerServices.Syntax.NodeReferences;

public sealed class VariableReferenceNode : IExpressionNode
{
    public VariableReferenceNode(
        SyntaxToken variableIdentifierToken,
        TypeReferenceValue resultTypeReference)
    {
        VariableIdentifierToken = variableIdentifierToken;
        
        if (resultTypeReference.IsDefault())
        {
            ResultTypeReference = TypeFacts.Empty.ToTypeReference();
        }
        else
        {
            ResultTypeReference = resultTypeReference;
        }
    }
    
    public VariableReferenceNode(VariableReferenceValue variableReference)
    {
        VariableIdentifierToken = variableReference.VariableIdentifierToken;
        ResultTypeReference = variableReference.ResultTypeReference;
        IsFabricated = variableReference.IsFabricated;
    }

    public int ParentScopeSubIndex { get; set; }
    public bool _isFabricated;

    public SyntaxToken VariableIdentifierToken { get; set; }
    public TypeReferenceValue TypeReference => ResultTypeReference;
    public TypeReferenceValue ResultTypeReference { get; set; }

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
    public SyntaxKind SyntaxKind => SyntaxKind.VariableReferenceNode;
}
