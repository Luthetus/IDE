using Clair.Common.RazorLib;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.Common.RazorLib.Icons.Displays;
using Clair.CompilerServices.DotNetSolution.Models.Project;

namespace Clair.Extensions.DotNet.CSharpProjects.Models;

public class TreeViewCSharpProjectToProjectReference : TreeViewWithType<CSharpProjectToProjectReference>
{
    public TreeViewCSharpProjectToProjectReference(
            CSharpProjectToProjectReference cSharpProjectToProjectReference,
            CommonService commonService,
            bool isExpandable,
            bool isExpanded)
        : base(cSharpProjectToProjectReference, isExpandable, isExpanded)
    {
        CommonService = commonService;
    }

    public CommonService CommonService { get; }

    public override bool Equals(object? obj)
    {
        if (obj is not TreeViewCSharpProjectToProjectReference otherTreeView)
            return false;

        return otherTreeView.GetHashCode() == GetHashCode();
    }

    public override int GetHashCode()
    {
        var modifyProjectAbsolutePathString = Item.ModifyProjectAbsolutePath.Value;
        var referenceProjectAbsolutePathString = Item.ReferenceProjectAbsolutePath.Value;

        var uniqueAbsolutePathString = modifyProjectAbsolutePathString + referenceProjectAbsolutePathString;
        return uniqueAbsolutePathString.GetHashCode();
    }

    public override string GetDisplayText() => Item.ReferenceProjectAbsolutePath.Name;
    
    public override IconKind IconKind => IconKind.GoToFile;

    /*public override TreeViewRenderer GetTreeViewRenderer()
    {
        
        
        <div>

            @{
                var appOptionsState = AppOptionsService.GetAppOptionsState();
            
                var iconDriver = new IconDriver(
                    appOptionsState.Options.IconSizeInPixels,
                    appOptionsState.Options.IconSizeInPixels);
            }
        
            @IconGoToFileFragment.Render(iconDriver)
            @CSharpProjectToProjectReference.ReferenceProjectAbsolutePath.NameWithExtension
        </div>
        
        using Microsoft.AspNetCore.Components;
        using Clair.Common.RazorLib.Options.Models;
        using Clair.CompilerServices.DotNetSolution.Models.Project;
        using Clair.Extensions.DotNet.ComponentRenderers.Models;
        
        namespace Clair.Extensions.DotNet.CSharpProjects.Displays;
        
        public partial class TreeViewCSharpProjectToProjectReferenceDisplay : ComponentBase, ITreeViewCSharpProjectToProjectReferenceRendererType
        {
            [Inject]
            private IAppOptionsService AppOptionsService { get; set; } = null!;
            
            [Parameter, EditorRequired]
            public CSharpProjectToProjectReference CSharpProjectToProjectReference { get; set; } = null!;
        }
        
        
        return new TreeViewRenderer(
            DotNetComponentRenderers.CompilerServicesTreeViews.TreeViewCSharpProjectToProjectReferenceRendererType,
            new Dictionary<string, object?>
            {
                {
                    nameof(ITreeViewCSharpProjectToProjectReferenceRendererType.CSharpProjectToProjectReference),
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
