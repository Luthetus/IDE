using Clair.TextEditor.RazorLib.Exceptions;
using Clair.Extensions.CompilerServices.Syntax;
using Clair.CompilerServices.CSharp.ParserCase;
using Clair.CompilerServices.CSharp.ParserCase.Internals;
using Clair.CompilerServices.CSharp.BinderCase;
using Clair.CompilerServices.CSharp.CompilerServiceCase;
using Clair.Extensions.CompilerServices.Syntax.Enums;
using Clair.Extensions.CompilerServices.Syntax.NodeValues;

namespace Clair.CompilerServices.Razor;

public static class RazorParser
{
    public static void Parse(int absolutePathId, TokenWalkerBuffer tokenWalkerBuffer, ref CSharpCompilationUnit compilationUnit, CSharpBinder binder)
    {
        /*
        Any state that is "pooled" and cleared at the start of every Parse(...) invocation
        should be changed.
        
        Clear them inside CSharpBinder.FinalizeCompilationUnit(...) so that they aren't "dangling"
        in between Parse(...) invocations.
        */
        
        // Console.WriteLine("\n========");
        
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
            ref compilationUnit);
            
        CreateRazorPartialClass(ref parserModel);
        
        while (true)
        {
            // The last statement in this while loop is conditionally: '_ = parserModel.TokenWalker.Consume();'.
            // Knowing this to be the case is extremely important.
            
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
                case SyntaxKind.AtToken:
                    if (parserModel.StatementBuilder.StatementIsEmpty)
                    {
                        _ = Parser.ParseExpression(ref parserModel);
                    }
                    else
                    {
                        parserModel.StatementBuilder.MostRecentNode = Parser.ParseExpression(ref parserModel);
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
        
        // Console.WriteLine("========\n");
    }
    
    public static void CreateRazorPartialClass(ref CSharpParserState parserModel)
    {
        var storageModifierToken = parserModel.TokenWalker.Consume();
        
        // Given: public partial class MyClass { }
        // Then: partial
        var hasPartialModifier = false;
        if (parserModel.StatementBuilder.TryPeek(out var token))
        {
            if (token.SyntaxKind == SyntaxKind.PartialTokenContextualKeyword)
            {
                _ = parserModel.StatementBuilder.Pop();
                hasPartialModifier = true;
            }
        }
    
        // TODO: Fix; the code that parses the accessModifierKind is a mess
        //
        // Given: public class MyClass { }
        // Then: public
        var accessModifierKind = AccessModifierKind.Public;
        if (parserModel.StatementBuilder.TryPeek(out var firstSyntaxToken))
        {
            var firstOutput = UtilityApi.GetAccessModifierKindFromToken(firstSyntaxToken);

            if (firstOutput != AccessModifierKind.None)
            {
                _ = parserModel.StatementBuilder.Pop();
                accessModifierKind = firstOutput;

                // Given: protected internal class MyClass { }
                // Then: protected internal
                if (parserModel.StatementBuilder.TryPeek(out var secondSyntaxToken))
                {
                    var secondOutput = UtilityApi.GetAccessModifierKindFromToken(secondSyntaxToken);

                    if (secondOutput != AccessModifierKind.None)
                    {
                        _ = parserModel.StatementBuilder.Pop();

                        if ((firstOutput == AccessModifierKind.Protected && secondOutput == AccessModifierKind.Internal) ||
                            (firstOutput == AccessModifierKind.Internal && secondOutput == AccessModifierKind.Protected))
                        {
                            accessModifierKind = AccessModifierKind.ProtectedInternal;
                        }
                        else if ((firstOutput == AccessModifierKind.Private && secondOutput == AccessModifierKind.Protected) ||
                                (firstOutput == AccessModifierKind.Protected && secondOutput == AccessModifierKind.Private))
                        {
                            accessModifierKind = AccessModifierKind.PrivateProtected;
                        }
                        // else use the firstOutput.
                    }
                }
            }
        }
    
        // TODO: Fix nullability spaghetti code
        var storageModifierKind = UtilityApi.GetStorageModifierKindFromToken(storageModifierToken);
        if (storageModifierKind == StorageModifierKind.None)
            return;
        if (storageModifierKind == StorageModifierKind.Record)
        {
            if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.ClassTokenKeyword)
            {
                _ = parserModel.TokenWalker.Consume(); // classKeywordToken
                storageModifierKind = StorageModifierKind.RecordClass;
            }
            else if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.StructTokenKeyword)
            {
                _ = parserModel.TokenWalker.Consume(); // structKeywordToken
                storageModifierKind = StorageModifierKind.RecordStruct;
            }
        }

        // Given: public class MyClass<T> { }
        // Then: MyClass
        SyntaxToken identifierToken;
        // Retrospective: What is the purpose of this 'if (contextualKeyword) logic'?
        // Response: maybe it is because 'var' contextual keyword is allowed to be a class name?
        if (UtilityApi.IsContextualKeywordSyntaxKind(parserModel.TokenWalker.Current.SyntaxKind))
        {
            var contextualKeywordToken = parserModel.TokenWalker.Consume();
            // Take the contextual keyword as an identifier
            identifierToken = new SyntaxToken(SyntaxKind.IdentifierToken, contextualKeywordToken.TextSpan);
        }
        else
        {
            identifierToken = parserModel.TokenWalker.Match(SyntaxKind.IdentifierToken);
        }

