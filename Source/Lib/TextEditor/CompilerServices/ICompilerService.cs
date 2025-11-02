using Clair.Common.RazorLib.Menus.Models;
using Clair.Common.RazorLib.FileSystems.Models;
using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models.Internals;
using Clair.TextEditor.RazorLib.TextEditors.Displays.Internals;
using Clair.TextEditor.RazorLib.Autocompletes.Models;

namespace Clair.TextEditor.RazorLib.CompilerServices;

/// <summary>
/// See 'Clair.CompilerServices.CSharp.CompilerServiceCase.CSharpCompilerService'
/// for an example of an "implementation".
///
/// A lot of interfaces that relate to this one is not required to be implemented,
/// they instead act only as a guide on 'default' decisions if one desires it.
///
/// In order to avoid implementing the related interfaces you need to
/// implement ICompilerService.ParseAsync youself.
///
/// If you inherit 'CompilerService',
/// then you need to override 'CompilerService.ParseAsync'.
///
/// It is at this point that you now have full control
/// over how parsing is done.
/// </summary>
public interface ICompilerService
{
    public void RegisterResource(ResourceUri resourceUri, bool shouldTriggerResourceWasModified);
    public void DisposeResource(ResourceUri resourceUri);

    public void ResourceWasModified(ResourceUri resourceUri, IReadOnlyList<TextEditorTextSpan> editTextSpansList);

    public ICompilerServiceResource? GetResourceByResourceUri(ResourceUri resourceUri);

    public MenuContainer GetContextMenu(TextEditorVirtualizationResult virtualizationResult, ContextMenu contextMenu);

    public AutocompleteContainer? GetAutocompleteMenu(TextEditorVirtualizationResult virtualizationResult, AutocompleteMenu autocompleteMenu);

    public ValueTask<MenuContainer> GetQuickActionsSlashRefactorMenu(
        TextEditorEditContext editContext,
        TextEditorModel modelModifier,
        TextEditorViewModel viewModelModifier);
        
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
        ResourceUri resourceUri);
    
    public ValueTask ShowCallingSignature(
        TextEditorEditContext editContext,
        TextEditorModel modelModifier,
        TextEditorViewModel viewModelModifier,
        int positionIndex,
        TextEditorComponentData componentData,
        ResourceUri resourceUri);
        
    public ValueTask GoToDefinition(
        TextEditorEditContext editContext,
        TextEditorModel modelModifier,
        TextEditorViewModel viewModelModifier,
        Category category,
        int positionIndex);

    public ValueTask ParseAsync(TextEditorEditContext editContext, TextEditorModel modelModifier, bool shouldApplySyntaxHighlighting);
    
    public ValueTask FastParseAsync(
        TextEditorEditContext editContext,
        ResourceUri resourceUri,
        IFileSystemProvider fileSystemProvider,
        CompilationUnitKind compilationUnitKind);

    public void FastParse(
        TextEditorEditContext editContext,
        ResourceUri resourceUri,
        IFileSystemProvider fileSystemProvider,
        CompilationUnitKind compilationUnitKind);
}
