/*
// 2025-10-22 (rewrite TreeViews)
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.Common.RazorLib.FileSystems.Models;

namespace Clair.TextEditor.RazorLib.FindAlls.Models;

public class TreeViewFindAllGroup : TreeViewWithType<AbsolutePath>
{
    public TreeViewFindAllGroup(
            AbsolutePath absolutePath,
            bool isExpandable,
            bool isExpanded)
        : base(absolutePath, isExpandable, isExpanded)
    {
        AbsolutePath = absolutePath;
    }
    
    public AbsolutePath AbsolutePath { get; }
    
    public override bool Equals(object? obj)
    {
        if (obj is not TreeViewFindAllGroup otherTreeView)
            return false;

        return otherTreeView.GetHashCode() == GetHashCode();
    }

    public override int GetHashCode() => AbsolutePath.Value.GetHashCode();
    
    public override string GetDisplayText() => AbsolutePath.Name;

    public override Task LoadChildListAsync(TreeViewContainer container)
    {
        return Task.CompletedTask;
    }
}
*/