using Clair.Common.RazorLib;
using Clair.Common.RazorLib.Commands.Models;
using Clair.Common.RazorLib.Dropdowns.Models;
using Clair.Common.RazorLib.FileSystems.Models;
using Clair.Common.RazorLib.Icons.Displays;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.CompilerServices.DotNetSolution.Models;
using Clair.Extensions.DotNet.DotNetSolutions.Displays.Internals;
using Clair.Ide.RazorLib;
using Clair.TextEditor.RazorLib.TextEditors.Models;
using System.Text;

namespace Clair.Extensions.DotNet.DotNetSolutions.Models;

public class SolutionExplorerTreeViewContainer : TreeViewContainer
{
    public SolutionExplorerTreeViewContainer(IdeService ideService, DotNetSolutionModel dotNetSolutionModel)
        : base(ideService.CommonService)
    {
        Key = DotNetSolutionState.TreeViewSolutionExplorerStateKey;
        NodeValueList = new();
        DotNetSolutionModel = dotNetSolutionModel;
        IdeService = ideService;
    }
    
    public override Key<TreeViewContainer> Key { get; init; }
    public override List<TreeViewNodeValue> NodeValueList { get; }

    public IdeService IdeService { get; }
    public DotNetSolutionModel DotNetSolutionModel { get; }
    public List<AbsolutePath> DirectoryTraitsList { get; set; } = new();
    public List<AbsolutePath> FileTraitsList { get; set; } = new();

    public override Task LoadChildListAsync(int indexNodeValue)
    {
        var nodeValue = NodeValueList[indexNodeValue];
        nodeValue.ChildListLength = 0;
        nodeValue.ChildListOffset = NodeValueList.Count;

        switch (nodeValue.ByteKind)
        {
            case SolutionExplorerTreeViewContainer.ByteKind_Solution:
            {
                var indexAmongSiblings = 0;
            
                NodeValueList.AddRange(DotNetSolutionModel.SolutionFolderList
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
                            ByteKind = SolutionExplorerTreeViewContainer.ByteKind_SolutionFolder,
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
                            ByteKind = SolutionExplorerTreeViewContainer.ByteKind_Csproj,
                            TraitsIndex = tuple.i,
                            IsExpandable = true,
                            IsExpanded = false,
                        };
                    }));

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

                // The 'for loop' for `child.Parent` and the
                // 'for loop' for `child.RemoveRelatedFilesFromParent(...)`
                // cannot be combined.
                /*for (int i = nodeValue.ChildListOffset; i < nodeValue.ChildListOffset + nodeValue.ChildListLength; i++)
                {
                    child.RemoveRelatedFilesFromParent(copyOfChildrenToFindRelatedFiles);
                }*/

                // The parent directory gets what is left over after the
                // children take their respective 'code behinds'