        // Given: public class MyClass<T> { }
        // Then: <T>
        (SyntaxToken OpenAngleBracketToken, int IndexGenericParameterEntryList, int CountGenericParameterEntryList, SyntaxToken CloseAngleBracketToken) genericParameterListing = default;
        if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.OpenAngleBracketToken)
            genericParameterListing = Parser.HandleGenericParameters(ref parserModel);

        var typeDefinitionNode = parserModel.Rent_TypeDefinitionNode();
        
        typeDefinitionNode.AccessModifierKind = accessModifierKind;
        typeDefinitionNode.HasPartialModifier = hasPartialModifier;
        typeDefinitionNode.StorageModifierKind = storageModifierKind;
        typeDefinitionNode.TypeIdentifierToken = identifierToken;
        typeDefinitionNode.OpenAngleBracketToken = genericParameterListing.OpenAngleBracketToken;
        typeDefinitionNode.OffsetGenericParameterEntryList = genericParameterListing.IndexGenericParameterEntryList;
        typeDefinitionNode.LengthGenericParameterEntryList = genericParameterListing.CountGenericParameterEntryList;
        typeDefinitionNode.CloseAngleBracketToken = genericParameterListing.CloseAngleBracketToken;
        typeDefinitionNode.AbsolutePathId = parserModel.AbsolutePathId;
        
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
                            var currentParentIdentifierText = parserModel.Binder.CSharpCompilerService.SafeGetText(
                                parserModel.Binder.NodeList[parserModel.Compilation.NodeOffset + currentParent.NodeSubIndex].AbsolutePathId,
                                parserModel.Binder.NodeList[parserModel.Compilation.NodeOffset + currentParent.NodeSubIndex].IdentifierToken.TextSpan);
                            
                            var previousParentIdentifierText = parserModel.Binder.CSharpCompilerService.SafeGetText(
                                parserModel.Binder.NodeList[previousCompilationUnit.NodeOffset + previousParent.NodeSubIndex].AbsolutePathId,
                                parserModel.Binder.NodeList[previousCompilationUnit.NodeOffset + previousParent.NodeSubIndex].IdentifierToken.TextSpan);
                            
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
                                                parserModel.Binder.NodeList[previousCompilationUnit.NodeOffset + scope.NodeSubIndex].IdentifierToken.TextSpan) ==
                                            binder.GetIdentifierText(typeDefinitionNode, parserModel.AbsolutePathId, compilation))
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
            
            if (parserModel.ClearedPartialDefinitionHashSet.Add(parserModel.GetTextSpanText(identifierToken.TextSpan)) &&
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
        
        if (storageModifierKind == StorageModifierKind.Enum)
        {
            Parser.HandleEnumDefinitionNode(typeDefinitionNode, ref parserModel);
            parserModel.Return_TypeDefinitionNode(typeDefinitionNode);
            return;
        }
    
        if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.OpenParenthesisToken)
        {
            Parser.HandlePrimaryConstructorDefinition(
                typeDefinitionNode,
                ref parserModel);
        }
        
        if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.ColonToken)
        {
            _ = parserModel.TokenWalker.Consume(); // Consume the ColonToken
            var inheritedTypeClauseNode = Parser.MatchTypeClause(ref parserModel);
            // parserModel.BindTypeClauseNode(inheritedTypeClauseNode);
            typeDefinitionNode.SetInheritedTypeReference(new TypeReferenceValue(inheritedTypeClauseNode));
            parserModel.Return_TypeClauseNode(inheritedTypeClauseNode);
            
            while (!parserModel.TokenWalker.IsEof)
            {
                if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.CommaToken)
                {
                    _ = parserModel.TokenWalker.Consume(); // Consume the CommaToken
                
                    var consumeCounter = parserModel.TokenWalker.ConsumeCounter;
                    
                    _ = Parser.MatchTypeClause(ref parserModel);
                    // parserModel.BindTypeClauseNode();
                    
                    if (consumeCounter == parserModel.TokenWalker.ConsumeCounter)
                        break;
                }
                else
                {
                    break;
                }
            }
        }
        
        if (parserModel.TokenWalker.Current.SyntaxKind == SyntaxKind.WhereTokenContextualKeyword)
        {
            parserModel.ExpressionList.Add((SyntaxKind.OpenBraceToken, null));
            _ = Parser.ParseExpression(ref parserModel);
        }
        
        if (parserModel.TokenWalker.Current.SyntaxKind != SyntaxKind.OpenBraceToken)
            parserModel.SetCurrentScope_IsImplicitOpenCodeBlockTextSpan(true);
    
        if (typeDefinitionNode.HasPartialModifier)
            Parser.HandlePartialTypeDefinition(typeDefinitionNode, ref parserModel);
    
        parserModel.Return_TypeDefinitionNode(typeDefinitionNode);
    }
}

