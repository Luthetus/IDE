using System.Text;
using Clair.Common.RazorLib.FileSystems.Models;
using Clair.Common.RazorLib.Menus.Models;
using Clair.TextEditor.RazorLib;
using Clair.TextEditor.RazorLib.CompilerServices;
using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.TextEditor.RazorLib.Autocompletes.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models.Internals;
using Clair.TextEditor.RazorLib.TextEditors.Displays.Internals;
using Clair.Extensions.CompilerServices.Syntax;
using Clair.CompilerServices.CSharp.CompilerServiceCase;
using Clair.Extensions.CompilerServices.Syntax.Interfaces;
using Clair.Extensions.CompilerServices.Syntax.NodeReferences;

namespace Clair.CompilerServices.Razor;

public sealed class RazorCompilerService : ICompilerService
{
    private readonly TextEditorService _textEditorService;
    private readonly CSharpCompilerService _cSharpCompilerService;
    
    /// <summary>
    /// Cannot use shared for both the razor and the C#.
    /// </summary>
    private readonly StringWalker _htmlStringWalker = new();

    public RazorCompilerService(
        TextEditorService textEditorService,
        CSharpCompilerService cSharpCompilerService)
    {
        _textEditorService = textEditorService;
        _cSharpCompilerService = cSharpCompilerService;
    }

    public void RegisterResource(ResourceUri resourceUri, bool shouldTriggerResourceWasModified)
    {
        _cSharpCompilerService.RegisterResource(resourceUri, shouldTriggerResourceWasModified: false);
    }
    
    public void DisposeResource(ResourceUri resourceUri)
    {
        _cSharpCompilerService.DisposeResource(resourceUri);
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
        return _cSharpCompilerService.GetResourceByResourceUri(resourceUri);
    }
    
    public MenuContainer GetContextMenu(TextEditorVirtualizationResult virtualizationResult, ContextMenu contextMenu)
    {
        return contextMenu.GetDefaultMenuRecord();
    }

    public AutocompleteContainer GetAutocompleteMenu(TextEditorVirtualizationResult virtualizationResult, AutocompleteMenu autocompleteMenu)
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
        var resourceUri = modelModifier.PersistentState.ResourceUri;
        int absolutePathId = _cSharpCompilerService.TryGetFileAbsolutePathToInt(resourceUri.Value);
        if (absolutePathId == 0)
            absolutePathId = _cSharpCompilerService.TryAddFileAbsolutePath(resourceUri.Value);
    
        if (!_cSharpCompilerService.__CSharpBinder.__CompilationUnitMap.ContainsKey(absolutePathId))
            return ValueTask.CompletedTask;
        
        var cSharpCompilationUnit = new CSharpCompilationUnit(CompilationUnitKind.IndividualFile_AllData);

        var contentAtRequest = modelModifier.xGetAllText();

        _cSharpCompilerService._currentFileBeingParsedTuple = (absolutePathId, contentAtRequest);

        MemoryStream memoryStream;
        StreamReaderPooledBuffer? lexer_reader = null;
        
        try
        {
            // Convert the string to a byte array using a specific encoding
            memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(contentAtRequest));
            lexer_reader = _cSharpCompilerService.bbb_Rent_StreamReaderPooledBuffer(memoryStream);
            
            if (shouldApplySyntaxHighlighting)
            {
                editContext.TextEditorService.Model_BeginStreamSyntaxHighlighting(
                    editContext,
                    modelModifier);
            }

            _cSharpCompilerService._streamReaderWrap.ReInitialize(lexer_reader);
            
            _cSharpCompilerService._tokenWalkerBuffer.ReInitialize(
                _cSharpCompilerService.__CSharpBinder,
                resourceUri,
                modelModifier,
                _cSharpCompilerService._tokenWalkerBuffer,
                _cSharpCompilerService._streamReaderWrap,
                shouldUseSharedStringWalker: true);
            
