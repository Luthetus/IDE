using Clair.Common.RazorLib;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.Common.RazorLib.Icons.Displays;
using Clair.CompilerServices.DotNetSolution.Models.Project;

namespace Clair.Extensions.DotNet.CSharpProjects.Models;

public class TreeViewCSharpProjectDependencies : TreeViewWithType<CSharpProjectDependencies>
{
    public TreeViewCSharpProjectDependencies(
            CSharpProjectDependencies cSharpProjectDependencies,
            CommonService commonService,
            bool isExpandable,
            bool isExpanded)
        : base(cSharpProjectDependencies, isExpandable, isExpanded)
    {
        CommonService = commonService;
    }

    public CommonService CommonService { get; }

    public override bool Equals(object? obj)
    {
        if (obj is not TreeViewCSharpProjectDependencies otherTreeView)
            return false;

        return otherTreeView.GetHashCode() == GetHashCode();
    }

    public override int GetHashCode() => Item.CSharpProjectAbsolutePath.Value.GetHashCode();

    public override string GetDisplayText() => "Dependencies";    public override IconKind IconKind => IconKind.ProjectDependencies;

    /*public override TreeViewRenderer GetTreeViewRenderer()
    {
    
    
        using Microsoft.AspNetCore.Components;
        using Clair.Common.RazorLib.Options.Models;
        
        namespace Clair.Extensions.DotNet.CSharpProjects.Displays;
        
        public partial class TreeViewCSharpProjectDependenciesDisplay : ComponentBase
        {
            [Inject]
            private IAppOptionsService AppOptionsService { get; set; } = null!;
        }
        
    
    
        <div>

            @{
                var appOptionsState = AppOptionsService.GetAppOptionsState();
            
                var iconDriver = new IconDriver(
                    appOptionsState.Options.IconSizeInPixels,
                    appOptionsState.Options.IconSizeInPixels);
            }
        
            @IconProjectDependenciesFragment.Render(iconDriver)
            Dependencies
        </div>
    
    
        return new TreeViewRenderer(
            DotNetComponentRenderers.CompilerServicesTreeViews.TreeViewCSharpProjectDependenciesRendererType,
            null);
    }*/

    public override Task LoadChildListAsync(TreeViewContainer container)
    {
        return Task.CompletedTask;
        /*
        // 2025-10-15
        var previousChildren = new List<TreeViewNoType>(ChildList);

        var treeViewCSharpProjectNugetPackageReferences = new TreeViewCSharpProjectNugetPackageReferences(
            new CSharpProjectNugetPackageReferences(Item.CSharpProjectAbsolutePath),
            CommonService,
            true,
            false);

        var treeViewCSharpProjectToProjectReferences = new TreeViewCSharpProjectToProjectReferences(
            new CSharpProjectToProjectReferences(Item.CSharpProjectAbsolutePath),
            CommonService,
            true,
            false);

        var newChildList = new List<TreeViewNoType>
        {
            treeViewCSharpProjectNugetPackageReferences,
            treeViewCSharpProjectToProjectReferences
        };

        ChildList = newChildList;
        LinkChildren(previousChildren, ChildList);

        return Task.CompletedTask;
        */
    }

    public override void RemoveRelatedFilesFromParent(List<TreeViewNoType> siblingsAndSelfTreeViews)
    {
        return;
    }
}
