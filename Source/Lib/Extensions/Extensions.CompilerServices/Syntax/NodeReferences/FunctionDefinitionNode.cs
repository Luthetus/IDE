using Clair.Extensions.CompilerServices.Syntax.Enums;
using Clair.Extensions.CompilerServices.Syntax.Interfaces;
using Clair.Extensions.CompilerServices.Syntax.NodeValues;

namespace Clair.Extensions.CompilerServices.Syntax.NodeReferences;

/// <summary>
/// TODO: Track the open and close braces for the function body.
/// </summary>
public sealed class FunctionDefinitionNode : ICodeBlockOwner, IFunctionDefinitionNode, IGenericParameterNode
{
    public FunctionDefinitionNode(
        AccessModifierKind accessModifierKind,
        TypeReferenceValue returnTypeReference,
        SyntaxToken functionIdentifierToken,
        SyntaxToken openAngleBracketToken,
        int offsetGenericParameterEntryList,
        int lengthGenericParameterEntryList,
        SyntaxToken closeAngleBracketToken,
        SyntaxToken openParenthesisToken,
        int offsetFunctionArgumentEntryList,
        int lengthFunctionArgumentEntryList,
        SyntaxToken closeParenthesisToken,
        int absolutePathId)
    {
        AccessModifierKind = accessModifierKind;
        ReturnTypeReference = returnTypeReference;
        FunctionIdentifierToken = functionIdentifierToken;
        
        OpenAngleBracketToken = openAngleBracketToken;
        OffsetGenericParameterEntryList = offsetGenericParameterEntryList;
        LengthGenericParameterEntryList = lengthGenericParameterEntryList;
        CloseAngleBracketToken = closeAngleBracketToken;
        
        OpenParenthesisToken = openParenthesisToken;
        OffsetFunctionArgumentEntryList = offsetFunctionArgumentEntryList;
        LengthFunctionArgumentEntryList = lengthFunctionArgumentEntryList;
        CloseParenthesisToken = closeParenthesisToken;
        AbsolutePathId = absolutePathId;
    }

    public AccessModifierKind AccessModifierKind { get; set; }
    public SyntaxToken FunctionIdentifierToken { get; set; }
    
    public SyntaxToken OpenAngleBracketToken { get; set; }
    public int OffsetGenericParameterEntryList { get; set; }
    public int LengthGenericParameterEntryList { get; set; }
    public SyntaxToken CloseAngleBracketToken { get; set; }
    
    public SyntaxToken OpenParenthesisToken { get; set; }
    public int OffsetFunctionArgumentEntryList { get; set; }
    public int LengthFunctionArgumentEntryList { get; set; }
    public SyntaxToken CloseParenthesisToken { get; set; }
    public int AbsolutePathId { get; set; }
    public int IndexMethodOverloadDefinition { get; set; } = -1;

    public int ParentScopeSubIndex { get; set; } = -1;
    public int SelfScopeSubIndex { get; set; } = -1;

    public bool _isFabricated;
    public bool IsFabricated
    {
        get => _isFabricated;
        init => _isFabricated = value;
    }
    
    public SyntaxKind SyntaxKind => SyntaxKind.FunctionDefinitionNode;
    
    public bool IsParsingGenericParameters { get; set; }
    
    /// <summary>
    /// TraitsIndex can be -1 here to indicate they weren't set yet.
    /// </summary>
    public int TraitsIndex { get; set; } = -1;
    
    TypeReferenceValue IExpressionNode.ResultTypeReference => TypeFacts.Pseudo.ToTypeReference();
    public TypeReferenceValue ReturnTypeReference { get; set; }
}
