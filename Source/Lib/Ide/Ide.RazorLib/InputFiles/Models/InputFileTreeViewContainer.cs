using System.Text;
using Clair.Common.RazorLib;
using Clair.Common.RazorLib.Commands.Models;
using Clair.Common.RazorLib.Dropdowns.Models;
using Clair.Common.RazorLib.FileSystems.Models;
using Clair.Common.RazorLib.Icons.Displays;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models;
using Clair.Ide.RazorLib;
using Clair.Ide.RazorLib.InputFiles.Displays;

namespace Clair.Ide.RazorLib.InputFiles.Models;

public class InputFileTreeViewContainer : TreeViewContainer
{
    public InputFileTreeViewContainer(IdeService ideService)
        : base(ideService.CommonService)
    {
        Key = InputFileDisplay.InputFileSidebar_TreeViewContainerKey;
        NodeValueList = new();
        IdeService = ideService;
    }
    
    public override Key<TreeViewContainer> Key { get; init; }
    public override List<TreeViewNodeValue> NodeValueList { get; }

    public IdeService IdeService { get; }
    public List<AbsolutePath> DirectoryTraitsList { get; set; } = new();
    public List<AbsolutePath> FileTraitsList { get; set; } = new();

    public override Task LoadChildListAsync(int indexNodeValue)
    {
        var nodeValue = NodeValueList[indexNodeValue];
        nodeValue.ChildListLength = 0;
        nodeValue.ChildListOffset = NodeValueList.Count;

        switch (nodeValue.ByteKind)
        {
            case InputFileTreeViewContainer.ByteKind_Dir:
            {
                var indexAmongSiblings = 0;
                
                var directoryAbsolutePath = DirectoryTraitsList[nodeValue.TraitsIndex];

                var directoryList = CommonService.FileSystemProvider.Directory.GetDirectories(directoryAbsolutePath.Value);
                var fileList = CommonService.FileSystemProvider.Directory.GetFiles(directoryAbsolutePath.Value);

                var tokenBuilder = new StringBuilder();
                var formattedBuilder = new StringBuilder();

                var childDirectoryNamespacePathList = directoryList
                    .OrderBy(pathString => pathString)
                    .Select(x =>
                    {
                        return new AbsolutePath(
                            x, true, CommonService.FileSystemProvider, tokenBuilder, formattedBuilder, AbsolutePathNameKind.NameNoExtension);
                    });

                foreach (var absolutePath in childDirectoryNamespacePathList)
                {
                    DirectoryTraitsList.Add(absolutePath);
                    NodeValueList.Add(new TreeViewNodeValue
                    {
                        ParentIndex = indexNodeValue,
                        IndexAmongSiblings = indexAmongSiblings++,
                        ChildListOffset = 0,
                        ChildListLength = 0,
                        ByteKind = InputFileTreeViewContainer.ByteKind_Dir,
                        TraitsIndex = DirectoryTraitsList.Count - 1,
                        IsExpandable = true,
                        IsExpanded = false,
                    });
                }

                var fileAbsolutePathList = fileList
                    .OrderBy(pathString => pathString)
                    .Select(x =>
                    {
                        return new AbsolutePath(x, false, CommonService.FileSystemProvider, tokenBuilder, formattedBuilder, AbsolutePathNameKind.NameWithExtension);
                    });

                foreach (var absolutePath in fileAbsolutePathList)
                {
                    FileTraitsList.Add(absolutePath);
                    NodeValueList.Add(new TreeViewNodeValue
                    {
                        ParentIndex = indexNodeValue,
                        IndexAmongSiblings = indexAmongSiblings++,
                        ChildListOffset = 0,
                        ChildListLength = 0,
                        ByteKind = InputFileTreeViewContainer.ByteKind_File,
                        TraitsIndex = FileTraitsList.Count - 1,
                        IsExpandable = false,
                        IsExpanded = false,
                    });
                }

                nodeValue.ChildListLength = NodeValueList.Count - nodeValue.ChildListOffset;
                
                /*
                // The 'for loop' for `child.Parent` and the
                // 'for loop' for `child.RemoveRelatedFilesFromParent(...)`
                // cannot be combined.
                for (int i = nodeValue.ChildListOffset; i < nodeValue.ChildListOffset + nodeValue.ChildListLength; i++)
                {
                    var child = NodeValueList[i];
                    child.IndexAmongSiblings = i - nodeValue.ChildListOffset;
                    NodeValueList[i] = child;
                }
                */

                /*var cSharpProjectDependenciesTreeViewNode = new TreeViewCSharpProjectDependencies(
                    new CSharpProjectDependencies(project.AbsolutePath),
                    cSharpProjectTreeView.CommonService,
                    true,
                    false);*/

                // file system list vs the filtered list has a negligible length difference vs the cost of enumerating filtered list to get length / internal reallocations of list.
                /*var result = new List<TreeViewNodeValue>(capacity: directoryList.Length + fileList.Length)
                {
                    //cSharpProjectDependenciesTreeViewNode
                };*/

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
            case InputFileTreeViewContainer.ByteKind_Dir:
                return DirectoryTraitsList[nodeValue.TraitsIndex].Name;
            case InputFileTreeViewContainer.ByteKind_File:
                return FileTraitsList[nodeValue.TraitsIndex].Name;
            default:
                return "asdfg";
        }
    }

    public override IconKind GetIconKind(int indexNodeValue)
    {
        var nodeValue = NodeValueList[indexNodeValue];
        switch (nodeValue.ByteKind)
        {
            case InputFileTreeViewContainer.ByteKind_Dir:
                return IconKind.Folder;
            case InputFileTreeViewContainer.ByteKind_File:
                var file = FileTraitsList[nodeValue.TraitsIndex];
                return IdeFacts.GetIconKind(file);
            default:
                return IconKind.None;
        }
    }
    
    public override Task OnClickAsync(TreeViewCommandArgs commandArgs, int indexNodeValue)
    {
        base.OnClickAsync(commandArgs, indexNodeValue);
        
        SetSelectedTreeViewModel(commandArgs, indexNodeValue);
        
        return Task.CompletedTask;
    }

    public override Task OnDoubleClickAsync(TreeViewCommandArgs commandArgs, int indexNodeValue)
    {
        base.OnDoubleClickAsync(commandArgs, indexNodeValue);
        
        return Task.CompletedTask;
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
                SetSelectedTreeViewModel(commandArgs, indexNodeValue);
                return Task.CompletedTask;
            }
            case CommonFacts.SPACE_CODE:
            {
                SetSelectedTreeViewModel(commandArgs, indexNodeValue);
                return Task.CompletedTask;
            }
        }

        return Task.CompletedTask;
    }
    
    private void SetSelectedTreeViewModel(TreeViewCommandArgs commandArgs, int indexNodeValue)
    {
        var activeNode = NodeValueList[indexNodeValue];
        
        switch (activeNode.ByteKind)
        {
            case InputFileTreeViewContainer.ByteKind_Dir:
                IdeService.InputFile_SetSelectedTreeViewModel(DirectoryTraitsList[activeNode.TraitsIndex]);
                break;
            case InputFileTreeViewContainer.ByteKind_File:
                IdeService.InputFile_SetSelectedTreeViewModel(FileTraitsList[activeNode.TraitsIndex]);
                break;
        }
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
        /*var dropdownRecord = new DropdownRecord(
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

        CommonService.Dropdown_ReduceRegisterAction(dropdownRecord);*/
        return Task.CompletedTask;
    }
    
    public const byte ByteKind_Aaa = 1;
    public const byte ByteKind_Dir = 2;
    public const byte ByteKind_File = 3;
}
