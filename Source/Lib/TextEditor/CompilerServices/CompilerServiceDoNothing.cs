using Clair.Common.RazorLib.Menus.Models;
using Clair.Common.RazorLib.FileSystems.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models.Internals;
using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.TextEditor.RazorLib.TextEditors.Displays.Internals;

namespace Clair.TextEditor.RazorLib.CompilerServices;

public class CompilerServiceDoNothing : ICompilerService
{
    public IReadOnlyList<ICompilerServiceResource> CompilerServiceResources { get; }

    public void RegisterResource(ResourceUri resourceUri, bool shouldTriggerResourceWasModified)
    {
    }

    public void DisposeResource(ResourceUri resourceUri)
    {
    }

    public void ResourceWasModified(ResourceUri resourceUri, IReadOnlyList<TextEditorTextSpan> editTextSpansList)
    {
    }

    public ICompilerServiceResource? GetResourceByResourceUri(ResourceUri resourceUri)
    {
        return null;
    }

    public MenuContainer GetContextMenu(TextEditorVirtualizationResult virtualizationResult, ContextMenu contextMenu)
    {
        return contextMenu.GetDefaultMenuRecord();
    }

    public MenuContainer GetAutocompleteMenu(TextEditorVirtualizationResult virtualizationResult, AutocompleteMenu autocompleteMenu)
    {
        return autocompleteMenu.GetDefaultMenuRecord();
    }

    public ValueTask<MenuContainer> GetQuickActionsSlashRefactorMenu(
        TextEditorEditContext editContext,
        TextEditorModel modelModifier,
        TextEditorViewModel viewModel)
    {
        return ValueTask.FromResult(new MenuContainer());
    }
    
    public ValueTask OnInspect(
        TextEditorEditContext editContext,
        TextEditorModel modelModifier,
        TextEditorViewModel viewModel,
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
}
