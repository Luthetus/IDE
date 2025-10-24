using Microsoft.AspNetCore.Components;
using Clair.Common.RazorLib.Reactives.Models;
using Clair.Common.RazorLib.Commands.Models;
using Clair.Common.RazorLib.BackgroundTasks.Models;
using Clair.Common.RazorLib.Dropdowns.Models;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.TreeViews.Models;

namespace Clair.Extensions.DotNet.Outputs.Displays.Internals;

public sealed partial class OutputDisplay : ComponentBase, IDisposable
{
    [Inject]
    private DotNetService DotNetService { get; set; } = null!;
    
    private readonly Throttle _eventThrottle = new Throttle(TimeSpan.FromMilliseconds(333));
    
    private TreeViewContainerParameter _treeViewContainerParameter;

    protected override void OnInitialized()
    {
        DotNetService.DotNetStateChanged += DotNetCliOutputParser_StateChanged;
        
        if (DotNetService.GetOutputState().DotNetRunParseResultId != DotNetService.GetDotNetRunParseResult().Id)
            DotNetCliOutputParser_StateChanged(DotNetStateChangedKind.CliOutputParserStateChanged);
    }
    
    public async void DotNetCliOutputParser_StateChanged(DotNetStateChangedKind dotNetStateChangedKind)
    {
        if (dotNetStateChangedKind == DotNetStateChangedKind.OutputStateChanged)
        {
            await InvokeAsync(StateHasChanged);
        }
        else if (dotNetStateChangedKind == DotNetStateChangedKind.CliOutputParserStateChanged)
        {
            _eventThrottle.Run((Func<CancellationToken, Task>)(_ =>
            {
                if (DotNetService.GetOutputState().DotNetRunParseResultId == DotNetService.GetDotNetRunParseResult().Id)
                    return Task.CompletedTask;
    
                DotNetService.TextEditorService.CommonService.Continuous_Enqueue((IBackgroundTaskGroup)new BackgroundTask(
                    Key<IBackgroundTaskGroup>.Empty,
                    DotNetService.OutputService_Do_ConstructTreeView));
                return Task.CompletedTask;
            }));
        }
    }
    
    private Task OnTreeViewContextMenuFunc(TreeViewCommandArgs treeViewCommandArgs)
    {
        var dropdownRecord = new DropdownRecord(
            OutputContextMenu.ContextMenuEventDropdownKey,
            treeViewCommandArgs.ContextMenuFixedPosition.LeftPositionInPixels,
            treeViewCommandArgs.ContextMenuFixedPosition.TopPositionInPixels,
            typeof(OutputContextMenu),
            new Dictionary<string, object?>
            {
                {
                    nameof(OutputContextMenu.TreeViewCommandArgs),
                    treeViewCommandArgs
                }
            },
            restoreFocusOnClose: null);

        DotNetService.TextEditorService.CommonService.Dropdown_ReduceRegisterAction(dropdownRecord);
        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
        DotNetService.DotNetStateChanged -= DotNetCliOutputParser_StateChanged;
    }
}
