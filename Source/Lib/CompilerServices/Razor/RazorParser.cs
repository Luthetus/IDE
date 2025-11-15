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
using Clair.TextEditor.RazorLib.TextEditors.Models;
using static Clair.CompilerServices.Razor.RazorLexer;

namespace Clair.CompilerServices.Razor;

public static class RazorParser
{
    public static void Parse(
        int absolutePathId,
        TokenWalkerBuffer tokenWalkerBuffer,
        StreamReaderPooledBufferWrap streamReaderWrap,
        TextEditorModel textEditorModel,
        ref CSharpCompilationUnit compilationUnit,
        CSharpBinder binder,
        RazorCompilerService razorCompilerService)
    {
        /*
        Any state that is "pooled" and cleared at the start of every Parse(...) invocation
        should be changed.
        
        Clear them inside CSharpBinder.FinalizeCompilationUnit(...) so that they aren't "dangling"
        in between Parse(...) invocations.
        */
        
        /*
        You could have initially, that the lexing only tracks the @code and @functions blocks
        and then defer parses them.
        
        This then would permit C# to know everything it needs to about the razor markup
        (short of I guess if you can make a public variable / static variable
         outside of @code / @functions?).
        */
        
        // The UseRazorShortcircuits isn't necessary because you
        // are going to first try:
        // - lexing all the razor
        //     - This will NOT initially include recursive scenarios
        //       i.e.: html -> C# which contains more html and that contains C# etc...
        // - tracking the @code and @functions blocks
        // Give @code and @functions to the C# parser with strick start and end indices
        // What if the @code or @functions contains templated logic that writes
        // like razor (html with C#)?
        
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
        
        var namespaceIdentifier = new SyntaxToken(
            SyntaxKind.IdentifierToken,
            new TextEditorTextSpan(
                startInclusiveIndex: 0,
                endExclusiveIndex: namespaceString.Length,
                decorationByte: (byte)SyntaxKind.ImplicitTextSource,
                byteIndex: streamReaderWrap.ByteIndex,
                charIntSum));
        var namespaceStatementNode = parserModel.Rent_NamespaceStatementNode();
        namespaceStatementNode.KeywordToken = default;
        namespaceStatementNode.IdentifierToken = namespaceIdentifier;
        namespaceStatementNode.AbsolutePathId = parserModel.AbsolutePathId;
        parserModel.SetCurrentNamespaceStatementValue(new NamespaceStatementValue(namespaceStatementNode));
        parserModel.RegisterScope(
        	new Scope(
        		ScopeDirectionKind.Both,
        		scope_StartInclusiveIndex: 0,
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
        
        CreateRazorPartialClass(streamReaderWrap, ref parserModel, razorCompilerService);
        
        _ = RazorLexer.Lex(
            binder.KeywordCheckBuffer,
            streamReaderWrap,
            textEditorModel);
        
        parserModel.Binder.FinalizeCompilationUnit(parserModel.AbsolutePathId, compilationUnit);
        
        // Random note: consider finding matches by iterating over the scope rather than the scope...
        // ...filtered by SyntaxKind?
        //
        // Or perhaps iterate over all possible scopes but start at the closest ancestor scope.
        // i.e.: something about not invoking those Hierarchical methods that search for a definition
        // over and over. That is a lot of copying of data for the parameters.
    }
    
    public static void CreateRazorPartialClass(StreamReaderPooledBufferWrap streamReaderWrap, ref CSharpParserState parserModel, RazorCompilerService razorCompilerService)
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
                startInclusiveIndex: 0,
                endExclusiveIndex: streamReaderWrap.PositionIndex + componentName.Length/* + 1*/,
                decorationByte: (byte)SyntaxKind.ImplicitTextSource,
                byteIndex: streamReaderWrap.ByteIndex,
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
        		scope_StartInclusiveIndex: 0,
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

                            string? previousParentIdentifierText;
                            if (parserModel.Binder.NodeList[previousCompilationUnit.NodeOffset + previousParent.NodeSubIndex].IdentifierToken.TextSpan.DecorationByte ==
                                    (byte)SyntaxKind.ImplicitTextSource)
                            {
                                if (parserModel.Binder.NodeList[previousCompilationUnit.NodeOffset + previousParent.NodeSubIndex].SyntaxKind ==
                                        SyntaxKind.NamespaceStatementNode)
                                {
                                    previousParentIdentifierText = parserModel.Binder.CSharpCompilerService.GetRazorNamespace(
                                        parserModel.Binder.NodeList[previousCompilationUnit.NodeOffset + previousParent.NodeSubIndex].AbsolutePathId,
                                        isTextEditorContext: true);
                                }
                                else if (parserModel.Binder.NodeList[previousCompilationUnit.NodeOffset + previousParent.NodeSubIndex].SyntaxKind ==
                                        SyntaxKind.TypeDefinitionNode)
                                {
                                    previousParentIdentifierText = parserModel.Binder.CSharpCompilerService.GetRazorComponentName(
                                        parserModel.Binder.NodeList[previousCompilationUnit.NodeOffset + previousParent.NodeSubIndex].AbsolutePathId);
                                }
                                else
                                {
                                    throw new NotImplementedException("Currently only namespaces and types are ever made implicit text source.");
                                }
                            }
                            else
                            {
                                previousParentIdentifierText = parserModel.Binder.CSharpCompilerService.SafeGetText(
                                    parserModel.Binder.NodeList[previousCompilationUnit.NodeOffset + previousParent.NodeSubIndex].AbsolutePathId,
                                    parserModel.Binder.NodeList[previousCompilationUnit.NodeOffset + previousParent.NodeSubIndex].IdentifierToken.TextSpan);
                            }
                            
                            
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
            
            if (parserModel.ClearedPartialDefinitionHashSet.Add(componentName) &&
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
