using Clair.Common.RazorLib;
using Clair.Common.RazorLib.TreeViews.Models;

namespace Clair.Extensions.DotNet.TestExplorers.Models;

public class TestExplorerRenderBatch : ITestExplorerRenderBatch
{
    public TestExplorerRenderBatch(
        TestExplorerState testExplorerState,
        AppOptionsState appOptionsState,
        TreeViewContainer? treeViewContainer)
    {
        TestExplorerState = testExplorerState;
        AppOptionsState = appOptionsState;
        TreeViewContainer = treeViewContainer;
    }

    public TestExplorerState TestExplorerState { get; }
    public AppOptionsState AppOptionsState { get; }
    public TreeViewContainer? TreeViewContainer { get; }
}
