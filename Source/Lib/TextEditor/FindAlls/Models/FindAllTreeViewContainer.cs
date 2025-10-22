using Clair.Common.RazorLib;
using Clair.Common.RazorLib.TreeViews.Models;

namespace Clair.TextEditor.RazorLib.FindAlls.Models;

public class FindAllTreeViewContainer : TreeViewContainer
{
    public FindAllTreeViewContainer(CommonService commonService)
        : base(commonService)
    {
    }

    public override Task LoadChildListAsync(int indexNodeValue)
    {
        return Task.CompletedTask;
    }
}
