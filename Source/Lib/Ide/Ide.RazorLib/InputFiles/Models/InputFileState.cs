using Clair.Common.RazorLib;
using Clair.Common.RazorLib.FileSystems.Models;
using Clair.Common.RazorLib.TreeViews.Models;

namespace Clair.Ide.RazorLib.InputFiles.Models;

public record struct InputFileState(
    int IndexInHistory,
    IReadOnlyList<TreeViewNodeValue> OpenedTreeViewModelHistoryList,
    TreeViewNodeValue SelectedTreeViewModel,
    Func<AbsolutePath, Task> OnAfterSubmitFunc,
    Func<AbsolutePath, Task<bool>> SelectionIsValidFunc,
    IReadOnlyList<InputFilePattern> InputFilePatternsList,
    InputFilePattern? SelectedInputFilePattern,
    string SearchQuery,
    string Message)
{
    public InputFileState() : this(
        -1,
        Array.Empty<TreeViewNodeValue>(),
        default,
        _ => Task.CompletedTask,
        _ => Task.FromResult(false),
        Array.Empty<InputFilePattern>(),
        null,
        string.Empty,
        string.Empty)
    {
    }
    
    public bool CanMoveBackwardsInHistory => IndexInHistory > 0;
    public bool CanMoveForwardsInHistory => IndexInHistory < OpenedTreeViewModelHistoryList.Count - 1;

    public TreeViewNodeValue GetOpenedTreeView()
    {
        if (IndexInHistory == -1 || IndexInHistory >= OpenedTreeViewModelHistoryList.Count)
            return default;

        return OpenedTreeViewModelHistoryList[IndexInHistory];
    }

    public static InputFileState NewOpenedTreeViewModelHistory(
        InputFileState inInputFileState,
        TreeViewNodeValue selectedTreeViewModel,
        CommonService commonService)
    {
        /*
        // 2025-10-22 (rewrite TreeViews)
        var selectionClone = new TreeViewAbsolutePath(
            selectedTreeViewModel.Item,
            commonService,
            false,
            true);

        selectionClone.IsExpanded = true;
        selectionClone.ChildListOffset = selectedTreeViewModel.ChildListOffset;
        selectionClone.ChildListLength = selectedTreeViewModel.ChildListLength;
        
        var nextHistory = new List<TreeViewAbsolutePath>(inInputFileState.OpenedTreeViewModelHistoryList);

        // If not at end of history the more recent history is
        // replaced by the to be selected TreeViewModel
        if (inInputFileState.IndexInHistory != inInputFileState.OpenedTreeViewModelHistoryList.Count - 1)
        {
            var historyCount = inInputFileState.OpenedTreeViewModelHistoryList.Count;
            var startingIndexToRemove = inInputFileState.IndexInHistory + 1;
            var countToRemove = historyCount - startingIndexToRemove;

            nextHistory.RemoveRange(
                startingIndexToRemove,
                countToRemove);
        }

        nextHistory.Add(selectionClone);

        return inInputFileState with
        {
            IndexInHistory = inInputFileState.IndexInHistory + 1,
            OpenedTreeViewModelHistoryList = nextHistory,
        };
        */
        return inInputFileState;
    }
}
