using Clair.CompilerServices.CSharp.Facts;
using Clair.Extensions.CompilerServices;
using Clair.Extensions.CompilerServices.Syntax;
using Clair.Extensions.CompilerServices.Syntax.Enums;
using Clair.Extensions.CompilerServices.Syntax.Interfaces;
using Clair.Extensions.CompilerServices.Syntax.NodeReferences;
using Clair.Extensions.CompilerServices.Syntax.NodeValues;

namespace Clair.CompilerServices.CSharp.ParserCase;

public ref partial struct CSharpParserState
{
    /*public static int POOL_HIT { get; set; }
    public static int POOL_MISS { get; set; }
    public static int POOL_RETURN { get; set; }*/
    
    /// <summary>
    /// NOTE: This will NOT return 'NamespaceClauseNode', because these
    /// are a sort of linked list and must be handled directly.
    ///
    /// WARNING: This is far trickier to use in while in the expression loop
    /// and for that reason it is not advised...
    /// ...because the expressions are possibly referencing one another in "unexpected" ways.
    /// 
    ///
    /// If the 'node.SyntaxKind' has a switch case, then it gets "returned to the pool",
    /// otherwise nothing happens.
    /// </summary>
    public readonly void Return_Helper(ISyntaxNode node)
    {
        switch (node.SyntaxKind)
        {
            case SyntaxKind.VariableDeclarationNode:
                Return_VariableDeclarationNode((VariableDeclarationNode)node);
                break;
            case SyntaxKind.LiteralExpressionNode:
                Return_LiteralExpressionNode((LiteralExpressionNode)node);
                break;
            case SyntaxKind.BinaryExpressionNode:
                Return_BinaryExpressionNode((BinaryExpressionNode)node);
                break;
            case SyntaxKind.TypeClauseNode:
                Return_TypeClauseNode((TypeClauseNode)node);
                break;
            case SyntaxKind.VariableReferenceNode:
                Return_VariableReferenceNode((VariableReferenceNode)node);
                break;
            case SyntaxKind.AmbiguousIdentifierNode:
                Return_AmbiguousIdentifierNode((AmbiguousIdentifierNode)node);
                break;
            case SyntaxKind.FunctionInvocationNode:
                Return_FunctionInvocationNode((FunctionInvocationNode)node);
                break;
            case SyntaxKind.ConstructorInvocationNode:
                Return_ConstructorInvocationExpressionNode((ConstructorInvocationNode)node);
                break;
        }
    }
    
    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned LiteralExpressionNode instance's:
    /// - TODO
    /// Thus, the Return_LiteralExpressionNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly LiteralExpressionNode Rent_LiteralExpressionNode()
    {
        if (Binder.Pool_LiteralExpressionNode_Queue.TryDequeue(out var literalExpressionNode))
        {
            //++POOL_HIT;
            return literalExpressionNode;
        }

        //++POOL_MISS;
        return new LiteralExpressionNode(
            literalSyntaxToken: default,
            typeReference: default);
    }

    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned LiteralExpressionNode instance's:
    /// - TODO
    /// Thus, the Return_LiteralExpressionNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly void Return_LiteralExpressionNode(LiteralExpressionNode literalExpressionNode)
    {
        //++POOL_RETURN;
        
        literalExpressionNode.LiteralSyntaxToken = default;
        literalExpressionNode.ResultTypeReference = default;
    
        literalExpressionNode._isFabricated = false;

        literalExpressionNode.ParentScopeSubIndex = 0;

        Binder.Pool_LiteralExpressionNode_Queue.Enqueue(literalExpressionNode);
    }

    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned BinaryExpressionNode instance's:
    /// - TODO
    /// Thus, the Return_BinaryExpressionNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly BinaryExpressionNode Rent_BinaryExpressionNode()
    {
        if (Binder.Pool_BinaryExpressionNode_Queue.TryDequeue(out var binaryExpressionNode))
        {
            return binaryExpressionNode;
        }

        return new BinaryExpressionNode(
            leftOperandTypeReference: default,
            operatorToken: default,
            rightOperandTypeReference: default,
            resultTypeReference: default);
    }

    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned BinaryExpressionNode instance's:
    /// - TODO
    /// Thus, the Return_BinaryExpressionNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly void Return_BinaryExpressionNode(BinaryExpressionNode binaryExpressionNode)
    {
        binaryExpressionNode._isFabricated = false;

        binaryExpressionNode.LeftOperandTypeReference = default;
        binaryExpressionNode.OperatorToken = default;
        binaryExpressionNode.RightOperandTypeReference = default;
        binaryExpressionNode.ResultTypeReference = default;

        binaryExpressionNode._rightExpressionResultTypeReference = default;
        binaryExpressionNode.RightExpressionNodeWasSet = false;
        binaryExpressionNode.ParentScopeSubIndex = 0;

        Binder.Pool_BinaryExpressionNode_Queue.Enqueue(binaryExpressionNode);
    }

    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned TypeClauseNode instance's:
    /// - TypeIdentifierToken
    /// Thus, the Return_TypeClauseNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly TypeClauseNode Rent_TypeClauseNode()
    {
        if (Binder.Pool_TypeClauseNode_Queue.TryDequeue(out var typeClauseNode))
        {
            return typeClauseNode;
        }

        return new TypeClauseNode(
            typeIdentifier: default,
            openAngleBracketToken: default,
            offsetGenericParameterEntryList: -1,
            lengthGenericParameterEntryList: 0,
            closeAngleBracketToken: default,
            isKeywordType: false);
    }

    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned TypeClauseNode instance's:
    /// - TypeIdentifierToken
    /// Thus, the Return_TypeClauseNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly void Return_TypeClauseNode(TypeClauseNode typeClauseNode)
    {
        typeClauseNode.OpenAngleBracketToken = default;
        typeClauseNode.OffsetGenericParameterEntryList = -1;
        typeClauseNode.LengthGenericParameterEntryList = 0;
        typeClauseNode.CloseAngleBracketToken = default;
        typeClauseNode.IsKeywordType = false;
        typeClauseNode.TypeKind = TypeKind.None;
        typeClauseNode.HasQuestionMark = false;
        typeClauseNode.ArrayRank = 0;
        // IsFabricated is not currently being used for this type, so the pooling logic doesn't need to reset it.
        //typeClauseNode._isFabricated = false;
        typeClauseNode.IsParsingGenericParameters = false;
        typeClauseNode.ExplicitDefinitionTextSpan = default;
        typeClauseNode.ExplicitDefinitionAbsolutePathId = default;

        Binder.Pool_TypeClauseNode_Queue.Enqueue(typeClauseNode);
    }

    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned ExplicitCastNode instance's:
    /// - TODO
    /// Thus, the Return_ExplicitCastNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly ExplicitCastNode Rent_ExplicitCastNode()
    {
        if (Binder.Pool_ExplicitCastNode_Instance is not null)
        {
            var instance = Binder.Pool_ExplicitCastNode_Instance;
            Binder.Pool_ExplicitCastNode_Instance = null;
            //++POOL_HIT;
            return instance;
        }

        //++POOL_MISS;
        return new ExplicitCastNode(
            openParenthesisToken: default,
            resultTypeReference: CSharpFacts.Types.VoidTypeReferenceValue,
            closeParenthesisToken: default);
    }

    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned ExplicitCastNode instance's:
    /// - TODO
    /// Thus, the Return_ExplicitCastNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly void Return_ExplicitCastNode(ExplicitCastNode explicitCastNode)
    {
        //++POOL_RETURN;

        explicitCastNode.OpenParenthesisToken = default;
        explicitCastNode.ResultTypeReference = CSharpFacts.Types.VoidTypeReferenceValue;
        explicitCastNode.CloseParenthesisToken = default;
        explicitCastNode.ParentScopeSubIndex = 0;
        explicitCastNode._isFabricated = false;

        Binder.Pool_ExplicitCastNode_Instance = explicitCastNode;
    }

    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned ConstructorDefinitionNode instance's:
    /// - TODO
    /// Thus, the Return_ConstructorDefinitionNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly ConstructorDefinitionNode Rent_ConstructorDefinitionNode()
    {
        if (Binder.Pool_ConstructorDefinitionNode_Instance is not null)
        {
            var instance = Binder.Pool_ConstructorDefinitionNode_Instance;
            Binder.Pool_ConstructorDefinitionNode_Instance = null;
            //++POOL_HIT;
            return instance;
        }

        //++POOL_MISS;
        return new ConstructorDefinitionNode(
            returnTypeReference: CSharpFacts.Types.VoidTypeReferenceValue,
            functionIdentifier: default,
            openAngleBracketToken: default,
            offsetGenericParameterEntryList: -1,
            lengthGenericParameterEntryList: 0,
            closeAngleBracketToken: default,
            openParenthesisToken: default,
            offsetFunctionArgumentEntryList: -1,
            lengthFunctionArgumentEntryList: 0,
            closeParenthesisToken: default,
            absolutePathId: AbsolutePathId);
    }

    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned ConstructorDefinitionNode instance's:
    /// - TODO
    /// Thus, the Return_ConstructorDefinitionNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly void Return_ConstructorDefinitionNode(ConstructorDefinitionNode constructorDefinitionNode)
    {
        //++POOL_RETURN;

        constructorDefinitionNode.FunctionIdentifier = default;

        constructorDefinitionNode.OpenAngleBracketToken = default;
        constructorDefinitionNode.OffsetGenericParameterEntryList = -1;
        constructorDefinitionNode.LengthGenericParameterEntryList = 0;
        constructorDefinitionNode.CloseAngleBracketToken = default;

        constructorDefinitionNode.OpenParenthesisToken = default;
        constructorDefinitionNode.OffsetFunctionArgumentEntryList = -1;
        constructorDefinitionNode.LengthFunctionArgumentEntryList = 0;
        constructorDefinitionNode.CloseParenthesisToken = default;
        constructorDefinitionNode.AbsolutePathId = default;

        constructorDefinitionNode.ParentScopeSubIndex = 0;
        constructorDefinitionNode.SelfScopeSubIndex = 0;

        constructorDefinitionNode._isFabricated = false;

        constructorDefinitionNode.ReturnTypeReference = CSharpFacts.Types.VoidTypeReferenceValue;

        Binder.Pool_ConstructorDefinitionNode_Instance = constructorDefinitionNode;
    }

    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned TypeDefinitionNode instance's:
    /// - TODO
    /// Thus, the Return_TypeDefinitionNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly TypeDefinitionNode Rent_TypeDefinitionNode()
    {
        if (Binder.Pool_TypeDefinitionNode_Instance is not null)
        {
            var instance = Binder.Pool_TypeDefinitionNode_Instance;
            Binder.Pool_TypeDefinitionNode_Instance = null;
            //++POOL_HIT;
            return instance;
        }

        //++POOL_MISS;
        return new TypeDefinitionNode(
            AccessModifierKind.Public,
            hasPartialModifier: false,
            StorageModifierKind.Class,
            typeIdentifier: default,
            openAngleBracketToken: default,
            offsetGenericParameterEntryList: -1,
            lengthGenericParameterEntryList: 0,
            closeAngleBracketToken: default,
            openParenthesisToken: default,
            offsetFunctionArgumentEntryList: -1,
            lengthFunctionArgumentEntryList: 0,
            closeParenthesisToken: default,
            inheritedTypeReference: TypeFacts.NotApplicable.ToTypeReference(),
            absolutePathId: AbsolutePathId,
            TextSourceKind.Explicit);
    }

    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned TypeDefinitionNode instance's:
    /// - TODO
    /// Thus, the Return_TypeDefinitionNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly void Return_TypeDefinitionNode(TypeDefinitionNode typeDefinitionNode)
    {
        //++POOL_RETURN;
    
        typeDefinitionNode.AccessModifierKind = AccessModifierKind.Public;
        
        typeDefinitionNode.HasPartialModifier = false;
        typeDefinitionNode.IndexPartialTypeDefinition = -1;
        typeDefinitionNode.StorageModifierKind = StorageModifierKind.Class;
        
        typeDefinitionNode.TypeIdentifierToken = default;
        
        typeDefinitionNode.OpenAngleBracketToken = default;
        
        typeDefinitionNode.OffsetGenericParameterEntryList = -1;
        typeDefinitionNode.LengthGenericParameterEntryList = 0;
        
        typeDefinitionNode.CloseAngleBracketToken = default;
        
        typeDefinitionNode.OpenParenthesisToken = default;
        typeDefinitionNode.OffsetFunctionArgumentEntryList = -1;
        typeDefinitionNode.LengthFunctionArgumentEntryList = 0;
        typeDefinitionNode.CloseParenthesisToken = default;

        typeDefinitionNode.InheritedTypeReference = default;
        typeDefinitionNode.AbsolutePathId = default;
    
        typeDefinitionNode._isFabricated = false;
        
        typeDefinitionNode.ParentScopeSubIndex = 0;
        typeDefinitionNode.SelfScopeSubIndex = 0;

        typeDefinitionNode.TraitsIndex = -1;
    
        typeDefinitionNode.IsKeywordType = false;
        
        typeDefinitionNode.IsParsingGenericParameters = false;
        
        Binder.Pool_TypeDefinitionNode_Instance = typeDefinitionNode;
    }

    ///
    ///
    ///
    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned NamespaceStatementNode instance's:
    /// - TODO
    /// Thus, the Return_NamespaceStatementNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly NamespaceStatementNode Rent_NamespaceStatementNode()
    {
        if (Binder.Pool_NamespaceStatementNode_Instance is not null)
        {
            var instance = Binder.Pool_NamespaceStatementNode_Instance;
            Binder.Pool_NamespaceStatementNode_Instance = null;
            //++POOL_HIT;
            return instance;
        }

        //++POOL_MISS;
        return new NamespaceStatementNode(
            keywordToken: default,
            identifierToken: default,
            absolutePathId: AbsolutePathId,
            TextSourceKind.Explicit);
    }

    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned NamespaceStatementNode instance's:
    /// - TODO
    /// Thus, the Return_NamespaceStatementNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly void Return_NamespaceStatementNode(NamespaceStatementNode namespaceStatementNode)
    {
        //++POOL_RETURN;

        namespaceStatementNode.KeywordToken = default;
        namespaceStatementNode.IdentifierToken = default;
        namespaceStatementNode.AbsolutePathId = default;

        namespaceStatementNode.ParentScopeSubIndex = 0;
        namespaceStatementNode.SelfScopeSubIndex = 0;

        namespaceStatementNode._isFabricated = false;

        Binder.Pool_NamespaceStatementNode_Instance = namespaceStatementNode;
    }
    ///
    ///
    ///

    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned FunctionDefinitionNode instance's:
    /// - TODO
    /// Thus, the Return_FunctionDefinitionNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly FunctionDefinitionNode Rent_FunctionDefinitionNode()
    {
        if (Binder.Pool_FunctionDefinitionNode_Instance is not null)
        {
            var instance = Binder.Pool_FunctionDefinitionNode_Instance;
            Binder.Pool_FunctionDefinitionNode_Instance = null;
            //++POOL_HIT;
            return instance;
        }

        //++POOL_MISS;
        return new FunctionDefinitionNode(
            AccessModifierKind.Public,
            returnTypeReference: CSharpFacts.Types.VoidTypeReferenceValue,
            functionIdentifierToken: default,
            openAngleBracketToken: default,
            offsetGenericParameterEntryList: -1,
            lengthGenericParameterEntryList: 0,
            closeAngleBracketToken: default,
            openParenthesisToken: default,
            offsetFunctionArgumentEntryList: -1,
            lengthFunctionArgumentEntryList: 0,
            closeParenthesisToken: default,
            AbsolutePathId);
    }

    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned FunctionDefinitionNode instance's:
    /// - TODO
    /// Thus, the Return_FunctionDefinitionNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly void Return_FunctionDefinitionNode(FunctionDefinitionNode functionDefinitionNode)
    {
        //++POOL_RETURN;
    
        functionDefinitionNode.AccessModifierKind = AccessModifierKind.Public;
        functionDefinitionNode.FunctionIdentifierToken = default;
        
        functionDefinitionNode.OpenAngleBracketToken = default;
        functionDefinitionNode.OffsetGenericParameterEntryList = -1;
        functionDefinitionNode.LengthGenericParameterEntryList = 0;
        functionDefinitionNode.CloseAngleBracketToken = default;
        
        functionDefinitionNode.OpenParenthesisToken = default;
        functionDefinitionNode.OffsetFunctionArgumentEntryList = -1;
        functionDefinitionNode.LengthFunctionArgumentEntryList = 0;
        functionDefinitionNode.CloseParenthesisToken = default;
        functionDefinitionNode.AbsolutePathId = default;
        functionDefinitionNode.IndexMethodOverloadDefinition = -1;
    
        functionDefinitionNode.ParentScopeSubIndex = default;
        functionDefinitionNode.SelfScopeSubIndex = default;
    
        functionDefinitionNode._isFabricated = false;
        
        functionDefinitionNode.IsParsingGenericParameters = default;
        
        functionDefinitionNode.TraitsIndex = -1;

        functionDefinitionNode.ReturnTypeReference = CSharpFacts.Types.VoidTypeReferenceValue;
        
        Binder.Pool_FunctionDefinitionNode_Instance = functionDefinitionNode;
    }

    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned TemporaryLocalVariableDeclarationNode instance's:
    /// - TODO
    /// Thus, the Return_TemporaryLocalVariableDeclarationNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly VariableDeclarationNode Rent_VariableDeclarationNode()
    {
        if (Binder.Pool_VariableDeclarationNode_Queue.TryDequeue(out var variableDeclarationNode))
        {
            //++POOL_HIT;
            return variableDeclarationNode;
        }

        //++POOL_MISS;
        return new VariableDeclarationNode(
            typeReference: Facts.CSharpFacts.Types.VarTypeReferenceValue,
            identifierToken: default,
            VariableKind.Local,
            isInitialized: false,
            AbsolutePathId);
    }

    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned TemporaryLocalVariableDeclarationNode instance's:
    /// - TODO
    /// Thus, the Return_TemporaryLocalVariableDeclarationNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly void Return_VariableDeclarationNode(VariableDeclarationNode variableDeclarationNode)
    {
        //++POOL_RETURN;
    
        variableDeclarationNode.TypeReference = default;
        variableDeclarationNode.IdentifierToken = default;
        variableDeclarationNode.VariableKind = VariableKind.Local;
        variableDeclarationNode.IsInitialized = false;
        variableDeclarationNode.AbsolutePathId = default;
        variableDeclarationNode.HasGetter = default;
        variableDeclarationNode.GetterIsAutoImplemented = default;
        variableDeclarationNode.HasSetter = default;
        variableDeclarationNode.SetterIsAutoImplemented = default;
        variableDeclarationNode.ParentScopeSubIndex = default;
        variableDeclarationNode._isFabricated = default;
        variableDeclarationNode.TraitsIndex = -1;

        Binder.Pool_VariableDeclarationNode_Queue.Enqueue(variableDeclarationNode);
    }

    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned VariableReferenceNode instance's:
    /// - VariableIdentifierToken
    /// Thus, the Return_VariableReferenceNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly VariableReferenceNode Rent_VariableReferenceNode()
    {
        if (Binder.Pool_VariableReferenceNode_Queue.TryDequeue(out var variableReferenceNode))
        {
            return variableReferenceNode;
        }

        return new VariableReferenceNode(
            variableIdentifierToken: default,
            resultTypeReference: default);
    }

    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned VariableReferenceNode instance's:
    /// - VariableIdentifierToken
    /// Thus, the Return_VariableReferenceNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly void Return_VariableReferenceNode(VariableReferenceNode variableReferenceNode)
    {
        variableReferenceNode.ResultTypeReference = TypeFacts.Empty.ToTypeReference();
        variableReferenceNode._isFabricated = false;

        Binder.Pool_VariableReferenceNode_Queue.Enqueue(variableReferenceNode);
    }

    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned VariableReferenceNode instance's:
    /// - VariableIdentifierToken
    /// Thus, the Return_VariableReferenceNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly VariableReferenceValue Return_VariableReferenceNode_ToStruct(VariableReferenceNode variableReferenceNode)
    {
        var variableReference = new VariableReferenceValue(variableReferenceNode);
        Return_VariableReferenceNode(variableReferenceNode);
        return variableReference;
    }

    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned NamespaceClauseNode instance's:
    /// - IdentifierToken
    /// Thus, the Return_NamespaceClauseNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly NamespaceClauseNode Rent_NamespaceClauseNode()
    {
        if (Binder.Pool_NamespaceClauseNode_Queue.TryDequeue(out var namespaceClauseNode))
        {
            return namespaceClauseNode;
        }

        return new NamespaceClauseNode(
            identifierToken: default);
    }

    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned NamespaceClauseNode instance's:
    /// - IdentifierToken
    /// Thus, the Return_NamespaceClauseNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly void Return_NamespaceClauseNode(NamespaceClauseNode namespaceClauseNode)
    {
        // IsFabricated is not currently being used for this type, so the pooling logic doesn't need to reset it.
        //namespaceClauseNode._isFabricated = false;

        // namespaceClauseNode.NamespacePrefixNode = null;
        namespaceClauseNode.PreviousNamespaceClauseNode = null;
        namespaceClauseNode.StartOfMemberAccessChainPositionIndex = default;

        Binder.Pool_NamespaceClauseNode_Queue.Enqueue(namespaceClauseNode);
    }

    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned AmbiguousIdentifierNode instance's:
    /// - Token
    /// - FollowsMemberAccessToken
    /// Thus, the Return_AmbiguousIdentifierNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly AmbiguousIdentifierNode Rent_AmbiguousIdentifierNode()
    {
        if (Binder.Pool_AmbiguousIdentifierNode_Queue.TryDequeue(out var ambiguousIdentifierNode))
        {
            //++POOL_HIT;
            return ambiguousIdentifierNode;
        }

        //++POOL_MISS;
        return new AmbiguousIdentifierNode(
            token: default,
            openAngleBracketToken: default,
            offsetGenericParameterEntryList: -1,
            lengthGenericParameterEntryList: 0,
            closeAngleBracketToken: default,
            resultTypeReference: Facts.CSharpFacts.Types.VoidTypeReferenceValue);
    }

    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned AmbiguousIdentifierNode instance's:
    /// - Token
    /// - FollowsMemberAccessToken
    /// Thus, the Return_AmbiguousIdentifierNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly void Return_AmbiguousIdentifierNode(AmbiguousIdentifierNode ambiguousIdentifierNode)
    {
        //++POOL_RETURN;
        
        ambiguousIdentifierNode.OpenAngleBracketToken = default;
        ambiguousIdentifierNode.OffsetGenericParameterEntryList = -1;
        ambiguousIdentifierNode.LengthGenericParameterEntryList = 0;
        ambiguousIdentifierNode.CloseAngleBracketToken = default;

        ambiguousIdentifierNode.ResultTypeReference = Facts.CSharpFacts.Types.VoidTypeReferenceValue;
        ambiguousIdentifierNode.HasQuestionMark = false;

        Binder.Pool_AmbiguousIdentifierNode_Queue.Enqueue(ambiguousIdentifierNode);
    }

    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned FunctionInvocationNode instance's:
    /// - FunctionInvocationIdentifierToken
    /// Thus, the Return_FunctionInvocationNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly FunctionInvocationNode Rent_FunctionInvocationNode()
    {
        if (Binder.Pool_FunctionInvocationNode_Queue.TryDequeue(out var functionInvocationNode))
        {
            return functionInvocationNode;
        }

        return new FunctionInvocationNode(
            functionInvocationIdentifierToken: default,
            openAngleBracketToken: default,
            offsetGenericParameterEntryList: -1,
            lengthGenericParameterEntryList: 0,
            closeAngleBracketToken: default,
            openParenthesisToken: default,
            offsetFunctionParameterEntryList: -1,
            lengthFunctionParameterEntryList: 0,
            closeParenthesisToken: default,
            resultTypeReference: Facts.CSharpFacts.Types.VoidTypeReferenceValue);
    }

    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned FunctionInvocationNode instance's:
    /// - FunctionInvocationIdentifierToken
    /// Thus, the Return_FunctionInvocationNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly void Return_FunctionInvocationNode(FunctionInvocationNode functionInvocationNode)
    {
        functionInvocationNode.OpenAngleBracketToken = default;
        functionInvocationNode.OffsetGenericParameterEntryList = -1;
        functionInvocationNode.LengthGenericParameterEntryList = 0;
        functionInvocationNode.CloseAngleBracketToken = default;

        functionInvocationNode.OpenParenthesisToken = default;
        functionInvocationNode.OffsetFunctionParameterEntryList = -1;
        functionInvocationNode.LengthFunctionParameterEntryList = 0;
        functionInvocationNode.CloseParenthesisToken = default;

        functionInvocationNode.ResultTypeReference = Facts.CSharpFacts.Types.VoidTypeReferenceValue;

        functionInvocationNode.ExplicitDefinitionAbsolutePathId = default;
        functionInvocationNode.ExplicitDefinitionTextSpan = default;

        // IsFabricated is not currently being used for this type, so the pooling logic doesn't need to reset it.
        //functionInvocationNode._isFabricated = false;

        functionInvocationNode.IsParsingFunctionParameters = false;
        functionInvocationNode.IsParsingGenericParameters = false;

        Binder.Pool_FunctionInvocationNode_Queue.Enqueue(functionInvocationNode);
    }

    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned ConstructorInvocationExpressionNode instance's:
    /// - NewKeywordToken
    /// Thus, the Return_ConstructorInvocationExpressionNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly ConstructorInvocationNode Rent_ConstructorInvocationExpressionNode()
    {
        if (Binder.Pool_ConstructorInvocationExpressionNode_Queue.TryDequeue(out var constructorInvocationExpressionNode))
        {
            return constructorInvocationExpressionNode;
        }

        return new ConstructorInvocationNode(
            newKeywordToken: default,
            typeReference: default,
            openParenthesisToken: default,
            offsetFunctionParameterEntryList: -1,
            lengthFunctionParameterEntryList: 0,
            closeParenthesisToken: default);
    }

    /// <summary>
    /// It is expected that any invoker of this method will immediately set the returned ConstructorInvocationExpressionNode instance's:
    /// - NewKeywordToken
    /// Thus, the Return_ConstructorInvocationExpressionNode(...) method will NOT clear that property's state.
    /// </summary>
    public readonly void Return_ConstructorInvocationExpressionNode(ConstructorInvocationNode constructorInvocationExpressionNode)
    {
        constructorInvocationExpressionNode.ResultTypeReference = default;

        constructorInvocationExpressionNode.OpenParenthesisToken = default;
        constructorInvocationExpressionNode.OffsetFunctionParameterEntryList = -1;
        constructorInvocationExpressionNode.LengthFunctionParameterEntryList = 0;
        constructorInvocationExpressionNode.CloseParenthesisToken = default;

        constructorInvocationExpressionNode.ConstructorInvocationStageKind = ConstructorInvocationStageKind.Unset;

        // IsFabricated is not currently being used for this type, so the pooling logic doesn't need to reset it.
        //constructorInvocationExpressionNode._isFabricated = false;

        constructorInvocationExpressionNode.IsParsingFunctionParameters = false;

        Binder.Pool_ConstructorInvocationExpressionNode_Queue.Enqueue(constructorInvocationExpressionNode);
    }
}
