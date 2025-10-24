using Clair.Common.RazorLib.TreeViews.Models;

namespace Clair.Ide.RazorLib.FileSystems.Models;

public class TreeViewHelperAbsolutePathDirectory
{
    public static Task<List<TreeViewNodeValue>> LoadChildrenAsync(TreeViewNodeValue directoryTreeView)
    {
        /*
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
                    return (TreeViewNoType)new TreeViewAbsolutePath(
                        new AbsolutePath(x, true, directoryTreeView.CommonService.FileSystemProvider, tokenBuilder, formattedBuilder, AbsolutePathNameKind.NameWithExtension),
                        directoryTreeView.CommonService,
                        true,
                        false);
                }));
        list.AddRange(
            fileList
                .OrderBy(pathString => pathString)
                .Select(x =>
                {
                    return (TreeViewNoType)new TreeViewAbsolutePath(
                        new AbsolutePath(x, false, directoryTreeView.CommonService.FileSystemProvider, tokenBuilder, formattedBuilder, AbsolutePathNameKind.NameWithExtension),
                        directoryTreeView.CommonService,
                        false,
                        false);
                }));

        return Task.FromResult(list);
        */
        return Task.FromResult(new List<TreeViewNodeValue>());
    }
}
