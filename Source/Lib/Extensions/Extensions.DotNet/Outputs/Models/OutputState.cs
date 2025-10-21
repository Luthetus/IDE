using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.TreeViews.Models;

namespace Clair.Extensions.DotNet.Outputs.Models;

public record struct OutputState(Guid DotNetRunParseResultId)
{
    public static readonly Key<TreeViewContainer> TreeViewContainerKey = Key<TreeViewContainer>.NewKey();
    
    public OutputState() : this(Guid.Empty)
    {
    }
}
