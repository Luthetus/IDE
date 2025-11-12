using Clair.Extensions.CompilerServices;
using Clair.Extensions.CompilerServices.Syntax;
using Clair.Extensions.CompilerServices.Syntax.Enums;
using Clair.Extensions.CompilerServices.Syntax.NodeValues;
using Clair.TextEditor.RazorLib.Decorations.Models;
using Clair.TextEditor.RazorLib.Lexers.Models;

namespace Clair.CompilerServices.CSharp.Facts;

public partial class CSharpFacts
{
    /// <summary>
    /// 'string.Empty' is used as a special case to store language primitives,
    /// since 'string.Empty' is not a valid 'ResourceUri' for the 'TextEditorService'.
    ///
    /// Perhaps this is odd to do, but the TextEditorTextSpan requires "source text"
    /// to read from.
    ///
    /// So doing this means any special case handling of the language primitives
    /// will "just work" regardless of who tries to read them.
    ///
    /// go-to definition won't do anything since string.Empty isn't a valid file path.
    ///
    /// In particular, this 'string.Empty' file only exists in the CSharpCompilerService's resources.
    /// It never actually gets added to the TextEditorService as a TextEditorModel, only a CSharpResource.
    /// 
    /// The file contents:
    ///     "NotApplicable empty" + " void int char string bool var"
    /// 
    /// 'Clair.Extensions.CompilerServices.TypeFacts' contains some types as well and those are the first to appear in the text.
    ///
    /// I just got this to work.
    /// It feels super hacky, so once I think of a better way to do this I'd like to change it.
    /// </summary>
    public static class Types
    {
        static Types()
        {
            VoidValue = new SyntaxNodeValue(
                new SyntaxToken(
                    SyntaxKind.IdentifierToken,
                    new TextEditorTextSpan(20, 24, (byte)GenericDecorationKind.None, byteIndex: 0, charIntSum: 434)),                ResourceUri.EmptyAbsolutePathId,                isFabricated: false,                SyntaxKind.TypeDefinitionNode,                parentScopeSubIndex: 0,                selfScopeSubIndex: 1,                traitsIndex: 0);
            VoidTraits = new TypeDefinitionTraits(
                indexPartialTypeDefinition: 0,                inheritedTypeReference: TypeFacts.NotApplicable.ToTypeReference(),                AccessModifierKind.Public,                StorageModifierKind.Class,                isKeywordType: true,                openAngleBracketToken: default,                offsetGenericParameterEntryList: -1,
                lengthGenericParameterEntryList: 0,                closeAngleBracketToken: default,
                openParenthesisToken: default,
                offsetFunctionArgumentEntryList: -1,
                lengthFunctionArgumentEntryList: 0,
                closeParenthesisToken: default,
                TextSourceKind.Explicit);            VoidTypeReferenceValue = new TypeReferenceValue(                VoidValue.IdentifierToken,                openAngleBracketToken: default,                offsetGenericParameterEntryList: -1,                lengthGenericParameterEntryList: 0,                closeAngleBracketToken: default,                isKeywordType: true,                TypeKind.None,                hasQuestionMark: false,                arrayRank: 0,                isFabricated: false)
            {
                ExplicitDefinitionTextSpan = VoidValue.IdentifierToken.TextSpan,
                ExplicitDefinitionAbsolutePathId = VoidValue.AbsolutePathId
            };            IntValue = new SyntaxNodeValue(
                new SyntaxToken(
                    SyntaxKind.IdentifierToken,
                    new TextEditorTextSpan(25, 28, (byte)GenericDecorationKind.None, byteIndex: 0, charIntSum: 331)),                ResourceUri.EmptyAbsolutePathId,                isFabricated: false,                SyntaxKind.TypeDefinitionNode,                parentScopeSubIndex: 0,                selfScopeSubIndex: 1,                traitsIndex: 0);
            IntTraits = new TypeDefinitionTraits(
                indexPartialTypeDefinition: 0,                inheritedTypeReference: TypeFacts.NotApplicable.ToTypeReference(),                AccessModifierKind.Public,                StorageModifierKind.Class,                isKeywordType: true,                openAngleBracketToken: default,                offsetGenericParameterEntryList: -1,
                lengthGenericParameterEntryList: 0,                closeAngleBracketToken: default,
                openParenthesisToken: default,
                offsetFunctionArgumentEntryList: -1,
                lengthFunctionArgumentEntryList: 0,
                closeParenthesisToken: default,
                TextSourceKind.Explicit);            IntTypeReferenceValue = new TypeReferenceValue(                IntValue.IdentifierToken,                openAngleBracketToken: default,                offsetGenericParameterEntryList: -1,                lengthGenericParameterEntryList: 0,                closeAngleBracketToken: default,                isKeywordType: true,                TypeKind.None,                hasQuestionMark: false,                arrayRank: 0,                isFabricated: false)
            {
                ExplicitDefinitionTextSpan = IntValue.IdentifierToken.TextSpan,
                ExplicitDefinitionAbsolutePathId = IntValue.AbsolutePathId
            };            CharValue = new SyntaxNodeValue(
                new SyntaxToken(
                    SyntaxKind.IdentifierToken,
                    new TextEditorTextSpan(29, 33, (byte)GenericDecorationKind.None, byteIndex: 0, charIntSum: 414)),                ResourceUri.EmptyAbsolutePathId,                isFabricated: false,                SyntaxKind.TypeDefinitionNode,                parentScopeSubIndex: 0,                selfScopeSubIndex: 1,                traitsIndex: 0);
            CharTraits = new TypeDefinitionTraits(
                indexPartialTypeDefinition: 0,                inheritedTypeReference: TypeFacts.NotApplicable.ToTypeReference(),                AccessModifierKind.Public,                StorageModifierKind.Class,                isKeywordType: true,                openAngleBracketToken: default,                offsetGenericParameterEntryList: -1,
                lengthGenericParameterEntryList: 0,                closeAngleBracketToken: default,
                openParenthesisToken: default,
                offsetFunctionArgumentEntryList: -1,
                lengthFunctionArgumentEntryList: 0,
                closeParenthesisToken: default,
                TextSourceKind.Explicit);            CharTypeReferenceValue = new TypeReferenceValue(                CharValue.IdentifierToken,                openAngleBracketToken: default,                offsetGenericParameterEntryList: -1,                lengthGenericParameterEntryList: 0,                closeAngleBracketToken: default,                isKeywordType: true,                TypeKind.None,                hasQuestionMark: false,                arrayRank: 0,                isFabricated: false)
            {
                ExplicitDefinitionTextSpan = CharValue.IdentifierToken.TextSpan,
                ExplicitDefinitionAbsolutePathId = CharValue.AbsolutePathId
            };            StringValue = new SyntaxNodeValue(
                new SyntaxToken(
                    SyntaxKind.IdentifierToken,
                    new TextEditorTextSpan(34, 40, (byte)GenericDecorationKind.None, byteIndex: 0, charIntSum: 663)),                ResourceUri.EmptyAbsolutePathId,                isFabricated: false,                SyntaxKind.TypeDefinitionNode,                parentScopeSubIndex: 0,                selfScopeSubIndex: 1,                traitsIndex: 0);
            StringTraits = new TypeDefinitionTraits(
                indexPartialTypeDefinition: 0,                inheritedTypeReference: TypeFacts.NotApplicable.ToTypeReference(),                AccessModifierKind.Public,                StorageModifierKind.Class,                isKeywordType: true,                openAngleBracketToken: default,                offsetGenericParameterEntryList: -1,
                lengthGenericParameterEntryList: 0,                closeAngleBracketToken: default,
                openParenthesisToken: default,
                offsetFunctionArgumentEntryList: -1,
                lengthFunctionArgumentEntryList: 0,
                closeParenthesisToken: default,
                TextSourceKind.Explicit);            StringTypeReferenceValue = new TypeReferenceValue(                StringValue.IdentifierToken,                openAngleBracketToken: default,                offsetGenericParameterEntryList: -1,                lengthGenericParameterEntryList: 0,                closeAngleBracketToken: default,                isKeywordType: true,                TypeKind.None,                hasQuestionMark: false,                arrayRank: 0,                isFabricated: false)
            {
                ExplicitDefinitionTextSpan = StringValue.IdentifierToken.TextSpan,
                ExplicitDefinitionAbsolutePathId = StringValue.AbsolutePathId
            };

            BoolValue = new SyntaxNodeValue(
                new SyntaxToken(
                    SyntaxKind.IdentifierToken,
                    new TextEditorTextSpan(41, 45, (byte)GenericDecorationKind.None, byteIndex: 0, charIntSum: 428)),                ResourceUri.EmptyAbsolutePathId,                isFabricated: false,                SyntaxKind.TypeDefinitionNode,                parentScopeSubIndex: 0,                selfScopeSubIndex: 1,                traitsIndex: 0);
            BoolTraits = new TypeDefinitionTraits(                indexPartialTypeDefinition: 0,                inheritedTypeReference: TypeFacts.NotApplicable.ToTypeReference(),                AccessModifierKind.Public,                StorageModifierKind.Class,                isKeywordType: true,                openAngleBracketToken: default,                offsetGenericParameterEntryList: -1,
                lengthGenericParameterEntryList: 0,                closeAngleBracketToken: default,
                openParenthesisToken: default,
                offsetFunctionArgumentEntryList: -1,
                lengthFunctionArgumentEntryList: 0,
                closeParenthesisToken: default,
                TextSourceKind.Explicit);            BoolTypeReferenceValue = new TypeReferenceValue(                BoolValue.IdentifierToken,                openAngleBracketToken: default,                offsetGenericParameterEntryList: -1,                lengthGenericParameterEntryList: 0,                closeAngleBracketToken: default,                isKeywordType: true,                TypeKind.None,                hasQuestionMark: false,                arrayRank: 0,                isFabricated: false)
            {
                ExplicitDefinitionTextSpan = BoolValue.IdentifierToken.TextSpan,
                ExplicitDefinitionAbsolutePathId = BoolValue.AbsolutePathId
            };            VarValue = new SyntaxNodeValue(
                new SyntaxToken(
                    SyntaxKind.IdentifierToken,
                    new TextEditorTextSpan(46, 49, (byte)GenericDecorationKind.None, byteIndex: 0, charIntSum: 329)),                ResourceUri.EmptyAbsolutePathId,                isFabricated: false,                SyntaxKind.TypeDefinitionNode,                parentScopeSubIndex: 0,                selfScopeSubIndex: 1,                traitsIndex: 0);
            VarTraits = new TypeDefinitionTraits(                indexPartialTypeDefinition: 0,                inheritedTypeReference: TypeFacts.NotApplicable.ToTypeReference(),                AccessModifierKind.Public,                StorageModifierKind.Class,                isKeywordType: true,                openAngleBracketToken: default,                offsetGenericParameterEntryList: -1,
                lengthGenericParameterEntryList: 0,                closeAngleBracketToken: default,
                openParenthesisToken: default,
                offsetFunctionArgumentEntryList: -1,
                lengthFunctionArgumentEntryList: 0,
                closeParenthesisToken: default,
                TextSourceKind.Explicit);            VarTypeReferenceValue = new TypeReferenceValue(                VarValue.IdentifierToken,                openAngleBracketToken: default,                offsetGenericParameterEntryList: -1,                lengthGenericParameterEntryList: 0,                closeAngleBracketToken: default,                isKeywordType: true,                TypeKind.None,                hasQuestionMark: false,                arrayRank: 0,                isFabricated: false)
            {
                ExplicitDefinitionTextSpan = VarValue.IdentifierToken.TextSpan,
                ExplicitDefinitionAbsolutePathId = VarValue.AbsolutePathId
            };        }

        public static SyntaxNodeValue VoidValue { get; }
        public static TypeDefinitionTraits VoidTraits { get; }
        public static TypeReferenceValue VoidTypeReferenceValue { get; }

        public static SyntaxNodeValue IntValue { get; }
        public static TypeDefinitionTraits IntTraits { get; }
        public static TypeReferenceValue IntTypeReferenceValue { get; }

        public static SyntaxNodeValue CharValue { get; }
        public static TypeDefinitionTraits CharTraits { get; }
        public static TypeReferenceValue CharTypeReferenceValue { get; }

        public static SyntaxNodeValue StringValue { get; }
        public static TypeDefinitionTraits StringTraits { get; }
        public static TypeReferenceValue StringTypeReferenceValue { get; }

        public static SyntaxNodeValue BoolValue { get; }
        public static TypeDefinitionTraits BoolTraits { get; }
        public static TypeReferenceValue BoolTypeReferenceValue { get; }

        public static SyntaxNodeValue VarValue { get; }
        public static TypeDefinitionTraits VarTraits { get; }
        public static TypeReferenceValue VarTypeReferenceValue { get; }
    }
}
