using Clair.Common.RazorLib;
using Clair.Common.RazorLib.Menus.Models;
using Clair.Common.RazorLib.FileSystems.Models;
using Clair.TextEditor.RazorLib;
using Clair.TextEditor.RazorLib.CompilerServices;
using Clair.TextEditor.RazorLib.Autocompletes.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models.Internals;
using Clair.TextEditor.RazorLib.TextEditors.Displays.Internals;
using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.Extensions.CompilerServices;
using Clair.Extensions.CompilerServices.Syntax;
using Clair.CompilerServices.Xml;
using Clair.Extensions.CompilerServices.Syntax.Interfaces;
using Clair.Extensions.CompilerServices.Syntax.NodeReferences;

namespace Clair.CompilerServices.DotNetSolution;

public sealed class DotNetSolutionCompilerService : ICompilerService
{
    private readonly TextEditorService _textEditorService;
    
    private readonly Dictionary<ResourceUri, DotNetSolutionResource> _resourceMap = new();
    private readonly object _resourceMapLock = new();

    public DotNetSolutionCompilerService(TextEditorService textEditorService)
    {
        _textEditorService = textEditorService;
    }

    public IReadOnlyList<ICompilerServiceResource> CompilerServiceResources { get; }
    
    public IReadOnlyDictionary<string, TypeDefinitionNode> AllTypeDefinitions { get; }

    public void RegisterResource(ResourceUri resourceUri, bool shouldTriggerResourceWasModified)
    {
        lock (_resourceMapLock)
        {
            if (_resourceMap.ContainsKey(resourceUri))
                return;

            _resourceMap.Add(resourceUri, new DotNetSolutionResource(resourceUri, this));
        }

        if (shouldTriggerResourceWasModified)
            ResourceWasModified(resourceUri, Array.Empty<TextEditorTextSpan>());
    }
    
    public void DisposeResource(ResourceUri resourceUri)
    {
        lock (_resourceMapLock)
        {
            _resourceMap.Remove(resourceUri);
        }
    }

    public void ResourceWasModified(ResourceUri resourceUri, IReadOnlyList<TextEditorTextSpan> editTextSpansList)
    {
        _textEditorService.WorkerArbitrary.PostUnique(editContext =>
        {
            var modelModifier = editContext.GetModelModifier(resourceUri);

            if (modelModifier is null)
                return ValueTask.CompletedTask;

            return ParseAsync(editContext, modelModifier, shouldApplySyntaxHighlighting: true);
        });
    }

    public ICompilerServiceResource? GetResourceByResourceUri(ResourceUri resourceUri)
    {
        var model = _textEditorService.Model_GetOrDefault(resourceUri);

        if (model is null)
            return null;

        lock (_resourceMapLock)
        {
            if (!_resourceMap.ContainsKey(resourceUri))
                return null;

            return _resourceMap[resourceUri];
        }
    }
    
    public MenuContainer GetContextMenu(TextEditorVirtualizationResult virtualizationResult, ContextMenu contextMenu)
    {
        return contextMenu.GetDefaultMenuRecord();
    }

    public AutocompleteContainer? GetAutocompleteMenu(TextEditorVirtualizationResult virtualizationResult, AutocompleteMenu autocompleteMenu)
    {
        return null;
    }
    
    public ValueTask<MenuContainer> GetQuickActionsSlashRefactorMenu(
        TextEditorEditContext editContext,
        TextEditorModel modelModifier,
        TextEditorViewModel viewModelModifier)
    {
        return ValueTask.FromResult(new MenuContainer());
    }
    
    public ValueTask OnInspect(
        TextEditorEditContext editContext,
        TextEditorModel modelModifier,
        TextEditorViewModel viewModelModifier,
        double clientX,
        double clientY,
        bool shiftKey,
        bool ctrlKey,
        bool altKey,
        TextEditorComponentData componentData,
        ResourceUri resourceUri)
    {
        return ValueTask.CompletedTask;
    }
    
    public ValueTask ShowCallingSignature(
        TextEditorEditContext editContext,
        TextEditorModel modelModifier,
        TextEditorViewModel viewModelModifier,
        int positionIndex,
        TextEditorComponentData componentData,
        ResourceUri resourceUri)
    {
        return ValueTask.CompletedTask;
    }
    
