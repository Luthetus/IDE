using Clair.Common.RazorLib.FileSystems.Models;
using Clair.CompilerServices.CSharp.BinderCase;
using Clair.CompilerServices.CSharp.CompilerServiceCase;
using Clair.CompilerServices.CSharp.ParserCase;
using Clair.CompilerServices.CSharp.ParserCase.Internals;
using Clair.Extensions.CompilerServices.Syntax;
using Clair.Extensions.CompilerServices.Syntax.Enums;
using Clair.Extensions.CompilerServices.Syntax.NodeValues;
using Clair.TextEditor.RazorLib.Exceptions;
using Clair.TextEditor.RazorLib.Lexers.Models;
using static Clair.CompilerServices.Razor.RazorLexer;

namespace Clair.CompilerServices.Razor;

public static class RazorParser
{
    public static void Parse(
        int absolutePathId,
        TokenWalkerBuffer tokenWalkerBuffer,
        ref CSharpCompilationUnit compilationUnit,
        CSharpBinder binder,
        RazorCompilerService razorCompilerService)
    {
        // Remove RazorParserState
        // Only need CSharpParserState
        //
        // Difference is that the Razor parser also needs to invoke Razor Lexer
        //
        // CSharp static methods for parsing will exit and return to the razor statement loop.
        // Razor statement loop then delegates either the CSharpLexer.Lex or RazorLexer.Lex
        //
        // Short circuit C# expression on unmatched '<' (also maybe '@')
        //
        // Probably let the Razor lexer just run until it is necessary to short circuit for language transition to C#.
        //
        // Long term perhaps can use HtmlLexer and drop RazorLexer entirely, for now though I'm not getting involved in that.
        //
        // Might have to delete RazorTokenWalkerBuffer and pass a delegate to TokenWalker.
        // Or add a parameter then if statement.
        //
        // Optionally parameter have it default to C# lexer so the C# code can stay unchanged.
        // Then the RazorParser can explicitly ask for the Razor lexer to be invoked.
        //
        // I don't want to use inheritance nor an interface because then each invocation of the
        // TokenWalker has overhead on each invocation of Consume().
        // Also I'm not sure but I think inheritance or interfaces would
        // impact whether the Consume() method is inlined.
        //
        // What is the overhead of invoking a delegate vs a static method?
    
        /*
        Any state that is "pooled" and cleared at the start of every Parse(...) invocation
        should be changed.
        
        Clear them inside CSharpBinder.FinalizeCompilationUnit(...) so that they aren't "dangling"
        in between Parse(...) invocations.
        */
        
        compilationUnit.ScopeOffset = binder.ScopeList.Count;
        compilationUnit.NamespaceContributionOffset = binder.NamespaceContributionList.Count;

        binder.ScopeList.Insert(
            compilationUnit.ScopeOffset + compilationUnit.ScopeLength,
            new Scope(
        		ScopeDirectionKind.Both,
        		scope_StartInclusiveIndex: 0,
        		scope_EndExclusiveIndex: -1,
        		codeBlock_StartInclusiveIndex: -1,
        		codeBlock_EndExclusiveIndex: -1,
        		parentScopeSubIndex: -1,
        		selfScopeSubIndex: 0,
        		nodeSubIndex: -1,
        		permitCodeBlockParsing: true,
        		isImplicitOpenCodeBlockTextSpan: true,
        		ownerSyntaxKind: SyntaxKind.GlobalCodeBlockNode));
        ++compilationUnit.ScopeLength;
        
        var parserModel = new CSharpParserState(
            binder,
            tokenWalkerBuffer,
            absolutePathId,
            ref compilationUnit,
            includeRazorShortCircuits: true);

        // TODO: Do these two steps after you've lexed incase there is @namespace directive...
        // ...also consider '.csproj' RootNamespace.
        // Parser.implicit_HandleNamespaceTokenKeyword(ref parserModel, binder.CSharpCompilerService.GetRazorNamespace(absolutePathId, isTextEditorContext: true));
        var namespaceString = binder.CSharpCompilerService.GetRazorNamespace(absolutePathId, isTextEditorContext: true);
        var charIntSum = 0;
        foreach (var c in namespaceString)
        {
            charIntSum += (int)c;
        }
        // bathroom then compare if implicit vs explicit
        // then
        // search for @code section first (can there be more than 1?)
        // then for @functions (can there be more than 1?)
        // can you have @code and @functions?
        //
        // Parse those first,
        // then open a method scope and reset the seek position.
        var namespaceIdentifier = new SyntaxToken(
            SyntaxKind.IdentifierToken,
            new TextEditorTextSpan(
                startInclusiveIndex: 0,
                endExclusiveIndex: namespaceString.Length,
                decorationByte: 0,
                byteIndex: parserModel.TokenWalker.StreamReaderWrap.ByteIndex,
                charIntSum));
        var namespaceStatementNode = parserModel.Rent_NamespaceStatementNode();
        namespaceStatementNode.KeywordToken = default;
        namespaceStatementNode.IdentifierToken = namespaceIdentifier;
        namespaceStatementNode.AbsolutePathId = parserModel.AbsolutePathId;
        namespaceStatementNode.TextSourceKind = TextSourceKind.Implicit;
        parserModel.SetCurrentNamespaceStatementValue(new NamespaceStatementValue(namespaceStatementNode));
        parserModel.RegisterScope(
        	new Scope(
        		ScopeDirectionKind.Both,
        		scope_StartInclusiveIndex: parserModel.TokenWalker.Current.TextSpan.StartInclusiveIndex,
        		scope_EndExclusiveIndex: -1,
        		codeBlock_StartInclusiveIndex: -1,
        		codeBlock_EndExclusiveIndex: -1,
        		parentScopeSubIndex: parserModel.ScopeCurrentSubIndex,
        		selfScopeSubIndex: parserModel.Compilation.ScopeLength,
        		nodeSubIndex: parserModel.Compilation.NodeLength,
        		permitCodeBlockParsing: true,
        		isImplicitOpenCodeBlockTextSpan: false,
        		ownerSyntaxKind: namespaceStatementNode.SyntaxKind),
    	    namespaceStatementNode);
        parserModel.Return_NamespaceStatementNode(namespaceStatementNode);
        
        CreateRazorPartialClass(ref parserModel, razorCompilerService);
        
        parserModel.TokenWalker.SetUseCSharpLexer(useCSharpLexer: false);
        var initialToken = parserModel.TokenWalker.Current;
        parserModel.TokenWalker.IsInitialParse = true;
        while (true)
        {
            switch (parserModel.TokenWalker.Current.SyntaxKind)
            {
                case SyntaxKind.EndOfFileToken:
                    goto exitInitialLexing;
            }
            _ = parserModel.TokenWalker.Consume();
        }
        exitInitialLexing:
        parserModel.TokenWalker.IsInitialParse = false;

        // Random note: consider finding matches by iterating over the scope rather than the scope...
        // ...filtered by SyntaxKind?
        //
        // Or perhaps iterate over all possible scopes but start at the closest ancestor scope.
        // i.e.: something about not invoking those Hierarchical methods that search for a definition
        // over and over. That is a lot of copying of data for the parameters.

        //parserModel.TokenWalker.SetUseCSharpLexer(useCSharpLexer: true);
        tokenWalkerBuffer.Seek_SeekOriginBegin(initialToken, tokenIndex: 0, rootConsumeCounter: 0);

        while (true)
        {
            // The last statement in this while loop is conditionally: '_ = parserModel.TokenWalker.Consume();'.
            // Knowing this to be the case is extremely important.

            //parserModel.TokenWalker.SetUseCSharpLexer(useCSharpLexer: false);

            switch (parserModel.TokenWalker.Current.SyntaxKind)
            {
                case SyntaxKind.NumericLiteralToken:
                case SyntaxKind.CharLiteralToken:
                case SyntaxKind.StringLiteralToken:
                case SyntaxKind.StringInterpolatedStartToken:
                case SyntaxKind.PlusToken:
                case SyntaxKind.PlusPlusToken:
                case SyntaxKind.MinusToken:
                case SyntaxKind.StarToken:
                case SyntaxKind.DollarSignToken:
                    if (parserModel.StatementBuilder.StatementIsEmpty)
                    {
                        _ = Parser.ParseExpression(ref parserModel);
                    }
                    else
                    {
                        parserModel.StatementBuilder.MostRecentNode = Parser.ParseExpression(ref parserModel);
                    }
                    break;
                case SyntaxKind.AtToken:
                    var identifierOrKeyword = RazorLexer.SkipCSharpdentifierOrKeyword(
                        binder.KeywordCheckBuffer,
                        tokenWalkerBuffer,
                        SyntaxContinuationKind.None);

                    var isSupportedRazorDirective = false;

                    if (identifierOrKeyword.SyntaxKind == SyntaxKind.RazorDirective)
                    {
                        if (identifierOrKeyword.TextSpan.CharIntSum == 413) // page
                        {
                            isSupportedRazorDirective = true;
                        }
                    }
                    
                    if (!isSupportedRazorDirective)
                    {
                        parserModel.TokenWalker.SetUseCSharpLexer(useCSharpLexer: true);
                        _ = parserModel.TokenWalker.Consume();
                    }
                    break;
                case SyntaxKind.IdentifierToken:
                    Parser.ParseIdentifierToken(ref parserModel);
                    break;
                case SyntaxKind.OpenBraceToken:
                {
                    var deferredParsingOccurred = parserModel.StatementBuilder.FinishStatement(parserModel.TokenWalker.Index, parserModel.TokenWalker.Current, ref parserModel);
                    if (deferredParsingOccurred)
                        break;

                    Parser.ParseOpenBraceToken(ref parserModel);
                    break;
                }
                case SyntaxKind.CloseBraceToken:
                {
                    var deferredParsingOccurred = parserModel.StatementBuilder.FinishStatement(parserModel.TokenWalker.Index, parserModel.TokenWalker.Current, ref parserModel);
                    if (deferredParsingOccurred)
                        break;
                    
                    // When consuming a 'CloseBraceToken' it is possible for the TokenWalker to change the 'Index'
                    // to a value that is more than 1 larger than the current index.
                    //
                    // This is an issue because some code presumes that 'parserModel.TokenWalker.Index - 1'
                    // will always give them the index of the previous token.
                    //
                    // So, the ParseCloseBraceToken(...) method needs to be passed the index that was consumed
                    // in order to get the CloseBraceToken.
                    var closeBraceTokenIndex = parserModel.TokenWalker.Index;
                    
                    if (parserModel.ParseChildScopeStack.Count > 0 &&
                        parserModel.ParseChildScopeStack.Peek().ScopeSubIndex == parserModel.ScopeCurrentSubIndex)
                    {
                        parserModel.TokenWalker.SetNullDeferredParsingTuple();
                    }
                    
                    Parser.ParseCloseBraceToken(closeBraceTokenIndex, ref parserModel);
                    break;
                }
                case SyntaxKind.OpenParenthesisToken:
                    Parser.ParseOpenParenthesisToken(ref parserModel);
                    break;
                case SyntaxKind.OpenSquareBracketToken:
                    Parser.ParseOpenSquareBracketToken(ref parserModel);
                    break;
                case SyntaxKind.OpenAngleBracketToken:
                    if (parserModel.StatementBuilder.StatementIsEmpty)
                        _ = Parser.ParseExpression(ref parserModel);
                    else
                        _ = parserModel.TokenWalker.Consume();
                    break;
                case SyntaxKind.PreprocessorDirectiveToken:
                case SyntaxKind.CloseParenthesisToken:
                case SyntaxKind.CloseAngleBracketToken:
                case SyntaxKind.CloseSquareBracketToken:
                case SyntaxKind.ColonToken:
                case SyntaxKind.MemberAccessToken:
                    _ = parserModel.TokenWalker.Consume();
                    break;
                case SyntaxKind.EqualsToken:
                    Parser.ParseEqualsToken(ref parserModel);
                    break;
                case SyntaxKind.EqualsCloseAngleBracketToken:
                {
                    _ = parserModel.TokenWalker.Consume(); // Consume 'EqualsCloseAngleBracketToken'
                    parserModel.Return_Helper(Parser.ParseExpression(ref parserModel));
                    break;
                }
                case SyntaxKind.StatementDelimiterToken:
                {
                    var deferredParsingOccurred = parserModel.StatementBuilder.FinishStatement(parserModel.TokenWalker.Index, parserModel.TokenWalker.Current, ref parserModel);
                    if (deferredParsingOccurred)
                        break;

                    Parser.ParseStatementDelimiterToken(ref parserModel);
                    break;
                }
                case SyntaxKind.EndOfFileToken:
                    break;
                default:
                    if (UtilityApi.IsContextualKeywordSyntaxKind(parserModel.TokenWalker.Current.SyntaxKind))
                        Parser.ParseKeywordContextualToken(ref parserModel);
                    else if (UtilityApi.IsKeywordSyntaxKind(parserModel.TokenWalker.Current.SyntaxKind))
                        Parser.ParseKeywordToken(ref parserModel);
                    break;
            }

            if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.EndOfFileToken)
            {
                bool deferredParsingOccurred = false;
                
                if (parserModel.ParseChildScopeStack.Count > 0)
                {
                    var tuple = parserModel.ParseChildScopeStack.Peek();
                    
                    if (tuple.ScopeSubIndex == parserModel.ScopeCurrentSubIndex)
                    {
                        tuple = parserModel.ParseChildScopeStack.Pop();
                        tuple.DeferredChildScope.PrepareMainParserLoop(
                            parserModel.TokenWalker.Index,
                            parserModel.TokenWalker.Current,
                            ref parserModel);
                        deferredParsingOccurred = true;
                    }
                }
                
                if (!deferredParsingOccurred)
                {
                    // This second 'deferredParsingOccurred' is for any lambda expressions with one or many statements in its body.
                    deferredParsingOccurred = parserModel.StatementBuilder.FinishStatement(parserModel.TokenWalker.Index, parserModel.TokenWalker.Current, ref parserModel);
                    if (!deferredParsingOccurred)
                        break;
                }
            }
            
            if (parserModel.TokenWalker.ConsumeCounter == 0)
            {
                // This means either:
                //     - None of the methods for syntax could make sense of the token, so they didn't consume it.
                //     - For whatever reason the method that handled the syntax made sense of the token, but never consumed it.
                //     - The token was consumed, then for some reason a backtrack occurred.
                //
                // To avoid an infinite loop, this will ensure at least 1 token is consumed each iteration of the while loop.
                // 
                // (and that the token index increased by at least 1 from the previous loop; this is implicitly what is implied).
                _ = parserModel.TokenWalker.Consume();
            }
            else if (parserModel.TokenWalker.ConsumeCounter < 0)
            {
                // This means that a syntax invoked 'parserModel.TokenWalker.Backtrack()'.
                // Without invoking an equal amount of 'parserModel.TokenWalker.Consume()' to avoid an infinite loop.
                throw new ClairTextEditorException($"parserModel.TokenWalker.ConsumeCounter:{parserModel.TokenWalker.ConsumeCounter} < 0");
            }
            
            parserModel.TokenWalker.ConsumeCounterReset();
        }

