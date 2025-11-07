namespace Clair.TextEditor.RazorLib.TextEditors.Models;

public sealed class TextEditorViewModelLiason
{
    private readonly TextEditorService _textEditorService;
    
    public TextEditorViewModelLiason(TextEditorService textEditorService)
    {
        _textEditorService = textEditorService;
    }

    public PartitionWalker PartitionWalker => _textEditorService.__PartitionWalker;

    /// <summary>
    /// 'TextEditorEditContext' is more-so just a way to indicate thread safety
    /// for a given method.
    ///
    /// It doesn't actually store anything, all the state is still on the ITextEditorService.
    ///
    /// This method 'InsertRepositionInlineUiList(...)' is quite deep inside a chain of calls,
    /// and this method is meant for internal use.
    ///
    /// Therefore, I'm going to construct the 'TextEditorEditContext' out of thin air.
    /// But, everything will still work because 'TextEditorEditContext' never actually stored anything.
    ///
    /// I need the 'TextEditorEditContext' because if they have a pending edit on the viewmodel
    /// that I'm about to reposition the InlineUiList for, then everything will get borked.
    ///
    /// This will get me their pending edit if it exists, otherwise it will start a pending edit.
    /// </summary>
    public void InsertRepositionInlineUiList(
        int initialCursorPositionIndex,
        int insertionLength,
        List<int> viewModelKeyList,
        int initialCursorLineIndex,
        int lineEndPositionAddedCount)
    {
        var editContext = new TextEditorEditContext(_textEditorService);
        
        foreach (var viewModelKey in viewModelKeyList)
        {
            var viewModel = editContext.GetViewModelModifier(viewModelKey);
            
            var componentData = viewModel.PersistentState.ComponentData;
            if (componentData is not null)
            {
                if (lineEndPositionAddedCount > 0)
                {
                    for (int i = componentData.LineIndexCache.ExistsKeyList.Count - 1; i >= 0; i--)
                    {
                        var lineIndex = componentData.LineIndexCache.ExistsKeyList[i];
                        
                        if (lineIndex >= initialCursorLineIndex)
                            componentData.LineIndexCache.ModifiedLineIndexList.Add(lineIndex);
                    }
                
                    /*
                    // TODO: You cannot do this code. The UI might re-render while you're modifying the cache.
                    // ...you need to capture in a separate list the "shifted indices"
                    // and from a "thread safe" location modify the cache then.
                    // Perhaps a "redirection" logic could be useful instead, so that you don't have to move the cache entries around.
                    
                    for (int i = componentData.LineIndexCache.ExistsKeyList.Count - 1; i >= 0; i--)
                    {
                        var lineIndex = componentData.LineIndexCache.ExistsKeyList[i];
                        var newEntry = componentData.LineIndexCache.Map[lineIndex] with { LineIndex = lineIndex + lineEndPositionAddedCount };
                        
                        if (lineIndex >= initialCursorLineIndex &&
                            lineIndex <= initialCursorLineIndex + lineEndPositionAddedCount)
                        {
                            componentData.LineIndexCache.ModifiedLineIndexList.Add(lineIndex);
                            
                            if (componentData.LineIndexCache.Map.ContainsKey(lineIndex + lineEndPositionAddedCount))
                            {
                                componentData.LineIndexCache.Map[lineIndex + lineEndPositionAddedCount] = newEntry;
                            }
                            else
                            {
                                componentData.LineIndexCache.Map.Add(
                                    lineIndex + lineEndPositionAddedCount,
                                    newEntry);
                            }
                        }
                        else if (lineIndex > initialCursorLineIndex + lineEndPositionAddedCount)
                        {
                            if (componentData.LineIndexCache.Map.ContainsKey(lineIndex + lineEndPositionAddedCount))
                            {
                                componentData.LineIndexCache.Map[lineIndex + lineEndPositionAddedCount] = newEntry;
                            }
                            else
                            {
                                componentData.LineIndexCache.Map.Add(
                                    lineIndex + lineEndPositionAddedCount,
                                    newEntry);
                            }
                        }
                    }
                    */
                }
                else
                {
                    componentData.LineIndexCache.ModifiedLineIndexList.Add(initialCursorLineIndex);
                }
            }
        }
    }
    
    /// <summary>
    /// See: 'InsertRepositionInlineUiList(...)' summary
    ///      for 'TextEditorEditContext' explanation.
    /// </summary>
    public void DeleteRepositionInlineUiList(
        int startInclusiveIndex,
        int endExclusiveIndex,
        List<int> viewModelKeyList,
        int initialCursorLineIndex,
        bool lineEndPositionWasAdded)
    {
        var editContext = new TextEditorEditContext(_textEditorService);
        
        foreach (var viewModelKey in viewModelKeyList)
        {
            var viewModel = editContext.GetViewModelModifier(viewModelKey);
            
            var componentData = viewModel.PersistentState.ComponentData;
            if (componentData is not null)
            {
                if (lineEndPositionWasAdded)
                    componentData.LineIndexCache.IsInvalid = true;
                else
                    componentData.LineIndexCache.ModifiedLineIndexList.Add(initialCursorLineIndex);
            }
        }
    }
    
    public void SetContent(List<int> viewModelKeyList)
    {
        var editContext = new TextEditorEditContext(_textEditorService);
        
        foreach (var viewModelKey in viewModelKeyList)
        {
            var viewModel = editContext.GetViewModelModifier(viewModelKey);
            
            var componentData = viewModel.PersistentState.ComponentData;
            if (componentData is not null)
                componentData.LineIndexCache.IsInvalid = true;
        }
    }
    
    public TextEditorPartition Exchange_Partition(TextEditorPartition original)
    {
        var partitionExchange = _textEditorService._partition_Exchange;
    
        partitionExchange.RichCharacterList.Clear();
        partitionExchange.RichCharacterList.AddRange(original.RichCharacterList);
        
        _textEditorService._partition_Exchange = original;
        return partitionExchange;
    }
}
