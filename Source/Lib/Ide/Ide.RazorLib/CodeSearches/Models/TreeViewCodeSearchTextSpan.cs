/*
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.Common.RazorLib.FileSystems.Models;
using Clair.TextEditor.RazorLib.Lexers.Models;

namespace Clair.Ide.RazorLib.CodeSearches.Models;

public class TreeViewCodeSearchTextSpan : TreeViewWithType<TextEditorTextSpan>
{
    public TreeViewCodeSearchTextSpan(
            TextEditorTextSpan textSpan,
            AbsolutePath absolutePath,
            IFileSystemProvider fileSystemProvider,
            bool isExpandable,
            bool isExpanded)
        : base(textSpan, isExpandable, isExpanded)
    {
        FileSystemProvider = fileSystemProvider;
        AbsolutePath = absolutePath;
    }
    
    public IFileSystemProvider FileSystemProvider { get; }
    public AbsolutePath AbsolutePath { get; }
    
    public override bool Equals(object? obj)
    {
        if (obj is not TreeViewCodeSearchTextSpan otherTreeView)
            return false;

        return otherTreeView.GetHashCode() == GetHashCode();
    }

    public override int GetHashCode() => AbsolutePath.Value.GetHashCode();
    
    public override string GetDisplayText() => AbsolutePath.Name;

    /*public override TreeViewRenderer GetTreeViewRenderer()
    {
    
        using Microsoft.AspNetCore.Components;
        using Clair.Ide.RazorLib.CodeSearches.Models;
        
        namespace Clair.Ide.RazorLib.CodeSearches.Displays;
        
        public partial class TreeViewCodeSearchTextSpanDisplay : ComponentBase
        {
            [Parameter, EditorRequired]
            public TreeViewCodeSearchTextSpan TreeViewCodeSearchTextSpan { get; set; } = null!;
        }
    
    
    
        <div title="@TreeViewCodeSearchTextSpan.AbsolutePath.Value">
            @(TreeViewCodeSearchTextSpan.AbsolutePath.NameWithExtension)
        <//*div>

    
    
    
    
    
        return new TreeViewRenderer(
            typeof(TreeViewCodeSearchTextSpanDisplay),
            new Dictionary<string, object?>
            {
                {
                    nameof(TreeViewCodeSearchTextSpanDisplay.TreeViewCodeSearchTextSpan),
                    this
                }
            });
    }*//*
    
    public override Task LoadChildListAsync(TreeViewContainer container)
    {
        return Task.CompletedTask;
    }
}
*/
