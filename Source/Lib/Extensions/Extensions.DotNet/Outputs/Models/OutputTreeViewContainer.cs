using Clair.Common.RazorLib;
using Clair.Common.RazorLib.Icons.Displays;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.Common.RazorLib.Commands.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models;
using Clair.Extensions.DotNet.CommandLines.Models;

namespace Clair.Extensions.DotNet.Outputs.Models;

public class OutputTreeViewContainer : TreeViewContainer
{
    public OutputTreeViewContainer(DotNetService dotNetService, DotNetRunParseResult dotNetRunParseResult)
        : base(dotNetService.CommonService)
    {
        Key = OutputState.TreeViewContainerKey;
        NodeValueList = new();
        DotNetService = dotNetService;
        DotNetRunParseResult = dotNetRunParseResult;
    }
    
    public override Key<TreeViewContainer> Key { get; init; }
    public override List<TreeViewNodeValue> NodeValueList { get; }

    public DotNetService DotNetService { get; }
    public DotNetRunParseResult DotNetRunParseResult { get; }

    public override Task LoadChildListAsync(int indexNodeValue)
    {
        var nodeValue = NodeValueList[indexNodeValue];
        nodeValue.ChildListLength = 0;
        nodeValue.ChildListOffset = NodeValueList.Count;

        switch (nodeValue.ByteKind)
        {
            case OutputTreeViewContainer.ByteKind_Aaa:
            {
                var indexAmongSiblings = 0;
            
                /*NodeValueList.AddRange(DotNetSolutionModel.SolutionFolderList
                    .Select((x, i) => (x, i))
                    .OrderBy(x => x.x.DisplayName)
                    .Select(tuple =>
                    {
                        return new TreeViewNodeValue
                        {
                            ParentIndex = indexNodeValue,
                            IndexAmongSiblings = indexAmongSiblings++,
                            ChildListOffset = 0,
                            ChildListLength = 0,
                            ByteKind = OutputTreeViewContainer.ByteKind_SolutionFolder,
                            TraitsIndex = tuple.i,
                            IsExpandable = true,
                            IsExpanded = false,
                        };
                    }));

                NodeValueList.AddRange(DotNetSolutionModel.DotNetProjectList
                    .Select((x, i) => (x, i))
                    .OrderBy(x => x.x.AbsolutePath.Name)
                    .Select(tuple =>
                    {
                        return new TreeViewNodeValue
                        {
                            ParentIndex = indexNodeValue,
                            IndexAmongSiblings = indexAmongSiblings++,
                            ChildListOffset = 0,
                            ChildListLength = 0,
                            ByteKind = OutputTreeViewContainer.ByteKind_Csproj,
                            TraitsIndex = tuple.i,
                            IsExpandable = true,
                            IsExpanded = false,
                        };
                    }));*/

                nodeValue.ChildListLength = NodeValueList.Count - nodeValue.ChildListOffset;

                NodeValueList[indexNodeValue] = nodeValue;
                return Task.CompletedTask;
            }
            default:
            {
                break;
            }
        }
        return Task.CompletedTask;
    }
    
    public override string GetDisplayText(int indexNodeValue)
    {
        var nodeValue = NodeValueList[indexNodeValue];
        switch (nodeValue.ByteKind)
        {
            case OutputTreeViewContainer.ByteKind_Aaa:
                return nameof(ByteKind_Aaa);
            case OutputTreeViewContainer.ByteKind_Diagnostic:
                var diagnostic = DotNetRunParseResult.AllDiagnosticLineList[nodeValue.TraitsIndex];
                return diagnostic.TextShort;
            default:
                return "asdfg";
        }
    }

    public override IconKind GetIconKind(int indexNodeValue)
    {
        var nodeValue = NodeValueList[indexNodeValue];
        switch (nodeValue.ByteKind)
        {
            case OutputTreeViewContainer.ByteKind_Aaa:
                return IconKind.None;
            default:
                return IconKind.None;
        }
    }

