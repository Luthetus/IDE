using Clair.CompilerServices.CSharp.Facts;
using Clair.Extensions.CompilerServices.Syntax;
using Clair.Extensions.CompilerServices.Syntax.Enums;
using Clair.Extensions.CompilerServices.Syntax.Interfaces;
using Clair.Extensions.CompilerServices.Syntax.NodeReferences;
using Clair.Extensions.CompilerServices.Syntax.NodeValues;

namespace Clair.CompilerServices.CSharp.ParserCase.Internals;

public static partial class Parser
{
    public static void ParseIdentifierToken(ref CSharpParserState parserModel)
    {
        if (parserModel.TokenWalker.Current.TextSpan.Length == 1 &&
            // 95 is ASCII code for '_'
            parserModel.TokenWalker.Current.TextSpan.CharIntSum == 95)
        {
            if (!parserModel.TryGetVariableDeclarationHierarchically(
                    parserModel.AbsolutePathId,
                    parserModel.Compilation,
                    parserModel.ScopeCurrentSubIndex,
                    parserModel.AbsolutePathId,
                    parserModel.TokenWalker.Current.TextSpan,
                    out _))
            {
                parserModel.BindDiscard(parserModel.TokenWalker.Current);
                var identifierToken = parserModel.TokenWalker.Consume();
                
                var variableReferenceNode = parserModel.Rent_VariableReferenceNode();
                variableReferenceNode.VariableIdentifierToken = identifierToken;
                    
                parserModel.StatementBuilder.MostRecentNode = variableReferenceNode;
                return;
            }
        }
        
        if (parserModel.TokenWalker.Next.SyntaxKind == SyntaxKind.ColonToken)
        {
            Parser.HandleLabelDeclaration(ref parserModel);
            return;
        }
        
        var originalTokenIndex = parserModel.TokenWalker.Index;
        
        parserModel.TryParseExpressionSyntaxKindList.Add(SyntaxKind.TypeClauseNode);
        parserModel.TryParseExpressionSyntaxKindList.Add(SyntaxKind.VariableDeclarationNode);
        parserModel.TryParseExpressionSyntaxKindList.Add(SyntaxKind.VariableReferenceNode);
        parserModel.TryParseExpressionSyntaxKindList.Add(SyntaxKind.ConstructorInvocationNode);
        
        if (parserModel.ScopeCurrent.OwnerSyntaxKind != SyntaxKind.TypeDefinitionNode)
        {
            // There is a syntax conflict between a ConstructorDefinitionNode and a FunctionInvocationNode.
            //
            // Disambiguation is done based on the 'CodeBlockOwner' until a better solution is found.
            //
            // If the supposed "ConstructorDefinitionNode" does not have the same name as
            // the CodeBlockOwner.
            //
            // Then, it perhaps should be treated as a function invocation (or function definition).
            // The main case for this being someone typing out pseudo code within a CodeBlockOwner
            // that is a TypeDefinitionNode.
            parserModel.TryParseExpressionSyntaxKindList.Add(SyntaxKind.FunctionInvocationNode);
        }
        
        parserModel.ParserContextKind = CSharpParserContextKind.ForceStatementExpression;
        
        var successParse = Parser.TryParseExpression(ref parserModel, out var expressionNode);
        
        if (!successParse)
        {
            expressionNode = Parser.ParseExpression(ref parserModel);
            parserModel.StatementBuilder.MostRecentNode = expressionNode;
            return;
        }
        
        switch (expressionNode.SyntaxKind)
        {
            case SyntaxKind.TypeClauseNode:
                MoveToHandleTypeClauseNode(originalTokenIndex, (TypeClauseNode)expressionNode, ref parserModel);
                return;
            case SyntaxKind.VariableDeclarationNode:
                if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.OpenParenthesisToken ||
                    parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.OpenAngleBracketToken)
                {
                    MoveToHandleFunctionDefinition((VariableDeclarationNode)expressionNode, ref parserModel);
                    return;
                }
                
                MoveToHandleVariableDeclarationNode((VariableDeclarationNode)expressionNode, ref parserModel);
                if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.CommaToken)
                {
                    HandleMultiVariableDeclaration((VariableDeclarationNode)expressionNode, ref parserModel);
                }
                return;
            case SyntaxKind.VariableReferenceNode:
            
                var isQuestionMarkMemberAccessToken = parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.QuestionMarkToken &&
                    parserModel.TokenWalker.Next.SyntaxKind == SyntaxKind.MemberAccessToken;
                
                var isBangMemberAccessToken = parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.BangToken &&
                    parserModel.TokenWalker.Next.SyntaxKind == SyntaxKind.MemberAccessToken;
            
