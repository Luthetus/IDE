using Clair.Extensions.CompilerServices.Syntax.NodeReferences;

namespace Clair.Extensions.CompilerServices.Syntax.NodeValues;

public struct SyntaxNodeValue
{
    public SyntaxNodeValue(
        TypeDefinitionNode typeDefinitionNode,
        List<TypeDefinitionTraits> typeDefinitionTraitsList)
    {
        IdentifierToken = typeDefinitionNode.TypeIdentifierToken;
        AbsolutePathId = typeDefinitionNode.AbsolutePathId;
        IsFabricated = typeDefinitionNode.IsFabricated;
        SyntaxKind = typeDefinitionNode.SyntaxKind;
        ParentScopeSubIndex = typeDefinitionNode.ParentScopeSubIndex;
        SelfScopeSubIndex = typeDefinitionNode.SelfScopeSubIndex;
        TraitsIndex = typeDefinitionTraitsList.Count;
        typeDefinitionNode.TraitsIndex = TraitsIndex;

        typeDefinitionTraitsList.Add(new(typeDefinitionNode));
    }

    public SyntaxNodeValue(NamespaceStatementNode namespaceStatementNode)
    {
        IdentifierToken = namespaceStatementNode.IdentifierToken;
        AbsolutePathId = namespaceStatementNode.AbsolutePathId;
        IsFabricated = namespaceStatementNode.IsFabricated;
        SyntaxKind = namespaceStatementNode.SyntaxKind;
        ParentScopeSubIndex = namespaceStatementNode.ParentScopeSubIndex;
        SelfScopeSubIndex = namespaceStatementNode.SelfScopeSubIndex;
        TraitsIndex = -1;
    }

    public SyntaxNodeValue(
        FunctionDefinitionNode functionDefinitionNode,
        List<FunctionDefinitionTraits> functionDefinitionTraitsList)
    {
        IdentifierToken = functionDefinitionNode.FunctionIdentifierToken;
        AbsolutePathId = functionDefinitionNode.AbsolutePathId;
        IsFabricated = functionDefinitionNode.IsFabricated;
        SyntaxKind = functionDefinitionNode.SyntaxKind;
        ParentScopeSubIndex = functionDefinitionNode.ParentScopeSubIndex;
        SelfScopeSubIndex = functionDefinitionNode.SelfScopeSubIndex;
        TraitsIndex = functionDefinitionTraitsList.Count;
        functionDefinitionNode.TraitsIndex = TraitsIndex;

        functionDefinitionTraitsList.Add(new(functionDefinitionNode));
    }

    public SyntaxNodeValue(
        ConstructorDefinitionNode constructorDefinitionNode,
        List<ConstructorDefinitionTraits> constructorDefinitionTraitsList)
    {
        IdentifierToken = constructorDefinitionNode.FunctionIdentifier;
        AbsolutePathId = constructorDefinitionNode.AbsolutePathId;
        IsFabricated = constructorDefinitionNode.IsFabricated;
        SyntaxKind = constructorDefinitionNode.SyntaxKind;
        ParentScopeSubIndex = constructorDefinitionNode.ParentScopeSubIndex;
        SelfScopeSubIndex = constructorDefinitionNode.SelfScopeSubIndex;
        TraitsIndex = constructorDefinitionTraitsList.Count;

        constructorDefinitionTraitsList.Add(new(constructorDefinitionNode));
    }
    
    public SyntaxNodeValue(
        VariableDeclarationNode variableDeclarationNode,
        List<VariableDeclarationTraits> variableDeclarationTraitsList)
    {
        IdentifierToken = variableDeclarationNode.IdentifierToken;
        AbsolutePathId = variableDeclarationNode.AbsolutePathId;
        IsFabricated = variableDeclarationNode.IsFabricated;
        SyntaxKind = variableDeclarationNode.SyntaxKind;
        ParentScopeSubIndex = variableDeclarationNode.ParentScopeSubIndex;
        SelfScopeSubIndex = -1;
        TraitsIndex = variableDeclarationTraitsList.Count;
        variableDeclarationNode.TraitsIndex = TraitsIndex;

        variableDeclarationTraitsList.Add(new(variableDeclarationNode));
    }

    public SyntaxNodeValue(
        LabelDeclarationNode labelDeclarationNode,
        int absolutePathId)
    {
        IdentifierToken = labelDeclarationNode.IdentifierToken;
        AbsolutePathId = absolutePathId;
        IsFabricated = labelDeclarationNode.IsFabricated;
        SyntaxKind = labelDeclarationNode.SyntaxKind;
        ParentScopeSubIndex = labelDeclarationNode.ParentScopeSubIndex;
        SelfScopeSubIndex = -1;
        TraitsIndex = -1;
    }

    public SyntaxNodeValue(
        SyntaxToken identifierToken,
        int absolutePathId,
        bool isFabricated,
        SyntaxKind syntaxKind,
        int parentScopeSubIndex,
        int selfScopeSubIndex,
        int traitsIndex)
    {
        IdentifierToken = identifierToken;
        AbsolutePathId = absolutePathId;
        IsFabricated = isFabricated;
        SyntaxKind = syntaxKind;
        ParentScopeSubIndex = parentScopeSubIndex;
        SelfScopeSubIndex = selfScopeSubIndex;
        TraitsIndex = traitsIndex;
    }

    public SyntaxToken IdentifierToken { get; set; }
    public int AbsolutePathId { get; set; }
    public bool IsFabricated { get; set; }
    public SyntaxKind SyntaxKind { get; set; }
    public int ParentScopeSubIndex { get; set; }
    public int SelfScopeSubIndex { get; set; }
    public int TraitsIndex { get; set; }
    
    public bool IsDefault()
    {
        return SyntaxKind == SyntaxKind.NotApplicable;
    }
}