    public override Task OnDoubleClickAsync(TreeViewCommandArgs commandArgs, int indexNodeValue)
    {
        base.OnDoubleClickAsync(commandArgs, indexNodeValue);
        
        var nodeValue = NodeValueList[indexNodeValue];
        if (nodeValue.ByteKind != ByteKind_Diagnostic)
            return Task.CompletedTask;
        
        return OutputTextSpanHelper.OpenInEditorOnClick(
            DotNetRunParseResult.AllDiagnosticLineList[nodeValue.TraitsIndex],
            true,
            DotNetService.TextEditorService);
        
        /*string? path = null;

        var nodeValue = NodeValueList[indexNodeValue];
        switch (nodeValue.ByteKind)
        {
            case OutputTreeViewContainer.ByteKind_Aaa:
                path = string.Empty;
                return Task.CompletedTask;
            default:
                return Task.CompletedTask;
        }
        
        if (path is null)
            return Task.CompletedTask;

        DotNetService.TextEditorService.WorkerArbitrary.PostUnique(async editContext =>
        {
            await DotNetService.TextEditorService.OpenInEditorAsync(
                editContext,
                path,
                true,
                null,
                new Category("main"),
                editContext.TextEditorService.NewViewModelKey());
        });
        return Task.CompletedTask;*/
    }
    
    public override Task OnKeyDownAsync(TreeViewCommandArgs commandArgs, int indexNodeValue)
    {
        if (commandArgs.KeyboardEventArgs is null)
            return Task.CompletedTask;

        base.OnKeyDownAsync(commandArgs, indexNodeValue);

        switch (commandArgs.KeyboardEventArgs.Code)
        {
            case CommonFacts.ENTER_CODE:
            {
                string? path = null;

                var nodeValue = NodeValueList[indexNodeValue];
                switch (nodeValue.ByteKind)
                {
                    case OutputTreeViewContainer.ByteKind_Aaa:
                        path = string.Empty;
                        return Task.CompletedTask;
                    default:
                        return Task.CompletedTask;
                }
                
                if (path is null)
                    return Task.CompletedTask;
        
                DotNetService.TextEditorService.WorkerArbitrary.PostUnique(async editContext =>
                {
                    await DotNetService.TextEditorService.OpenInEditorAsync(
                        editContext,
                        path,
                        true,
                        null,
                        new Category("main"),
                        editContext.TextEditorService.NewViewModelKey());
                });
                return Task.CompletedTask;
            }
            case CommonFacts.SPACE_CODE:
            {
                string? path = null;

                var nodeValue = NodeValueList[indexNodeValue];
                switch (nodeValue.ByteKind)
                {
                    case OutputTreeViewContainer.ByteKind_Aaa:
                        path = string.Empty;
                        return Task.CompletedTask;
                    default:
                        return Task.CompletedTask;
                }
                
                if (path is null)
                    return Task.CompletedTask;
        
                DotNetService.TextEditorService.WorkerArbitrary.PostUnique(async editContext =>
                {
                    await DotNetService.TextEditorService.OpenInEditorAsync(
                        editContext,
                        path,
                        true,
                        null,
                        new Category("main"),
                        editContext.TextEditorService.NewViewModelKey());
                });
                return Task.CompletedTask;
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// If the context menu event occurred due to a "RightClick" event then
    /// <br/>
    /// <see cref="LeftPositionInPixels"/> == <see cref="MouseEventArgs.ClientX"/>
    /// <br/>
    /// <see cref="TopPositionInPixels"/> == <see cref="MouseEventArgs.ClientY"/>
    /// <br/><br/>
    /// If the context menu event occurred due to a "keyboard" event then
    /// <br/>
    /// <see cref="LeftPositionInPixels"/> == element.getBoundingClientRect().left
    /// <br/>
    /// <see cref="TopPositionInPixels"/> == element.getBoundingClientRect().top + element.getBoundingClientRect().height
    /// </summary>
    public override Task OnContextMenuAsync(
        int indexNodeValue,
        bool occurredDueToMouseEvent,
        double leftPositionInPixels,
        double topPositionInPixels)
    {
        /*
        var dropdownRecord = new DropdownRecord(
            SolutionExplorerContextMenu.ContextMenuEventDropdownKey,
            leftPositionInPixels,
            topPositionInPixels,
            typeof(SolutionExplorerContextMenu),
            new Dictionary<string, object?>
            {
                {
                    nameof(SolutionExplorerContextMenu.SolutionExplorerContextMenuData),
                    new SolutionExplorerContextMenuData(
                        this,
                        indexNodeValue,
                        occurredDueToMouseEvent,
                        leftPositionInPixels,
                        topPositionInPixels)
                }
            },
            null);

        CommonService.Dropdown_ReduceRegisterAction(dropdownRecord);
        */
        return Task.CompletedTask;
    }
    
    public const byte ByteKind_Aaa = 1;
    public const byte ByteKind_Diagnostic = 2;
}
