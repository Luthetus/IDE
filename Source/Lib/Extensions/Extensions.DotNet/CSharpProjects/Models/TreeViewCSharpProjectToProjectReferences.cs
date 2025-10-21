using System.Text;
using Clair.Common.RazorLib;
using Clair.Common.RazorLib.FileSystems.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.Common.RazorLib.Icons.Displays;
using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.CompilerServices.DotNetSolution.Models.Project;
using Clair.CompilerServices.Xml;

namespace Clair.Extensions.DotNet.CSharpProjects.Models;

public class TreeViewCSharpProjectToProjectReferences : TreeViewWithType<CSharpProjectToProjectReferences>
{
    public TreeViewCSharpProjectToProjectReferences(
            CSharpProjectToProjectReferences cSharpProjectToProjectReferences,
            CommonService commonService,
            bool isExpandable,
            bool isExpanded)
        : base(cSharpProjectToProjectReferences, isExpandable, isExpanded)
    {
        CommonService = commonService;
    }

    public CommonService CommonService { get; }

    public override bool Equals(object? obj)
    {
        if (obj is not TreeViewCSharpProjectToProjectReferences otherTreeView)
            return false;

        return otherTreeView.GetHashCode() == GetHashCode();
    }

    public override int GetHashCode() => Item.CSharpProjectAbsolutePath.Value.GetHashCode();

    public override string GetDisplayText() => "Project References";
    
    public override IconKind IconKind => IconKind.References;

    /*public override TreeViewRenderer GetTreeViewRenderer()
    {
    
        using Microsoft.AspNetCore.Components;
        using Clair.Common.RazorLib.Options.Models;
        
        namespace Clair.Extensions.DotNet.CSharpProjects.Displays;
        
        public partial class TreeViewCSharpProjectToProjectReferencesDisplay : ComponentBase
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
        
            @IconReferencesFragment.Render(iconDriver)
            Project References
        </div>
        
        
        
        return new TreeViewRenderer(
            DotNetComponentRenderers.CompilerServicesTreeViews.TreeViewCSharpProjectToProjectReferencesRendererType,
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
        
        List<string> relativePathReferenceList = new();
        
        var outputReader = new XmlOutputReader(lexerOutput.TextSpanList);
        
        outputReader.FindTagGetAttributeValue(
            targetTagName: "ProjectReference",
            targetAttributeOne: "Include",
            shouldIncludeFullMissLines: false,
            sr,
            stringBuilder,
            getTextBuffer,
            relativePathReferenceList);
        
        List<CSharpProjectToProjectReference> cSharpProjectToProjectReferences = new();
        
        var tokenBuilder = new StringBuilder();
        var formattedBuilder = new StringBuilder();
        
        var moveUpDirectoryToken = $"..{CommonService.FileSystemProvider.DirectorySeparatorChar}";
        // "./" is being called the 'sameDirectoryToken'
        var sameDirectoryToken = $".{CommonService.FileSystemProvider.DirectorySeparatorChar}";

        var projectAncestorDirectoryList = Item.CSharpProjectAbsolutePath.GetAncestorDirectoryList(
            CommonService.FileSystemProvider,
            tokenBuilder,
            formattedBuilder,
            AbsolutePathNameKind.NameWithExtension);
        
        foreach (var projectReference in relativePathReferenceList)
        {
            var referenceProjectAbsolutePathString = CommonFacts.GetAbsoluteFromAbsoluteAndRelative(
                Item.CSharpProjectAbsolutePath,
                projectReference,
                CommonService.FileSystemProvider,
                tokenBuilder,
                formattedBuilder,
                moveUpDirectoryToken: moveUpDirectoryToken,
                sameDirectoryToken: sameDirectoryToken,
                projectAncestorDirectoryList);

            var referenceProjectAbsolutePath = new AbsolutePath(
                referenceProjectAbsolutePathString,
                false,
                CommonService.FileSystemProvider,
                tokenBuilder,
                formattedBuilder,
                AbsolutePathNameKind.NameWithExtension);

            var cSharpProjectToProjectReference = new CSharpProjectToProjectReference(
                Item.CSharpProjectAbsolutePath,
                referenceProjectAbsolutePath);

            cSharpProjectToProjectReferences.Add(cSharpProjectToProjectReference);
        }

        var newChildList = cSharpProjectToProjectReferences
            .Select(x => (TreeViewNoType)new TreeViewCSharpProjectToProjectReference(
                x,
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
