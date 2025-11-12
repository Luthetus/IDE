using Clair.Extensions.CompilerServices.Syntax.Enums;
using Clair.Extensions.CompilerServices.Syntax.NodeReferences;

namespace Clair.Extensions.CompilerServices.Syntax.NodeValues;

public struct TypeDefinitionTraits
{
    public TypeDefinitionTraits(TypeDefinitionNode typeDefinitionNode)
    {
        IndexPartialTypeDefinition = typeDefinitionNode.IndexPartialTypeDefinition;
        InheritedTypeReference = typeDefinitionNode.InheritedTypeReference;
        AccessModifierKind = typeDefinitionNode.AccessModifierKind;
        StorageModifierKind = typeDefinitionNode.StorageModifierKind;
        IsKeywordType = typeDefinitionNode.IsKeywordType;

        OpenAngleBracketToken = typeDefinitionNode.OpenAngleBracketToken;
        OffsetGenericParameterEntryList = typeDefinitionNode.OffsetGenericParameterEntryList;
        LengthGenericParameterEntryList = typeDefinitionNode.LengthGenericParameterEntryList;
        CloseAngleBracketToken = typeDefinitionNode.CloseAngleBracketToken;

        OpenParenthesisToken = typeDefinitionNode.OpenParenthesisToken;
        OffsetFunctionArgumentEntryList = typeDefinitionNode.OffsetFunctionArgumentEntryList;
        LengthFunctionArgumentEntryList = typeDefinitionNode.LengthFunctionArgumentEntryList;
        CloseParenthesisToken = typeDefinitionNode.CloseParenthesisToken;
        
        TextSourceKind = typeDefinitionNode.TextSourceKind;
    }

    public TypeDefinitionTraits(
        int indexPartialTypeDefinition,
        TypeReferenceValue inheritedTypeReference,
        AccessModifierKind accessModifierKind,
        StorageModifierKind storageModifierKind,
        bool isKeywordType,
        SyntaxToken openAngleBracketToken,
        int offsetGenericParameterEntryList,
        int lengthGenericParameterEntryList,
        SyntaxToken closeAngleBracketToken,
        SyntaxToken openParenthesisToken,
        int offsetFunctionArgumentEntryList,
        int lengthFunctionArgumentEntryList,
        SyntaxToken closeParenthesisToken,
        TextSourceKind textSourceKind)
    {
        IndexPartialTypeDefinition = indexPartialTypeDefinition;
        InheritedTypeReference = inheritedTypeReference;
        AccessModifierKind = accessModifierKind;
        StorageModifierKind = storageModifierKind;
        IsKeywordType = isKeywordType;

        OpenAngleBracketToken = openAngleBracketToken;
        OffsetGenericParameterEntryList = offsetGenericParameterEntryList;
        LengthGenericParameterEntryList = lengthGenericParameterEntryList;
        CloseAngleBracketToken = closeAngleBracketToken;

        OpenParenthesisToken = openParenthesisToken;
        OffsetFunctionArgumentEntryList = offsetFunctionArgumentEntryList;
        LengthFunctionArgumentEntryList = lengthFunctionArgumentEntryList;
        CloseParenthesisToken = closeParenthesisToken;
    
        TextSourceKind = textSourceKind;
    }

    public int IndexPartialTypeDefinition { get; set; } = -1;
    public TypeReferenceValue InheritedTypeReference { get; set; }
    public AccessModifierKind AccessModifierKind { get; set; }
    public StorageModifierKind StorageModifierKind { get; set; }
    public bool IsKeywordType { get; init; }

    public SyntaxToken OpenAngleBracketToken { get; set; }
    public int OffsetGenericParameterEntryList { get; set; }
    public int LengthGenericParameterEntryList { get; set; }
    public SyntaxToken CloseAngleBracketToken { get; set; }

    public SyntaxToken OpenParenthesisToken { get; set; }
    public int OffsetFunctionArgumentEntryList { get; set; }
    public int LengthFunctionArgumentEntryList { get; set; }
    public SyntaxToken CloseParenthesisToken { get; set; }
    
    public TextSourceKind TextSourceKind { get; set; }
}
