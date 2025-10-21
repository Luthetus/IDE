using Clair.Common.RazorLib;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.Common.RazorLib.Icons.Displays;
using Clair.Extensions.DotNet.Nugets.Models;

namespace Clair.Extensions.DotNet.CSharpProjects.Models;

public class TreeViewCSharpProjectNugetPackageReference : TreeViewWithType<CSharpProjectNugetPackageReference>
{
    public TreeViewCSharpProjectNugetPackageReference(
            CSharpProjectNugetPackageReference cSharpProjectNugetPackageReference,
            CommonService commonService,
            bool isExpandable,
            bool isExpanded)
        : base(cSharpProjectNugetPackageReference, isExpandable, isExpanded)
    {
        CommonService = commonService;
    }

    public CommonService CommonService { get; }

    public override bool Equals(object? obj)
    {
        if (obj is not TreeViewCSharpProjectNugetPackageReference otherTreeView)
            return false;

        return otherTreeView.GetHashCode() == GetHashCode();
    }

    public override int GetHashCode()
    {
        var uniqueString = Item.CSharpProjectAbsolutePathString + Item.LightWeightNugetPackageRecord.Id;
        return uniqueString.GetHashCode();
    }

    public override string GetDisplayText() => $"{Item.LightWeightNugetPackageRecord.Title}/{Item.LightWeightNugetPackageRecord.Version}";
    
    public override IconKind IconKind => IconKind.Package;

    /*public override TreeViewRenderer GetTreeViewRenderer()
    {
    
        using Microsoft.AspNetCore.Components;
        using Clair.Common.RazorLib.Options.Models;
        using Clair.Extensions.DotNet.Nugets.Models;
        using Clair.Extensions.DotNet.ComponentRenderers.Models;
        
        namespace Clair.Extensions.DotNet.CSharpProjects.Displays;
        
        public partial class TreeViewCSharpProjectNugetPackageReferenceDisplay : ComponentBase, ITreeViewCSharpProjectNugetPackageReferenceRendererType
        {
            [Inject]
            private IAppOptionsService AppOptionsService { get; set; } = null!;
            
            [Parameter, EditorRequired]
            public CSharpProjectNugetPackageReference CSharpProjectNugetPackageReference { get; set; } = null!;
        }
    
    
        <div>
        
            @{
                var appOptionsState = AppOptionsService.GetAppOptionsState();
            
                var iconDriver = new IconDriver(
                    appOptionsState.Options.IconSizeInPixels,
                    appOptionsState.Options.IconSizeInPixels);
            }
        
            @IconPackageFragment.Render(iconDriver)
            @CSharpProjectNugetPackageReference.LightWeightNugetPackageRecord.Title<!--
            -->/<!--
            -->@CSharpProjectNugetPackageReference.LightWeightNugetPackageRecord.Version
        </div>
    
    
    
        return new TreeViewRenderer(
            DotNetComponentRenderers.CompilerServicesTreeViews.TreeViewCSharpProjectNugetPackageReferenceRendererType,
            new Dictionary<string, object?>
            {
                {
                    nameof(ITreeViewCSharpProjectNugetPackageReferenceRendererType.CSharpProjectNugetPackageReference),
                    Item
                },
            });
    }*/

    public override Task LoadChildListAsync(TreeViewContainer container)
    {
        return Task.CompletedTask;
    }

    public override void RemoveRelatedFilesFromParent(List<TreeViewNoType> siblingsAndSelfTreeViews)
    {
        return;
    }
}
