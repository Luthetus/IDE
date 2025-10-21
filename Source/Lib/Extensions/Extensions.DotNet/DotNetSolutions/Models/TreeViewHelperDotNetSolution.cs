using Clair.Common.RazorLib.TreeViews.Models;
using Clair.CompilerServices.DotNetSolution.Models.Project;
using Clair.Extensions.DotNet.Namespaces.Models;

namespace Clair.Extensions.DotNet.DotNetSolutions.Models;

public class TreeViewHelperDotNetSolution
{
    public static Task<List<TreeViewNoType>> LoadChildrenAsync(TreeViewSolution treeViewSolution)
    {
        var childSolutionFolders = treeViewSolution.Item.SolutionFolderList.Select(
            x => (TreeViewNoType)new TreeViewSolutionFolder(
                x,
                treeViewSolution.CommonService,
                true,
                false))
            .OrderBy(x => ((TreeViewSolutionFolder)x).Item.DisplayName)
            .ToList();

        var childProjects = treeViewSolution.Item.DotNetProjectList
            .Where(x => x.ProjectTypeGuid != SolutionFolder.SolutionFolderProjectTypeGuid)
            .Select(x =>
            {
                return (TreeViewNoType)new TreeViewNamespacePath(
                    x.AbsolutePath,
                    treeViewSolution.CommonService,
                    true,
                    false);
            })
            .OrderBy(x => ((TreeViewNamespacePath)x).Item.Name)
            .ToList();

        var children = childSolutionFolders.Concat(childProjects).ToList();

        var copyOfChildrenToFindRelatedFiles = new List<TreeViewNoType>(children);

        // The foreach for child.Parent and the
        // foreach for child.RemoveRelatedFilesFromParent(...)
        // cannot be combined.
        foreach (var child in children)
        {
            child.Parent = treeViewSolution;
        }

        // The foreach for child.Parent and the
        // foreach for child.RemoveRelatedFilesFromParent(...)
        // cannot be combined.
        foreach (var child in children)
        {
            child.RemoveRelatedFilesFromParent(copyOfChildrenToFindRelatedFiles);
        }

        // The parent directory gets what is left over after the
        // children take their respective 'code behinds'
        return Task.FromResult(copyOfChildrenToFindRelatedFiles);
    }
}
