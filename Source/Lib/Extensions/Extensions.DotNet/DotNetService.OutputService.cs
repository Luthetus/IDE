using Clair.Common.RazorLib.Reactives.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.Extensions.DotNet.Outputs.Models;
using Clair.Extensions.DotNet.CommandLines.Models;

namespace Clair.Extensions.DotNet;

public partial class DotNetService
{
    private readonly Throttle _throttleCreateTreeView = new Throttle(TimeSpan.FromMilliseconds(333));

    private OutputState _outputState = new();

    public OutputState GetOutputState() => _outputState;

    public void ReduceStateHasChangedAction(Guid dotNetRunParseResultId)
    {
        var inState = GetOutputState();

        _outputState = inState with
        {
            DotNetRunParseResultId = dotNetRunParseResultId
        };

        DotNetStateChanged?.Invoke(DotNetStateChangedKind.OutputStateChanged);
        return;
    }

    public Task HandleConstructTreeViewEffect()
    {
        _throttleCreateTreeView.Run(async _ => await OutputService_Do_ConstructTreeView());
        return Task.CompletedTask;
    }

    public async ValueTask OutputService_Do_ConstructTreeView()
    {
        var dotNetRunParseResult = GetDotNetRunParseResult();
            
        CommonService.TreeView_DisposeContainerAction(OutputState.TreeViewContainerKey, shouldFireStateChangedEvent: false);
            
        var treeViewContainer = new OutputTreeViewContainer(this, dotNetRunParseResult);
        
        var rootNode = new TreeViewNodeValue
        {
            ParentIndex = -1,
            IndexAmongSiblings = 0,
            ChildListOffset = 1,
            ChildListLength = 0,
            ByteKind = OutputTreeViewContainer.ByteKind_Aaa,
            TraitsIndex = 0,
            IsExpandable = true,
            IsExpanded = true
        };
        treeViewContainer.NodeValueList.Add(rootNode);
        
        var indexAmongSiblings = 0;
        
        for (int i = 0; i < dotNetRunParseResult.AllDiagnosticLineList.Count; i++)
        {
            var diagnostic = dotNetRunParseResult.AllDiagnosticLineList[i];
            if (diagnostic.DiagnosticLineKind == DiagnosticLineKind.Error)
            {
                treeViewContainer.NodeValueList.Add(new TreeViewNodeValue
                {
                    ParentIndex = 0,
                    IndexAmongSiblings = indexAmongSiblings++,
                    ChildListOffset = treeViewContainer.NodeValueList.Count,
                    ChildListLength = 0,
                    ByteKind = OutputTreeViewContainer.ByteKind_Bbb,
                    TraitsIndex = i,
                    IsExpandable = false,
                    IsExpanded = false
                });
            }
        }
        
        treeViewContainer.NodeValueList[0] = treeViewContainer.NodeValueList[0] with
        {
            ChildListLength = treeViewContainer.NodeValueList.Count - 1
        };

        CommonService.TreeView_RegisterContainerAction(treeViewContainer);
    
        ReduceStateHasChangedAction(dotNetRunParseResult.Id);
    }
}
