using Clair.Common.RazorLib.Keys.Models;

namespace Clair.Common.RazorLib.TreeViews.Models;

public interface ITreeViewContainer
{
    /// <summary>
    /// WARNING: modification of this list from a non-`ITreeViewContainer` is extremely unsafe.
    /// ...it is the responsibility of the container to ensure any modifications it performs
    /// are thread safe.
    /// ...
    /// ... Making this an IReadOnlyList only serves to increase overhead when enumerating the NodeValue
    /// (and the enumeration of this is a hot path),
    /// just don't touch the list and there won't be any problems.
    /// </summary>
    public List<TreeViewNodeValue> NodeValueList { get; }
    public Key<TreeViewContainer> Key { get; init; }
    /// <summary>
    /// The <see cref="ActiveNode"/> is the last or default entry in <see cref="SelectedNodeList"/>
    /// </summary>
    public TreeViewNoType? ActiveNode => SelectedNodeList.FirstOrDefault();
    public IReadOnlyList<TreeViewNoType> SelectedNodeList { get; init; }
    public Guid StateId { get; init; } = Guid.NewGuid();
    /// <summary>
    /// In your constructor:
    /// `ActiveNodeElementId = $"ci_node-{Key.Guid}";`
    /// </summary>
    public string ActiveNodeElementId { get; }
    /// <summary>Quite hacky</summary>
    public string ElementIdOfComponentRenderingThis { get; set; }
    /// <summary>
    /// A TreeViewNoType's children are enumerated by the span of
    /// the container's (inclusive) ChildList[TreeViewNoType.ChildListOffset] to
    /// (exclusive) ChildList[TreeViewNoType.ChildListOffset + TreeViewNoType.ChildListLength].
    /// </summary>
    public List<TreeViewNoType> ChildList { get; set; } = new();
    
    public List<> Asd;
    
    public void Hydrate(TreeViewNodeValue nodeValue)
    {
        
    }
    
    // ???????????? wtf are these names
    public void Dessicate()
    {
        
    }
}
