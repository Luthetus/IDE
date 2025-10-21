using Clair.Common.RazorLib;
using Clair.Common.RazorLib.Icons.Displays;
using Clair.Common.RazorLib.FileSystems.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.Common.RazorLib.TreeViews.Models.Utils;
using Clair.Ide.RazorLib.Namespaces.Models;
using Clair.Ide.RazorLib;

namespace Clair.Extensions.DotNet.Namespaces.Models;

public class TreeViewNamespacePath : TreeViewWithType<AbsolutePath>
{
    public TreeViewNamespacePath(
            AbsolutePath namespacePath,
            CommonService commonService,
            bool isExpandable,
            bool isExpanded)
        : base(namespacePath, isExpandable, isExpanded)
    {
        CommonService = commonService;
    }

    public CommonService CommonService { get; }

    public override bool Equals(object? obj)
    {
        if (obj is not TreeViewNamespacePath treeViewSolutionExplorer)
            return false;

        return treeViewSolutionExplorer.Item.Value == Item.Value;
    }

    public override int GetHashCode() => Item.Value.GetHashCode();
    
    public override string GetDisplayText() => Item.Name;
    
    public override IconKind IconKind => IdeFacts.GetIconKind(Item);

    /*public override TreeViewRenderer GetTreeViewRenderer()
    {
        
        using Microsoft.AspNetCore.Components;
        using Clair.Common.RazorLib.Namespaces.Models;
        using Clair.Common.RazorLib.Options.Models;
        using Clair.Ide.RazorLib.ComponentRenderers.Models;
        
        namespace Clair.Ide.RazorLib.Namespaces.Displays;
        
        public partial class TreeViewNamespacePathDisplay : ComponentBase, ITreeViewNamespacePathRendererType
        {
            [Inject]
            private IAppOptionsService AppOptionsService { get; set; } = null!;
        
            [CascadingParameter(Name="ClairCommonIconWidthOverride")]
            public int? ClairCommonIconWidthOverride { get; set; }
            [CascadingParameter(Name="ClairCommonIconHeightOverride")]
            public int? ClairCommonIconHeightOverride { get; set; }
        
            [Parameter, EditorRequired]
            public NamespacePath NamespacePath { get; set; }
            [Parameter]
            public string CssStyleString { get; set; } = string.Empty;
            
            public int WidthInPixels => ClairCommonIconWidthOverride ??
                AppOptionsService.GetAppOptionsState().Options.IconSizeInPixels;
        
            public int HeightInPixels => ClairCommonIconHeightOverride ??
                AppOptionsService.GetAppOptionsState().Options.IconSizeInPixels;
        }
        
        
        @using Clair.Common.RazorLib.Icons.Models
        @using Clair.Ide.RazorLib.FileSystems.Displays
        
        <div title="@NamespacePath.AbsolutePath.Value">
            
            @{ var iconDriver = new IconDriver(WidthInPixels, HeightInPixels); }
            
            @FileIconStaticRenderFragments.GetRenderFragment((iconDriver, NamespacePath.AbsolutePath))
            @NamespacePath.AbsolutePath.NameWithExtension
        </div>
        
        
        return new TreeViewRenderer(
            IdeComponentRenderers.IdeTreeViews.TreeViewNamespacePathRendererType,
            new Dictionary<string, object?>
            {
                {
                    nameof(ITreeViewNamespacePathRendererType.NamespacePath),
                    Item
                },
            });
    }*/

    public override async Task LoadChildListAsync(TreeViewContainer container)
    {
        // Codebehinds: Children need to
        // - new instance
        // - try map to old instance
        // - opportunity for child to do something (like take siblings as their children)
    
        ChildListLength = 0;
        ChildListOffset = container.ChildList.Count;
        
        try
        {
            var previousChildren = GetChildList(container);
            
            var newChildList = Enumerable.Empty<TreeViewNoType>();

            // new instance
            if (Item.IsDirectory)
            {
                var helperList = await TreeViewHelperNamespacePathDirectory.LoadChildrenAsync(this).ConfigureAwait(false);
                foreach (var child in helperList)
                {
                    container.ChildList.Add(child);
                }
                ChildListLength = helperList.Count;
                newChildList = helperList;
            }
            else
            {
                if (Item.Name.EndsWith(CommonFacts.DOT_NET_SOLUTION))
                {
                    return;
                }
                else if (Item.Name.EndsWith(CommonFacts.C_SHARP_PROJECT))
                {
                    var helperList = await TreeViewHelperCSharpProject.LoadChildrenAsync(this).ConfigureAwait(false);
                    foreach (var child in helperList)
                    {
                        container.ChildList.Add(child);
                    }
                    ChildListLength = helperList.Count;
                    newChildList = helperList;
                }
                else if (Item.Name.EndsWith(CommonFacts.RAZOR_MARKUP))
                {
                    // newChildList = await TreeViewHelperRazorMarkup.LoadChildrenAsync(this).ConfigureAwait(false);
                }
            }

            // try map to old instance
            LinkChildren(previousChildren, newChildList, container);
            
            /*
            // 2025-10-15
            // opportunity for child to do something (like take siblings as their children)
            {
                var shouldPermitChildToTakeSiblingsAsChildren = false;
                
                if (Item.IsDirectory)
                {
                    shouldPermitChildToTakeSiblingsAsChildren = true;
                }
                else
                {
                    if (Item.Name.EndsWith(CommonFacts.C_SHARP_PROJECT))
                    {
                        shouldPermitChildToTakeSiblingsAsChildren = true;
                    }
                }
                
                if (shouldPermitChildToTakeSiblingsAsChildren)
                {
                    // Codebehind logic
                    var copyOfChildrenToFindRelatedFiles = new List<TreeViewNoType>(newChildList);
            
                    // Note that this loops over the original, and passes the copy
                    foreach (var child in newChildList)
                    {
                        child.RemoveRelatedFilesFromParent(copyOfChildrenToFindRelatedFiles);
                    }
            
                    // The parent directory gets what is left over after the
                    // children take their respective 'code behinds'
                    newChildList = copyOfChildrenToFindRelatedFiles;
                    
                    // This time, 'LinkChildren(...)' is invoked
                    // in order to wire up the 'TreeViewNoType.IndexAmongSiblings'.
                    // 
                    // This index is used by the keyboard events to move
                    // throughout the tree view.
                    ChildList = newChildList;
                    LinkChildren(ChildList, ChildList);
                }
            }
            */
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

    /// <summary>
    /// This method is called on each child when loading children for a parent node.
    /// This method allows for code-behinds
    /// </summary>
    public override void RemoveRelatedFilesFromParent(List<TreeViewNoType> siblingsAndSelfTreeViews)
    {
        if (Item.Name.EndsWith(CommonFacts.RAZOR_MARKUP))
            TreeViewHelperRazorMarkup.FindRelatedFiles(this, siblingsAndSelfTreeViews);
    }
}
