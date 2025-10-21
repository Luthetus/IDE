using System.Text;
using Clair.Common.RazorLib;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.Common.RazorLib.Icons.Displays;
using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.CompilerServices.DotNetSolution.Models.Project;
using Clair.CompilerServices.Xml;
using Clair.Extensions.DotNet.Nugets.Models;

namespace Clair.Extensions.DotNet.CSharpProjects.Models;

public class TreeViewCSharpProjectNugetPackageReferences : TreeViewWithType<CSharpProjectNugetPackageReferences>
{
    public TreeViewCSharpProjectNugetPackageReferences(
            CSharpProjectNugetPackageReferences cSharpProjectNugetPackageReferences,
            CommonService commonService,
            bool isExpandable,
            bool isExpanded)
        : base(cSharpProjectNugetPackageReferences, isExpandable, isExpanded)
    {
        CommonService = commonService;
    }

    public CommonService CommonService { get; }

    public override bool Equals(object? obj)
    {
        if (obj is not TreeViewCSharpProjectNugetPackageReferences otherTreeView)
            return false;

        return otherTreeView.GetHashCode() == GetHashCode();
    }

    public override int GetHashCode() => Item.CSharpProjectAbsolutePath.Value.GetHashCode();

    public override string GetDisplayText() => "NuGet Packages";
    
    public override IconKind IconKind => IconKind.NuGetPackages;

    /*public override TreeViewRenderer GetTreeViewRenderer()
    {
    
        using Microsoft.AspNetCore.Components;
        using Clair.Common.RazorLib.Options.Models;
        
        namespace Clair.Extensions.DotNet.CSharpProjects.Displays;
        
        public partial class TreeViewCSharpProjectNugetPackageReferencesDisplay : ComponentBase
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
        
            @IconNuGetPackagesFragment.Render(iconDriver)
            NuGet Packages
        </div>
        
        
        
        
        return new TreeViewRenderer(
            DotNetComponentRenderers.CompilerServicesTreeViews.TreeViewCSharpProjectNugetPackageReferencesRendererType,
            null);
    }*/

    public override async Task LoadChildListAsync(TreeViewContainer container)
    {
        /*
        // 2025-10-15
        var previousChildren = new List<TreeViewNoType>(ChildList);

        using StreamReader sr = new StreamReader(Item.CSharpProjectAbsolutePath.Value);
        var lexerOutput = XmlLexer.Lex(new StreamReaderWrap(sr), modelModifier: null, textSpanList: new());
        
        var stringBuilder = new StringBuilder();
        var getTextBuffer = new char[1];
        
        List<(string ValueAttributeOne, string ValueAttributeTwo)> lightWeightNugetPackageRecords = new();
        
        var outputReader = new XmlOutputReader(lexerOutput.TextSpanList);
        
        outputReader.FindTagGetEitherOrBothAttributeValue(
            targetTagName: "PackageReference",
            targetAttributeOne: "Include",
            targetAttributeTwo: "Version",
            shouldIncludeFullMissLines: false,
            sr,
            stringBuilder,
            getTextBuffer,
            lightWeightNugetPackageRecords);

        var cSharpProjectAbsolutePathString = Item.CSharpProjectAbsolutePath.Value;

        var newChildList = lightWeightNugetPackageRecords.Select(
            npr => (TreeViewNoType)new TreeViewCSharpProjectNugetPackageReference(
                new(cSharpProjectAbsolutePathString, new LightWeightNugetPackageRecord(npr.ValueAttributeOne, npr.ValueAttributeOne, npr.ValueAttributeTwo)),
                CommonService,
                false,
                false))
            .ToList();

        ChildList = newChildList;
        LinkChildren(previousChildren, ChildList);
        */
    }

    public override void RemoveRelatedFilesFromParent(List<TreeViewNoType> siblingsAndSelfTreeViews)
    {
        return;
    }
}