                NodeValueList[indexNodeValue] = nodeValue;
                return Task.CompletedTask;
            }
            case SolutionExplorerTreeViewContainer.ByteKind_Csproj:
            {
                var indexAmongSiblings = 0;
            
                var project = DotNetSolutionModel.DotNetProjectList[nodeValue.TraitsIndex];

                var directoryAbsolutePathString = project.AbsolutePath.CreateSubstringParentDirectory();
                if (directoryAbsolutePathString is null)
                    return Task.CompletedTask;

                var directoryList = CommonService.FileSystemProvider.Directory.GetDirectories(directoryAbsolutePathString);
                var fileList = CommonService.FileSystemProvider.Directory.GetFiles(directoryAbsolutePathString);

                var tokenBuilder = new StringBuilder();
                var formattedBuilder = new StringBuilder();

                var childDirectoryNamespacePathList = directoryList
                    .Where(x => !IdeFacts.IsHiddenFileByContainerFileExtension(CommonFacts.C_SHARP_PROJECT, x))
                    .OrderBy(pathString => pathString)
                    .Select(x =>
                    {
                        return new AbsolutePath(
                            x, true, CommonService.FileSystemProvider, tokenBuilder, formattedBuilder, AbsolutePathNameKind.NameNoExtension);
                    });

                // The way I sort the unique to come before the default directories feels scuffed
                // but it is an extremely minor detail relative to the task of rewriting the treeviews
                foreach (var absolutePath in childDirectoryNamespacePathList)
                {
                    if (IdeFacts.IsUniqueFileByContainerFileExtension(CommonFacts.C_SHARP_PROJECT, absolutePath.Name))
                    {
                        DirectoryTraitsList.Add(absolutePath);
                        NodeValueList.Add(new TreeViewNodeValue
                        {
                            ParentIndex = indexNodeValue,
                            IndexAmongSiblings = indexAmongSiblings++,
                            ChildListOffset = 0,
                            ChildListLength = 0,
                            ByteKind = SolutionExplorerTreeViewContainer.ByteKind_Dir,
                            TraitsIndex = DirectoryTraitsList.Count - 1,
                            IsExpandable = true,
                            IsExpanded = false,
                        });
                    }
                }

                // The way I sort the unique to come before the default directories feels scuffed
                // but it is an extremely minor detail relative to the task of rewriting the treeviews
                foreach (var absolutePath in childDirectoryNamespacePathList)
                {
                    if (!IdeFacts.IsUniqueFileByContainerFileExtension(CommonFacts.C_SHARP_PROJECT, absolutePath.Name))
                    {
                        DirectoryTraitsList.Add(absolutePath);
                        NodeValueList.Add(new TreeViewNodeValue
                        {
                            ParentIndex = indexNodeValue,
                            IndexAmongSiblings = indexAmongSiblings++,
                            ChildListOffset = 0,
                            ChildListLength = 0,
                            ByteKind = SolutionExplorerTreeViewContainer.ByteKind_Dir,
                            TraitsIndex = DirectoryTraitsList.Count - 1,
                            IsExpandable = true,
                            IsExpanded = false,
                        });
                    }
                }

                var fileAbsolutePathList = fileList
                    .Where(x => !x.EndsWith(CommonFacts.C_SHARP_PROJECT))
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
                        ByteKind = SolutionExplorerTreeViewContainer.ByteKind_File,
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
            case SolutionExplorerTreeViewContainer.ByteKind_Dir:
            {
                var indexAmongSiblings = 0;
                
                var directoryAbsolutePath = DirectoryTraitsList[nodeValue.TraitsIndex];

                var directoryList = CommonService.FileSystemProvider.Directory.GetDirectories(directoryAbsolutePath.Value);
                var fileList = CommonService.FileSystemProvider.Directory.GetFiles(directoryAbsolutePath.Value);

                var tokenBuilder = new StringBuilder();
                var formattedBuilder = new StringBuilder();

                var childDirectoryNamespacePathList = directoryList
                    .Where(x => !IdeFacts.IsHiddenFileByContainerFileExtension(CommonFacts.C_SHARP_PROJECT, x))
                    .OrderBy(pathString => pathString)
                    .Select(x =>
                    {
                        return new AbsolutePath(
                            x, true, CommonService.FileSystemProvider, tokenBuilder, formattedBuilder, AbsolutePathNameKind.NameNoExtension);
                    });

                // The way I sort the unique to come before the default directories feels scuffed
                // but it is an extremely minor detail relative to the task of rewriting the treeviews
                foreach (var absolutePath in childDirectoryNamespacePathList)
                {
                    if (IdeFacts.IsUniqueFileByContainerFileExtension(CommonFacts.C_SHARP_PROJECT, absolutePath.Name))
                    {
                        DirectoryTraitsList.Add(absolutePath);
                        NodeValueList.Add(new TreeViewNodeValue
                        {
                            ParentIndex = indexNodeValue,
                            IndexAmongSiblings = indexAmongSiblings++,
                            ChildListOffset = 0,
                            ChildListLength = 0,
                            ByteKind = SolutionExplorerTreeViewContainer.ByteKind_Dir,
                            TraitsIndex = DirectoryTraitsList.Count - 1,
                            IsExpandable = true,
                            IsExpanded = false,
                        });
                    }
                }

                // The way I sort the unique to come before the default directories feels scuffed
                // but it is an extremely minor detail relative to the task of rewriting the treeviews
                foreach (var absolutePath in childDirectoryNamespacePathList)
                {
                    if (!IdeFacts.IsUniqueFileByContainerFileExtension(CommonFacts.C_SHARP_PROJECT, absolutePath.Name))
                    {
                        DirectoryTraitsList.Add(absolutePath);
                        NodeValueList.Add(new TreeViewNodeValue
                        {
                            ParentIndex = indexNodeValue,
                            IndexAmongSiblings = indexAmongSiblings++,
                            ChildListOffset = 0,
                            ChildListLength = 0,
                            ByteKind = SolutionExplorerTreeViewContainer.ByteKind_Dir,
                            TraitsIndex = DirectoryTraitsList.Count - 1,
                            IsExpandable = true,
                            IsExpanded = false,
                        });
                    }
                }

                var fileAbsolutePathList = fileList
                    .Where(x => !x.EndsWith(CommonFacts.C_SHARP_PROJECT))
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
                        ByteKind = SolutionExplorerTreeViewContainer.ByteKind_File,
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
            case SolutionExplorerTreeViewContainer.ByteKind_Solution:
                return DotNetSolutionModel.AbsolutePath.Name;
            case SolutionExplorerTreeViewContainer.ByteKind_SolutionFolder:
                return DotNetSolutionModel.SolutionFolderList[nodeValue.TraitsIndex].DisplayName;
            case SolutionExplorerTreeViewContainer.ByteKind_Csproj:
                return DotNetSolutionModel.DotNetProjectList[nodeValue.TraitsIndex].AbsolutePath.Name;
            case SolutionExplorerTreeViewContainer.ByteKind_Dir:
                return DirectoryTraitsList[nodeValue.TraitsIndex].Name;
            case SolutionExplorerTreeViewContainer.ByteKind_File:
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
            case SolutionExplorerTreeViewContainer.ByteKind_Solution:
                return IconKind.DotNetSolution;
            case SolutionExplorerTreeViewContainer.ByteKind_SolutionFolder:
                return IconKind.DotNetSolutionFolder;
            case SolutionExplorerTreeViewContainer.ByteKind_Csproj:
                return IconKind.CSharpProject;
            case SolutionExplorerTreeViewContainer.ByteKind_Dir:
                return IconKind.Folder;
            case SolutionExplorerTreeViewContainer.ByteKind_File:
                var file = FileTraitsList[nodeValue.TraitsIndex];
                return IdeFacts.GetIconKind(file);
            default:
                return IconKind.None;
        }
    }

    public override Task OnDoubleClickAsync(TreeViewCommandArgs commandArgs, int indexNodeValue)
    {
        base.OnDoubleClickAsync(commandArgs, indexNodeValue);
        
        string? path = null;

        var nodeValue = NodeValueList[indexNodeValue];
        switch (nodeValue.ByteKind)
        {
            case SolutionExplorerTreeViewContainer.ByteKind_Solution:
                return Task.CompletedTask;
            case SolutionExplorerTreeViewContainer.ByteKind_SolutionFolder:
                return Task.CompletedTask;
            case SolutionExplorerTreeViewContainer.ByteKind_Csproj:
                path = DotNetSolutionModel.DotNetProjectList[nodeValue.TraitsIndex].AbsolutePath.Value;
                break;
            case SolutionExplorerTreeViewContainer.ByteKind_Dir:
                return Task.CompletedTask;
            case SolutionExplorerTreeViewContainer.ByteKind_File:
                path = FileTraitsList[nodeValue.TraitsIndex].Value;
                break;
            default:
                return Task.CompletedTask;
        }
        
        if (path is null)
            return Task.CompletedTask;

        IdeService.TextEditorService.WorkerArbitrary.PostUnique(async editContext =>
        {
            await IdeService.TextEditorService.OpenInEditorAsync(
                editContext,
                path,
                true,
                null,
                new Category("main"),
                editContext.TextEditorService.NewViewModelKey());
        });
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
                string? path = null;

                var nodeValue = NodeValueList[indexNodeValue];
                switch (nodeValue.ByteKind)
                {
                    case SolutionExplorerTreeViewContainer.ByteKind_Solution:
                        return Task.CompletedTask;
                    case SolutionExplorerTreeViewContainer.ByteKind_SolutionFolder:
                        return Task.CompletedTask;
                    case SolutionExplorerTreeViewContainer.ByteKind_Csproj: // .csproj
                        path = DotNetSolutionModel.DotNetProjectList[nodeValue.TraitsIndex].AbsolutePath.Value;
                        break;
                    case SolutionExplorerTreeViewContainer.ByteKind_Dir:
                        return Task.CompletedTask;
                    case SolutionExplorerTreeViewContainer.ByteKind_File:
                        path = FileTraitsList[nodeValue.TraitsIndex].Value;
                        break;
                    default:
                        return Task.CompletedTask;
                }
                
                if (path is null)
                    return Task.CompletedTask;
        
                IdeService.TextEditorService.WorkerArbitrary.PostUnique(async editContext =>
                {
                    await IdeService.TextEditorService.OpenInEditorAsync(
                        editContext,
                        path,
                        shouldSetFocusToEditor: true,
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
                    case SolutionExplorerTreeViewContainer.ByteKind_Solution:
                        return Task.CompletedTask;
                    case SolutionExplorerTreeViewContainer.ByteKind_SolutionFolder:
                        return Task.CompletedTask;
                    case SolutionExplorerTreeViewContainer.ByteKind_Csproj:
                        path = DotNetSolutionModel.DotNetProjectList[nodeValue.TraitsIndex].AbsolutePath.Value;
                        break;
                    case SolutionExplorerTreeViewContainer.ByteKind_Dir:
                        return Task.CompletedTask;
                    case SolutionExplorerTreeViewContainer.ByteKind_File:
                        path = FileTraitsList[nodeValue.TraitsIndex].Value;
                        break;
                    default:
                        return Task.CompletedTask;
                }
                
                if (path is null)
                    return Task.CompletedTask;
        
                IdeService.TextEditorService.WorkerArbitrary.PostUnique(async editContext =>
                {
                    await IdeService.TextEditorService.OpenInEditorAsync(
                        editContext,
                        path,
                        shouldSetFocusToEditor: false,
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
        return Task.CompletedTask;
    }
    
    public const byte ByteKind_Solution = 1;
    public const byte ByteKind_SolutionFolder = 2;
    public const byte ByteKind_Csproj = 3;
    public const byte ByteKind_Dir = 4;
    public const byte ByteKind_File = 5;
}
