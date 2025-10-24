namespace Clair.Common.RazorLib.TreeViews.Models;

public struct TreeViewNodeValue
{
    /// <summary>
    /// The corresponding TreeViewContainer will have this
    /// 
    /// Storing this and the IndexAmongSiblings should permit "random access"
    /// without tracking an enumeration.
    /// </summary>
    public int ParentIndex { get; set; }
    /// <summary>
    /// Storing this and the parent's index should permit "random access"
    /// without tracking an enumeration.
    /// </summary>
    public int IndexAmongSiblings { get; set; }
    
    /// <summary>
    /// A TreeViewNoType's children are enumerated by the span of
    /// the container's (inclusive) ChildList[TreeViewNoType.ChildListOffset] to
    /// (exclusive) ChildList[TreeViewNoType.ChildListOffset + TreeViewNoType.ChildListLength].
    ///
    /// When modifying ChildListOffset, it is safest to set ChildListLength to 0 before doing so.
    /// </summary>
    public int ChildListOffset { get; set; }
    /// <summary>
    /// A TreeViewNoType's children are enumerated by the span of
    /// the container's (inclusive) ChildList[TreeViewNoType.ChildListOffset] to
    /// (exclusive) ChildList[TreeViewNoType.ChildListOffset + TreeViewNoType.ChildListLength].
    /// </summary>
    public int ChildListLength { get; set; }
    
    /// <summary>
    /// '.None' should NEVER be used.
    /// It marks whether a nodeValue is the default value or not.
    /// </summary>
    public TreeViewNodeValueKind TreeViewNodeValueKind { get; set; }
    
    /// <summary>
    /// All data should be stored on the ITreeViewContainer.
    ///
    /// This property either can be an index within some List wherein
    /// the nodeValue's data is stored.
    ///
    /// Or you can use this as a unique identifier / whatever, if the data is non-list required.
    ///
    /// The ITreeViewContainer might consider a variety of lists, each containing a value type
    /// representation of the data as per TreeViewNodeValueKind.
    ///
    /// Or might just put all the data in one class, use inheritance etc... /whatever
    /// </summary>
    public int TraitsIndex { get; set; }
    
    public bool IsExpandable { get; set; }
    public bool IsExpanded { get; set; }
    
    public bool IsDefault() => TreeViewNodeValueKind == TreeViewNodeValueKind.None;
}
