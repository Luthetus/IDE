using Clair.Common.RazorLib;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.Common.RazorLib.Keys.Models;
using Clair.CompilerServices.DotNetSolution.Models;
using Clair.Ide.RazorLib.BackgroundTasks.Models;
using Clair.Ide.RazorLib.InputFiles.Models;
using Clair.Ide.RazorLib;

namespace Clair.Extensions.DotNet.DotNetSolutions.Models;

/// <summary>
/// TODO: 'Key<DotNetSolutionModel>? DotNetSolutionModelKey' should not be nullable use Key<DotNetSolutionModel>.Empty.
/// </summary>
public struct DotNetSolutionState
{
    public DotNetSolutionModel? DotNetSolutionModel { get; init; }

    public static readonly Key<TreeViewContainer> TreeViewSolutionExplorerStateKey = Key<TreeViewContainer>.NewKey();

    public static void ShowInputFile(
        IdeService ideService,
        DotNetService dotNetService)
    {
        ideService.Enqueue(new IdeWorkArgs
        {
            WorkKind = IdeWorkKind.RequestInputFileStateForm,
            StringValue = "Solution Explorer",
            OnAfterSubmitFunc = absolutePath =>
            {
                if (absolutePath.Value is not null)
                    dotNetService.Enqueue(new DotNetWorkArgs
                    {
                        WorkKind = DotNetWorkKind.SetDotNetSolution,
                        DotNetSolutionAbsolutePath = absolutePath,
                    });

                return Task.CompletedTask;
            },
            SelectionIsValidFunc = absolutePath =>
            {
                if (absolutePath.Value is null || absolutePath.IsDirectory)
                    return Task.FromResult(false);

                return Task.FromResult(
                    absolutePath.Name.EndsWith(CommonFacts.DOT_NET_SOLUTION) ||
                    absolutePath.Name.EndsWith(CommonFacts.DOT_NET_SOLUTION_X));
            },
            InputFilePatterns = new()
            {
                new InputFilePattern(
                    ".NET Solution",
                    absolutePath => absolutePath.Name.EndsWith(CommonFacts.DOT_NET_SOLUTION) ||
                                    absolutePath.Name.EndsWith(CommonFacts.DOT_NET_SOLUTION_X))
            }
        });
    }
}
