namespace Clair.Common.RazorLib.TreeViews.Models;

/// <summary>
/// Implement the abstract class <see cref="TreeViewWithType{T}"/> in order to make a TreeView.<br/><br/>
/// An abstract class is used because a good deal of customization is required on a per
/// TreeView basis depending on what data type one displays in that TreeView.
/// </summary>
public abstract class TreeViewWithType<TItem, THydrator> : TreeViewNoType where T : notnull
{
    public TreeViewWithType(bool isExpandable, bool isExpanded)
    {
        Item = item;
        IsExpandable = isExpandable;
        IsExpanded = isExpanded;
    }

    public override object Item => Item;
    public override Type ItemType => typeof(T);
}
