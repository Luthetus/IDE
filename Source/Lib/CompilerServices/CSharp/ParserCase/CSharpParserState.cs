using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.TextEditor.RazorLib.Decorations.Models;
using Clair.TextEditor.RazorLib.CompilerServices;
using Clair.Extensions.CompilerServices;
using Clair.Extensions.CompilerServices.Syntax;
using Clair.CompilerServices.CSharp.BinderCase;
using Clair.CompilerServices.CSharp.Facts;
using Clair.CompilerServices.CSharp.CompilerServiceCase;
using Clair.Extensions.CompilerServices.Syntax.Enums;
using Clair.Extensions.CompilerServices.Syntax.Interfaces;
using Clair.Extensions.CompilerServices.Syntax.NodeValues;
using Clair.Extensions.CompilerServices.Syntax.NodeReferences;

namespace Clair.CompilerServices.CSharp.ParserCase;

/// <summary>
/// The computational state for the CSharpParser is contained within this type.
/// The output of the CSharpParser is the <see cref="CSharpCompilationUnit"/>.<see cref="CSharpCompilationUnit.RootCodeBlockNode"/>
/// </summary>
public ref partial struct CSharpParserState
{
    /// <summary>
    /// 0 is the global scope
    /// </summary>
    private int _indexKey = 0;
    
    private int _symbolId = 0;

    public CSharpParserState(
        CSharpBinder binder,
        TokenWalkerBuffer tokenWalkerBuffer,
        int absolutePathId,
        ref CSharpCompilationUnit compilationUnit)
    {
        Binder = binder;
        Compilation = ref compilationUnit;
        ScopeCurrentSubIndex = 0;
        CurrentNamespaceStatementValue = new NamespaceStatementValue(binder.TopLevelNamespaceStatementNode);
        AbsolutePathId = absolutePathId;

        TokenWalker = tokenWalkerBuffer;
        
        ForceParseExpressionInitialPrimaryExpression = EmptyExpressionNode.Empty;
        
        StatementBuilder = Binder.CSharpParserModel_StatementBuilder;
        
        ParseChildScopeStack = Binder.CSharpParserModel_ParseChildScopeStack;
        ParseChildScopeStack.Clear();
        
        ExpressionList = Binder.CSharpParserModel_ExpressionList;
        ExpressionList.Clear();
        ExpressionList.Add((SyntaxKind.EndOfFileToken, null));
        ExpressionList.Add((SyntaxKind.CloseBraceToken, null));
        ExpressionList.Add((SyntaxKind.StatementDelimiterToken, null));
        
        TryParseExpressionSyntaxKindList = Binder.CSharpParserModel_TryParseExpressionSyntaxKindList;
        TryParseExpressionSyntaxKindList.Clear();
        
        ClearedPartialDefinitionHashSet = Binder.CSharpParserModel_ClearedPartialDefinitionHashSet;
        ClearedPartialDefinitionHashSet.Clear();
        
        // Binder.MethodOverload_ResourceUri_WasCleared = false;
        
        Binder.CSharpParserModel_AddedNamespaceList.Clear();
        
        ExternalTypeDefinitionList = Binder.CSharpParserModel_ExternalTypeDefinitionList;
        // NOTE: change CSharpBinder's constructor if changing this (6 types that I always want in the list so skip 6 remove the rest).
        ExternalTypeDefinitionList.RemoveRange(6, ExternalTypeDefinitionList.Count - 6);
        
        Binder.AmbiguousParenthesizedNodeChildList.Clear();
        Binder.LambdaExpressionNodeChildList.Clear();
        
        if (Compilation.CompilationUnitKind == CompilationUnitKind.IndividualFile_AllData)
        {
            if (Binder.SymbolIdToExternalTextSpanMap.TryGetValue(AbsolutePathId, out var symbolIdToExternalTextSpanMap))
                symbolIdToExternalTextSpanMap.Clear();
            else 
                Binder.SymbolIdToExternalTextSpanMap.Add(AbsolutePathId, new());
        }
        else
        {
            Binder.SymbolIdToExternalTextSpanMap.Remove(AbsolutePathId);
        }
        
        Compilation.DiagnosticOffset = Binder.DiagnosticList.Count;
        
        Compilation.SymbolOffset = Binder.SymbolList.Count;
        
        Compilation.NodeOffset = Binder.NodeList.Count;
    }
    
    public TokenWalkerBuffer TokenWalker{ get; }
    public CSharpStatementBuilder StatementBuilder { get; set; }
    
    public int AbsolutePathId { get; }

    /// <summary>
    /// Prior to closing a statement-codeblock, you must check whether ParseChildScopeStack has a child that needs to be parsed.
    ///
    /// The ScopeSubIndex is that of the parent which contains the scope that was deferred.
    /// </summary>
    public Stack<(int ScopeSubIndex, CSharpDeferredChildScope DeferredChildScope)> ParseChildScopeStack { get; }
    
    /// <summary>
    /// The C# IParserModel implementation will only "short circuit" if the 'SyntaxKind DelimiterSyntaxKind'
    /// is registered as a delimiter.
    ///
    /// This is done in order to speed up the while loop, as the list of short circuits doesn't have to be
    /// iterated unless the current token is a possible delimiter.
    ///
    /// Clair.CompilerServices.CSharp.ParserCase.Internals.ParseOthers.SyntaxIsEndDelimiter(SyntaxKind syntaxKind) {...}
    /// </summary>
    public List<(SyntaxKind DelimiterSyntaxKind, IExpressionNode? ExpressionNode)> ExpressionList { get; set; }
    
    public List<SyntaxNodeValue> ExternalTypeDefinitionList { get; }
    
    public IExpressionNode? NoLongerRelevantExpressionNode { get; set; }
    public List<SyntaxKind> TryParseExpressionSyntaxKindList { get; }
    public IExpressionNode ForceParseExpressionInitialPrimaryExpression { get; set; }
    
    /// <summary>
    /// When parsing a value tuple, this needs to be remembered,
    /// then reset to the initial value foreach of the value tuple's members.
    ///
    /// 'CSharpParserContextKind.ForceStatementExpression' is related
    /// to disambiguating the less than operator '<' and
    /// generic arguments '<...>'.
    ///
    /// Any case where 'ParserContextKind' says that
    /// generic arguments '<...>' for variable declaration
    /// this needs to be available as information to each member.
    /// </summary>
    public CSharpParserContextKind ParserContextKind { get; set; }
    
    public CSharpBinder Binder { get; set; }
    
    public ref CSharpCompilationUnit Compilation;
    
    public int ScopeCurrentSubIndex { get; set; }
    
    public NamespaceStatementValue CurrentNamespaceStatementValue { get; set; }
    public TypeReferenceValue MostRecentLeftHandSideAssignmentExpressionTypeClauseNode { get; set; } = CSharpFacts.Types.VoidTypeReferenceValue;
    
    /// <summary>
    /// In order to have many partial definitions for the same type in the same file,
    /// you need to set the ScopeIndexKey to -1 for any entry in the
    /// 'CSharpBinder.PartialTypeDefinitionList' only once per parse.
    ///
    /// Thus, this will track whether a type had been handled already or not.
    /// </summary>
    public HashSet<string> ClearedPartialDefinitionHashSet { get; }
    
    public ParameterModifierKind ParameterModifierKind { get; set; } = ParameterModifierKind.None;
    
    public ArgumentModifierKind ArgumentModifierKind { get; set; } = ArgumentModifierKind.None;
    
    public IExpressionNode ExpressionPrimary { get; set; }

    public readonly Scope ScopeCurrent => Binder.ScopeList[Compilation.ScopeOffset + ScopeCurrentSubIndex];
    
    public readonly Scope GetParent(
        int parentScopeSubIndex,
        Clair.CompilerServices.CSharp.CompilerServiceCase.CSharpCompilationUnit cSharpCompilationUnit)
    {
        if (parentScopeSubIndex == -1)
            return default;
            
        return Binder.ScopeList[Compilation.ScopeOffset + parentScopeSubIndex];
    }
    
    public int GetNextIndexKey()
    {
        return ++_indexKey;
    }
    
    public int GetNextSymbolId()
    {
        return ++_symbolId;
    }
    
    public void BindDiscard(SyntaxToken identifierToken)
    {
        Binder.SymbolList.Insert(
            Compilation.SymbolOffset + Compilation.SymbolLength,
            new Symbol(
                SyntaxKind.DiscardSymbol,
                GetNextSymbolId(),
                identifierToken.TextSpan.StartInclusiveIndex,
                identifierToken.TextSpan.EndExclusiveIndex,
                identifierToken.TextSpan.ByteIndex));
        ++Compilation.SymbolLength;
    }
    
    public void BindFunctionDefinitionNode(FunctionDefinitionNode functionDefinitionNode)
    {
        Binder.SymbolList.Insert(
            Compilation.SymbolOffset + Compilation.SymbolLength,
            new Symbol(
            SyntaxKind.FunctionSymbol,
            GetNextSymbolId(),
            functionDefinitionNode.FunctionIdentifierToken.TextSpan.StartInclusiveIndex,
            functionDefinitionNode.FunctionIdentifierToken.TextSpan.EndExclusiveIndex,
            functionDefinitionNode.FunctionIdentifierToken.TextSpan.ByteIndex));
        ++Compilation.SymbolLength;

        TokenWalker.TextEditorModel?.ApplySyntaxHighlightingByTextSpan(functionDefinitionNode.FunctionIdentifierToken.TextSpan with
        {
            DecorationByte = (byte)GenericDecorationKind.Function
        });
    }
    
    public readonly void BindNamespaceStatementNode(NamespaceStatementNode namespaceStatementNode)
    {
        var namespaceContributionEntry = new NamespaceContribution(namespaceStatementNode.IdentifierToken.TextSpan);
        Binder.NamespaceContributionList.Add(namespaceContributionEntry);
        ++Compilation.NamespaceContributionLength;

        var tuple = Binder.FindNamespaceGroup_Reversed_WithMatchedIndex(
            AbsolutePathId,
            namespaceStatementNode.IdentifierToken.TextSpan);
            
        if (tuple.TargetGroup.ConstructorWasInvoked)
        {
            tuple.TargetGroup.NamespaceStatementValueList.Add(new NamespaceStatementValue(namespaceStatementNode));
        }
        else
        {
            Binder._namespaceGroupList.Add(new NamespaceGroup(
                namespaceStatementNode.IdentifierToken.TextSpan.CharIntSum,
                new List<NamespaceStatementValue> { new NamespaceStatementValue(namespaceStatementNode) }));
        }
    }
    
    public void BindVariableDeclarationNode(VariableDeclarationNode variableDeclarationNode, bool shouldCreateVariableSymbol = true)
    {
        if (shouldCreateVariableSymbol)
            CreateVariableSymbol(variableDeclarationNode.IdentifierToken, variableDeclarationNode.VariableKind);
        
        if (TryGetVariableDeclarationNodeByScope(
                AbsolutePathId,
                Compilation,
                ScopeCurrentSubIndex,
                AbsolutePathId,
                variableDeclarationNode.IdentifierToken.TextSpan,
                out var existingVariableDeclarationNode))
        {
            if (existingVariableDeclarationNode.IsDefault() || existingVariableDeclarationNode.IsFabricated)
            {
                // Overwrite the fabricated definition with a real one
                //
                // TODO: Track one or many declarations?...
                // (if there is an error where something is defined twice for example)
                SetVariableDeclarationNodeByScope(
                    variableDeclarationNode.IdentifierToken.TextSpan,
                    variableDeclarationNode);
            }
        }
        else
        {
            _ = TryAddVariableDeclarationNodeByScope(
                variableDeclarationNode.IdentifierToken.TextSpan,
                variableDeclarationNode);
        }
    }
    
    public void BindLabelDeclarationNode(LabelDeclarationNode labelDeclarationNode)
    {
        Binder.SymbolList.Insert(
            Compilation.SymbolOffset + Compilation.SymbolLength,
            new Symbol(
                SyntaxKind.LabelSymbol,
                GetNextSymbolId(),
                labelDeclarationNode.IdentifierToken.TextSpan.StartInclusiveIndex,
                labelDeclarationNode.IdentifierToken.TextSpan.EndExclusiveIndex,
                labelDeclarationNode.IdentifierToken.TextSpan.ByteIndex));
        ++Compilation.SymbolLength;
    
        if (TryGetLabelDeclarationNodeByScope(
                AbsolutePathId,
                Compilation,
                ScopeCurrentSubIndex,
                AbsolutePathId,
                labelDeclarationNode.IdentifierToken.TextSpan,
                out var existingLabelDeclarationNode))
        {
            if (existingLabelDeclarationNode.IsFabricated)
            {
                // Overwrite the fabricated definition with a real one
                //
                // TODO: Track one or many declarations?...
                // (if there is an error where something is defined twice for example)
                SetLabelDeclarationNodeByScope(
                    ScopeCurrentSubIndex,
                    labelDeclarationNode.IdentifierToken.TextSpan,
                    labelDeclarationNode);
            }
        }
        else
        {
            _ = TryAddLabelDeclarationNodeByScope(
                ScopeCurrentSubIndex,
                labelDeclarationNode.IdentifierToken.TextSpan,
                labelDeclarationNode);
        }
    }

    public VariableReferenceNode ConstructAndBindVariableReferenceNode(
        SyntaxToken variableIdentifierToken,
        bool shouldCreateSymbol = true)
    {
        VariableReferenceNode? variableReferenceNode;
        TypeReferenceValue typeReference;
        VariableKind variableKind;

        if (TryGetVariableDeclarationHierarchically(
                AbsolutePathId,
                Compilation,
                ScopeCurrentSubIndex,
                AbsolutePathId,
                variableIdentifierToken.TextSpan,
                out var variableDeclarationNode))
        {
            
            var variableReferenceTraits = Binder.VariableDeclarationTraitsList[variableDeclarationNode.TraitsIndex];
            typeReference = variableReferenceTraits.TypeReference;
            variableKind = variableReferenceTraits.VariableKind;
        }
        else
        {
            typeReference = CSharpFacts.Types.VarTypeReferenceValue;
            variableKind = VariableKind.Local;
        }
        
        variableReferenceNode = Rent_VariableReferenceNode();
        variableReferenceNode.VariableIdentifierToken = variableIdentifierToken;
        variableReferenceNode.ResultTypeReference = typeReference;

        if (shouldCreateSymbol)
            CreateVariableSymbol(variableReferenceNode.VariableIdentifierToken, variableKind);
            
        return variableReferenceNode;
    }
    
    public void BindLabelReferenceNode(LabelReferenceNode labelReferenceNode)
    {
        Binder.SymbolList.Insert(
            Compilation.SymbolOffset + Compilation.SymbolLength,
            new Symbol(
                SyntaxKind.LabelSymbol,
                GetNextSymbolId(),
                labelReferenceNode.IdentifierToken.TextSpan.StartInclusiveIndex,
                labelReferenceNode.IdentifierToken.TextSpan.EndExclusiveIndex,
                labelReferenceNode.IdentifierToken.TextSpan.ByteIndex));
        ++Compilation.SymbolLength;
    }

    public void BindConstructorDefinitionIdentifierToken(SyntaxToken identifierToken)
    {
        Binder.SymbolList.Insert(
            Compilation.SymbolOffset + Compilation.SymbolLength,
            new Symbol(
                SyntaxKind.ConstructorSymbol,
                GetNextSymbolId(),
                identifierToken.TextSpan.StartInclusiveIndex,
                identifierToken.TextSpan.EndExclusiveIndex,
                identifierToken.TextSpan.ByteIndex));
        ++Compilation.SymbolLength;

        TokenWalker.TextEditorModel?.ApplySyntaxHighlightingByTextSpan(identifierToken.TextSpan with
        {
            DecorationByte = (byte)GenericDecorationKind.Type
        });
    }

    public void BindFunctionInvocationNode(FunctionInvocationNode functionInvocationNode)
    {
        Binder.SymbolList.Insert(
            Compilation.SymbolOffset + Compilation.SymbolLength,
            new Symbol(
                SyntaxKind.FunctionSymbol,
                GetNextSymbolId(),
                functionInvocationNode.FunctionInvocationIdentifierToken.TextSpan.StartInclusiveIndex,
                functionInvocationNode.FunctionInvocationIdentifierToken.TextSpan.EndExclusiveIndex,
                functionInvocationNode.FunctionInvocationIdentifierToken.TextSpan.ByteIndex));
        ++Compilation.SymbolLength;

        TokenWalker.TextEditorModel?.ApplySyntaxHighlightingByTextSpan(functionInvocationNode.FunctionInvocationIdentifierToken.TextSpan with
        {
            DecorationByte = (byte)GenericDecorationKind.Function
        });

        if (TryGetFunctionHierarchically(
                AbsolutePathId,
                Compilation,
                ScopeCurrentSubIndex,
                AbsolutePathId,
                functionInvocationNode.FunctionInvocationIdentifierToken.TextSpan,
                out var functionDefinitionValue))
        {
            var functionDefinitionTraits = Binder.FunctionDefinitionTraitsList[functionDefinitionValue.TraitsIndex];
            functionInvocationNode.ResultTypeReference = functionDefinitionTraits.ReturnTypeReference;
        }
    }

    public void BindNamespaceReference(SyntaxToken namespaceIdentifierToken)
    {
        Binder.SymbolList.Insert(
            Compilation.SymbolOffset + Compilation.SymbolLength,
            new Symbol(
                SyntaxKind.NamespaceSymbol,
                GetNextSymbolId(),
                namespaceIdentifierToken.TextSpan.StartInclusiveIndex,
                namespaceIdentifierToken.TextSpan.EndExclusiveIndex,
                namespaceIdentifierToken.TextSpan.ByteIndex));
        ++Compilation.SymbolLength;
    }

    public void BindTypeClauseNode(TypeClauseNode typeClauseNode)
    {
        if (!typeClauseNode.IsKeywordType)
        {
            Binder.SymbolList.Insert(
                Compilation.SymbolOffset + Compilation.SymbolLength,
                new Symbol(
                    SyntaxKind.TypeSymbol,
                    GetNextSymbolId(),
                    typeClauseNode.TypeIdentifierToken.TextSpan.StartInclusiveIndex,
                    typeClauseNode.TypeIdentifierToken.TextSpan.EndExclusiveIndex,
                    typeClauseNode.TypeIdentifierToken.TextSpan.ByteIndex));
            ++Compilation.SymbolLength;

            TokenWalker.TextEditorModel?.ApplySyntaxHighlightingByTextSpan(typeClauseNode.TypeIdentifierToken.TextSpan with
            {
                DecorationByte = (byte)GenericDecorationKind.Type
            });
        }
    }
    
    public void BindTypeIdentifier(SyntaxToken identifierToken)
    {
        if (identifierToken.SyntaxKind == SyntaxKind.IdentifierToken)
        {
            Binder.SymbolList.Insert(
                Compilation.SymbolOffset + Compilation.SymbolLength,
                new Symbol(
                    SyntaxKind.TypeSymbol,
                    GetNextSymbolId(),
                    identifierToken.TextSpan.StartInclusiveIndex,
                    identifierToken.TextSpan.EndExclusiveIndex,
                    identifierToken.TextSpan.ByteIndex));
            ++Compilation.SymbolLength;

            TokenWalker.TextEditorModel?.ApplySyntaxHighlightingByTextSpan(identifierToken.TextSpan with
            {
                DecorationByte = (byte)GenericDecorationKind.Type
            });
        }
    }

    public readonly void BindUsingStatementTuple(SyntaxToken usingKeywordToken, SyntaxToken namespaceIdentifierToken)
    {
        AddNamespaceToCurrentScope(namespaceIdentifierToken.TextSpan);
    }
    
    public readonly void BindTypeDefinitionNode(TypeDefinitionNode typeDefinitionNode, bool shouldOverwrite = false)
    {
        /*var findTuple = Binder.TypeDefinition_FindRange(typeDefinitionNode.TypeIdentifierToken.TextSpan);
        bool shouldInsert = true;
        
        for (int i = findTuple.StartIndex; i < findTuple.EndIndex; i++)
        {
            var node = Binder.AllTypeDefinitionList[i];
            
            if (Binder.CSharpCompilerService.SafeCompareTextSpans(
                ResourceUri.Value,
                typeDefinitionNode.TypeIdentifierToken.TextSpan,
                node.ResourceUri.Value,
                node.TypeIdentifierToken.TextSpan))
            {
                shouldInsert = false;
                if (shouldOverwrite || node.IsFabricated)
                    Binder.AllTypeDefinitionList[i] = typeDefinitionNode;
                break;
            }
        }
        
        if (shouldInsert)
            Binder.AllTypeDefinitionList.Insert(findTuple.InsertionIndex, typeDefinitionNode);*/
    }

    public void RegisterScope(Scope scope, ICodeBlockOwner codeBlockOwner)
    {
        scope.ParentScopeSubIndex = ScopeCurrentSubIndex;
        scope.SelfScopeSubIndex = Compilation.ScopeLength;
        
        var parent = ScopeCurrent;
        var parentScopeDirection = parent.IsDefault()
            ? ScopeDirectionKind.Both
            : parent.ScopeDirectionKind;
        if (parentScopeDirection == ScopeDirectionKind.Both)
            scope.PermitCodeBlockParsing = false;
        
        scope.NodeSubIndex = -1;
        
        if (codeBlockOwner is not null)
        {
            codeBlockOwner.ParentScopeSubIndex = scope.ParentScopeSubIndex;
            codeBlockOwner.SelfScopeSubIndex = scope.SelfScopeSubIndex;
            
            SyntaxNodeValue nodeValue = default;
            
            switch (scope.OwnerSyntaxKind)
            {
                case SyntaxKind.TypeDefinitionNode:
                    var typeDefinitionNode = (TypeDefinitionNode)codeBlockOwner;
                    
                    nodeValue = new SyntaxNodeValue(
                        typeDefinitionNode,
                        Binder.TypeDefinitionTraitsList);
                    
                    break;
                case SyntaxKind.NamespaceStatementNode:
                    var namespaceStatementNode = (NamespaceStatementNode)codeBlockOwner;
                    nodeValue = new SyntaxNodeValue(namespaceStatementNode);
                    break;
                case SyntaxKind.ConstructorDefinitionNode:
                    var constructorDefinitionNode = (ConstructorDefinitionNode)codeBlockOwner;
                    nodeValue = new SyntaxNodeValue(
                        constructorDefinitionNode,
                        Binder.ConstructorDefinitionTraitsList);
                    break;
                case SyntaxKind.FunctionDefinitionNode:
                    var functionDefinitionNode = (FunctionDefinitionNode)codeBlockOwner;
                    nodeValue = new SyntaxNodeValue(
                        functionDefinitionNode,
                        Binder.FunctionDefinitionTraitsList);
                    break;
                default:
                    break;
            }
            switch (scope.OwnerSyntaxKind)
            {
                case SyntaxKind.TypeDefinitionNode:
                case SyntaxKind.NamespaceStatementNode:
                case SyntaxKind.ConstructorDefinitionNode:
                case SyntaxKind.FunctionDefinitionNode:
                    scope.NodeSubIndex = Compilation.NodeLength;
                    Binder.NodeList.Add(nodeValue);
                    ++Compilation.NodeLength;
                    break;
                default:
                    scope.NodeSubIndex = -1;
                    break;
            }
        }
        
        Binder.ScopeList.Add(scope);
        ++Compilation.ScopeLength;
    
        ScopeCurrentSubIndex = scope.SelfScopeSubIndex;
        
        if (codeBlockOwner is not null)
        {
            switch (codeBlockOwner.SyntaxKind)
            {
                case SyntaxKind.NamespaceStatementNode:
                    var namespaceStatementNode = (NamespaceStatementNode)codeBlockOwner;
                    AddNamespaceToCurrentScope(namespaceStatementNode.IdentifierToken.TextSpan);
                    BindNamespaceStatementNode((NamespaceStatementNode)codeBlockOwner);
                    break;
                case SyntaxKind.TypeDefinitionNode:
                    var typeDefinitionNode = (TypeDefinitionNode)codeBlockOwner;
                    BindTypeDefinitionNode(typeDefinitionNode, true);
                    if (typeDefinitionNode.OffsetGenericParameterEntryList != -1)
                    {
                        for (int i = typeDefinitionNode.OffsetGenericParameterEntryList;
                             i < typeDefinitionNode.OffsetGenericParameterEntryList + typeDefinitionNode.LengthGenericParameterEntryList;
                             i++)
                        {
                            var entry = Binder.GenericParameterList[i];

                            BindTypeDefinitionNode(
                                new TypeDefinitionNode(
                                    AccessModifierKind.Public,
                                    hasPartialModifier: false,
                                    StorageModifierKind.Class,
                                    entry.TypeReference.TypeIdentifierToken,
                                    entry.TypeReference.OpenAngleBracketToken,
                                    entry.TypeReference.OffsetGenericParameterEntryList,
                                    entry.TypeReference.LengthGenericParameterEntryList,
                                    entry.TypeReference.CloseAngleBracketToken,
                                    openParenthesisToken: default,
                                    offsetFunctionArgumentEntryList: -1,
                                    lengthFunctionArgumentEntryList: 0,
                                    closeParenthesisToken: default,
                                    inheritedTypeReference: TypeFacts.NotApplicable.ToTypeReference(),
                                    AbsolutePathId));
                        }
                    }

                    break;
            }
        }
    }

    public readonly void AddNamespaceToCurrentScope(TextEditorTextSpan textSpan)
    {
        var namespaceContribution = new NamespaceContribution(textSpan);

        if (Binder.CheckAlreadyAddedNamespace(
                AbsolutePathId,
                textSpan))
        {
            return;
        }
        
        Binder.CSharpParserModel_AddedNamespaceList.Add(textSpan);
        
        var tuple = Binder.FindNamespaceGroup_Reversed_WithMatchedIndex(
            AbsolutePathId,
            namespaceContribution.TextSpan);

        if (tuple.TargetGroup.ConstructorWasInvoked)
        {
            var typeDefinitionNodeList = Binder.Internal_GetTopLevelTypeDefinitionNodes_NamespaceGroup(tuple.TargetGroup);
            ExternalTypeDefinitionList.AddRange(typeDefinitionNodeList);
        }
    }

    public void CloseScope(TextEditorTextSpan textSpan, bool isStatementLoop = true)
    {
        // Check if it is the global scope, if so return early.
        if (ScopeCurrentSubIndex == 0)
            return;
            
        var originalScope = ScopeCurrent.OwnerSyntaxKind;
        
        SetCurrentScope_Scope_EndExclusiveIndex(textSpan.EndExclusiveIndex);
        ScopeCurrentSubIndex = ScopeCurrent.ParentScopeSubIndex;
        
        if (isStatementLoop &&
            originalScope == SyntaxKind.LambdaExpressionNode &&
            ScopeCurrent.OwnerSyntaxKind == SyntaxKind.LambdaExpressionNode)
        {
            var nonLambdaScopeParentSubIndex = ScopeCurrentSubIndex;
            while (Binder.ScopeList[Compilation.ScopeOffset + nonLambdaScopeParentSubIndex].OwnerSyntaxKind == SyntaxKind.LambdaExpressionNode)
            {
                nonLambdaScopeParentSubIndex = Binder.ScopeList[Compilation.ScopeOffset + nonLambdaScopeParentSubIndex].ParentScopeSubIndex;
            }
            ScopeCurrentSubIndex = nonLambdaScopeParentSubIndex;
        }
    }

    /// <summary>
    /// Returns the 'symbolId: Compilation.BinderSession.GetNextSymbolId();'
    /// that was used to construct the ITextEditorSymbol.
    /// </summary>
    public int CreateVariableSymbol(SyntaxToken identifierToken, VariableKind variableKind)
    {
        var symbolId = GetNextSymbolId();
        
        switch (variableKind)
        {
            case VariableKind.Field:
                Binder.SymbolList.Insert(
                    Compilation.SymbolOffset + Compilation.SymbolLength,
                    new Symbol(
                        SyntaxKind.FieldSymbol,
                        symbolId,
                        identifierToken.TextSpan.StartInclusiveIndex,
                        identifierToken.TextSpan.EndExclusiveIndex,
                        identifierToken.TextSpan.ByteIndex));
                ++Compilation.SymbolLength;

                TokenWalker.TextEditorModel?.ApplySyntaxHighlightingByTextSpan(identifierToken.TextSpan with
                {
                    DecorationByte = (byte)GenericDecorationKind.Field
                });
                break;
            case VariableKind.Property:
                Binder.SymbolList.Insert(
                    Compilation.SymbolOffset + Compilation.SymbolLength,
                    new Symbol(
                        SyntaxKind.PropertySymbol,
                        symbolId,
                        identifierToken.TextSpan.StartInclusiveIndex,
                        identifierToken.TextSpan.EndExclusiveIndex,
                        identifierToken.TextSpan.ByteIndex));
                ++Compilation.SymbolLength;


                TokenWalker.TextEditorModel?.ApplySyntaxHighlightingByTextSpan(identifierToken.TextSpan with
                {
                    DecorationByte = (byte)GenericDecorationKind.Property
                });
                break;
            case VariableKind.EnumMember:
                Binder.SymbolList.Insert(
                    Compilation.SymbolOffset + Compilation.SymbolLength,
                    new Symbol(
                        SyntaxKind.EnumMemberSymbol,
                        symbolId,
                        identifierToken.TextSpan.StartInclusiveIndex,
                        identifierToken.TextSpan.EndExclusiveIndex,
                        identifierToken.TextSpan.ByteIndex));
                ++Compilation.SymbolLength;

                TokenWalker.TextEditorModel?.ApplySyntaxHighlightingByTextSpan(identifierToken.TextSpan with
                {
                    DecorationByte = (byte)GenericDecorationKind.Property
                });
                break;
            case VariableKind.Local:
                goto default;
            case VariableKind.Closure:
                goto default;
            default:
                Binder.SymbolList.Insert(
                    Compilation.SymbolOffset + Compilation.SymbolLength,
                    new Symbol(
                        SyntaxKind.VariableSymbol,
                        symbolId,
                        identifierToken.TextSpan.StartInclusiveIndex,
                        identifierToken.TextSpan.EndExclusiveIndex,
                        identifierToken.TextSpan.ByteIndex));
                ++Compilation.SymbolLength;

                TokenWalker.TextEditorModel?.ApplySyntaxHighlightingByTextSpan(identifierToken.TextSpan with
                {
                    DecorationByte = (byte)GenericDecorationKind.Variable
                });
                break;
        }
        
        return symbolId;
    }
    
    public void SetCurrentNamespaceStatementValue(NamespaceStatementValue namespaceStatementValue)
    {
        CurrentNamespaceStatementValue = namespaceStatementValue;
    }
    
    /// <summary>
    /// 'definition...' argument name pattern:
    /// This implies the "presumed" value. The invoker must provide a
    /// place to search. This isn't the invoker saying they know where the definition is.
    /// Instead, the invoker is saying, I think the definition is somewhere
    /// in this 'definitionResourceUri', 'defintionCompilationUnit' 'definitionInitialScopeIndexKey'.
    /// 
    /// 'reference...' argument name pattern:
    /// This implies the resourceUri and textSpan combo that prompted you to try getting the respective
    /// TypeDefinitionNode.
    /// These arguments are used to compare character by character the definition's
    /// textSpan to see if they are equal.
    /// 
    /// Search hierarchically through all the scopes, starting at the <see cref="initialScope"/>.<br/><br/>
    /// If a match is found, then set the out parameter to it and return true.<br/><br/>
    /// If none of the searched scopes contained a match then set the out parameter to null and return false.
    /// </summary>
    public readonly bool TryGetTypeDefinitionHierarchically(
        int definitionAbsolutePathId,
        CSharpCompilationUnit definitionCompilationUnit,
        int definitionInitialScopeSubIndex,
        int referenceAbsolutePathId,
        TextEditorTextSpan referenceTextSpan,
        out SyntaxNodeValue typeDefinitionValue)
    {
        var localScope = Binder.GetScopeByOffset(definitionCompilationUnit, definitionInitialScopeSubIndex);

        while (!localScope.IsDefault())
        {
            if (TryGetTypeDefinitionNodeByScope(
                    definitionAbsolutePathId,
                    definitionCompilationUnit,
                    localScope.SelfScopeSubIndex,
                    referenceAbsolutePathId,
                    referenceTextSpan,
                    out typeDefinitionValue))
            {
                return true;
            }

            if (localScope.ParentScopeSubIndex == -1)
                localScope = default;
            else
                localScope = Binder.GetScopeByOffset(definitionCompilationUnit, localScope.ParentScopeSubIndex);
        }

        typeDefinitionValue = default;
        return false;
    }
    
    public readonly bool TryGetTypeDefinitionNodeByScope(
        int definitionAbsolutePathId,
        CSharpCompilationUnit definitionCompilationUnit,
        int definitionScopeSubIndex,
        int referenceAbsolutePathId,
        TextEditorTextSpan referenceTextSpan,
        out SyntaxNodeValue typeDefinitionValue)
    {
        typeDefinitionValue = default;
        
        for (int i = definitionCompilationUnit.NodeOffset; i < definitionCompilationUnit.NodeOffset + definitionCompilationUnit.NodeLength; i++)
        {
            var node = Binder.NodeList[i];
            if (node.ParentScopeSubIndex == definitionScopeSubIndex &&
                node.SyntaxKind == SyntaxKind.TypeDefinitionNode)
            {
                if (Binder.CSharpCompilerService.SafeCompareTextSpans(
                        referenceAbsolutePathId, referenceTextSpan, definitionAbsolutePathId, node.IdentifierToken.TextSpan))
                {
                    typeDefinitionValue = node;
                    break;
                }
            }
        }
        
        if (typeDefinitionValue.IsDefault())
        {
            if (definitionScopeSubIndex == 0)
            {
                foreach (var externalDefinitionNode in ExternalTypeDefinitionList)
                {
                    if (Binder.CSharpCompilerService.SafeCompareTextSpans(
                            referenceAbsolutePathId, referenceTextSpan, externalDefinitionNode.AbsolutePathId, externalDefinitionNode.IdentifierToken.TextSpan))
                    {
                        typeDefinitionValue = externalDefinitionNode;
                        break;
                    }
                }
                if (!typeDefinitionValue.IsDefault())
                {
                    return true;
                }
            }

            return false;
        }
        else
        {
            return true;
        }
    }
    
    /// <summary>
    /// 'definition...' argument name pattern:
    /// This implies the "presumed" value. The invoker must provide a
    /// place to search. This isn't the invoker saying they know where the definition is.
    /// Instead, the invoker is saying, I think the definition is somewhere
    /// in this 'definitionResourceUri', 'defintionCompilationUnit' 'definitionInitialScopeIndexKey'.
    /// 
    /// 'reference...' argument name pattern:
    /// This implies the resourceUri and textSpan combo that prompted you to try getting the respective
    /// TypeDefinitionNode.
    /// These arguments are used to compare character by character the definition's
    /// textSpan to see if they are equal.
    /// 
    /// Search hierarchically through all the scopes, starting at the <see cref="initialScope"/>.<br/><br/>
    /// If a match is found, then set the out parameter to it and return true.<br/><br/>
    /// If none of the searched scopes contained a match then set the out parameter to null and return false.
    /// </summary>
    public readonly bool TryGetFunctionHierarchically(
        int definitionAbsolutePathId,
        CSharpCompilationUnit definitionCompilationUnit,
        int definitionInitialScopeSubIndex,
        int referenceAbsolutePathId,
        TextEditorTextSpan referenceTextSpan,
        out SyntaxNodeValue functionDefinitionValue)
    {
        var localScope = Binder.GetScopeByOffset(definitionCompilationUnit, definitionInitialScopeSubIndex);

        while (!localScope.IsDefault())
        {
            if (TryGetFunctionDefinitionNodeByScope(
                    definitionAbsolutePathId,
                    definitionCompilationUnit,
                    localScope.SelfScopeSubIndex,
                    referenceAbsolutePathId,
                    referenceTextSpan,
                    out functionDefinitionValue))
            {
                return true;
            }

            if (localScope.ParentScopeSubIndex == -1)
                localScope = default;
            else
                localScope = Binder.GetScopeByOffset(definitionCompilationUnit, localScope.ParentScopeSubIndex);
        }

        functionDefinitionValue = default;
        return false;
    }
    
    public readonly bool TryGetFunctionDefinitionNodeByScope(
        int definitionAbsolutePathId,
        CSharpCompilationUnit definitionCompilationUnit,
        int definitionScopeSubIndex,
        int referenceAbsolutePathId,
        TextEditorTextSpan referenceTextSpan,
        out SyntaxNodeValue functionDefinitionValue)
    {
        functionDefinitionValue = default;
        
        for (int i = definitionCompilationUnit.NodeOffset; i < definitionCompilationUnit.NodeOffset + definitionCompilationUnit.NodeLength; i++)
        {
            var node = Binder.NodeList[i];
            
            if (node.ParentScopeSubIndex == definitionScopeSubIndex &&
                node.SyntaxKind == SyntaxKind.FunctionDefinitionNode)
            {
                if (Binder.CSharpCompilerService.SafeCompareTextSpans(referenceAbsolutePathId, referenceTextSpan, definitionAbsolutePathId, node.IdentifierToken.TextSpan))
                {
                    functionDefinitionValue = node;
                    break;
                }
            }
        }
        
        if (functionDefinitionValue.IsDefault())
            return false;
        else
            return true;
    }

    /// <summary>
    /// 'definition...' argument name pattern:
    /// This implies the "presumed" value. The invoker must provide a
    /// place to search. This isn't the invoker saying they know where the definition is.
    /// Instead, the invoker is saying, I think the definition is somewhere
    /// in this 'definitionResourceUri', 'defintionCompilationUnit' 'definitionInitialScopeIndexKey'.
    /// 
    /// 'reference...' argument name pattern:
    /// This implies the resourceUri and textSpan combo that prompted you to try getting the respective
    /// TypeDefinitionNode.
    /// These arguments are used to compare character by character the definition's
    /// textSpan to see if they are equal.
    /// 
    /// Search hierarchically through all the scopes, starting at the <see cref="_currentScope"/>.<br/><br/>
    /// If a match is found, then set the out parameter to it and return true.<br/><br/>
    /// If none of the searched scopes contained a match then set the out parameter to null and return false.
    /// </summary>
    public bool TryGetVariableDeclarationHierarchically(
        int declarationAbsolutePathId,
        CSharpCompilationUnit declarationCompilationUnit,
        int declarationInitialScopeSubIndex,
        int referenceAbsolutePathId,
        TextEditorTextSpan referenceTextSpan,
        out SyntaxNodeValue variableDeclarationStatementValue)
    {
        var localScope = Binder.GetScopeByOffset(declarationCompilationUnit, declarationInitialScopeSubIndex);

        while (!localScope.IsDefault())
        {
            if (TryGetVariableDeclarationNodeByScope(
                    declarationAbsolutePathId,
                    declarationCompilationUnit,
                    localScope.SelfScopeSubIndex,
                    referenceAbsolutePathId,
                    referenceTextSpan,
                    out variableDeclarationStatementValue))
            {
                return true;
            }

            if (localScope.ParentScopeSubIndex == -1)
                localScope = default;
            else
                localScope = Binder.GetScopeByOffset(declarationCompilationUnit, localScope.ParentScopeSubIndex);
        }
        
        variableDeclarationStatementValue = default;
        return false;
    }
    
    public bool TryGetVariableDeclarationByPartialType(
        int declarationAbsolutePathId,
        CSharpCompilationUnit declarationCompilationUnit,
        int declarationScopeSubIndex,
        int referenceAbsolutePathId,
        TextEditorTextSpan referenceTextSpan,
        SyntaxNodeValue typeDefinitionNode,
        out SyntaxNodeValue variableDeclarationValue)
    {
        var typeDefinitionMetadata = Binder.TypeDefinitionTraitsList[typeDefinitionNode.TraitsIndex];
        int positionExclusive = typeDefinitionMetadata.IndexPartialTypeDefinition;
        while (positionExclusive < Binder.PartialTypeDefinitionList.Count)
        {
            if (Binder.PartialTypeDefinitionList[positionExclusive].IndexStartGroup == typeDefinitionMetadata.IndexPartialTypeDefinition)
            {
                CSharpCompilationUnit innerCompilationUnit;
                int innerAbsolutePathId;
                
                if (Binder.PartialTypeDefinitionList[positionExclusive].ScopeSubIndex != -1)
                {
                    if (Binder.PartialTypeDefinitionList[positionExclusive].AbsolutePathId != declarationAbsolutePathId)
                    {
                        if (Binder.__CompilationUnitMap.TryGetValue(Binder.PartialTypeDefinitionList[positionExclusive].AbsolutePathId, out var temporaryCompilationUnit))
                        {
                            innerCompilationUnit = temporaryCompilationUnit;
                            innerAbsolutePathId = Binder.PartialTypeDefinitionList[positionExclusive].AbsolutePathId;
                        }
                        else
                        {
                            innerCompilationUnit = default;
                            innerAbsolutePathId = default;
                        }
                    }
                    else
                    {
                        innerCompilationUnit = declarationCompilationUnit;
                        innerAbsolutePathId = declarationAbsolutePathId;
                    }
                    
                    if (!innerCompilationUnit.IsDefault())
                    {
                        var innerScopeIndexKey = Binder.PartialTypeDefinitionList[positionExclusive].ScopeSubIndex;
                    
                        if (TryGetVariableDeclarationNodeByScope(
                                innerAbsolutePathId,
                                innerCompilationUnit,
                                innerScopeIndexKey,
                                referenceAbsolutePathId,
                                referenceTextSpan,
                                out variableDeclarationValue,
                                isRecursive: true))
                        {
                            return true;
                        }
                    }
                }
                
                positionExclusive++;
            }
            else
            {
                break;
            }
        }
        
        variableDeclarationValue = default;
        return false;
    }
    
    public bool TryGetVariableDeclarationNodeByScope(
        int declarationAbsolutePathId,
        CSharpCompilationUnit declarationCompilationUnit,
        int declarationScopeSubIndex,
        int referenceAbsolutePathId,
        TextEditorTextSpan referenceTextSpan,
        out SyntaxNodeValue variableDeclarationValue,
        bool isRecursive = false)
    {
        variableDeclarationValue = default;
        for (int i = declarationCompilationUnit.NodeOffset; i < declarationCompilationUnit.NodeOffset + declarationCompilationUnit.NodeLength; i++)
        {
            var node = Binder.NodeList[i];
            
            if (node.ParentScopeSubIndex == declarationScopeSubIndex &&
                node.SyntaxKind == SyntaxKind.VariableDeclarationNode)
            {
                if (Binder.CSharpCompilerService.SafeCompareTextSpans(
                        referenceAbsolutePathId, referenceTextSpan, declarationAbsolutePathId, node.IdentifierToken.TextSpan))
                {
                    variableDeclarationValue = node;
                    break;
                }
            }
        }
        
        if (variableDeclarationValue.IsDefault())
        {
            var scope = Binder.ScopeList[declarationCompilationUnit.ScopeOffset + declarationScopeSubIndex];
            
            if (!isRecursive && scope.OwnerSyntaxKind == SyntaxKind.TypeDefinitionNode)
            {
                var typeDefinitionNode = Binder.NodeList[declarationCompilationUnit.NodeOffset + scope.NodeSubIndex];
                var typeDefinitionMetadata = Binder.TypeDefinitionTraitsList[typeDefinitionNode.TraitsIndex];
                if (typeDefinitionMetadata.IndexPartialTypeDefinition != -1)
                {
                    if (TryGetVariableDeclarationByPartialType(
                            declarationAbsolutePathId,
                            declarationCompilationUnit,
                            declarationScopeSubIndex,
                            referenceAbsolutePathId,
                            referenceTextSpan,
                            typeDefinitionNode,
                            out variableDeclarationValue))
                    {
                        return true;
                    }
                }
                
                if (!typeDefinitionMetadata.InheritedTypeReference.IsDefault())
                {
                    TextEditorTextSpan typeDefinitionTextSpan;
                    CSharpCompilationUnit typeDefinitionCompilationUnit;
                    int typeDefinitionAbsolutePathId;
                    
                    if (typeDefinitionMetadata.InheritedTypeReference.ExplicitDefinitionAbsolutePathId == referenceAbsolutePathId)
                    {
                        typeDefinitionCompilationUnit = Compilation;
                        typeDefinitionAbsolutePathId = referenceAbsolutePathId;
                        typeDefinitionTextSpan = typeDefinitionMetadata.InheritedTypeReference.TypeIdentifierToken.TextSpan;
                    }
                    else
                    {
                        if (Binder.__CompilationUnitMap.TryGetValue(typeDefinitionMetadata.InheritedTypeReference.ExplicitDefinitionAbsolutePathId, out typeDefinitionCompilationUnit))
                        {
                            typeDefinitionTextSpan = typeDefinitionMetadata.InheritedTypeReference.TypeIdentifierToken.TextSpan;
                            typeDefinitionAbsolutePathId = typeDefinitionMetadata.InheritedTypeReference.ExplicitDefinitionAbsolutePathId;
                        }
                        else
                        {
                            typeDefinitionTextSpan = default;
                            typeDefinitionAbsolutePathId = default;
                        }
                    }

                    var typeDefinitionScopeIndexKey = Binder.GetScopeByPositionIndex(typeDefinitionCompilationUnit, typeDefinitionTextSpan.StartInclusiveIndex);

                    if (!typeDefinitionScopeIndexKey.IsDefault())
                    {
                        if (TryGetTypeDefinitionHierarchically(
                                typeDefinitionAbsolutePathId,
                                typeDefinitionCompilationUnit,
                                typeDefinitionScopeIndexKey.SelfScopeSubIndex,
                                typeDefinitionAbsolutePathId,
                                typeDefinitionTextSpan,
                                out var inheritedTypeDefinitionValue))
                        {
                            if (!inheritedTypeDefinitionValue.IsDefault())
                            {
                                var innerScopeIndexKey = inheritedTypeDefinitionValue.SelfScopeSubIndex;

                                if (TryGetVariableDeclarationNodeByScope(
                                        typeDefinitionAbsolutePathId,
                                        typeDefinitionCompilationUnit,
                                        innerScopeIndexKey,
                                        referenceAbsolutePathId,
                                        referenceTextSpan,
                                        out variableDeclarationValue,
                                        isRecursive: true))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
        
            return false;
        }
        else
        {
            return true;
        }
    }
    
    public bool TryAddVariableDeclarationNodeByScope(
        TextEditorTextSpan variableIdentifierTextSpan,
        VariableDeclarationNode variableDeclarationNode)
    {
        var scopeSubIndex = ScopeCurrentSubIndex;

        SyntaxNodeValue matchNode = default;
        for (int i = Compilation.NodeOffset; i < Compilation.NodeOffset + Compilation.NodeLength; i++)
        {
            var node = Binder.NodeList[i];
            if (node.ParentScopeSubIndex == scopeSubIndex &&
                node.SyntaxKind == SyntaxKind.VariableDeclarationNode)
            {
                if (Binder.CSharpCompilerService.SafeCompareTextSpans(
                        AbsolutePathId, variableIdentifierTextSpan, AbsolutePathId, node.IdentifierToken.TextSpan))
                {
                    matchNode = node;
                    break;
                }
            }
        }
        
        if (matchNode.IsDefault())
        {
            variableDeclarationNode.ParentScopeSubIndex = scopeSubIndex;
            
            var variableDeclarationValue = new SyntaxNodeValue(
                variableDeclarationNode,
                Binder.VariableDeclarationTraitsList);
            
            Binder.NodeList.Insert(
                Compilation.NodeOffset + Compilation.NodeLength,
                variableDeclarationValue);
            ++Compilation.NodeLength;
            
            return true;
        }
        else
        {
            return false;
        }
    }
    
    public readonly void SetVariableDeclarationNodeByScope(
        TextEditorTextSpan variableIdentifierTextSpan,
        VariableDeclarationNode variableDeclarationNode)
    {
        int scopeSubIndex = ScopeCurrentSubIndex;
        
        SyntaxNodeValue matchNode = default;
        int index = Compilation.NodeOffset;
        for (; index < Compilation.NodeOffset + Compilation.NodeLength; index++)
        {
            var node = Binder.NodeList[index];
            
            if (node.ParentScopeSubIndex == scopeSubIndex &&
                node.SyntaxKind == SyntaxKind.VariableDeclarationNode)
            {
                if (Binder.CSharpCompilerService.SafeCompareTextSpans(
                        AbsolutePathId, variableIdentifierTextSpan, AbsolutePathId, node.IdentifierToken.TextSpan))
                {
                    matchNode = node;
                    break;
                }
            }
        }
        
        if (index != -1)
        {
            variableDeclarationNode.ParentScopeSubIndex = scopeSubIndex;
            
            var variableDeclarationValue = new SyntaxNodeValue(
                variableDeclarationNode.IdentifierToken,
                variableDeclarationNode.AbsolutePathId,
                variableDeclarationNode.IsFabricated,
                variableDeclarationNode.SyntaxKind,
                variableDeclarationNode.ParentScopeSubIndex,
                -1,
                matchNode.TraitsIndex);
            variableDeclarationNode.TraitsIndex = matchNode.TraitsIndex;

            Binder.VariableDeclarationTraitsList[matchNode.TraitsIndex] = new VariableDeclarationTraits(variableDeclarationNode);
            
            Binder.NodeList[index] = variableDeclarationValue;
        }
    }
    
    public readonly bool TryGetLabelDeclarationHierarchically(
        int declarationAbsolutePathId,
        CSharpCompilationUnit declarationCompilationUnit,
        int declarationScopeSubIndex,
        int referenceAbsolutePathId,
        TextEditorTextSpan referenceTextSpan,
        out SyntaxNodeValue labelDeclarationValue)
    {
        int initialScopeOffset = ScopeCurrentSubIndex;

        var localScope = Binder.GetScopeByOffset(Compilation, initialScopeOffset);

        while (!localScope.IsDefault())
        {
            if (TryGetLabelDeclarationNodeByScope(
                    declarationAbsolutePathId,
                    declarationCompilationUnit,
                    localScope.SelfScopeSubIndex,
                    referenceAbsolutePathId,
                    referenceTextSpan,
                    out labelDeclarationValue))
            {
                return true;
            }

            if (localScope.ParentScopeSubIndex == -1)
                localScope = default;
            else
                localScope = Binder.GetScopeByOffset(Compilation, localScope.ParentScopeSubIndex);
        }
        
        labelDeclarationValue = default;
        return false;
    }
    
    public readonly bool TryGetLabelDeclarationNodeByScope(
        int declarationAbsolutePathId,
        CSharpCompilationUnit declarationCompilationUnit,
        int declarationScopeSubIndex,
        int referenceAbsolutePathId,
        TextEditorTextSpan referenceTextSpan,
        out SyntaxNodeValue labelDeclarationValue)
    {
        labelDeclarationValue = default;
        for (int i = Compilation.NodeOffset; i < Compilation.NodeOffset + Compilation.NodeLength; i++)
        {
            var node = Binder.NodeList[i];
            
            if (node.ParentScopeSubIndex == declarationScopeSubIndex &&
                node.SyntaxKind == SyntaxKind.LabelDeclarationNode)
            {
                if (Binder.CSharpCompilerService.SafeCompareTextSpans(referenceAbsolutePathId, referenceTextSpan, declarationAbsolutePathId, node.IdentifierToken.TextSpan))
                {
                    labelDeclarationValue = node;
                    break;
                }
            }
        }
        
        if (labelDeclarationValue.IsDefault())
            return false;
        else
            return true;
    }
    
    public bool TryAddLabelDeclarationNodeByScope(
        int scopeSubIndex,
        TextEditorTextSpan labelIdentifierTextSpan,
        LabelDeclarationNode labelDeclarationNode)
    {
        SyntaxNodeValue matchNode = default;
        for (var i = Compilation.NodeOffset; i < Compilation.NodeOffset + Compilation.NodeLength; i++)
        {
            var node = Binder.NodeList[i];
            
            if (node.ParentScopeSubIndex == scopeSubIndex &&
                node.SyntaxKind == SyntaxKind.LabelDeclarationNode)
            {
                if (Binder.CSharpCompilerService.SafeCompareTextSpans(AbsolutePathId, labelIdentifierTextSpan, AbsolutePathId, node.IdentifierToken.TextSpan))
                {
                    matchNode = node;
                    break;
                }
            }
        }
        
        if (matchNode.IsDefault())
        {
            labelDeclarationNode.ParentScopeSubIndex = scopeSubIndex;
            
            Binder.NodeList.Insert(
                Compilation.NodeOffset + Compilation.NodeLength,
                new SyntaxNodeValue(
                    labelDeclarationNode,
                    AbsolutePathId));
            ++Compilation.NodeLength;
            
            return true;
        }
        else
        {
            return false;
        }
    }
    
    public readonly void SetLabelDeclarationNodeByScope(
        int scopeSubIndex,
        TextEditorTextSpan referenceTextSpan,
        LabelDeclarationNode labelDeclarationNode)
    {
        SyntaxNodeValue matchNode = default;
        int index = Compilation.NodeOffset;
        for (; index < Compilation.NodeOffset + Compilation.NodeLength; index++)
        {
            var node = Binder.NodeList[index];
            
            if (node.ParentScopeSubIndex == scopeSubIndex &&
                node.SyntaxKind == SyntaxKind.LabelDeclarationNode)
            {
                if (Binder.CSharpCompilerService.SafeCompareTextSpans(AbsolutePathId, referenceTextSpan, AbsolutePathId, node.IdentifierToken.TextSpan))
                {
                    matchNode = node;
                    break;
                }
            }
        }

        if (index != -1)
        {
            labelDeclarationNode.ParentScopeSubIndex = scopeSubIndex;
            Binder.NodeList[index] = new SyntaxNodeValue(
                labelDeclarationNode,
                AbsolutePathId);
        }
    }
    
    public readonly string GetTextSpanText(TextEditorTextSpan textSpan)
    {
        return Binder.CSharpCompilerService.SafeGetText(AbsolutePathId, textSpan) ?? string.Empty;
    }

    public readonly TypeClauseNode ToTypeClause(TypeDefinitionNode typeDefinitionNode)
    {
        var typeClauseNode = Rent_TypeClauseNode();
        typeClauseNode.TypeIdentifierToken = typeDefinitionNode.TypeIdentifierToken;
        typeClauseNode.IsKeywordType = typeDefinitionNode.IsKeywordType;
            
        typeClauseNode.ExplicitDefinitionTextSpan = typeDefinitionNode.TypeIdentifierToken.TextSpan;
        typeClauseNode.ExplicitDefinitionAbsolutePathId = typeDefinitionNode.AbsolutePathId;
        
        return typeClauseNode;
    }
    
    public readonly void SetCurrentScope_IsImplicitOpenCodeBlockTextSpan(bool value)
    {
        var scope = Binder.ScopeList[Compilation.ScopeOffset + ScopeCurrentSubIndex];
        scope.IsImplicitOpenCodeBlockTextSpan = value;
        Binder.ScopeList[Compilation.ScopeOffset + ScopeCurrentSubIndex] = scope;
    }
    
    public readonly void SetCurrentScope_PermitCodeBlockParsing(bool value)
    {
        var scope = Binder.ScopeList[Compilation.ScopeOffset + ScopeCurrentSubIndex];
        scope.PermitCodeBlockParsing = value;
        Binder.ScopeList[Compilation.ScopeOffset + ScopeCurrentSubIndex] = scope;
    }
    
    public readonly void SetCurrentScope_Scope_EndExclusiveIndex(int endExclusiveIndex)
    {
        var scope = Binder.ScopeList[Compilation.ScopeOffset + ScopeCurrentSubIndex];
        scope.Scope_EndExclusiveIndex = endExclusiveIndex;
        Binder.ScopeList[Compilation.ScopeOffset + ScopeCurrentSubIndex] = scope;
    }
    
    public readonly void SetCurrentScope_CodeBlock_EndExclusiveIndex(int endExclusiveIndex)
    {
        var scope = Binder.ScopeList[Compilation.ScopeOffset + ScopeCurrentSubIndex];
        scope.CodeBlock_EndExclusiveIndex = endExclusiveIndex;
        Binder.ScopeList[Compilation.ScopeOffset + ScopeCurrentSubIndex] = scope;
    }
    
    public readonly void SetCurrentScope_CodeBlock_StartInclusiveIndex(int startInclusiveIndex)
    {
        var scope = Binder.ScopeList[Compilation.ScopeOffset + ScopeCurrentSubIndex];
        scope.CodeBlock_StartInclusiveIndex = startInclusiveIndex;
        Binder.ScopeList[Compilation.ScopeOffset + ScopeCurrentSubIndex] = scope;
    }
}
