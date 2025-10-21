using Clair.Common.RazorLib;
using Clair.Common.RazorLib.TreeViews.Models;

namespace Clair.Extensions.DotNet.TestExplorers.Models;

public interface ITestExplorerRenderBatch
{
    public TestExplorerState TestExplorerState { get; }
    public AppOptionsState AppOptionsState { get; }
    public TreeViewContainer? TreeViewContainer { get; }
}
