using Clair.Common.RazorLib.Keys.Models;

namespace Clair.Common.RazorLib.TreeViews.Models;

/// <summary>
/// TODO: SphagettiCode - some logic was added to multi-select nodes, yet it was never
/// finished, and is buggy.(2023-09-19)
/// </summary>
public record TreeViewContainer
{
    /// <summary>
    /// If <see cref="rootNode"/> is null then <see cref="TreeViewAdhoc.ConstructTreeViewAdhoc()"/>
    /// will be invoked and the return value will be used as the <see cref="RootNode"/>
    /// </summary>
    public TreeViewContainer(
        Key<TreeViewContainer> key,
        TreeViewNoType? rootNode,
        IReadOnlyList<TreeViewNoType> selectedNodeList)
    {    
        rootNode ??= TreeViewAdhoc.ConstructTreeViewAdhoc();

        Key = key;
        RootNode = rootNode;
        SelectedNodeList = selectedNodeList;
        
        ActiveNodeElementId = $"ci_node-{Key.Guid}";
    }
    
    private TreeViewNoType _rootNode;

    public Key<TreeViewContainer> Key { get; init; }
    /// <summary>
    /// Indicates when a node in the TreeView has been changed
    /// such that the UI needs to recalculate the flat list of nodes.
    ///
    /// i.e.: essentially this changes when expanding/collapsing a node.
    /// </summary>
    public int FlatListVersion { get; set; }
    public TreeViewNoType RootNode
    {
        get => _rootNode;
        init
        {
            _rootNode = value;
            ++FlatListVersion;
        }
    }
    /// <summary>
    /// The <see cref="ActiveNode"/> is the last or default entry in <see cref="SelectedNodeList"/>
    /// </summary>
    public TreeViewNoType? ActiveNode => SelectedNodeList.FirstOrDefault();
    public IReadOnlyList<TreeViewNoType> SelectedNodeList { get; init; }
    public Guid StateId { get; init; } = Guid.NewGuid();
    public string ActiveNodeElementId { get; }
    /// <summary>Quite hacky</summary>
    public string ElementIdOfComponentRenderingThis { get; set; }
    /// <summary>
    /// A TreeViewNoType's children are enumerated by the span of
    /// the container's (inclusive) ChildList[TreeViewNoType.ChildListOffset] to
    /// (exclusive) ChildList[TreeViewNoType.ChildListOffset + TreeViewNoType.ChildListLength].
    /// </summary>
    public List<TreeViewNoType> ChildList { get; set; } = new();
}
