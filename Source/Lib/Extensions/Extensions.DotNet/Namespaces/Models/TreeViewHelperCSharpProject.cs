using System.Text;
using Clair.Common.RazorLib;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.Common.RazorLib.FileSystems.Models;
using Clair.CompilerServices.DotNetSolution.Models.Project;
using Clair.Extensions.DotNet.CSharpProjects.Models;
using Clair.Ide.RazorLib;

namespace Clair.Extensions.DotNet.Namespaces.Models;

public class TreeViewHelperCSharpProject
{
    public static Task<List<TreeViewNoType>> LoadChildrenAsync(TreeViewNamespacePath cSharpProjectTreeView)
    {
        var directoryAbsolutePathString = cSharpProjectTreeView.Item.CreateSubstringParentDirectory();
        if (directoryAbsolutePathString is null)
            return Task.FromResult(new List<TreeViewNoType>());

        var directoryList = cSharpProjectTreeView.CommonService.FileSystemProvider.Directory.GetDirectories(directoryAbsolutePathString);
        var fileList = cSharpProjectTreeView.CommonService.FileSystemProvider.Directory.GetFiles(directoryAbsolutePathString);

        var tokenBuilder = new StringBuilder();
        var formattedBuilder = new StringBuilder();

        var childDirectoryTreeViewModelsList = directoryList
            .Where(x => !IdeFacts.IsHiddenFileByContainerFileExtension(CommonFacts.C_SHARP_PROJECT, x))
            .OrderBy(pathString => pathString)
            .Select(x =>
                new TreeViewNamespacePath(
                    new AbsolutePath(
                        x, true, cSharpProjectTreeView.CommonService.FileSystemProvider, tokenBuilder, formattedBuilder, AbsolutePathNameKind.NameNoExtension),
                    cSharpProjectTreeView.CommonService,
                    true,
                    false));

        var foundUniqueDirectories = new List<TreeViewNamespacePath>();
        var foundDefaultDirectories = new List<TreeViewNamespacePath>();

        foreach (var directoryTreeViewModel in childDirectoryTreeViewModelsList)
        {
            if (IdeFacts.IsUniqueFileByContainerFileExtension(CommonFacts.C_SHARP_PROJECT, directoryTreeViewModel.Item.Name))
                foundUniqueDirectories.Add(directoryTreeViewModel);
            else
                foundDefaultDirectories.Add(directoryTreeViewModel);
        }

        var cSharpProjectDependenciesTreeViewNode = new TreeViewCSharpProjectDependencies(
            new CSharpProjectDependencies(cSharpProjectTreeView.Item),
            cSharpProjectTreeView.CommonService,
            true,
            false);

        // file system list vs the filtered list has a negligible length difference vs the cost of enumerating filtered list to get length / internal reallocations of list.
        var result = new List<TreeViewNoType>(capacity: directoryList.Length + fileList.Length)
        {
            cSharpProjectDependenciesTreeViewNode
        };
        result.AddRange(foundUniqueDirectories);
        result.AddRange(foundDefaultDirectories);
        result.AddRange(
            fileList
                .Where(x => !x.EndsWith(CommonFacts.C_SHARP_PROJECT))
                .OrderBy(pathString => pathString)
                .Select(x =>
                {
                    var absolutePath = new AbsolutePath(x, false, cSharpProjectTreeView.CommonService.FileSystemProvider, tokenBuilder, formattedBuilder, AbsolutePathNameKind.NameWithExtension);

                    return (TreeViewNoType)new TreeViewNamespacePath(
                        absolutePath,
                        cSharpProjectTreeView.CommonService,
                        false,
                        false);
                }));
        
        return Task.FromResult(result);
    }
}
