using Clair.Extensions.CompilerServices.Syntax;
using Clair.Extensions.CompilerServices.Syntax.Enums;
using Clair.Extensions.CompilerServices.Syntax.NodeReferences;
using Clair.Extensions.CompilerServices.Syntax.NodeValues;

namespace Clair.CompilerServices.CSharp.ParserCase.Internals;

public static partial class Parser
{
    /// <summary>Function invocation which uses the 'out' keyword.</summary>
    public static VariableDeclarationNode? HandleVariableDeclarationExpression(
        TypeClauseNode consumedTypeClauseNode,
        SyntaxToken consumedIdentifierToken,
        VariableKind variableKind,
        ref CSharpParserState parserModel)
    {
        var variableDeclarationNode = parserModel.Rent_VariableDeclarationNode();
        variableDeclarationNode.TypeReference = new TypeReferenceValue(consumedTypeClauseNode);
        variableDeclarationNode.IdentifierToken = consumedIdentifierToken;
        variableDeclarationNode.VariableKind = variableKind;
        variableDeclarationNode.AbsolutePathId = parserModel.AbsolutePathId;
        
        parserModel.Return_TypeClauseNode(consumedTypeClauseNode);
        
        parserModel.BindVariableDeclarationNode(variableDeclarationNode);
        return variableDeclarationNode;
    }
}
