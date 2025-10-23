using Clair.Common.RazorLib;
using Clair.Common.RazorLib.Commands.Models;
using Clair.Common.RazorLib.Icons.Displays;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.CompilerServices.DotNetSolution.Models;
using Clair.Ide.RazorLib;
using Clair.TextEditor.RazorLib.TextEditors.Models;

namespace Clair.Extensions.DotNet.DotNetSolutions.Models;

public class SolutionExplorerTreeViewContainer : TreeViewContainer
{
    // DotNetSolutionState.TreeViewSolutionExplorerStateKey
    
    public SolutionExplorerTreeViewContainer(IdeService ideService, DotNetSolutionModel dotNetSolutionModel)
        : base(ideService.CommonService)
    {
        Key = DotNetSolutionState.TreeViewSolutionExplorerStateKey;
        NodeValueList = new();
        DotNetSolutionModel = dotNetSolutionModel;
        IdeService = ideService;
    }
    
    public override Key<TreeViewContainer> Key { get; init; }
    public override List<TreeViewNodeValue> NodeValueList { get; }
    public DotNetSolutionModel DotNetSolutionModel { get; }
    public IdeService IdeService { get; }

    public override Task LoadChildListAsync(int indexNodeValue)
    {
        var nodeValue = NodeValueList[indexNodeValue];
        nodeValue.ChildListLength = 0;
        nodeValue.ChildListOffset = NodeValueList.Count;

        switch (nodeValue.TreeViewNodeValueKind)
        {
            case TreeViewNodeValueKind.b0:
            {
                NodeValueList.AddRange(DotNetSolutionModel.SolutionFolderList
                    .OrderBy(x => x.DisplayName)
                    .Select((x, i) =>
                    {
                        return new TreeViewNodeValue
                        {
                            ParentIndex = -1,
                            IndexAmongSiblings = 0,
                            ChildListOffset = 0,
                            ChildListLength = 0,
                            TreeViewNodeValueKind = TreeViewNodeValueKind.b1, // SolutionFolder
                            TraitsIndex = i,
                            IsExpandable = true,
                            IsExpanded = false,
                        };
                    }));

                NodeValueList.AddRange(DotNetSolutionModel.DotNetProjectList
                    .OrderBy(x => x.AbsolutePath.Name)
                    .Select((x, i) =>
                    {
                        return new TreeViewNodeValue
                        {
                            ParentIndex = -1,
                            IndexAmongSiblings = 0,
                            ChildListOffset = 0,
                            ChildListLength = 0,
                            TreeViewNodeValueKind = TreeViewNodeValueKind.b2, // .csproj
                            TraitsIndex = i,
                            IsExpandable = true,
                            IsExpanded = false,
                        };
                    }));

                nodeValue.ChildListLength = NodeValueList.Count - nodeValue.ChildListOffset;

                // The 'for loop' for `child.Parent` and the
                // 'for loop' for `child.RemoveRelatedFilesFromParent(...)`
                // cannot be combined.
                for (int i = nodeValue.ChildListOffset; i < nodeValue.ChildListOffset + nodeValue.ChildListLength; i++)
                {
                    var child = NodeValueList[i];
                    child.IndexAmongSiblings = i - nodeValue.ChildListOffset;
                    child.ParentIndex = indexNodeValue;
                    NodeValueList[i] = child;
                }

                // The 'for loop' for `child.Parent` and the
                // 'for loop' for `child.RemoveRelatedFilesFromParent(...)`
                // cannot be combined.
                /*for (int i = nodeValue.ChildListOffset; i < nodeValue.ChildListOffset + nodeValue.ChildListLength; i++)
                {
                    child.RemoveRelatedFilesFromParent(copyOfChildrenToFindRelatedFiles);
                }*/

                // The parent directory gets what is left over after the
                // children take their respective 'code behinds'
                return Task.CompletedTask;
            }
            default:
            {
                break;
            }
        }
        return Task.CompletedTask;
    }
    
    public override string GetDisplayText(int indexNodeValue)
    {
        var nodeValue = NodeValueList[indexNodeValue];
        switch (nodeValue.TreeViewNodeValueKind)
        {
            case TreeViewNodeValueKind.b0: // .sln
                return DotNetSolutionModel.AbsolutePath.Name;
            case TreeViewNodeValueKind.b1: // SolutionFolder
                return DotNetSolutionModel.SolutionFolderList[nodeValue.TraitsIndex].DisplayName;
            case TreeViewNodeValueKind.b2: // .csproj
                return DotNetSolutionModel.DotNetProjectList[nodeValue.TraitsIndex].AbsolutePath.Name;
            default:
                return "asdfg";
        }
    }

    public override IconKind GetIconKind(int indexNodeValue)
    {
        var nodeValue = NodeValueList[indexNodeValue];
        switch (nodeValue.TreeViewNodeValueKind)
        {
            case TreeViewNodeValueKind.b0: // .sln
                return IconKind.DotNetSolution;
            case TreeViewNodeValueKind.b1: // SolutionFolder
                return IconKind.DotNetSolutionFolder;
            case TreeViewNodeValueKind.b2: // .csproj
                return IconKind.CSharpProject;
            default:
                return IconKind.None;
        }
    }

    public override Task OnDoubleClickAsync(TreeViewCommandArgs commandArgs)
    {
        base.OnDoubleClickAsync(commandArgs);

        if (commandArgs.NodeThatReceivedMouseEvent.TreeViewNodeValueKind != TreeViewNodeValueKind.b4) // NamespacePath
            return Task.CompletedTask;

        IdeService.TextEditorService.WorkerArbitrary.PostUnique(async editContext =>
        {
            /*await IdeService.TextEditorService.OpenInEditorAsync(
                editContext,
                treeViewNamespacePath.Item.Value,
                true,
                null,
                new Category("main"),
                editContext.TextEditorService.NewViewModelKey());*/
        });
        return Task.CompletedTask;
    }
}
