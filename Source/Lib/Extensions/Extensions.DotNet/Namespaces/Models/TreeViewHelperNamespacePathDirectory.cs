using System.Text;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.Common.RazorLib.FileSystems.Models;
using Clair.Extensions.DotNet.Namespaces.Models;

namespace Clair.Ide.RazorLib.Namespaces.Models;

public class TreeViewHelperNamespacePathDirectory
{
    /// <summary>Used with <see cref="TreeViewNamespacePath"/></summary>
    public static Task<List<TreeViewNoType>> LoadChildrenAsync(TreeViewNamespacePath directoryTreeView)
    {
        var directoryAbsolutePathString = directoryTreeView.Item.Value;

        var directoryList = directoryTreeView.CommonService.FileSystemProvider.Directory.GetDirectories(directoryAbsolutePathString);
        var fileList = directoryTreeView.CommonService.FileSystemProvider.Directory.GetFiles(directoryAbsolutePathString);

        var tokenBuilder = new StringBuilder();
        var formattedBuilder = new StringBuilder();

        var list = new List<TreeViewNoType>(capacity: directoryList.Length + fileList.Length);
        list.AddRange(
            directoryList
                .OrderBy(pathString => pathString)
                .Select(x =>
                {
                    var absolutePath = new AbsolutePath(x, true, directoryTreeView.CommonService.FileSystemProvider, tokenBuilder, formattedBuilder, AbsolutePathNameKind.NameNoExtension);

                    return (TreeViewNoType)new TreeViewNamespacePath(
                        absolutePath,
                        directoryTreeView.CommonService,
                        true,
                        false);
                }));
        list.AddRange(
            fileList
                .OrderBy(pathString => pathString)
                .Select(x =>
                {
                    var absolutePath = new AbsolutePath(x, false, directoryTreeView.CommonService.FileSystemProvider, tokenBuilder, formattedBuilder, AbsolutePathNameKind.NameWithExtension);

                    return (TreeViewNoType)new TreeViewNamespacePath(
                        absolutePath,
                        directoryTreeView.CommonService,
                        false,
                        false);
                }));

        return Task.FromResult(list);
    }
}
