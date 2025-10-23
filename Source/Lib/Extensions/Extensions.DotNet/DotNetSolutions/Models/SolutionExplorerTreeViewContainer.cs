using Clair.Common.RazorLib;
using Clair.Common.RazorLib.Commands.Models;
using Clair.Common.RazorLib.FileSystems.Models;
using Clair.Common.RazorLib.Icons.Displays;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.CompilerServices.DotNetSolution.Models;
using Clair.Ide.RazorLib;
using System.Text;

namespace Clair.Extensions.DotNet.DotNetSolutions.Models;

public class SolutionExplorerTreeViewContainer : TreeViewContainer
{
    // DotNetSolutionState.TreeViewSolutionExplorerStateKey
    
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

        switch (nodeValue.TreeViewNodeValueKind)
        {
            case TreeViewNodeValueKind.b0:
            {
                NodeValueList.AddRange(DotNetSolutionModel.SolutionFolderList
                    .Select((x, i) => (x, i))
                    .OrderBy(x => x.x.DisplayName)
                    .Select(tuple =>
                    {
                        return new TreeViewNodeValue
                        {
                            ParentIndex = -1,
                            IndexAmongSiblings = 0,
                            ChildListOffset = 0,
                            ChildListLength = 0,
                            TreeViewNodeValueKind = TreeViewNodeValueKind.b1, // SolutionFolder
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
                            IndexAmongSiblings = 0,
                            ChildListOffset = 0,
                            ChildListLength = 0,
                            TreeViewNodeValueKind = TreeViewNodeValueKind.b2, // .csproj
                            TraitsIndex = tuple.i,
                            IsExpandable = true,
                            IsExpanded = false,
                        };
                    }));

                nodeValue.ChildListLength = NodeValueList.Count - nodeValue.ChildListOffset;

                // The 'for loop' for `child.Parent` and the
                // 'for loop' for `child.RemoveRelatedFilesFromParent(...)`
                // cannot be combined.
                for (int i = nodeValue.ChildListOffset; i < nodeValue.ChildListOffset + nodeValue.ChildListLength; i++)
                {
                    var child = NodeValueList[i];
                    child.IndexAmongSiblings = i - nodeValue.ChildListOffset;
                    NodeValueList[i] = child;
                }

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
            case TreeViewNodeValueKind.b2: // .csproj
            {
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
                            IndexAmongSiblings = 0,
                            ChildListOffset = 0,
                            ChildListLength = 0,
                            TreeViewNodeValueKind = TreeViewNodeValueKind.b3, // dir
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
                            IndexAmongSiblings = 0,
                            ChildListOffset = 0,
                            ChildListLength = 0,
                            TreeViewNodeValueKind = TreeViewNodeValueKind.b3, // dir
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
                        IndexAmongSiblings = 0,
                        ChildListOffset = 0,
                        ChildListLength = 0,
                        TreeViewNodeValueKind = TreeViewNodeValueKind.b4, // file
                        TraitsIndex = FileTraitsList.Count - 1,
                        IsExpandable = false,
                        IsExpanded = false,
                    });
                }

                nodeValue.ChildListLength = NodeValueList.Count - nodeValue.ChildListOffset;

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
        switch (nodeValue.TreeViewNodeValueKind)
        {
            case TreeViewNodeValueKind.b0: // .sln
                return DotNetSolutionModel.AbsolutePath.Name;
            case TreeViewNodeValueKind.b1: // SolutionFolder
                return DotNetSolutionModel.SolutionFolderList[nodeValue.TraitsIndex].DisplayName;
            case TreeViewNodeValueKind.b2: // .csproj
                return DotNetSolutionModel.DotNetProjectList[nodeValue.TraitsIndex].AbsolutePath.Name;
            case TreeViewNodeValueKind.b3: // dir
                return DirectoryTraitsList[nodeValue.TraitsIndex].Name;
            case TreeViewNodeValueKind.b4: // file
                return FileTraitsList[nodeValue.TraitsIndex].Name;
            default:
                return "asdfg";
        }
    }

    public override IconKind GetIconKind(int indexNodeValue)
    {
        var nodeValue = NodeValueList[indexNodeValue];
        switch (nodeValue.TreeViewNodeValueKind)
        {
            case TreeViewNodeValueKind.b0: // .sln
                return IconKind.DotNetSolution;
            case TreeViewNodeValueKind.b1: // SolutionFolder
                return IconKind.DotNetSolutionFolder;
            case TreeViewNodeValueKind.b2: // .csproj
                return IconKind.CSharpProject;
            case TreeViewNodeValueKind.b3: // dir
                return IconKind.Folder;
            case TreeViewNodeValueKind.b4: // file
                return IconKind.File;
            default:
                return IconKind.None;
        }
    }

    public override Task OnDoubleClickAsync(TreeViewCommandArgs commandArgs)
    {
        base.OnDoubleClickAsync(commandArgs);

        if (commandArgs.NodeThatReceivedMouseEvent.TreeViewNodeValueKind != TreeViewNodeValueKind.b4) // NamespacePath
            return Task.CompletedTask;

        IdeService.TextEditorService.WorkerArbitrary.PostUnique(async editContext =>
        {
            /*await IdeService.TextEditorService.OpenInEditorAsync(
                editContext,
                treeViewNamespacePath.Item.Value,
                true,
                null,
                new Category("main"),
                editContext.TextEditorService.NewViewModelKey());*/
        });
        return Task.CompletedTask;
    }
}
