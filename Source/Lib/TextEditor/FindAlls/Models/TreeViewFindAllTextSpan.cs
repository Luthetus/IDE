using Clair.Common.RazorLib.TreeViews.Models;
using Clair.Common.RazorLib.FileSystems.Models;
using Clair.TextEditor.RazorLib.Lexers.Models;

namespace Clair.TextEditor.RazorLib.FindAlls.Models;

public class TreeViewFindAllTextSpan : TreeViewWithType<TextEditorTextSpan>
{
    public TreeViewFindAllTextSpan(
            TextEditorTextSpan textSpan,
            bool isExpandable,
            bool isExpanded)
        : base(textSpan, isExpandable, isExpanded)
    {
        TextSpan = textSpan;
    }
    
    public TextEditorTextSpan TextSpan { get; }
    
    public override bool Equals(object? obj)
    {
        return false;
    }

    public override int GetHashCode() => 0;
    
    public override string GetDisplayText()
    {
        Item.TextSpan.GetText(Item.SourceText, textEditorService: null);
    }

    public override Task LoadChildListAsync(TreeViewContainer container)
    {
        return Task.CompletedTask;
    }
}
