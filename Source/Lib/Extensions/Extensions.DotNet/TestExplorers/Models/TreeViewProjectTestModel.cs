using Clair.Common.RazorLib.TreeViews.Models;
using Clair.Common.RazorLib.TreeViews.Models.Utils;

namespace Clair.Extensions.DotNet.TestExplorers.Models;

public class TreeViewProjectTestModel : TreeViewWithType<ProjectTestModel>
{
    public TreeViewProjectTestModel(
            ProjectTestModel projectTestModel,
            bool isExpandable,
            bool isExpanded)
        : base(projectTestModel, isExpandable, isExpanded)
    {
    }

    public override bool Equals(object? obj)
    {
        if (obj is not TreeViewProjectTestModel treeViewProjectTestModel)
            return false;

        return treeViewProjectTestModel.Item.ProjectIdGuid == Item.ProjectIdGuid;
    }

    public override int GetHashCode() => Item.ProjectIdGuid.GetHashCode();

    public override string GetDisplayText() => Item.AbsolutePath.Name;

    /*public override TreeViewRenderer GetTreeViewRenderer()
    {
    
        using Microsoft.AspNetCore.Components;
        using Clair.Extensions.DotNet.TestExplorers.Models;
        
        namespace Clair.Extensions.DotNet.TestExplorers.Displays.Internals;
        
        public partial class TreeViewProjectTestModelDisplay : ComponentBase
        {
            [Parameter, EditorRequired]
            public TreeViewProjectTestModel TreeViewProjectTestModel { get; set; } = null!;
        }
    
    
        @TreeViewProjectTestModel.Item.AbsolutePath.NameWithExtension
        &nbsp;
        (@(TreeViewProjectTestModel.Item.TestNameFullyQualifiedList?.Count.ToString() ?? "?"))

    
        return new TreeViewRenderer(
            typeof(TreeViewProjectTestModelDisplay),
            new Dictionary<string, object?>
            {
                {
                    nameof(TreeViewProjectTestModelDisplay.TreeViewProjectTestModel),
                    this
                },
            });
    }*/

    public override Task LoadChildListAsync(TreeViewContainer container)
    {
        if (Item.TestNameFullyQualifiedList is not null)
            return Task.CompletedTask;

        ChildListLength = 0;
        ChildListOffset = container.ChildList.Count;
        
        var previousChildren = GetChildList(container);

        container.ChildList.Add(new TreeViewSpinner(
            Item.ProjectIdGuid,
            false,
            false));
        ++ChildListLength;

        LinkChildrenNoMap(GetChildList(container), container);

        return Item.EnqueueDiscoverTestsFunc(async rootStringFragmentMap =>
        {
            try
            {
                ChildListLength = 0;
                ChildListOffset = container.ChildList.Count;
                
                var previousChildren = GetChildList(container);

                if (rootStringFragmentMap.Values.Any())
                {
                    var rootStringFragment = new StringFragment(string.Empty);
                    rootStringFragment.Map = rootStringFragmentMap;

                    var newChildList = rootStringFragment.Map.Select(kvp =>
                        (TreeViewNoType)new TreeViewStringFragment(
                            kvp.Value,
                            true,
                            true))
                        .ToArray();

                    for (var i = 0; i < newChildList.Length; i++)
                    {
                        var node = (TreeViewStringFragment)newChildList[i];
                        await node.LoadChildListAsync(container).ConfigureAwait(false);
                        
                        container.ChildList.Add(node);
                        ++ChildListLength;
                    }
                    
                    LinkChildrenNoMap(newChildList, container);
                }
                else
                {
                    container.ChildList.Add(new TreeViewException(new Exception("No results"), false, false)
                    {
                        Parent = this,
                        IndexAmongSiblings = 0,
                    });
                    ++ChildListLength;
                    
                    LinkChildrenNoMap(GetChildList(container), container);
                }
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

            Item.ReRenderNodeAction.Invoke(this);
        });
    }

    public override void RemoveRelatedFilesFromParent(List<TreeViewNoType> siblingsAndSelfTreeViews)
    {
        return;
    }
}