    public ValueTask GoToDefinition(
        TextEditorEditContext editContext,
        TextEditorModel modelModifier,
        TextEditorViewModel viewModelModifier,
        Category category,
        int positionIndex)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask ParseAsync(TextEditorEditContext editContext, TextEditorModel modelModifier, bool shouldApplySyntaxHighlighting)
    {
        List<SyntaxToken> syntaxTokenList;
    
        if (modelModifier.PersistentState.ResourceUri.Value.EndsWith(CommonFacts.DOT_NET_SOLUTION_X))
        {
            using StreamReader sr = new StreamReader(modelModifier.PersistentState.ResourceUri.Value);

            editContext.TextEditorService.Model_BeginStreamSyntaxHighlighting(
                editContext,
                modelModifier);

            var lexerOutput = XmlLexer.Lex(new StreamReaderWrap(sr), modelModifier, textSpanList: null);
            syntaxTokenList = lexerOutput.TextSpanList.Select(x => new SyntaxToken(SyntaxKind.NotApplicable, x)).ToList();

            editContext.TextEditorService.Model_FinalizeStreamSyntaxHighlighting(
                editContext,
                modelModifier);
        }
        else
        {
            using StreamReader sr = new StreamReader(modelModifier.PersistentState.ResourceUri.Value);

            editContext.TextEditorService.Model_BeginStreamSyntaxHighlighting(
                editContext,
                modelModifier);

            var lexerOutput = DotNetSolutionLexer.Lex(new StreamReaderWrap(sr));
            syntaxTokenList = lexerOutput.TextSpanList.Select(x => new SyntaxToken(SyntaxKind.NotApplicable, x)).ToList();

            editContext.TextEditorService.Model_FinalizeStreamSyntaxHighlighting(
                editContext,
                modelModifier);
        }
        
        lock (_resourceMapLock)
        {
            if (_resourceMap.ContainsKey(modelModifier.PersistentState.ResourceUri))
            {
                var resource = (CompilerServiceResource)_resourceMap[modelModifier.PersistentState.ResourceUri];
                
                resource.CompilationUnit = new ExtendedCompilationUnit
                {
                    TokenList = syntaxTokenList
                };
            }
        }
        
        editContext.TextEditorService.Model_ApplySyntaxHighlighting(
            editContext,
            modelModifier,
            syntaxTokenList.Select(x => x.TextSpan));
        
        return ValueTask.CompletedTask;
    }
    
    public ValueTask FastParseAsync(TextEditorEditContext editContext, ResourceUri resourceUri, IFileSystemProvider fileSystemProvider, CompilationUnitKind compilationUnitKind)
    {
        return ValueTask.CompletedTask;
    }
    
    public void FastParse(TextEditorEditContext editContext, ResourceUri resourceUri, IFileSystemProvider fileSystemProvider, CompilationUnitKind compilationUnitKind)
    {
        return;
    }
    
    /// <summary>
    /// Looks up the <see cref="IScope"/> that encompasses the provided positionIndex.
    ///
    /// Then, checks the <see cref="IScope"/>.<see cref="IScope.CodeBlockOwner"/>'s children
    /// to determine which node exists at the positionIndex.
    ///
    /// If the <see cref="IScope"/> cannot be found, then as a fallback the provided compilationUnit's
    /// <see cref="CompilationUnit.RootCodeBlockNode"/> will be treated
    /// the same as if it were the <see cref="IScope"/>.<see cref="IScope.CodeBlockOwner"/>.
    ///
    /// If the provided compilerServiceResource?.CompilationUnit is null, then the fallback step will not occur.
    /// The fallback step is expected to occur due to the global scope being implemented with a null
    /// <see cref="IScope"/>.<see cref="IScope.CodeBlockOwner"/> at the time of this comment.
    /// </summary>
    public ISyntaxNode? GetSyntaxNode(int positionIndex, ResourceUri resourceUri, ICompilerServiceResource? compilerServiceResource)
    {
        return null;
    }

    public ICodeBlockOwner GetScopeByPositionIndex(ResourceUri resourceUri, int positionIndex)
    {
        return default;
    }
    
    /// <summary>
    /// Returns the <see cref="ISyntaxNode"/> that represents the definition in the <see cref="CompilationUnit"/>.
    ///
    /// The option argument 'symbol' can be provided if available. It might provide additional information to the method's implementation
    /// that is necessary to find certain nodes (ones that are in a separate file are most common to need a symbol to find).
    /// </summary>
    public ISyntaxNode? GetDefinitionNode(TextEditorTextSpan textSpan, ICompilerServiceResource compilerServiceResource, Symbol? symbol = null)
    {
        return null;
    }
}
