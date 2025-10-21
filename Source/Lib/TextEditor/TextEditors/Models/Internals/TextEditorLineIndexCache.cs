namespace Clair.TextEditor.RazorLib.TextEditors.Models.Internals;

public sealed class TextEditorLineIndexCache
{
    /// <summary>TODO: Don't do this.</summary>
    public static List<TextEditorVirtualizationSpan> VirtualizationSpanList_Empty { get; } = new();

    /// <summary>
    /// Every virtualized line has its "spans" stored in this flat list.
    ///
    /// Then, 'virtualizationSpan_StartInclusiveIndex' and 'virtualizationSpan_EndExclusiveIndex'
    /// indicate the section of the flat list that relates to each individual line.
    ///
    /// This points to a TextEditorViewModel('s) VirtualizationGrid('s) list directly.
    /// If you clear it that'll cause a UI race condition exception.
    /// </summary>
    public List<TextEditorVirtualizationSpan> VirtualizationSpanList { get; set; } = new();
    
    public bool IsInvalid { get; set; }
    public HashSet<int> UsedKeyHashSet { get; set; } = new();
    public List<int> ExistsKeyList { get; set; } = new();
    public List<int> ModifiedLineIndexList { get; set; } = new();
    /// <summary>If the scroll left changes you have to discard the virtualized line cache.</summary>
    public int ScrollLeftMarker { get; set; } = -1;
    public int ViewModelKeyMarker { get; set; } = 0;
    public Dictionary<int, TextEditorLineIndexCacheEntry> Map { get; set; } = new();
    
    public void Clear()
    {
        ScrollLeftMarker = -1;
        
        Map.Clear();
        
        // This points to a TextEditorViewModel('s) VirtualizationGrid('s) list directly.
        // If you clear it that'll cause a UI race condition exception.
        VirtualizationSpanList = VirtualizationSpanList_Empty;
        
        UsedKeyHashSet.Clear();
        
        ExistsKeyList.Clear();
        
        ViewModelKeyMarker = 0;
        
        IsInvalid = false;
        ModifiedLineIndexList.Clear();
    }
}
