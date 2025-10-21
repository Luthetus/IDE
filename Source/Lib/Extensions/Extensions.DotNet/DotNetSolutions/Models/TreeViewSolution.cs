using Clair.Common.RazorLib;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.Common.RazorLib.TreeViews.Models.Utils;
using Clair.Common.RazorLib.Icons.Displays;
using Clair.CompilerServices.DotNetSolution.Models;

namespace Clair.Extensions.DotNet.DotNetSolutions.Models;

public class TreeViewSolution : TreeViewWithType<DotNetSolutionModel>
{
    public TreeViewSolution(
            DotNetSolutionModel dotNetSolutionModel,
            CommonService commonService,
            bool isExpandable,
            bool isExpanded)
        : base(dotNetSolutionModel, isExpandable, isExpanded)
    {
        CommonService = commonService;
    }

    public CommonService CommonService { get; }

    public override bool Equals(object? obj)
    {
        if (obj is not TreeViewSolution treeViewSolution)
            return false;

        return treeViewSolution.Item.AbsolutePath.Value ==
               Item.AbsolutePath.Value;
    }

    public override int GetHashCode() => Item.AbsolutePath.Value.GetHashCode();

    public override string GetDisplayText() => Item.AbsolutePath.Name;
    
    public override IconKind IconKind => IconKind.DotNetSolution;

    /*public override TreeViewRenderer GetTreeViewRenderer()
    {
        
    
        return new TreeViewRenderer(
            IdeComponentRenderers.IdeTreeViews.TreeViewNamespacePathRendererType,
            new Dictionary<string, object?>
            {
                {
                    nameof(ITreeViewNamespacePathRendererType.NamespacePath),
                    Item.NamespacePath
                },
            });
    }*/

    public override async Task LoadChildListAsync(TreeViewContainer container)
    {
        ChildListLength = 0;
        ChildListOffset = container.ChildList.Count;
    
        try
        {
            var previousChildren = GetChildList(container);

            var newChildList = await TreeViewHelperDotNetSolution.LoadChildrenAsync(this).ConfigureAwait(false);
            foreach (var child in newChildList)
            {
                container.ChildList.Add(child);
            }
            ChildListLength = newChildList.Count;

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
        return;
    }
}
