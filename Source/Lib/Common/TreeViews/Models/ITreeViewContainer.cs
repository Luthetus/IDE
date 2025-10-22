using Clair.Common.RazorLib.Keys.Models;

namespace Clair.Common.RazorLib.TreeViews.Models;

/// <summary>
/// This interface should always be directly tied to UI of a TreeView actively being rendered.
/// To maintain TreeView state beyond the lifecycle of the UI, implement the Dispose
/// and store the TreeView yourself however you want, in an optimized manner.
/// </summary>
public interface ITreeViewContainer : IDisposable
{
    /// <summary>Unique identifier</summary>
    public Key<TreeViewContainer> Key { get; init; }
    
    /// <summary>
    /// WARNING: modification of this list from a non-`ITreeViewContainer` is extremely unsafe.
    /// ...it is the responsibility of the container to ensure any modifications it performs
    /// are thread safe.
    /// ...
    /// ... Making this an IReadOnlyList only serves to increase overhead when enumerating the NodeValue
    /// (and the enumeration of this is a hot path),
    /// just don't touch the list and there won't be any problems.
    ///
    /// A TreeViewNodeValue's children are enumerated by the span of
    /// the container's (inclusive) ChildList[TreeViewNoType.ChildListOffset] to
    /// (exclusive) ChildList[TreeViewNoType.ChildListOffset + TreeViewNoType.ChildListLength].
    /// </summary>
    public List<TreeViewNodeValue> NodeValueList { get; }
    
    public int ActiveNodeValueIndex { get; }
    
    /// <summary>
    /// In your constructor:
    /// `ActiveNodeElementId = $"ci_node-{Key.Guid}";`
    ///
    /// This is used for scrolling the ActiveNodeValueIndex into view.
    /// </summary>
    public string ActiveNodeElementId { get; }
    
    /// <summary>
    /// When the UI expands a nodeValue, then
    /// this method is invoked on every newly "shown" nodeValue.
    /// (virtualization isn't accounted for...
    ///  this gets invoked for every new child
    ///  whether it is visible or not).
    /// </summary>
    public void Saturate(ref TreeViewNodeValue nodeValue)
    {
        
    }
    
    /// <summary>
    /// When the UI collapses a nodeValue, then
    /// this method is invoked on every nodeValue which
    /// was a child of the collapsed nodeValue.
    /// </summary>
    public void Dessicate(ref TreeViewNodeValue nodeValue)
    {
        
    }
    
    /// <summary>
    /// This interface should always be directly tied to UI of a TreeView actively being rendered.
    /// To maintain TreeView state beyond the lifecycle of the UI, implement the Dispose
    /// and store the TreeView yourself however you want, in an optimized manner.
    /// </summary>
    public void Dispose()
    {
    }
}