                if ((parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.MemberAccessToken || isQuestionMarkMemberAccessToken || isBangMemberAccessToken) &&
                    originalTokenIndex == parserModel.TokenWalker.Index - 1)
                {
                    parserModel.TokenWalker.BacktrackNoReturnValue();
                    expressionNode = Parser.ParseExpression(ref parserModel);
                    parserModel.StatementBuilder.MostRecentNode = expressionNode;
                    return;
                }
                
                parserModel.StatementBuilder.MostRecentNode = expressionNode;
                return;
            case SyntaxKind.FunctionInvocationNode:
            case SyntaxKind.ConstructorInvocationNode:
                parserModel.StatementBuilder.MostRecentNode = expressionNode;
                return;
            default:
                // compilationUnit.DiagnosticBag.ReportTodoException(parserModel.TokenWalker.Current.TextSpan, $"nameof(ParseIdentifierToken) default case");
                return;
        }
    }
    
    public static void MoveToHandleFunctionDefinition(VariableDeclarationNode variableDeclarationNode, ref CSharpParserState parserModel)
    {
        Parser.HandleFunctionDefinition(
            variableDeclarationNode.IdentifierToken,
            variableDeclarationNode.TypeReference,
            ref parserModel);
        parserModel.Return_VariableDeclarationNode(variableDeclarationNode);
    }
    
    public static void MoveToHandleVariableDeclarationNode(VariableDeclarationNode variableDeclarationNode, ref CSharpParserState parserModel)
    {
        var variableKind = VariableKind.Local;
                
        if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.OpenBraceToken ||
            parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.EqualsCloseAngleBracketToken)
        {
            variableKind = VariableKind.Property;
        }
        else if (parserModel.ScopeCurrent.OwnerSyntaxKind == SyntaxKind.TypeDefinitionNode)
        {
            variableKind = VariableKind.Field;
        }
        
        variableDeclarationNode.VariableKind = variableKind;
        
        parserModel.BindVariableDeclarationNode(variableDeclarationNode);
        parserModel.StatementBuilder.MostRecentNode = variableDeclarationNode;
        
        if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.EqualsCloseAngleBracketToken)
        {
            ParsePropertyDefinition_ExpressionBound(ref parserModel);
        }
        else
        {
            if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.OpenBraceToken)
                ParsePropertyDefinition(variableDeclarationNode, ref parserModel);
            
            if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.EqualsToken)
            {
                parserModel.MostRecentLeftHandSideAssignmentExpressionTypeClauseNode = variableDeclarationNode.TypeReference;
            
                IExpressionNode expression;
            
                parserModel.ForceParseExpressionInitialPrimaryExpression = variableDeclarationNode;
                if (variableKind == VariableKind.Local)
                {
                    parserModel.ExpressionList.Add((SyntaxKind.CommaToken, null));
                }
                expression = Parser.ParseExpression(ref parserModel);
                parserModel.ForceParseExpressionInitialPrimaryExpression = EmptyExpressionNode.Empty;
                
                if (expression.SyntaxKind == SyntaxKind.BinaryExpressionNode &&
                    CSharpFacts.Types.VarValue.IdentifierToken.TextSpan.CharIntSum == variableDeclarationNode.TypeReference.TypeIdentifierToken.TextSpan.CharIntSum &&
                    parserModel.Binder.CSharpCompilerService.SafeCompareText(parserModel.AbsolutePathId, "var", variableDeclarationNode.TypeReference.TypeIdentifierToken.TextSpan))
                {
                    // 2025-09-14: `parserModel.StatementBuilder.MostRecentNode = expression;`...
                    // exists below but is this "certainly" going to return the binary expression node? can doing it this way miss a return?
                    var binaryExpressionNode = (BinaryExpressionNode)expression;
                    variableDeclarationNode.SetImplicitTypeReference(binaryExpressionNode.RightExpressionResultTypeReference);
                    if (variableDeclarationNode.TraitsIndex != -1)
                    {
                    	var traits = parserModel.Binder.VariableDeclarationTraitsList[variableDeclarationNode.TraitsIndex];
                    	traits.TypeReference = variableDeclarationNode.TypeReference;
                    	parserModel.Binder.VariableDeclarationTraitsList[variableDeclarationNode.TraitsIndex] = traits;
                    }
                }
                
                parserModel.StatementBuilder.MostRecentNode = expression;
            }
        }

        // !!! DO NOT EARLY RETURN
        parserModel.Return_VariableDeclarationNode(variableDeclarationNode);
    }
    
    public static void HandleMultiVariableDeclaration(VariableDeclarationNode variableDeclarationNode, ref CSharpParserState parserModel)
    {
        var previousTokenIndex = parserModel.TokenWalker.Index;
        
        while (!parserModel.TokenWalker.IsEof)
        {
            parserModel.ExpressionList.Add((SyntaxKind.CloseParenthesisToken, null));
            parserModel.ExpressionList.Add((SyntaxKind.CommaToken, null));
            
            if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.EqualsToken)
            {
                MoveToHandleVariableDeclarationNode(variableDeclarationNode, ref parserModel);
            }
            else if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.CommaToken)
            {
                parserModel.BindVariableDeclarationNode(variableDeclarationNode);
            }
            else if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.StatementDelimiterToken)
            {
                parserModel.BindVariableDeclarationNode(variableDeclarationNode);
                return;
            }
            
            if (parserModel.TokenWalker.Current.SyntaxKind != SyntaxKind.CommaToken)
            {
                break;
            }
            
            _ = parserModel.TokenWalker.Consume(); // Comma Token
            
            if (UtilityApi.IsConvertibleToIdentifierToken(parserModel.TokenWalker.Current.SyntaxKind))
            {
                var token = parserModel.TokenWalker.Consume();
                var identifierToken = UtilityApi.ConvertToIdentifierToken(ref token, ref parserModel);
            
                variableDeclarationNode = new VariableDeclarationNode(
                    variableDeclarationNode.TypeReference,
                    identifierToken,
                    VariableKind.Local,
                    isInitialized: false,
                    absolutePathId: parserModel.AbsolutePathId);
            }
            else
            {
                return;
            }
            
            if (previousTokenIndex == parserModel.TokenWalker.Index)
            {
                break;
            }
            
            previousTokenIndex = parserModel.TokenWalker.Index;
        }
    }
    
    public static void MoveToHandleTypeClauseNode(int originalTokenIndex, TypeClauseNode typeClauseNode, ref CSharpParserState parserModel)
    {
        if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.StatementDelimiterToken ||
            parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.EndOfFileToken ||
            parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.OpenBraceToken ||
            parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.CloseBraceToken)
        {
            parserModel.StatementBuilder.MostRecentNode = typeClauseNode;
            return;
        }

        var wasHandled = false;

        if (parserModel.ScopeCurrent.NodeSubIndex != -1)
        {
            var nodeValue = parserModel.Binder.NodeList[parserModel.Compilation.NodeOffset + parserModel.ScopeCurrent.NodeSubIndex];
        
            if (parserModel.ScopeCurrent.OwnerSyntaxKind == SyntaxKind.TypeDefinitionNode &&
                nodeValue.SyntaxKind == SyntaxKind.TypeDefinitionNode &&
                UtilityApi.IsConvertibleToIdentifierToken(typeClauseNode.TypeIdentifierToken.SyntaxKind) &&
                parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.OpenParenthesisToken &&
                parserModel.Binder.CSharpCompilerService.SafeCompareTextSpans(
                    parserModel.AbsolutePathId,
                    nodeValue.IdentifierToken.TextSpan,
                    parserModel.AbsolutePathId,
                    typeClauseNode.TypeIdentifierToken.TextSpan))
            {
                wasHandled = true;
            
                // ConstructorDefinitionNode
    
                var typeClauseToken = typeClauseNode.TypeIdentifierToken;
                var identifierToken = UtilityApi.ConvertToIdentifierToken(ref typeClauseToken, ref parserModel);
    
                Parser.HandleConstructorDefinition(
                    typeDefinitionIdentifierToken: nodeValue.IdentifierToken,
                    identifierToken,
                    ref parserModel);
            }
        }
        
        if (!wasHandled)
        {
            parserModel.StatementBuilder.MostRecentNode = typeClauseNode;
        }
    }
    
    public static void ParsePropertyDefinition(VariableDeclarationNode variableDeclarationNode, ref CSharpParserState parserModel)
    {
        _ = parserModel.TokenWalker.Consume(); // openBraceToken

        var openBraceCounter = 1;

        bool consumed;

        while (true)
        {
            consumed = false;
        
            if (parserModel.TokenWalker.IsEof)
                break;

            if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.OpenBraceToken)
            {
                ++openBraceCounter;
            }
            else if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.CloseBraceToken)
            {
                if (--openBraceCounter <= 0)
                    break;
            }
            else if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.GetTokenContextualKeyword)
            {
                variableDeclarationNode.HasGetter = true;
                
                if (parserModel.TokenWalker.Next.SyntaxKind != SyntaxKind.StatementDelimiterToken)
                {
                    consumed = true;
                    ParseGetterOrSetter(variableDeclarationNode, ref parserModel);
                }
            }
            else if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.SetTokenContextualKeyword)
            {
                variableDeclarationNode.HasSetter = true;
                
                if (parserModel.TokenWalker.Next.SyntaxKind != SyntaxKind.StatementDelimiterToken)
                {
                    consumed = true;
                    ParseGetterOrSetter(variableDeclarationNode, ref parserModel);
                }
            }
            else if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.InitTokenContextualKeyword)
            {
                variableDeclarationNode.HasSetter = true;
                
                if (parserModel.TokenWalker.Next.SyntaxKind != SyntaxKind.StatementDelimiterToken)
                {
                    consumed = true;
                    ParseGetterOrSetter(variableDeclarationNode, ref parserModel);
                }
            }

            if (!consumed)
                _ = parserModel.TokenWalker.Consume();
        }

        _ = parserModel.TokenWalker.Match(SyntaxKind.CloseBraceToken);
    }
    
    /// <summary>
    /// This method must consume at least once or an infinite loop in 'ParsePropertyDefinition(...)'
    /// will occur due to the 'bool consumed' variable.
    /// </summary>
    public static void ParseGetterOrSetter(VariableDeclarationNode variableDeclarationNode, ref CSharpParserState parserModel)
    {
        if (parserModel.TokenWalker.Current.SyntaxKind != SyntaxKind.EqualsCloseAngleBracketToken)
            _ = parserModel.TokenWalker.Consume(); // Consume the 'get' or 'set' contextual keyword.
    
        parserModel.RegisterScope(
        	new Scope(
        		ScopeDirectionKind.Down,
        		scope_StartInclusiveIndex: parserModel.TokenWalker.Current.TextSpan.StartInclusiveIndex,
        		scope_EndExclusiveIndex: -1,
        		codeBlock_StartInclusiveIndex: -1,
        		codeBlock_EndExclusiveIndex: -1,
        		parentScopeSubIndex: parserModel.ScopeCurrentSubIndex,
        		selfScopeSubIndex: parserModel.Compilation.ScopeLength,
        		nodeSubIndex: 0,
        		permitCodeBlockParsing: true,
        		isImplicitOpenCodeBlockTextSpan: false,
        		ownerSyntaxKind: SyntaxKind.GetterOrSetterNode),
            codeBlockOwner: null);
        
        if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.StatementDelimiterToken)
        {
            parserModel.SetCurrentScope_IsImplicitOpenCodeBlockTextSpan(true);
        }
        else if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.EqualsCloseAngleBracketToken)
        {
            MoveToExpressionBody(ref parserModel);
        }
        else if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.OpenBraceToken)
        {
            //var deferredParsingOccurred = parserModel.StatementBuilder.FinishStatement(parserModel.TokenWalker.Index, compilationUnit, ref parserModel);
            //if (deferredParsingOccurred)
            //    break;
            
            ParseOpenBraceToken(ref parserModel);
        }
    }
    
    public static void ParsePropertyDefinition_ExpressionBound(ref CSharpParserState parserModel)
    {
        ParseGetterOrSetter(variableDeclarationNode: null, ref parserModel);
    }

    public static void ParseOpenBraceToken(ref CSharpParserState parserModel)
    {
        var openBraceToken = parserModel.TokenWalker.Consume();
        
        if (parserModel.ScopeCurrent.IsImplicitOpenCodeBlockTextSpan ||
            parserModel.ScopeCurrent.CodeBlock_StartInclusiveIndex != -1)
        {
            var parent = parserModel.ScopeCurrent;

            parserModel.RegisterScope(
            	new Scope(
                    parent.IsDefault()
                        ? ScopeDirectionKind.Both
                        : parent.ScopeDirectionKind,
            		scope_StartInclusiveIndex: parserModel.TokenWalker.Current.TextSpan.StartInclusiveIndex,
            		scope_EndExclusiveIndex: -1,
            		codeBlock_StartInclusiveIndex: -1,
            		codeBlock_EndExclusiveIndex: -1,
            		parentScopeSubIndex: parserModel.ScopeCurrentSubIndex,
            		selfScopeSubIndex: parserModel.Compilation.ScopeLength,
            		nodeSubIndex: 0,
            		permitCodeBlockParsing: true,
            		isImplicitOpenCodeBlockTextSpan: false,
            		ownerSyntaxKind: SyntaxKind.ArbitraryCodeBlockNode),
            codeBlockOwner: null);
        }
        
        parserModel.SetCurrentScope_IsImplicitOpenCodeBlockTextSpan(false);

        var parentScope = parserModel.GetParent(parserModel.ScopeCurrent.ParentScopeSubIndex, parserModel.Compilation);
        var parentScopeDirection = parentScope.IsDefault()
            ? ScopeDirectionKind.Both
            : parentScope.ScopeDirectionKind;
        
        if (parentScopeDirection == ScopeDirectionKind.Both)
        {
            if (!parserModel.ScopeCurrent.PermitCodeBlockParsing)
            {
                parserModel.TokenWalker.DeferParsingOfChildScope(openBraceToken, ref parserModel);
                return;
            }

            parserModel.SetCurrentScope_PermitCodeBlockParsing(false);
        }
        
        // This has to come after the 'DeferParsingOfChildScope(...)'
        // or it makes an ArbitraryCodeBlockNode when it comes back around.
        parserModel.SetCurrentScope_CodeBlock_StartInclusiveIndex(openBraceToken.TextSpan.StartInclusiveIndex);
    }

    /// <summary>
    /// CloseBraceToken is passed in to the method because it is a protected token,
    /// and is preferably consumed from the main loop so it can be more easily tracked.
    /// </summary>
    public static void ParseCloseBraceToken(int closeBraceTokenIndex, ref CSharpParserState parserModel)
    {
        var closeBraceToken = parserModel.TokenWalker.Consume();
    
        // while () if not CloseBraceToken accepting bubble up until someone takes it or null parent.
        
        /*if (parserModel.CurrentCodeBlockBuilder.IsImplicitOpenCodeBlockTextSpan)
        {
            throw new NotImplementedException("ParseCloseBraceToken(...) -> if (parserModel.CurrentCodeBlockBuilder.IsImplicitOpenCodeBlockTextSpan)");
        }*/
    
        if (parserModel.ParseChildScopeStack.Count > 0)
        {
            var tuple = parserModel.ParseChildScopeStack.Peek();
            
            if (tuple.ScopeSubIndex == parserModel.ScopeCurrentSubIndex)
            {
                tuple = parserModel.ParseChildScopeStack.Pop();
                tuple.DeferredChildScope.PrepareMainParserLoop(closeBraceTokenIndex, closeBraceToken, ref parserModel);
                return;
            }
        }

        if (parserModel.ScopeCurrent.OwnerSyntaxKind != SyntaxKind.GlobalCodeBlockNode)
        {
            parserModel.SetCurrentScope_CodeBlock_EndExclusiveIndex(closeBraceToken.TextSpan.EndExclusiveIndex);
        }
        
        parserModel.CloseScope(closeBraceToken.TextSpan);
    }

    public static void ParseOpenParenthesisToken(ref CSharpParserState parserModel)
    {
        var originalTokenIndex = parserModel.TokenWalker.Index;
        
        parserModel.TryParseExpressionSyntaxKindList.Add(SyntaxKind.VariableDeclarationNode);
        parserModel.TryParseExpressionSyntaxKindList.Add(SyntaxKind.TypeClauseNode);
        parserModel.TryParseExpressionSyntaxKindList.Add(SyntaxKind.AmbiguousParenthesizedNode);
        
        parserModel.ParserContextKind = CSharpParserContextKind.ForceStatementExpression;
        
        var successParse = Parser.TryParseExpression(ref parserModel, out var expressionNode);
        
        if (!successParse)
        {
            expressionNode = Parser.ParseExpression(ref parserModel);
            parserModel.StatementBuilder.MostRecentNode = expressionNode;
            return;
        }
        
        if (expressionNode.SyntaxKind == SyntaxKind.VariableDeclarationNode)
        {
            if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.OpenParenthesisToken ||
                parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.OpenAngleBracketToken)
            {
                MoveToHandleFunctionDefinition((VariableDeclarationNode)expressionNode, ref parserModel);
                return;
            }
            
            MoveToHandleVariableDeclarationNode((VariableDeclarationNode)expressionNode, ref parserModel);
        }
        
        //// I am catching the next two but not doing anything with them
        //// only so that the TryParseExpression won't return early due to those being the
        //// SyntaxKind(s) that will appear during the process of parsing the VariableDeclarationNode
        //// given that the TypeClauseNode is a tuple.
        //
        // else if (expressionNode.SyntaxKind == SyntaxKind.TypeClauseNode)
        // else if (expressionNode.SyntaxKind == SyntaxKind.AmbiguousParenthesizedNode)
    }

    public static void ParseOpenSquareBracketToken(ref CSharpParserState parserModel)
    {
        _ = parserModel.TokenWalker.Consume(); // openSquareBracketToken

        if (!parserModel.StatementBuilder.StatementIsEmpty)
        {
            /*compilationUnit.DiagnosticBag.ReportTodoException(
                openSquareBracketToken.TextSpan,
                $"Unexpected '{nameof(SyntaxKind.OpenSquareBracketToken)}'");*/
            return;
        }
        var openSquareBracketCounter = 1;
        var corruptState = false;
        
        while (!parserModel.TokenWalker.IsEof)
        {
            if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.OpenSquareBracketToken)
            {
                ++openSquareBracketCounter;
            }
            else if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.CloseSquareBracketToken)
            {
                if (--openSquareBracketCounter <= 0)
                    break;
            }
            else if (!corruptState)
            {
                var tokenIndexOriginal = parserModel.TokenWalker.Index;
                
                parserModel.ExpressionList.Add((SyntaxKind.CloseSquareBracketToken, null));
                parserModel.ExpressionList.Add((SyntaxKind.CommaToken, null));
                var expression = Parser.ParseExpression(ref parserModel);
                
                if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.CommaToken)
                    _ = parserModel.TokenWalker.Consume();
                    
                if (tokenIndexOriginal < parserModel.TokenWalker.Index)
                    continue; // Already consumed so avoid the one at the end of the while loop
            }

            _ = parserModel.TokenWalker.Consume();
        }

        _ = parserModel.TokenWalker.Match(SyntaxKind.CloseSquareBracketToken);
    }

    public static void ParseEqualsToken(ref CSharpParserState parserModel)
    {
        var shouldBacktrack = false;
        IExpressionNode backtrackNode = EmptyExpressionNode.Empty;
        
        // No, this is not missing an else
        if (parserModel.StatementBuilder.MostRecentNode != EmptyExpressionNode.Empty)
        {
            var previousNode = parserModel.StatementBuilder.MostRecentNode;
            
            if (previousNode.SyntaxKind == SyntaxKind.VariableReferenceNode)
            {
                shouldBacktrack = true;
                parserModel.MostRecentLeftHandSideAssignmentExpressionTypeClauseNode = ((VariableReferenceNode)previousNode).ResultTypeReference;
                backtrackNode = (VariableReferenceNode)previousNode;
            }
            else if (previousNode.SyntaxKind == SyntaxKind.TypeClauseNode)
            {
                shouldBacktrack = true;
                parserModel.MostRecentLeftHandSideAssignmentExpressionTypeClauseNode = new TypeReferenceValue((TypeClauseNode)previousNode);
                // TODO: Why is this pool-return here?
                // parserModel.Return_TypeClauseNode((TypeClauseNode)previousNode);
                backtrackNode = (TypeClauseNode)previousNode;
            }
            else
            {
                parserModel.MostRecentLeftHandSideAssignmentExpressionTypeClauseNode = CSharpFacts.Types.VoidTypeReferenceValue;
            }
        }
        
        if (shouldBacktrack)
        {
            parserModel.ForceParseExpressionInitialPrimaryExpression = backtrackNode;
        }
        var expression = Parser.ParseExpression(ref parserModel);
        if (expression.SyntaxKind == SyntaxKind.BinaryExpressionNode)
        {
            parserModel.Return_BinaryExpressionNode((BinaryExpressionNode)expression);
        }
        parserModel.ForceParseExpressionInitialPrimaryExpression = EmptyExpressionNode.Empty;
    }

    /// <summary>
    /// StatementDelimiterToken is passed in to the method because it is a protected token,
    /// and is preferably consumed from the main loop so it can be more easily tracked.
    /// </summary>
    public static void ParseStatementDelimiterToken(ref CSharpParserState parserModel)
    {
        var statementDelimiterToken = parserModel.TokenWalker.Consume();
    
        if (parserModel.ScopeCurrent.OwnerSyntaxKind == SyntaxKind.NamespaceStatementNode)
        {
            var namespaceStatementNode = parserModel.Binder.NodeList[
                parserModel.Compilation.NodeOffset +
                parserModel.ScopeCurrent.NodeSubIndex];
                
            parserModel.SetCurrentScope_CodeBlock_EndExclusiveIndex(statementDelimiterToken.TextSpan.EndExclusiveIndex);
            parserModel.AddNamespaceToCurrentScope(namespaceStatementNode.IdentifierToken.TextSpan);
        }
        else 
        {
            while (parserModel.ScopeCurrent.OwnerSyntaxKind != SyntaxKind.GlobalCodeBlockNode &&
                   parserModel.ScopeCurrent.IsImplicitOpenCodeBlockTextSpan)
            {
                parserModel.SetCurrentScope_CodeBlock_EndExclusiveIndex(statementDelimiterToken.TextSpan.EndExclusiveIndex);
                parserModel.CloseScope(statementDelimiterToken.TextSpan);
            }
        }
    }
    
    public static void MoveToExpressionBody(ref CSharpParserState parserModel)
    {
        var equalsCloseAngleBracketToken = parserModel.TokenWalker.Consume();
    
        parserModel.SetCurrentScope_IsImplicitOpenCodeBlockTextSpan(true);
        
        var parentScope = parserModel.GetParent(parserModel.ScopeCurrent.ParentScopeSubIndex, parserModel.Compilation);
        var parentScopeDirection = parentScope.IsDefault()
            ? ScopeDirectionKind.Both
            : parentScope.ScopeDirectionKind;
        
        if (parentScopeDirection == ScopeDirectionKind.Both)
        {
            if (!parserModel.ScopeCurrent.PermitCodeBlockParsing)
            {
                parserModel.TokenWalker.DeferParsingOfChildScope(equalsCloseAngleBracketToken, ref parserModel);
                return;
            }

            parserModel.SetCurrentScope_PermitCodeBlockParsing(false);
        }
        else
        {
            parserModel.Return_Helper(Parser.ParseExpression(ref parserModel));
        }
    }

    public static void ParseKeywordToken(ref CSharpParserState parserModel)
    {
        // 'return', 'if', 'get', etc...
        switch (parserModel.TokenWalker.Current.SyntaxKind)
        {
            case SyntaxKind.AsTokenKeyword:
                Parser.HandleAsTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.BaseTokenKeyword:
                Parser.HandleBaseTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.BoolTokenKeyword:
                Parser.HandleBoolTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.BreakTokenKeyword:
                Parser.HandleBreakTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.ByteTokenKeyword:
                Parser.HandleByteTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.CaseTokenKeyword:
                Parser.HandleCaseTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.CatchTokenKeyword:
                Parser.HandleCatchTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.CharTokenKeyword:
                Parser.HandleCharTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.CheckedTokenKeyword:
                Parser.HandleCheckedTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.ConstTokenKeyword:
                Parser.HandleConstTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.ContinueTokenKeyword:
                Parser.HandleContinueTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.DecimalTokenKeyword:
                Parser.HandleDecimalTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.DefaultTokenKeyword:
                Parser.HandleDefaultTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.DelegateTokenKeyword:
                Parser.HandleDelegateTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.DoTokenKeyword:
                Parser.HandleDoTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.DoubleTokenKeyword:
                Parser.HandleDoubleTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.ElseTokenKeyword:
                Parser.HandleElseTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.EnumTokenKeyword:
                Parser.HandleEnumTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.EventTokenKeyword:
                Parser.HandleEventTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.ExplicitTokenKeyword:
                Parser.HandleExplicitTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.ExternTokenKeyword:
                Parser.HandleExternTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.FalseTokenKeyword:
                Parser.HandleFalseTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.FinallyTokenKeyword:
                Parser.HandleFinallyTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.FixedTokenKeyword:
                Parser.HandleFixedTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.FloatTokenKeyword:
                Parser.HandleFloatTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.ForTokenKeyword:
                Parser.HandleForTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.ForeachTokenKeyword:
                Parser.HandleForeachTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.GotoTokenKeyword:
                Parser.HandleGotoTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.ImplicitTokenKeyword:
                Parser.HandleImplicitTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.InTokenKeyword:
                Parser.HandleInTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.IntTokenKeyword:
                Parser.HandleIntTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.IsTokenKeyword:
                Parser.HandleIsTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.LockTokenKeyword:
                Parser.HandleLockTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.LongTokenKeyword:
                Parser.HandleLongTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.NullTokenKeyword:
                Parser.HandleNullTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.ObjectTokenKeyword:
                Parser.HandleObjectTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.OperatorTokenKeyword:
                Parser.HandleOperatorTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.OutTokenKeyword:
                Parser.HandleOutTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.ParamsTokenKeyword:
                Parser.HandleParamsTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.ProtectedTokenKeyword:
                Parser.HandleProtectedTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.ReadonlyTokenKeyword:
                Parser.HandleReadonlyTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.RefTokenKeyword:
                Parser.HandleRefTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.SbyteTokenKeyword:
                Parser.HandleSbyteTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.ShortTokenKeyword:
                Parser.HandleShortTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.SizeofTokenKeyword:
                Parser.HandleSizeofTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.StackallocTokenKeyword:
                Parser.HandleStackallocTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.StringTokenKeyword:
                Parser.HandleStringTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.StructTokenKeyword:
                Parser.HandleStructTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.SwitchTokenKeyword:
                Parser.HandleSwitchTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.ThisTokenKeyword:
                Parser.HandleThisTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.ThrowTokenKeyword:
                Parser.HandleThrowTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.TrueTokenKeyword:
                Parser.HandleTrueTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.TryTokenKeyword:
                Parser.HandleTryTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.TypeofTokenKeyword:
                Parser.HandleTypeofTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.UintTokenKeyword:
                Parser.HandleUintTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.UlongTokenKeyword:
                Parser.HandleUlongTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.UncheckedTokenKeyword:
                Parser.HandleUncheckedTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.UnsafeTokenKeyword:
                Parser.HandleUnsafeTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.UshortTokenKeyword:
                Parser.HandleUshortTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.VoidTokenKeyword:
                Parser.HandleVoidTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.VolatileTokenKeyword:
                Parser.HandleVolatileTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.WhileTokenKeyword:
                Parser.HandleWhileTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.UnrecognizedTokenKeyword:
                Parser.HandleUnrecognizedTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.ReturnTokenKeyword:
                Parser.HandleReturnTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.NamespaceTokenKeyword:
                Parser.HandleNamespaceTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.ClassTokenKeyword:
                Parser.HandleClassTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.InterfaceTokenKeyword:
                Parser.HandleInterfaceTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.UsingTokenKeyword:
                Parser.HandleUsingTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.PublicTokenKeyword:
                Parser.HandlePublicTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.InternalTokenKeyword:
                Parser.HandleInternalTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.PrivateTokenKeyword:
                Parser.HandlePrivateTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.StaticTokenKeyword:
                Parser.HandleStaticTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.OverrideTokenKeyword:
                Parser.HandleOverrideTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.VirtualTokenKeyword:
                Parser.HandleVirtualTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.AbstractTokenKeyword:
                Parser.HandleAbstractTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.SealedTokenKeyword:
                Parser.HandleSealedTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.IfTokenKeyword:
                Parser.HandleIfTokenKeyword(ref parserModel);
                break;
            case SyntaxKind.NewTokenKeyword:
                Parser.HandleNewTokenKeyword(ref parserModel);
                break;
            default:
                Parser.HandleDefault(ref parserModel);
                break;
        }
    }

    public static void ParseKeywordContextualToken(ref CSharpParserState parserModel)
    {
        if (parserModel.TokenWalker.Next.SyntaxKind == SyntaxKind.ColonToken)
        {
            Parser.HandleLabelDeclaration(ref parserModel);
            return;
        }
    
        switch (parserModel.TokenWalker.Current.SyntaxKind)
        {
            case SyntaxKind.VarTokenContextualKeyword:
                Parser.HandleVarTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.PartialTokenContextualKeyword:
                Parser.HandlePartialTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.AddTokenContextualKeyword:
                Parser.HandleAddTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.AndTokenContextualKeyword:
                Parser.HandleAndTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.AliasTokenContextualKeyword:
                Parser.HandleAliasTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.AscendingTokenContextualKeyword:
                Parser.HandleAscendingTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.ArgsTokenContextualKeyword:
                Parser.HandleArgsTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.AsyncTokenContextualKeyword:
                Parser.HandleAsyncTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.AwaitTokenContextualKeyword:
                Parser.HandleAwaitTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.ByTokenContextualKeyword:
                Parser.HandleByTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.DescendingTokenContextualKeyword:
                Parser.HandleDescendingTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.DynamicTokenContextualKeyword:
                Parser.HandleDynamicTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.EqualsTokenContextualKeyword:
                Parser.HandleEqualsTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.FileTokenContextualKeyword:
                Parser.HandleFileTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.FromTokenContextualKeyword:
                Parser.HandleFromTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.GetTokenContextualKeyword:
                Parser.HandleGetTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.GlobalTokenContextualKeyword:
                Parser.HandleGlobalTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.GroupTokenContextualKeyword:
                Parser.HandleGroupTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.InitTokenContextualKeyword:
                Parser.HandleInitTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.IntoTokenContextualKeyword:
                Parser.HandleIntoTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.JoinTokenContextualKeyword:
                Parser.HandleJoinTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.LetTokenContextualKeyword:
                Parser.HandleLetTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.ManagedTokenContextualKeyword:
                Parser.HandleManagedTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.NameofTokenContextualKeyword:
                Parser.HandleNameofTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.NintTokenContextualKeyword:
                Parser.HandleNintTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.NotTokenContextualKeyword:
                Parser.HandleNotTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.NotnullTokenContextualKeyword:
                Parser.HandleNotnullTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.NuintTokenContextualKeyword:
                Parser.HandleNuintTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.OnTokenContextualKeyword:
                Parser.HandleOnTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.OrTokenContextualKeyword:
                Parser.HandleOrTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.OrderbyTokenContextualKeyword:
                Parser.HandleOrderbyTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.RecordTokenContextualKeyword:
                Parser.HandleRecordTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.RemoveTokenContextualKeyword:
                Parser.HandleRemoveTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.RequiredTokenContextualKeyword:
                Parser.HandleRequiredTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.ScopedTokenContextualKeyword:
                Parser.HandleScopedTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.SelectTokenContextualKeyword:
                Parser.HandleSelectTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.SetTokenContextualKeyword:
                Parser.HandleSetTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.UnmanagedTokenContextualKeyword:
                Parser.HandleUnmanagedTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.ValueTokenContextualKeyword:
                Parser.HandleValueTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.WhenTokenContextualKeyword:
                Parser.HandleWhenTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.WhereTokenContextualKeyword:
                Parser.HandleWhereTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.WithTokenContextualKeyword:
                Parser.HandleWithTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.YieldTokenContextualKeyword:
                Parser.HandleYieldTokenContextualKeyword(ref parserModel);
                break;
            case SyntaxKind.UnrecognizedTokenContextualKeyword:
                Parser.HandleUnrecognizedTokenContextualKeyword(ref parserModel);
                break;
            default:
                // compilationUnit.DiagnosticBag.ReportTodoException(parserModel.TokenWalker.Current.TextSpan, $"Implement the {parserModel.TokenWalker.Current.SyntaxKind.ToString()} contextual keyword.");
                break;
        }
    }
}
