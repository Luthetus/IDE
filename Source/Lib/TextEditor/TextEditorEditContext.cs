using Clair.TextEditor.RazorLib.TextEditors.Models;
using Clair.TextEditor.RazorLib.Lexers.Models;

namespace Clair.TextEditor.RazorLib;

public struct TextEditorEditContext
{
    public TextEditorEditContext(TextEditorService textEditorService)
    {
        TextEditorService = textEditorService;
    }

    public TextEditorService TextEditorService { get; }

    /// <summary>
    /// 'isReadOnly == true' will not allocate a new TextEditorModel as well,
    /// nothing will be added to the '__ModelList'.
    /// </summary>
    public readonly TextEditorModel? GetModelModifier(
        ResourceUri modelResourceUri,
        bool isReadOnly = false)
    {
        if (modelResourceUri == ResourceUri.Empty)
            return null;
            
        TextEditorModel? modelModifier = null;
            
        for (int i = 0; i < TextEditorService.__ModelList.Count; i++)
        {
            if (TextEditorService.__ModelList[i].PersistentState.ResourceUri == modelResourceUri)
                modelModifier = TextEditorService.__ModelList[i];
        }
        
        if (modelModifier is null)
        {
            _ = TextEditorService.TextEditorState._modelMap.TryGetValue(
                modelResourceUri,
                out var model);
            
            if (isReadOnly || model is null)
                return model;
            
            modelModifier = new(model);
            TextEditorService.__ModelList.Add(modelModifier);
        }

        return modelModifier;
    }

    public readonly TextEditorViewModel? GetViewModelModifier(
        int viewModelKey,
        bool isReadOnly = false)
    {
        if (viewModelKey == 0)
            return null;
            
        TextEditorViewModel? viewModelModifier = null;
            
        for (int i = 0; i < TextEditorService.__ViewModelList.Count; i++)
        {
            if (TextEditorService.__ViewModelList[i].PersistentState.ViewModelKey == viewModelKey)
                viewModelModifier = TextEditorService.__ViewModelList[i];
        }
        
        if (viewModelModifier is null)
        {
            _ = TextEditorService.TextEditorState._viewModelMap.TryGetValue(
                viewModelKey,
                out var viewModel);
            
            if (isReadOnly || viewModel is null)
                return viewModel;
            
            viewModelModifier = TextEditorService.Exchange_ViewModel(viewModel);
            TextEditorService.__ViewModelList.Add(viewModelModifier);
        }

        return viewModelModifier;
    }
}
