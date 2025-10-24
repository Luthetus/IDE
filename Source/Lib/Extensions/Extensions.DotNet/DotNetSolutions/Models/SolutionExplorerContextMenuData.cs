using Clair.Common.RazorLib.TreeViews.Models;

namespace Clair.Extensions.DotNet.DotNetSolutions.Models;

public record struct SolutionExplorerContextMenuData(
    TreeViewContainer TreeViewContainer,
    int IndexNodeValue,
    bool OccurredDueToMouseEvent,
    double LeftPositionInPixels,
    double TopPositionInPixels);