            _cSharpCompilerService.__CSharpBinder.StartCompilationUnit(absolutePathId);
            RazorParser.Parse(absolutePathId, _cSharpCompilerService._tokenWalkerBuffer, ref cSharpCompilationUnit, _cSharpCompilerService.__CSharpBinder);
        }
        finally
        {
            //var diagnosticTextSpans = cSharpCompilationUnit.DiagnosticList
            //    .Select(x => x.TextSpan)
            //    .ToList();
            
            if (shouldApplySyntaxHighlighting)
            {
                editContext.TextEditorService.Model_FinalizeStreamSyntaxHighlighting(
                    editContext,
                    modelModifier);
            }

            _cSharpCompilerService._currentFileBeingParsedTuple = (absolutePathId, null);

            _cSharpCompilerService.FULL_Clear_MAIN_StreamReaderTupleCache();
            _cSharpCompilerService.FULL_Clear_BACKUP_StreamReaderTupleCache();
            _cSharpCompilerService._safe_streamReaderPooledBuffer_Pool.Clear();
            
            if (lexer_reader is not null)
                _cSharpCompilerService.Return_StreamReaderPooledBuffer(lexer_reader);
        
            _textEditorService.EditContext_GetText_Clear();
        }
        
        return ValueTask.CompletedTask;
    }
    
    public ValueTask FastParseAsync(TextEditorEditContext editContext, ResourceUri resourceUri, IFileSystemProvider fileSystemProvider, CompilationUnitKind compilationUnitKind)
    {
        throw new NotImplementedException();
    }
    
    public void FastParse(TextEditorEditContext editContext, ResourceUri resourceUri, IFileSystemProvider fileSystemProvider, CompilationUnitKind compilationUnitKind)
    {
        int absolutePathId = _cSharpCompilerService.TryGetFileAbsolutePathToInt(resourceUri.Value);
        if (absolutePathId == 0)
            absolutePathId = _cSharpCompilerService.TryAddFileAbsolutePath(resourceUri.Value);

        if (!_cSharpCompilerService.__CSharpBinder.__CompilationUnitMap.ContainsKey(absolutePathId))
            return;
    
        var cSharpCompilationUnit = new CSharpCompilationUnit(compilationUnitKind);

        StreamReaderPooledBuffer? lexer_sr = null;
        StreamReaderPooledBuffer? parser_sr = null;

        try
        {
            lexer_sr = _cSharpCompilerService.aaa_Rent_StreamReaderPooledBuffer(resourceUri.Value);
            parser_sr = _cSharpCompilerService.aaa_Rent_StreamReaderPooledBuffer(resourceUri.Value);

            _cSharpCompilerService._streamReaderWrap.ReInitialize(lexer_sr);

            _cSharpCompilerService._tokenWalkerBuffer.ReInitialize(
                _cSharpCompilerService.__CSharpBinder,
                resourceUri,
                textEditorModel: null,
                _cSharpCompilerService._tokenWalkerBuffer,
                _cSharpCompilerService._streamReaderWrap,
                shouldUseSharedStringWalker: true);

            _cSharpCompilerService.FastParseTuple = (absolutePathId, parser_sr);
            _cSharpCompilerService.__CSharpBinder.StartCompilationUnit(absolutePathId);
            RazorParser.Parse(absolutePathId, _cSharpCompilerService._tokenWalkerBuffer, ref cSharpCompilationUnit, _cSharpCompilerService.__CSharpBinder);
        }
        finally
        {
            if (lexer_sr is not null)
                _cSharpCompilerService.Return_StreamReaderPooledBuffer(lexer_sr);
            
            if (parser_sr is not null)
                _cSharpCompilerService.Return_StreamReaderPooledBuffer(parser_sr);
        }

        _cSharpCompilerService.FastParseTuple = (0, null);
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

    public ICodeBlockOwner? GetScopeByPositionIndex(ResourceUri resourceUri, int positionIndex)
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
