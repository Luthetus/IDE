using Microsoft.AspNetCore.Components;
using Clair.TextEditor.RazorLib;
using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models;
using Clair.Extensions.CompilerServices.Syntax;
using Clair.Extensions.CompilerServices.Syntax.NodeValues;

namespace Clair.Extensions.CompilerServices.Displays;

public partial class SymbolDisplay : ComponentBase
{
    [Inject]
    private TextEditorService TextEditorService { get; set; } = null!;

    [Parameter, EditorRequired]
    public Symbol Symbol { get; set; }
    [Parameter, EditorRequired]
    public int AbsolutePathId { get; set; }
    
    private int _shouldRenderHashCode = 0;
    
    protected override bool ShouldRender()
    {
        // When reading about 'HashCode.Combine'
        // it could not be determined whether it could throw an exception
        // (specifically thinking of integer overflow).
        //
        // The UI under no circumstance should cause a fatal exception,
        // especially a tooltip.
        //
        // Not much time was spent looking into this because far higher priority
        // work needs to be done.
        //
        // Therefore a try catch is being put around this to be safe.
    
        try
        {
            var outShouldRenderHashCode = HashCode.Combine(
                Symbol.StartInclusiveIndex,
                Symbol.EndExclusiveIndex,
                Symbol.ByteIndex,
                AbsolutePathId);
                
            if (outShouldRenderHashCode != _shouldRenderHashCode)
            {
                _shouldRenderHashCode = outShouldRenderHashCode;
                return true;
            }
            
            return false;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }
    
    private Task OpenInEditorOnClick(string filePath)
    {
        TextEditorService.WorkerArbitrary.PostUnique(async editContext =>
        {
            await TextEditorService.OpenInEditorAsync(
                editContext,
                filePath,
                true,
                null,
                new Category("main"),
                editContext.TextEditorService.NewViewModelKey());
        });
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// A TypeSymbol is used for both a TypeClauseNode and a TypeDefinitionNode.
    ///
    /// Therefore, if one hovers a TypeSymbol that maps to a TypeClauseNode.
    /// An additional step is needed to get the actual TypeDefinitionNode that the TypeClauseNode is referencing.
    ///
    /// The 'targetNode' is whichever node the ISymbol directly mapped to.
    /// </summary>
    public static SyntaxNodeValue GetTargetNodeValue(TextEditorService textEditorService, Symbol symbolLocal, ResourceUri resourceUri)
    {
        try
        {
            var textEditorModel = textEditorService.Model_GetOrDefault(resourceUri);
            if (textEditorModel is null)
                return default;
            
            var extendedCompilerService = (IExtendedCompilerService)textEditorModel.PersistentState.CompilerService;
            
            var compilerServiceResource = extendedCompilerService.GetResourceByResourceUri(textEditorModel.PersistentState.ResourceUri);
            if (compilerServiceResource is null)
                return default;
    
            return extendedCompilerService.GetSyntaxNode(
                symbolLocal.StartInclusiveIndex,
                textEditorModel.PersistentState.ResourceUri,
                compilerServiceResource);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return default;
        }
    }
    
    /// <summary>
    /// If the 'targetNode' itself is a definition, then return the 'targetNode'.
    ///
    /// Otherwise, ask the IBinder for the definition node:
    /// </summary>
    public static SyntaxNodeValue GetDefinitionNodeValue(TextEditorService textEditorService, Symbol symbolLocal, SyntaxNodeValue targetNode, ResourceUri resourceUri)
    {
        try
        {
            if (!targetNode.IsDefault())
            {
                switch (targetNode.SyntaxKind)
                {
                    case SyntaxKind.ConstructorDefinitionNode:
                    case SyntaxKind.FunctionDefinitionNode:
                    case SyntaxKind.NamespaceStatementNode:
                    case SyntaxKind.TypeDefinitionNode:
                    case SyntaxKind.VariableDeclarationNode:
                        return targetNode;
                }
            }
        
            var textEditorModel = textEditorService.Model_GetOrDefault(resourceUri);
            var extendedCompilerService = (IExtendedCompilerService)textEditorModel.PersistentState.CompilerService;
            var compilerServiceResource = extendedCompilerService.GetResourceByResourceUri(textEditorModel.PersistentState.ResourceUri);

            var absolutePathId = extendedCompilerService.TryGetFileAbsolutePathToInt(textEditorModel.PersistentState.ResourceUri.Value);
            if (absolutePathId == 0)
                return default;

            return extendedCompilerService.GetDefinitionNodeValue(
                symbolLocal.ToTextSpan(),
                absolutePathId,
                compilerServiceResource,
                symbolLocal);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return default;
        }
    }
}
