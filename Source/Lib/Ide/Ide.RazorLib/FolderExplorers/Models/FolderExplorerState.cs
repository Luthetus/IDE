using Clair.Common.RazorLib.FileSystems.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.Common.RazorLib.Keys.Models;

namespace Clair.Ide.RazorLib.FolderExplorers.Models;

public record struct FolderExplorerState(AbsolutePath? AbsolutePath)
{
    public static readonly Key<TreeViewContainer> TreeViewContentStateKey = Key<TreeViewContainer>.NewKey();

    public FolderExplorerState() : this(default)
    {
    }
}