        if (!parserModel.GetParent(parserModel.ScopeCurrent.ParentScopeSubIndex, compilationUnit).IsDefault())
            parserModel.CloseScope(parserModel.TokenWalker.Current.TextSpan); // The current token here would be the EOF token.
        
        parserModel.Binder.FinalizeCompilationUnit(parserModel.AbsolutePathId, compilationUnit);
    }
    
    public static void CreateRazorPartialClass(ref CSharpParserState parserModel, RazorCompilerService razorCompilerService)
    {
        var componentName = razorCompilerService._cSharpCompilerService.GetRazorComponentName(parserModel.AbsolutePathId);
        
        var charIntSum = 0;
        foreach (var c in componentName)
        {
            charIntSum += (int)c;
        }
    
        var identifierToken = new SyntaxToken(
            SyntaxKind.IdentifierToken,
            new TextEditorTextSpan(
                startInclusiveIndex: parserModel.TokenWalker.StreamReaderWrap.PositionIndex,
                endExclusiveIndex: parserModel.TokenWalker.StreamReaderWrap.PositionIndex + componentName.Length/* + 1*/,
                decorationByte: 0,
                byteIndex: parserModel.TokenWalker.StreamReaderWrap.ByteIndex,
                charIntSum));
        var typeDefinitionNode = parserModel.Rent_TypeDefinitionNode();
        
        typeDefinitionNode.AccessModifierKind = AccessModifierKind.Public;
        typeDefinitionNode.HasPartialModifier = true;
        typeDefinitionNode.StorageModifierKind = StorageModifierKind.Class;
        typeDefinitionNode.TypeIdentifierToken = identifierToken;
        typeDefinitionNode.OpenAngleBracketToken = default;
        typeDefinitionNode.OffsetGenericParameterEntryList = -1;
        typeDefinitionNode.LengthGenericParameterEntryList = 0;
        typeDefinitionNode.CloseAngleBracketToken = default;
        typeDefinitionNode.AbsolutePathId = parserModel.AbsolutePathId;
        typeDefinitionNode.TextSourceKind = TextSourceKind.Implicit;
        
        if (typeDefinitionNode.HasPartialModifier)
        {
            // NOTE: You do indeed use the current compilation unit here...
            // ...there is a different step that checks the previous.
            if (parserModel.TryGetTypeDefinitionHierarchically(
                    parserModel.AbsolutePathId,
                    parserModel.Compilation,
                    parserModel.ScopeCurrentSubIndex,
                    parserModel.AbsolutePathId,
                    identifierToken.TextSpan,
                    TextSourceKind.Implicit,
                    out SyntaxNodeValue previousTypeDefinitionNode))
            {
                var typeDefinitionMetadata = parserModel.Binder.TypeDefinitionTraitsList[previousTypeDefinitionNode.TraitsIndex];
                typeDefinitionNode.IndexPartialTypeDefinition = typeDefinitionMetadata.IndexPartialTypeDefinition;
            }
        }
        
        parserModel.BindTypeDefinitionNode(typeDefinitionNode);
        parserModel.BindTypeIdentifier(identifierToken);
        
        parserModel.StatementBuilder.MostRecentNode = typeDefinitionNode;
            
        parserModel.RegisterScope(
        	new Scope(
        		ScopeDirectionKind.Both,
        		scope_StartInclusiveIndex: parserModel.TokenWalker.Current.TextSpan.StartInclusiveIndex,
        		scope_EndExclusiveIndex: -1,
        		codeBlock_StartInclusiveIndex: -1,
        		codeBlock_EndExclusiveIndex: -1,
        		parentScopeSubIndex: parserModel.ScopeCurrentSubIndex,
        		selfScopeSubIndex: parserModel.Compilation.ScopeLength,
        		nodeSubIndex: parserModel.Compilation.NodeLength,
        		permitCodeBlockParsing: true,
        		isImplicitOpenCodeBlockTextSpan: false,
        		ownerSyntaxKind: typeDefinitionNode.SyntaxKind),
    	    typeDefinitionNode);
        
        parserModel.SetCurrentScope_IsImplicitOpenCodeBlockTextSpan(false);

        if (typeDefinitionNode.HasPartialModifier)
        {
            if (typeDefinitionNode.IndexPartialTypeDefinition == -1)
            {
                if (parserModel.Binder.__CompilationUnitMap.TryGetValue(parserModel.AbsolutePathId, out var previousCompilationUnit))
                {
                    if (typeDefinitionNode.ParentScopeSubIndex < previousCompilationUnit.ScopeLength)
                    {
                        var previousParent = parserModel.Binder.ScopeList[previousCompilationUnit.ScopeOffset + typeDefinitionNode.ParentScopeSubIndex];
                        var currentParent = parserModel.GetParent(typeDefinitionNode.ParentScopeSubIndex, parserModel.Compilation);
                        
                        if (currentParent.OwnerSyntaxKind == previousParent.OwnerSyntaxKind)
                        {
                            var currentParentIdentifierText = string.Empty;
                            
                            var previousParentIdentifierText = parserModel.Binder.CSharpCompilerService.SafeGetText(
                                parserModel.Binder.NodeList[previousCompilationUnit.NodeOffset + previousParent.NodeSubIndex].AbsolutePathId,
                                parserModel.Binder.NodeList[previousCompilationUnit.NodeOffset + previousParent.NodeSubIndex].IdentifierToken.TextSpan,
                                TextSourceKind.Explicit);
                            
                            if (currentParentIdentifierText is not null &&
                                currentParentIdentifierText == previousParentIdentifierText)
                            {
                                // All the existing entires will be "emptied"
                                // so don't both with checking whether the arguments are the same here.
                                //
                                // All that matters is that they're put in the same "group".
                                //
                                var binder = parserModel.Binder;
                                
                                // TODO: Cannot use ref, out, or in...
                                var compilation = parserModel.Compilation;
                                
                                SyntaxNodeValue previousNode = default;
                                
                                for (int i = previousCompilationUnit.ScopeOffset; i < previousCompilationUnit.ScopeOffset + previousCompilationUnit.ScopeLength; i++)
                                {
                                    var scope = parserModel.Binder.ScopeList[i];
                                    
                                    if (scope.ParentScopeSubIndex == previousParent.SelfScopeSubIndex &&
                                        scope.OwnerSyntaxKind == SyntaxKind.TypeDefinitionNode &&
                                        binder.CSharpCompilerService.SafeGetText(
                                                parserModel.Binder.NodeList[previousCompilationUnit.NodeOffset + scope.NodeSubIndex].AbsolutePathId,
                                                parserModel.Binder.NodeList[previousCompilationUnit.NodeOffset + scope.NodeSubIndex].IdentifierToken.TextSpan,
                                                TextSourceKind.Explicit) ==
                                            razorCompilerService._cSharpCompilerService.GetRazorComponentName(parserModel.AbsolutePathId))
                                    {
                                        previousNode = parserModel.Binder.NodeList[previousCompilationUnit.NodeOffset + scope.NodeSubIndex];
                                        break;
                                    }
                                }
                                
                                if (!previousNode.IsDefault())
                                {
                                    var previousTypeDefinitionNode = previousNode;
                                    var previousTypeDefinitionMetadata = parserModel.Binder.TypeDefinitionTraitsList[previousTypeDefinitionNode.TraitsIndex];
                                    typeDefinitionNode.IndexPartialTypeDefinition = previousTypeDefinitionMetadata.IndexPartialTypeDefinition;
                                }
                            }
                        }
                    }
                }
            }
            
            if (parserModel.ClearedPartialDefinitionHashSet.Add(parserModel.GetTextSpanText(identifierToken.TextSpan, TextSourceKind.Explicit)) &&
                typeDefinitionNode.IndexPartialTypeDefinition != -1)
            {
                // Partial definitions of the same type from the same ResourceUri are made contiguous.
                var seenResourceUri = false;
                
                int positionExclusive = typeDefinitionNode.IndexPartialTypeDefinition;
                while (positionExclusive < parserModel.Binder.PartialTypeDefinitionList.Count)
                {
                    if (parserModel.Binder.PartialTypeDefinitionList[positionExclusive].IndexStartGroup == typeDefinitionNode.IndexPartialTypeDefinition)
                    {
                        if (parserModel.Binder.PartialTypeDefinitionList[positionExclusive].AbsolutePathId == parserModel.AbsolutePathId)
                        {
                            seenResourceUri = true;
                        
                            var partialTypeDefinitionEntry = parserModel.Binder.PartialTypeDefinitionList[positionExclusive];
                            partialTypeDefinitionEntry.ScopeSubIndex = -1;
                            parserModel.Binder.PartialTypeDefinitionList[positionExclusive] = partialTypeDefinitionEntry;
                            
                            positionExclusive++;
                        }
                        else
                        {
                            if (seenResourceUri)
                                break;
                            positionExclusive++;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    
        if (typeDefinitionNode.HasPartialModifier)
        {
            Parser.HandlePartialTypeDefinition(typeDefinitionNode, ref parserModel);
        }
        
        parserModel.Return_TypeDefinitionNode(typeDefinitionNode);
    }
}
