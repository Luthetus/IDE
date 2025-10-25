using Clair.Common.RazorLib.Reactives.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.Extensions.DotNet.Outputs.Models;

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
            ChildListOffset = treeViewContainer.NodeValueList.Count,
            ChildListLength = 0,
            ByteKind = OutputTreeViewContainer.ByteKind_Aaa,
            TraitsIndex = 0,
            IsExpandable = true,
            IsExpanded = true
        };
        treeViewContainer.NodeValueList.Add(rootNode);

        await treeViewContainer.LoadChildListAsync(indexNodeValue: 0).ConfigureAwait(false);

        CommonService.TreeView_RegisterContainerAction(treeViewContainer);
    
    
    
        /*var flatListVersion = CommonService.TreeView_GetNextFlatListVersion(OutputState.TreeViewContainerKey);
        CommonService.TreeView_DisposeContainerAction(OutputState.TreeViewContainerKey);
        
        CommonService.TreeView_RegisterContainerAction(new TreeViewContainer(
                OutputState.TreeViewContainerKey,
                rootNode: null,
                selectedNodeList: Array.Empty<TreeViewNodeValue>())
            {
                FlatListVersion = flatListVersion
            });
    
        if (CommonService.TryGetTreeViewContainer(OutputState.TreeViewContainerKey, out var treeViewContainer))
        {
            var dotNetRunParseResult = GetDotNetRunParseResult();
    
            var treeViewNodeList = dotNetRunParseResult.AllDiagnosticLineList.Select(x =>
                (TreeViewNoType)new TreeViewDiagnosticLine(
                    x,
                    false,
                    false))
                .ToArray();
    
            var filePathGrouping = treeViewNodeList.GroupBy(
                x => ((TreeViewDiagnosticLine)x).Item.FilePathTextSpan.Text);
    
            var projectManualGrouping = new Dictionary<string, (TreeViewGroup TreeViewGroup, List<TreeViewNoType> ToBeChildList)>();
            var treeViewBadStateGroupList = new List<TreeViewNoType>();
    
            var tokenBuilder = new StringBuilder();
            var formattedBuilder = new StringBuilder();
    
            foreach (var group in filePathGrouping)
            {
                var absolutePath = new AbsolutePath(group.Key, false, CommonService.FileSystemProvider, tokenBuilder, formattedBuilder, AbsolutePathNameKind.NameWithExtension);
                var groupEnumerated = group.ToList();
                var groupNameBuilder = new StringBuilder();
    
                var errorCount = groupEnumerated.Count(x =>
                    ((TreeViewDiagnosticLine)x).Item.DiagnosticLineKind == DiagnosticLineKind.Error);
    
                var warningCount = groupEnumerated.Count(x =>
                    ((TreeViewDiagnosticLine)x).Item.DiagnosticLineKind == DiagnosticLineKind.Warning);
    
                groupNameBuilder
                    .Append(absolutePath.Name)
                    .Append(" (")
                    .Append(errorCount)
                    .Append(" errors)")
                    .Append(" (")
                    .Append(warningCount)
                    .Append(" warnings)");
    
                var titleText = absolutePath.CreateSubstringParentDirectory() ?? $"{nameof(absolutePath.CreateSubstringParentDirectory)} was null.";
                
                var treeViewGroup = new TreeViewGroup(
                    groupNameBuilder.ToString(),
                    true,
                    groupEnumerated.Any(x => ((TreeViewDiagnosticLine)x).Item.DiagnosticLineKind == DiagnosticLineKind.Error))
                {
                    TitleText = titleText
                };
    
                treeViewGroup.ChildListLength = 0;
                treeViewGroup.ChildListOffset = treeViewContainer.ChildList.Count;
                var newChildList = groupEnumerated;
                foreach (var child in newChildList)
                {
                    treeViewContainer.ChildList.Add(child);
                }
                treeViewGroup.ChildListLength = newChildList.Count;
                treeViewGroup.LinkChildrenNoMap(newChildList, treeViewContainer);
                
                var firstEntry = groupEnumerated.FirstOrDefault();
    
                if (firstEntry is not null)
                {
                    var projectText = ((TreeViewDiagnosticLine)firstEntry).Item.ProjectTextSpan.Text;
                    var projectAbsolutePath = new AbsolutePath(projectText, false, CommonService.FileSystemProvider, tokenBuilder, formattedBuilder, AbsolutePathNameKind.NameWithExtension);
    
                    if (!projectManualGrouping.ContainsKey(projectText))
                    {
                        var treeViewGroupProject = new TreeViewGroup(
                            projectAbsolutePath.Name,
                            true,
                            true)
                        {
                            TitleText = absolutePath.CreateSubstringParentDirectory() ?? $"{nameof(AbsolutePath.CreateSubstringParentDirectory)} was null"
                        };
    
                        projectManualGrouping.Add(projectText, (treeViewGroupProject, new()));
                    }
    
                    projectManualGrouping[projectText].ToBeChildList.Add(treeViewGroup);
                }
                else
                {
                    treeViewBadStateGroupList.Add(treeViewGroup);
                }
            }
    
            var treeViewProjectGroupList = projectManualGrouping.Values
                .Select(tuple =>
                {
                    tuple.TreeViewGroup.ChildListLength = 0;
                    tuple.TreeViewGroup.ChildListOffset = treeViewContainer.ChildList.Count;
                    var newChildList = tuple.ToBeChildList;
                    foreach (var child in newChildList)
                    {
                        treeViewContainer.ChildList.Add(child);
                    }
                    tuple.TreeViewGroup.ChildListLength = newChildList.Count;
                    tuple.TreeViewGroup.LinkChildrenNoMap(newChildList, treeViewContainer);
                    return tuple.TreeViewGroup;
                })
                .ToList();
    
            // Bad State
            if (treeViewBadStateGroupList.Count != 0)
            {
                var projectText = "Could not find project";
    
                var treeViewGroupProjectBadState = new TreeViewGroup(
                    projectText,
                    true,
                    true)
                {
                    TitleText = projectText
                };
    
                //treeViewGroupProjectBadState.ChildList = treeViewBadStateGroupList;
    
                treeViewProjectGroupList.Add(treeViewGroupProjectBadState);
            }
    
            foreach (var treeViewProjectGroup in treeViewProjectGroupList)
            {
                //treeViewProjectGroup.LinkChildrenNoMap(treeViewProjectGroup.ChildList);
            }
    
            var adhocRoot = TreeViewAdhoc.ConstructTreeViewAdhoc(treeViewContainer, treeViewProjectGroupList.ToArray());
            var firstNode = treeViewNodeList.FirstOrDefault();
    
            var activeNodes = firstNode is null
                ? new List<TreeViewNoType>()
                : new() { firstNode };
    
            CommonService.TreeView_WithRootNodeAction(OutputState.TreeViewContainerKey, adhocRoot);

            CommonService.TreeView_SetActiveNodeAction(
                OutputState.TreeViewContainerKey,
                firstNode,
                true,
                false);
        
            ReduceStateHasChangedAction(dotNetRunParseResult.Id);
        }*/
        //return ValueTask.CompletedTask;
    }
}
