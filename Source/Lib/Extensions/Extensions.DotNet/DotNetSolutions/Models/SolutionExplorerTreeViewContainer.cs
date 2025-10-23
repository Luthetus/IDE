using Clair.Common.RazorLib;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.CompilerServices.DotNetSolution.Models;
using Clair.CompilerServices.DotNetSolution.Models.Project;

namespace Clair.Extensions.DotNet.DotNetSolutions.Models;

public class SolutionExplorerTreeViewContainer : TreeViewContainer
{
    // DotNetSolutionState.TreeViewSolutionExplorerStateKey
    
    public SolutionExplorerTreeViewContainer(CommonService commonService, DotNetSolutionModel dotNetSolutionModel)
        : base(commonService)
    {
        Key = DotNetSolutionState.TreeViewSolutionExplorerStateKey;
        NodeValueList = new();
        DotNetSolutionModel = dotNetSolutionModel;
    }
    
    public override Key<TreeViewContainer> Key { get; init; }
    public override List<TreeViewNodeValue> NodeValueList { get; }
    public DotNetSolutionModel DotNetSolutionModel { get; }

    public override Task LoadChildListAsync(int indexNodeValue)
    {
        var nodeValue = NodeValueList[indexNodeValue];
        nodeValue.ChildListLength = 0;
        nodeValue.ChildListOffset = NodeValueList.Count;

        switch (nodeValue.TreeViewNodeValueKind)
        {
            case TreeViewNodeValueKind.b0:
            {
                var childSolutionFolders = DotNetSolutionModel.SolutionFolderList
                    .OrderBy(x => x.DisplayName)
                    .Select(x =>
                    {
                        var z = new TreeViewNodeValue(
                            x,
                            CommonService,
                            true,
                            false);
                        return z;
                    });
                NodeValueList.AddRange(childSolutionFolders);

                var childProjects = DotNetSolutionModel.DotNetProjectList
                    .Where(x => x.ProjectTypeGuid != SolutionFolder.SolutionFolderProjectTypeGuid)
                    .OrderBy(x => x.AbsolutePath.Name)
                    .Select(x =>
                    {
                        var z = new TreeViewNodeValue(
                            x.AbsolutePath,
                            CommonService,
                            true,
                            false);
                        return z;
                    })
                    .ToList();
                NodeValueList.AddRange(childProjects);

                nodeValue.ChildListLength = NodeValueList.Count - nodeValue.ChildListOffset;

                // The 'for loop' for `child.Parent` and the
                // 'for loop' for `child.RemoveRelatedFilesFromParent(...)`
                // cannot be combined.
                for (int i = nodeValue.ChildListOffset; i < nodeValue.ChildListOffset + nodeValue.ChildListLength; i++)
                {
                    var child = NodeValueList[i];
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
        if (nodeValue.TreeViewNodeValueKind == TreeViewNodeValueKind.b0)
        {
            return ".NET Solution";
        }
        return "asdfg";
    }
}
