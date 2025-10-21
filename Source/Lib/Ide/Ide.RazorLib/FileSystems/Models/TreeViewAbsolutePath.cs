using Clair.Common.RazorLib;
using Clair.Common.RazorLib.FileSystems.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.Common.RazorLib.TreeViews.Models.Utils;

namespace Clair.Ide.RazorLib.FileSystems.Models;

public class TreeViewAbsolutePath : TreeViewWithType<AbsolutePath>
{
    public TreeViewAbsolutePath(
            AbsolutePath absolutePath,
            CommonService commonService,
            bool isExpandable,
            bool isExpanded)
        : base(absolutePath, isExpandable, isExpanded)
    {
        CommonService = commonService;
    }

    public CommonService CommonService { get; }

    public override bool Equals(object? obj)
    {
        if (obj is not TreeViewAbsolutePath treeViewAbsolutePath)
            return false;

        return treeViewAbsolutePath.Item.Value == Item.Value;
    }

    public override int GetHashCode() => Item.Value.GetHashCode();
    
    public override string GetDisplayText() => Item.Name;

    public override async Task LoadChildListAsync(TreeViewContainer container)
    {
        ChildListLength = 0;
        ChildListOffset = container.ChildList.Count;
    
        try
        {
            var previousChildren = GetChildList(container);

            var newChildList = Enumerable.Empty<TreeViewNoType>();

            if (Item.IsDirectory)
            {
                var helperList = await TreeViewHelperAbsolutePathDirectory.LoadChildrenAsync(this).ConfigureAwait(false);
                foreach (var child in helperList)
                {
                    container.ChildList.Add(child);
                }
                ChildListLength = helperList.Count;
                newChildList = helperList;
            }
            
            LinkChildren(previousChildren, newChildList, container);
        }
        catch (Exception exception)
        {
            container.ChildList.Add(new TreeViewException(exception, false, false)
            {
                Parent = this,
                IndexAmongSiblings = 0,
            });
            ++ChildListLength;
        }
    }

    public override void RemoveRelatedFilesFromParent(List<TreeViewNoType> siblingsAndSelfTreeViews)
    {
        // This method is meant to do nothing in this case.
    }
}
