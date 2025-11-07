using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models;

namespace Clair.TextEditor.RazorLib;

public partial class TextEditorService
{
    #region CREATE_METHODS
    public void Model_RegisterCustom(TextEditorEditContext editContext, TextEditorModel model)
    {
        RegisterModel(editContext, model);
    }

    public void Model_RegisterTemplated(
        TextEditorEditContext editContext,
        string extensionNoPeriod,
        ResourceUri resourceUri,
        DateTime resourceLastWriteTime,
        string initialContent,
        string? overrideDisplayTextForFileExtension = null)
    {
        var model = new TextEditorModel(
            resourceUri,
            resourceLastWriteTime,
            overrideDisplayTextForFileExtension ?? extensionNoPeriod,
            initialContent,
            GetDecorationMapper(extensionNoPeriod),
            GetCompilerService(extensionNoPeriod),
            this);

        RegisterModel(editContext, model);
    }
    #endregion

    #region READ_METHODS
    [Obsolete("TextEditorModel.PersistentState.ViewModelKeyList")]
    public List<TextEditorViewModel> Model_GetViewModelsOrEmpty(ResourceUri resourceUri)
    {
        return TextEditorState.ModelGetViewModelsOrEmpty(resourceUri);
    }

    public string? xModel_GetAllText(ResourceUri resourceUri)
    {
        return Model_GetOrDefault(resourceUri)?.xGetAllText();
    }

    public TextEditorModel? Model_GetOrDefault(ResourceUri resourceUri)
    {
        return TextEditorState.ModelGetOrDefault(resourceUri);
    }
    #endregion

    #region UPDATE_METHODS
    public void Model_Reload(
        TextEditorEditContext editContext,
        TextEditorModel modelModifier,
        string content,
        DateTime resourceLastWriteTime)
    {
        modelModifier.SetContent(content);
        modelModifier.SetResourceData(modelModifier.PersistentState.ResourceUri, resourceLastWriteTime);
    }

    public void Model_ApplyDecorationRange(
        TextEditorEditContext editContext,
        TextEditorModel modelModifier,
        IEnumerable<TextEditorTextSpan> textSpans)
    {
        /*
        // 2025-11-04 partition changes
        var localRichCharacterList = modelModifier.RichCharacterList;

        var positionsPainted = new HashSet<int>();

        foreach (var textEditorTextSpan in textSpans)
        {
            for (var i = textEditorTextSpan.StartInclusiveIndex; i < textEditorTextSpan.EndExclusiveIndex; i++)
            {
                if (i < 0 || i >= localRichCharacterList.Length)
                    continue;

                modelModifier.__SetDecorationByte(i, textEditorTextSpan.DecorationByte);
                positionsPainted.Add(i);
            }
        }

        for (var i = 0; i < localRichCharacterList.Length; i++)
        {
            if (!positionsPainted.Contains(i))
            {
                // DecorationByte of 0 is to be 'None'
                modelModifier.__SetDecorationByte(i, 0);
            }
        }

        modelModifier.__SetPartitionListChanged(true);
        modelModifier.ShouldCalculateVirtualizationResult = true;
        */
    }

    public void Model_ApplySyntaxHighlighting(
        TextEditorEditContext editContext,
        TextEditorModel modelModifier,
        IEnumerable<TextEditorTextSpan> textSpanList)
    {
        foreach (var viewModelKey in modelModifier.PersistentState.ViewModelKeyList)
        {
            var viewModel = editContext.GetViewModelModifier(viewModelKey);

            var componentData = viewModel.PersistentState.ComponentData;
            if (componentData is not null)
                componentData.LineIndexCache.IsInvalid = true;
        }

        Model_ApplyDecorationRange(
            editContext,
            modelModifier,
            textSpanList);

        // TODO: Why does painting reload virtualization result???
        modelModifier.ShouldCalculateVirtualizationResult = true;
    }

    public void Model_BeginStreamSyntaxHighlighting(
        TextEditorEditContext editContext,
        TextEditorModel modelModifier)
    {
        modelModifier.__ZeroOutDecorationBytes();
    }
    
    public void Model_FinalizeStreamSyntaxHighlighting(
        TextEditorEditContext editContext,
        TextEditorModel modelModifier)
    {
        foreach (var viewModelKey in modelModifier.PersistentState.ViewModelKeyList)
        {
            var viewModel = editContext.GetViewModelModifier(viewModelKey);

            var componentData = viewModel.PersistentState.ComponentData;
            if (componentData is not null)
                componentData.LineIndexCache.IsInvalid = true;
        }

        // TODO: Why does painting reload virtualization result???
        modelModifier.__SetPartitionListChanged(true);
        modelModifier.ShouldCalculateVirtualizationResult = true;
    }
    #endregion

    #region DELETE_METHODS
    public void Model_Dispose(TextEditorEditContext editContext, ResourceUri resourceUri)
    {
        DisposeModel(editContext, resourceUri);
    }
    #endregion
}
