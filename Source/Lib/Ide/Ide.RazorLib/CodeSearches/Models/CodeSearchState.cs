using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.Dimensions.Models;
using Clair.Common.RazorLib.TreeViews.Models;

namespace Clair.Ide.RazorLib.CodeSearches.Models;

public record struct CodeSearchState(
    string Query,
    string? StartingAbsolutePathForSearch,
    CodeSearchFilterKind CodeSearchFilterKind,
    IReadOnlyList<string> ResultList,
    string PreviewFilePath,
    int PreviewViewModelKey)
{
    public static readonly Key<TreeViewContainer> TreeViewCodeSearchContainerKey = Key<TreeViewContainer>.NewKey();
    
    public CodeSearchState() : this(
        string.Empty,
        null,
        CodeSearchFilterKind.None,
        Array.Empty<string>(),
        string.Empty,
        0)
    {
        // topContentHeight
        TopContentElementDimensions.Height_Base_0 = new DimensionUnit(40, DimensionUnitKind.Percentage);
        TopContentElementDimensions.Height_Offset = new DimensionUnit(0, DimensionUnitKind.Pixels, DimensionOperatorKind.Subtract);

        // bottomContentHeight
        BottomContentElementDimensions.Height_Base_0 = new DimensionUnit(60, DimensionUnitKind.Percentage);
        BottomContentElementDimensions.Height_Offset = new DimensionUnit(0, DimensionUnitKind.Pixels, DimensionOperatorKind.Subtract);
    }

    public ElementDimensions TopContentElementDimensions = new();
    public ElementDimensions BottomContentElementDimensions = new();
}
