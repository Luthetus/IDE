using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.Extensions.CompilerServices.Syntax.Enums;
using Clair.Extensions.CompilerServices.Syntax.NodeReferences;

namespace Clair.Extensions.CompilerServices.Syntax.NodeValues;

public record struct TypeReferenceValue
{
    public TypeReferenceValue(
        SyntaxToken typeIdentifier,
        SyntaxToken openAngleBracketToken,
        int offsetGenericParameterEntryList,
        int lengthGenericParameterEntryList,
        SyntaxToken closeAngleBracketToken,
        bool isKeywordType,
        TypeKind typeKind,
        bool hasQuestionMark,
        int arrayRank,
        bool isFabricated)
    {
        IsKeywordType = isKeywordType;
        TypeIdentifierToken = typeIdentifier;
        
        OpenAngleBracketToken = openAngleBracketToken;
        OffsetGenericParameterEntryList = offsetGenericParameterEntryList;
        LengthGenericParameterEntryList = lengthGenericParameterEntryList;
        CloseAngleBracketToken = closeAngleBracketToken;
        
        TypeKind = typeKind;
        HasQuestionMark = hasQuestionMark;
        ArrayRank = arrayRank;
        IsFabricated = isFabricated;
    }
    
    public TypeReferenceValue(TypeClauseNode typeClauseNode)
    {
        IsKeywordType = typeClauseNode.IsKeywordType;
        TypeIdentifierToken = typeClauseNode.TypeIdentifierToken;
        
        OpenAngleBracketToken = typeClauseNode.OpenAngleBracketToken;
        OffsetGenericParameterEntryList = typeClauseNode.OffsetGenericParameterEntryList;
        LengthGenericParameterEntryList = typeClauseNode.LengthGenericParameterEntryList;
        CloseAngleBracketToken = typeClauseNode.CloseAngleBracketToken;
        
        TypeKind = typeClauseNode.TypeKind;
        HasQuestionMark = typeClauseNode.HasQuestionMark;
        ArrayRank = typeClauseNode.ArrayRank;
        IsFabricated = typeClauseNode.IsFabricated;
        ExplicitDefinitionTextSpan = typeClauseNode.ExplicitDefinitionTextSpan;
        ExplicitDefinitionAbsolutePathId = typeClauseNode.ExplicitDefinitionAbsolutePathId;
    }
    
    public TypeReferenceValue(TypeDefinitionNode typeDefinitionNode)
    {
        IsKeywordType = typeDefinitionNode.IsKeywordType;
        TypeIdentifierToken = typeDefinitionNode.TypeIdentifierToken;
        
        OpenAngleBracketToken = typeDefinitionNode.OpenAngleBracketToken;
        OffsetGenericParameterEntryList = typeDefinitionNode.OffsetGenericParameterEntryList;
        LengthGenericParameterEntryList = typeDefinitionNode.LengthGenericParameterEntryList;
        CloseAngleBracketToken = typeDefinitionNode.CloseAngleBracketToken;
        
        IsFabricated = typeDefinitionNode.IsFabricated;
        ExplicitDefinitionTextSpan = typeDefinitionNode.TypeIdentifierToken.TextSpan;
        ExplicitDefinitionAbsolutePathId = typeDefinitionNode.AbsolutePathId;
    }

    public SyntaxToken TypeIdentifierToken { get; }
    
    public SyntaxToken OpenAngleBracketToken { get; }
    public int OffsetGenericParameterEntryList { get; set; }
    public int LengthGenericParameterEntryList { get; set; }
    public SyntaxToken CloseAngleBracketToken { get; private set; }
    
    public bool IsKeywordType { get; }
    public TypeKind TypeKind { get; }
    public bool HasQuestionMark { get; }
    public int ArrayRank { get; init; }
    public bool IsFabricated { get; }
    
    public TextEditorTextSpan ExplicitDefinitionTextSpan { get; set; }
    public int ExplicitDefinitionAbsolutePathId { get; set; }
    
    public bool IsImplicit { get; set; }

    public readonly bool IsDefault()
    {
        return
            TypeIdentifierToken.TextSpan.StartInclusiveIndex == 0 &&
            TypeIdentifierToken.TextSpan.EndExclusiveIndex == 0;
    }
}
