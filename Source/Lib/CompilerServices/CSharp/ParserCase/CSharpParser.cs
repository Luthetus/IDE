using Clair.TextEditor.RazorLib.Exceptions;
using Clair.Extensions.CompilerServices.Syntax;
using Clair.CompilerServices.CSharp.ParserCase.Internals;
using Clair.CompilerServices.CSharp.BinderCase;
using Clair.CompilerServices.CSharp.CompilerServiceCase;
using Clair.Extensions.CompilerServices.Syntax.Enums;

namespace Clair.CompilerServices.CSharp.ParserCase;

public static class CSharpParser
{
    public static void Parse(int absolutePathId, TokenWalkerBuffer tokenWalkerBuffer, ref CSharpCompilationUnit compilationUnit, CSharpBinder binder)
    {
        /*
        Any state that is "pooled" and cleared at the start of every Parse(...) invocation
        should be changed.
        
        Clear them inside CSharpBinder.FinalizeCompilationUnit(...) so that they aren't "dangling"
        in between Parse(...) invocations.
        */
        
        compilationUnit.ScopeOffset = binder.ScopeList.Count;
        compilationUnit.NamespaceContributionOffset = binder.NamespaceContributionList.Count;
        
        // Console.WriteLine($"nco:{compilationUnit.NamespaceContributionOffset}");

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
    }
}
