using Clair.Common.RazorLib;
using Clair.Common.RazorLib.Commands.Models;
using Clair.Common.RazorLib.Dialogs.Models;
using Clair.Common.RazorLib.Dynamics.Models;
using Clair.Common.RazorLib.FileSystems.Models;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.Reactives.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.CompilerServices.CSharp.CompilerServiceCase;
using Clair.CompilerServices.DotNetSolution;
using Clair.CompilerServices.DotNetSolution.Models;
using Clair.CompilerServices.DotNetSolution.Models.Project;
using Clair.CompilerServices.Xml;
using Clair.Extensions.DotNet.AppDatas.Models;
using Clair.Extensions.DotNet.CommandLines.Models;
using Clair.Extensions.DotNet.DotNetSolutions.Displays;
using Clair.Extensions.DotNet.DotNetSolutions.Models;
using Clair.Extensions.DotNet.Nugets.Models;
using Clair.Extensions.DotNet.TestExplorers.Models;
using Clair.Ide.RazorLib;
using Clair.Ide.RazorLib.Terminals.Models;
using Clair.TextEditor.RazorLib;
using Clair.TextEditor.RazorLib.BackgroundTasks.Models;
using Clair.TextEditor.RazorLib.CompilerServices;
using Clair.TextEditor.RazorLib.Lexers.Models;
using CliWrap.EventStream;
using Microsoft.AspNetCore.Components.Forms;
using System.Runtime.InteropServices;
using System.Text;

namespace Clair.Extensions.DotNet;

public partial class DotNetService
{
    #region DotNetSolutionIdeApi
    // private readonly IServiceProvider _serviceProvider;

    public readonly Key<TerminalCommandRequest> NewDotNetSolutionTerminalCommandRequestKey = Key<TerminalCommandRequest>.NewKey();
    private readonly CancellationTokenSource _newDotNetSolutionCancellationTokenSource = new();
    #endregion

    private static readonly Key<IDynamicViewModel> _newDotNetSolutionDialogKey = Key<IDynamicViewModel>.NewKey();

    public async ValueTask Do_SolutionExplorer_TreeView_MultiSelect_DeleteFiles(TreeViewCommandArgs commandArgs)
    {
        /*
        // 2025-10-15
        foreach (var node in commandArgs.TreeViewContainer.SelectedNodeList)
        {
            var treeViewNamespacePath = (TreeViewNamespacePath)node;

            if (treeViewNamespacePath.Item.IsDirectory)
            {
                await IdeService.TextEditorService.CommonService.FileSystemProvider.Directory
                    .DeleteAsync(treeViewNamespacePath.Item.Value, true, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            else
            {
                await IdeService.TextEditorService.CommonService.FileSystemProvider.File
                    .DeleteAsync(treeViewNamespacePath.Item.Value)
                    .ConfigureAwait(false);
            }

            if (IdeService.TextEditorService.CommonService.TryGetTreeViewContainer(commandArgs.TreeViewContainer.Key, out var mostRecentContainer) &&
                mostRecentContainer is not null)
            {
                var localParent = node.Parent;

                if (localParent is not null)
                {
                    await localParent.LoadChildListAsync().ConfigureAwait(false);
                    IdeService.TextEditorService.CommonService.TreeView_ReRenderNodeAction(mostRecentContainer.Key, localParent);
                }
            }
        }
        */
    }

    public Task OpenNewDotNetSolutionDialog()
    {
        var dialogRecord = new DialogViewModel(
            _newDotNetSolutionDialogKey,
            "New .NET Solution",
            typeof(DotNetSolutionFormDisplay),
            null,
            null,
            true,
            null);

        IdeService.TextEditorService.CommonService.Dialog_ReduceRegisterAction(dialogRecord);
        return Task.CompletedTask;
    }

    public async ValueTask Do_SubmitNuGetQuery(INugetPackageManagerQuery query)
    {
        var localNugetResult = await QueryForNugetPackagesAsync(query)
            .ConfigureAwait(false);

        ReduceSetMostRecentQueryResultAction(localNugetResult);
    }

    /*public ValueTask Do_RunTestByFullyQualifiedName(TreeViewStringFragment treeViewStringFragment, string fullyQualifiedName, TreeViewProjectTestModel treeViewProjectTestModel)
    {
        var parentDirectory = treeViewProjectTestModel.Item.AbsolutePath.CreateSubstringParentDirectory();
        if (parentDirectory is null)
            return ValueTask.CompletedTask;
    
        RunTestByFullyQualifiedName(
            treeViewStringFragment,
            fullyQualifiedName,
            parentDirectory);

        return ValueTask.CompletedTask;
    }

    private void RunTestByFullyQualifiedName(
        TreeViewStringFragment treeViewStringFragment,
        string fullyQualifiedName,
        string? directoryNameForTestDiscovery)
    {
        var dotNetTestByFullyQualifiedNameFormattedCommandValue = DotNetCliCommandFormatter
            .FormatDotNetTestByFullyQualifiedName(fullyQualifiedName);

        if (string.IsNullOrWhiteSpace(directoryNameForTestDiscovery) ||
            string.IsNullOrWhiteSpace(fullyQualifiedName))
        {
            return;
        }

        var terminalCommandRequest = new TerminalCommandRequest(
            dotNetTestByFullyQualifiedNameFormattedCommandValue,
            directoryNameForTestDiscovery,
            treeViewStringFragment.Item.DotNetTestByFullyQualifiedNameFormattedTerminalCommandRequestKey)
        {
            BeginWithFunc = parsedCommand =>
            {
                treeViewStringFragment.Item.TerminalCommandParsed = parsedCommand;
                IdeService.TextEditorService.CommonService.TreeView_ReRenderNodeAction(TestExplorerState.TreeViewTestExplorerKey, treeViewStringFragment);
                return Task.CompletedTask;
            },
            ContinueWithFunc = parsedCommand =>
            {
                treeViewStringFragment.Item.TerminalCommandParsed = parsedCommand;
                var output = treeViewStringFragment.Item.TerminalCommandParsed?.OutputCache.ToString() ?? null;

                if (output is not null && output.Contains("Duration:"))
                {
                    if (output.Contains("Passed!"))
                    {
                        ReduceWithAction(inState =>
                        {
                            var passedTestHashSet = new HashSet<string>(inState.PassedTestHashSet);
                            passedTestHashSet.Add(fullyQualifiedName);

                            var notRanTestHashSet = new HashSet<string>(inState.NotRanTestHashSet);
                            notRanTestHashSet.Remove(fullyQualifiedName);

                            var failedTestHashSet = new HashSet<string>(inState.FailedTestHashSet);
                            failedTestHashSet.Remove(fullyQualifiedName);

                            return inState with
                            {
                                PassedTestHashSet = passedTestHashSet,
                                NotRanTestHashSet = notRanTestHashSet,
                                FailedTestHashSet = failedTestHashSet,
                            };
                        });
                    }
                    else
                    {
                        ReduceWithAction(inState =>
                        {
                            var failedTestHashSet = new HashSet<string>(inState.FailedTestHashSet);
                            failedTestHashSet.Add(fullyQualifiedName);

                            var notRanTestHashSet = new HashSet<string>(inState.NotRanTestHashSet);
                            notRanTestHashSet.Remove(fullyQualifiedName);

                            var passedTestHashSet = new HashSet<string>(inState.PassedTestHashSet);
                            passedTestHashSet.Remove(fullyQualifiedName);

                            return inState with
                            {
                                FailedTestHashSet = failedTestHashSet,
                                NotRanTestHashSet = notRanTestHashSet,
                                PassedTestHashSet = passedTestHashSet,
                            };
                        });
                    }
                }

                IdeService.TextEditorService.CommonService.TreeView_ReRenderNodeAction(TestExplorerState.TreeViewTestExplorerKey, treeViewStringFragment);
                return Task.CompletedTask;
            }
        };

        treeViewStringFragment.Item.TerminalCommandRequest = terminalCommandRequest;
        IdeService.GetTerminalState().ExecutionTerminal.EnqueueCommand(terminalCommandRequest);
    }
    */

    #region DotNetSolutionIdeApi
    private const int GET_TEXT_BUFFER_SIZE = 32;
    
    private async ValueTask Do_SetDotNetSolution(AbsolutePath inSolutionAbsolutePath)
    {
        var dotNetSolutionAbsolutePathString = inSolutionAbsolutePath.Value;

        var tokenBuilder = new StringBuilder();
        var formattedBuilder = new StringBuilder();
        
        var getTextBuffer = new char[GET_TEXT_BUFFER_SIZE];

        var textSpanList = new List<TextEditorTextSpan>();

        var solutionAbsolutePath = new AbsolutePath(
            dotNetSolutionAbsolutePathString,
            false,
            IdeService.TextEditorService.CommonService.FileSystemProvider,
            tokenBuilder,
            formattedBuilder,
            AbsolutePathNameKind.NameWithExtension);

        var resourceUri = new ResourceUri(solutionAbsolutePath.Value);

        DotNetSolutionModel dotNetSolutionModel;

        if (dotNetSolutionAbsolutePathString.EndsWith(CommonFacts.DOT_NET_SOLUTION_X))
            dotNetSolutionModel = ParseSlnx(solutionAbsolutePath, resourceUri, tokenBuilder, formattedBuilder, textSpanList, getTextBuffer);
        else
            dotNetSolutionModel = ParseSln(solutionAbsolutePath, resourceUri);

        dotNetSolutionModel.DotNetProjectList = SortProjectReferences(dotNetSolutionModel, tokenBuilder, formattedBuilder, textSpanList, getTextBuffer);

        SetDotNetSolution(dotNetSolutionModel);

        var dotNetSolutionCompilerService = (DotNetSolutionCompilerService)IdeService.TextEditorService.GetCompilerService(CommonFacts.DOT_NET_SOLUTION);

        dotNetSolutionCompilerService.ResourceWasModified(
            new ResourceUri(solutionAbsolutePath.Value),
            Array.Empty<TextEditorTextSpan>());

        var parentDirectory = solutionAbsolutePath.CreateSubstringParentDirectory();
        if (parentDirectory is not null)
        {
            IdeService.TextEditorService.CommonService.FileSystemProvider.DeletionPermittedRegister(new(parentDirectory, true), tokenBuilder, formattedBuilder);

            IdeService.TextEditorService.SetStartingDirectoryPath(parentDirectory);

            IdeService.CodeSearch_With(inState => inState with
            {
                StartingAbsolutePathForSearch = parentDirectory
            });

            TerminalCommandRequest terminalCommandRequest;

            var slnFoundString = $"sln found: {solutionAbsolutePath.Value}";
            var prefix = Terminal.RESERVED_TARGET_FILENAME_PREFIX + nameof(DotNetService);

            // Set 'generalTerminal' working directory
            terminalCommandRequest = new TerminalCommandRequest(
                prefix + "_General",
                parentDirectory)
            {
                BeginWithFunc = parsedCommand =>
                {
                    IdeService.GetTerminalState().GeneralTerminal.WriteOutput(
                        parsedCommand,
                        // If newlines are added to this make sure to use '.ReplaceLineEndings("\n")' because the syntax highlighting and text editor are expecting this line ending.
                        new StandardOutputCommandEvent(slnFoundString));
                    return Task.CompletedTask;
                }
            };
            IdeService.GetTerminalState().GeneralTerminal.EnqueueCommand(terminalCommandRequest);

            // Set 'executionTerminal' working directory
            terminalCommandRequest = new TerminalCommandRequest(
                prefix + "_Execution",
                parentDirectory)
            {
                BeginWithFunc = parsedCommand =>
                {
                    IdeService.GetTerminalState().ExecutionTerminal.WriteOutput(
                        parsedCommand,
                        // If newlines are added to this make sure to use '.ReplaceLineEndings("\n")' because the syntax highlighting and text editor are expecting this line ending.
                        new StandardOutputCommandEvent(slnFoundString));
                    return Task.CompletedTask;
                }
            };
            IdeService.GetTerminalState().ExecutionTerminal.EnqueueCommand(terminalCommandRequest);
        }

        try
        {
            await AppDataService.WriteAppDataAsync(new DotNetAppData
            {
                SolutionMostRecent = solutionAbsolutePath.Value
            });
        }
        catch (Exception e)
        {
            CommonFacts.DispatchError(
                $"ERROR: nameof(_appDataService.WriteAppDataAsync)",
                e.ToString(),
                IdeService.TextEditorService.CommonService,
                TimeSpan.FromSeconds(5));
        }

        IdeService.TextEditorService.WorkerArbitrary.EnqueueUniqueTextEditorWork(
            new UniqueTextEditorWork(IdeService.TextEditorService, editContext =>
            {
                var compilerService = IdeService.TextEditorService.GetCompilerService(CommonFacts.C_SHARP_CLASS);
                var cSharpCompilerService = compilerService as CSharpCompilerService;
                
                if (cSharpCompilerService is null)
                    return ValueTask.CompletedTask;

                try
                {
                    cSharpCompilerService.__CSharpBinder.ClearAllCompilationUnits();

                    var successParseSolution = ParseSolution(editContext, dotNetSolutionModel.Key, CompilationUnitKind.SolutionWide_DefinitionsOnly);
                    if (successParseSolution)
                    {
                        // Get the indices that the first 'ParseSolution' resides at.
                        // Then after the second 'ParseSolution' clear the first 'ParseSolution'.
                        // Then iterate over every compilationUnit and modify their indices by the amount removed.
                        //

                        int countDiagnosticList;
                        int countSymbolList;
                        int countFunctionInvocationParameterMetadataList;
                        int countScopeList;
                        int countNodeList;

                        countDiagnosticList = cSharpCompilerService.__CSharpBinder.DiagnosticList.Count;
                        countSymbolList = cSharpCompilerService.__CSharpBinder.SymbolList.Count;
                        countFunctionInvocationParameterMetadataList = cSharpCompilerService.__CSharpBinder.FunctionInvocationParameterMetadataList.Count;
                        countScopeList = cSharpCompilerService.__CSharpBinder.ScopeList.Count;
                        countNodeList = cSharpCompilerService.__CSharpBinder.NodeList.Count;

                        successParseSolution = ParseSolution(editContext, dotNetSolutionModel.Key, CompilationUnitKind.SolutionWide_MinimumLocalsData);

                        if (successParseSolution)
                        {
                            cSharpCompilerService.__CSharpBinder.DiagnosticList.RemoveRange(0, countDiagnosticList);
                            cSharpCompilerService.__CSharpBinder.SymbolList.RemoveRange(0, countSymbolList);
                            cSharpCompilerService.__CSharpBinder.FunctionInvocationParameterMetadataList.RemoveRange(0, countFunctionInvocationParameterMetadataList);
                            cSharpCompilerService.__CSharpBinder.ScopeList.RemoveRange(0, countScopeList);
                            cSharpCompilerService.__CSharpBinder.NodeList.RemoveRange(0, countNodeList);

                            foreach (var compilationUnitKvp in cSharpCompilerService.__CSharpBinder.__CompilationUnitMap)
                            {
                                var compilationUnit = compilationUnitKvp.Value;

                                compilationUnit.DiagnosticOffset -= countDiagnosticList;
                                compilationUnit.SymbolOffset -= countSymbolList;
                                compilationUnit.FunctionInvocationParameterMetadataOffset -= countFunctionInvocationParameterMetadataList;
                                compilationUnit.ScopeOffset -= countScopeList;
                                compilationUnit.NodeOffset -= countNodeList;

                                cSharpCompilerService.__CSharpBinder.__CompilationUnitMap[compilationUnitKvp.Key] = compilationUnit;
                            }
                        }
                    }
                    
                    // Where does the first definitions only parse allocate variable declarations
                    //
                    // The clearing is simple because you just do it after every file in the second round of solution wide parsing.
                    // there is no compilation unit after you so you only have to reposition your length.
                    //
                    // If you do the clearing after every file in the first too then you don't have to worry about any odd off variable declarations local
                    // that get through then you re-use the local node when pooling.
                }
                finally
                {
                    if (cSharpCompilerService is not null)
                    {
                        cSharpCompilerService.FastParseTuple = (0, null);

                        cSharpCompilerService.FULL_Clear_MAIN_StreamReaderTupleCache();
                        cSharpCompilerService.FULL_Clear_BACKUP_StreamReaderTupleCache();
                        cSharpCompilerService._safe_streamReaderPooledBuffer_Pool.Clear();
                        
                        IdeService.TextEditorService.EditContext_GetText_Clear();

                        GC.Collect();
                    }
                }
                return ValueTask.CompletedTask;
            }));

        await Do_SetDotNetSolutionTreeView(dotNetSolutionModel.Key).ConfigureAwait(false);
    }

    public DotNetSolutionModel ParseSlnx(
        AbsolutePath solutionAbsolutePath,
        ResourceUri resourceUri,
        StringBuilder tokenBuilder,
        StringBuilder formattedBuilder,
        List<TextEditorTextSpan> textSpanList,
        char[] getTextBuffer)
    {
        var dotNetProjectList = new List<IDotNetProject>();
        var solutionFolderList = new List<SolutionFolder>();
    
        using StreamReader sr = new StreamReader(solutionAbsolutePath.Value);
        var lexerOutput = XmlLexer.Lex(new StreamReaderWrap(sr), modelModifier: null, textSpanList);
        
        var stringBuilder = new StringBuilder();
        
        var xmlOutputReader = new XmlOutputReader(lexerOutput.TextSpanList);
        
        List<(string Name, List<string> ChildProjectRelativePathList)> folderTupleList = new();
        
        xmlOutputReader.CollectParentChildrenRelationship(
            parentTagName: "Folder",
            parentAttributeName: "Name",
            childTagName: "Project",
            childAttributeName: "Path",
            sr,
            stringBuilder,
            getTextBuffer,
            folderTupleList);
        
        var childProjectRelativePathList = new List<string>();

        var solutionFolderPathHashSet = new HashSet<string>();

        var stringNestedProjectEntryList = new List<StringNestedProjectEntry>();

        // Folder Name

        foreach (var folderTuple in folderTupleList)
        {
            var ancestorDirectoryList = new List<string>();

            var absolutePath = new AbsolutePath(
                folderTuple.Name,
                isDirectory: true,
                IdeService.TextEditorService.CommonService.FileSystemProvider,
                tokenBuilder,
                formattedBuilder,
                AbsolutePathNameKind.NameWithExtension,
                ancestorDirectoryList: ancestorDirectoryList);

            solutionFolderPathHashSet.Add(absolutePath.Value);

            for (int i = 0; i < ancestorDirectoryList.Count; i++)
            {
                if (i == 0)
                    continue;

                solutionFolderPathHashSet.Add(ancestorDirectoryList[i]);
            }

            foreach (var childRelativePath in folderTuple.ChildProjectRelativePathList)
            {
                stringNestedProjectEntryList.Add(new StringNestedProjectEntry(
                    ChildIsSolutionFolder: false,
                    childRelativePath,
                    absolutePath.Value));
            }
        }

        // I'm too tired to decide if enumerating a HashSet is safe
        var temporarySolutionFolderList = solutionFolderPathHashSet.ToList();

        foreach (var solutionFolderPath in temporarySolutionFolderList)
        {
            var absolutePath = new AbsolutePath(
                solutionFolderPath,
                isDirectory: true,
                IdeService.TextEditorService.CommonService.FileSystemProvider,
                tokenBuilder,
                formattedBuilder,
                AbsolutePathNameKind.NameNoExtension);

            solutionFolderList.Add(new SolutionFolder(
                absolutePath.Name,
                solutionFolderPath));
        }
        
        var relativePathProjectTagList = new List<string>();
        
        xmlOutputReader.FindTagGetAttributeValue(
            targetTagName: "Project",
            targetAttributeOne: "Path",
            shouldIncludeFullMissLines: false,
            sr,
            stringBuilder,
            getTextBuffer,
            relativePathProjectTagList,
            attributeValueMustEndsWith: ".csproj");

        foreach (var relativePathProject in relativePathProjectTagList)
        {
            var relativePath = new RelativePath(relativePathProject, isDirectory: false, IdeService.TextEditorService.CommonService.FileSystemProvider);

            dotNetProjectList.Add(new CSharpProjectModel(
                relativePath.NameNoExtension,
                Guid.Empty,
                relativePathProject,
                Guid.Empty,
                default(AbsolutePath)));
        }

        // You have to iterate in reverse so ascending will put longest words to shortest (when iterating reverse).
        var childSolutionFolderList = solutionFolderList.OrderBy(x => x.ActualName).ToList();
        var parentSolutionFolderList = new List<SolutionFolder>(childSolutionFolderList);

        for (int parentIndex = parentSolutionFolderList.Count - 1; parentIndex >= 0; parentIndex--)
        {
            var parentSolutionFolder = parentSolutionFolderList[parentIndex];

            for (int childIndex = childSolutionFolderList.Count - 1; childIndex >= 0; childIndex--)
            {
                var childSolutionFolder = childSolutionFolderList[childIndex];

                if (childSolutionFolder.ActualName != parentSolutionFolder.ActualName &&
                    childSolutionFolder.ActualName.StartsWith(parentSolutionFolder.ActualName))
                {
                    stringNestedProjectEntryList.Add(new StringNestedProjectEntry(
                        ChildIsSolutionFolder: true,
                        childSolutionFolder.ActualName,
                        parentSolutionFolder.ActualName));

                    childSolutionFolderList.RemoveAt(childIndex);
                }
            }
        }

        return new DotNetSolutionModel(
            solutionAbsolutePath,
            dotNetProjectList,
            solutionFolderList,
            guidNestedProjectEntryList: null,
            stringNestedProjectEntryList);
    }

    public DotNetSolutionModel ParseSln(
        AbsolutePath solutionAbsolutePath,
        ResourceUri resourceUri)
    {
        using StreamReader sr = new StreamReader(solutionAbsolutePath.Value);
        var lexerOutput = DotNetSolutionLexer.Lex(new StreamReaderWrap(sr));
        
        var stringBuilder = new StringBuilder();
        
        return new DotNetSolutionModel(
            solutionAbsolutePath,
            lexerOutput.DotNetProjectList,
            lexerOutput.SolutionFolderList,
            lexerOutput.GuidNestedProjectEntryList,
            stringNestedProjectEntryList: null);
    }

    /// <summary>
    /// This solution is incomplete, the current code for this was just to get a "feel" for things.
    /// </summary>
    private List<IDotNetProject> SortProjectReferences(
        DotNetSolutionModel dotNetSolutionModel,
        StringBuilder tokenBuilder,
        StringBuilder formattedBuilder,
        List<TextEditorTextSpan> textSpanList,
        char[] getTextBuffer)
    {
        var moveUpDirectoryToken = $"..{IdeService.TextEditorService.CommonService.FileSystemProvider.DirectorySeparatorChar}";
        // "./" is being called the 'sameDirectoryToken'
        var sameDirectoryToken = $".{IdeService.TextEditorService.CommonService.FileSystemProvider.DirectorySeparatorChar}";
        
        var dotNetSolutionAncestorDirectoryList = dotNetSolutionModel.AbsolutePath.GetAncestorDirectoryList(
            IdeService.TextEditorService.CommonService.FileSystemProvider,
            tokenBuilder,
            formattedBuilder,
            AbsolutePathNameKind.NameWithExtension);
            
        List<string> relativePathReferenceList = new();
        var stringBuilder = new StringBuilder();
        var streamReaderWrap = new StreamReaderWrap();

        for (int i = dotNetSolutionModel.DotNetProjectList.Count - 1; i >= 0; i--)
        {
            relativePathReferenceList.Clear();
            stringBuilder.Clear();

            var projectTuple = dotNetSolutionModel.DotNetProjectList[i];

            // Debugging Linux-Ubuntu (2024-04-28)
            // -----------------------------------
            // It is believed, that Linux-Ubuntu is not fully working correctly,
            // due to the directory separator character at the os level being '/',
            // meanwhile the .NET solution has as its directory separator character '\'.
            //
            // Will perform a string.Replace("\\", "/") here. And if it solves the issue,
            // then some standard way of doing this needs to be made available in the IEnvironmentProvider.
            //
            // Okay, this single replacement fixes 99% of the solution explorer issue.
            // And I say 99% instead of 100% just because I haven't tested every single part of it yet.
            var relativePathFromSolutionFileString = projectTuple.RelativePathFromSolutionFileString;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                relativePathFromSolutionFileString = relativePathFromSolutionFileString.Replace("\\", "/");
            var absolutePathString = CommonFacts.GetAbsoluteFromAbsoluteAndRelative(
                dotNetSolutionModel.AbsolutePath,
                relativePathFromSolutionFileString,
                IdeService.TextEditorService.CommonService.FileSystemProvider,
                tokenBuilder,
                formattedBuilder,
                moveUpDirectoryToken: moveUpDirectoryToken,
                sameDirectoryToken: sameDirectoryToken,
                dotNetSolutionAncestorDirectoryList);
            projectTuple.AbsolutePath = new AbsolutePath(absolutePathString, false, IdeService.TextEditorService.CommonService.FileSystemProvider, tokenBuilder, formattedBuilder, AbsolutePathNameKind.NameWithExtension);

            if (!IdeService.TextEditorService.CommonService.FileSystemProvider.File.Exists(projectTuple.AbsolutePath.Value))
            {
                dotNetSolutionModel.DotNetProjectList.RemoveAt(i);
                continue;
            }

            projectTuple.ProjectReferencesOffset = dotNetSolutionModel.ProjectReferencesList.Count;

            var innerParentDirectory = projectTuple.AbsolutePath.CreateSubstringParentDirectory();
            if (innerParentDirectory is not null)
            {
                IdeService.TextEditorService.CommonService.FileSystemProvider.DeletionPermittedRegister(new(innerParentDirectory, true), tokenBuilder, formattedBuilder);
            }

            using StreamReader sr = new StreamReader(projectTuple.AbsolutePath.Value);
            textSpanList.Clear();
            streamReaderWrap.ReInitialize(sr);
            var lexerOutput = XmlLexer.Lex(streamReaderWrap, modelModifier: null, textSpanList);

            var outputReader = new XmlOutputReader(lexerOutput.TextSpanList);
            
            outputReader.FindTagGetAttributeValue(
                targetTagName: "ProjectReference",
                targetAttributeOne: "Include",
                shouldIncludeFullMissLines: false,
                sr,
                stringBuilder,
                getTextBuffer,
                relativePathReferenceList);

            var projectAncestorDirectoryList = projectTuple.AbsolutePath.GetAncestorDirectoryList(
                IdeService.TextEditorService.CommonService.FileSystemProvider,
                tokenBuilder,
                formattedBuilder,
                AbsolutePathNameKind.NameWithExtension);
            
            foreach (var projectReference in relativePathReferenceList)
            {
                var referenceProjectAbsolutePathString = CommonFacts.GetAbsoluteFromAbsoluteAndRelative(
                    projectTuple.AbsolutePath,
                    projectReference,
                    IdeService.TextEditorService.CommonService.FileSystemProvider,
                    tokenBuilder,
                    formattedBuilder,
                    moveUpDirectoryToken: moveUpDirectoryToken,
                    sameDirectoryToken: sameDirectoryToken,
                    projectAncestorDirectoryList);

                dotNetSolutionModel.ProjectReferencesList.Add(AbsolutePath.GetFormattedStringOnly(
                    referenceProjectAbsolutePathString,
                    false,
                    IdeService.TextEditorService.CommonService.FileSystemProvider,
                    tokenBuilder,
                    formattedBuilder));
                ++projectTuple.ProjectReferencesLength;
            }
        }

        var upperLimit = dotNetSolutionModel.DotNetProjectList.Count > 4 // Extremely arbitrary number being used here.
            ? 4
            : dotNetSolutionModel.DotNetProjectList.Count;
        for (int outerIndex = 0; outerIndex < upperLimit; outerIndex++)
        {
            for (int i = 0; i < dotNetSolutionModel.DotNetProjectList.Count; i++)
            {
                var projectTuple = dotNetSolutionModel.DotNetProjectList[i];

                for (int indexReference = projectTuple.ProjectReferencesOffset; indexReference < projectTuple.ProjectReferencesOffset + projectTuple.ProjectReferencesLength; indexReference++)
                {
                    if (indexReference < dotNetSolutionModel.ProjectReferencesList.Count)
                    {
                        var referenceAbsolutePath = dotNetSolutionModel.ProjectReferencesList[indexReference];
                        var referenceIndex = dotNetSolutionModel.DotNetProjectList
                            .FindIndex(x => x.AbsolutePath.Value == referenceAbsolutePath);
    
                        if (referenceIndex > i)
                        {
                            var indexDestination = i - 1;
                            if (indexDestination == -1)
                                indexDestination = 0;
    
                            MoveAndShiftList(
                                dotNetSolutionModel.DotNetProjectList,
                                indexSource: referenceIndex,
                                indexDestination);
                        }
                    }
                }
            }
        }

        return dotNetSolutionModel.DotNetProjectList;
    }

    private void MoveAndShiftList(
        List<IDotNetProject> enumeratingProjectTupleList,
        int indexSource,
        int indexDestination)
    {
        if (indexSource == 1 && indexDestination == 0)
        {
            var otherTemporary = enumeratingProjectTupleList[indexDestination];
            enumeratingProjectTupleList[indexDestination] = enumeratingProjectTupleList[indexSource];
            enumeratingProjectTupleList[indexSource] = otherTemporary;
            return;
        }

        var temporary = enumeratingProjectTupleList[indexDestination];
        enumeratingProjectTupleList[indexDestination] = enumeratingProjectTupleList[indexSource];

        for (int i = indexSource; i > indexDestination; i--)
        {
            if (i - 1 == indexDestination)
                enumeratingProjectTupleList[i] = temporary;
            else
                enumeratingProjectTupleList[i] = enumeratingProjectTupleList[i - 1];
        }
    }

    private bool ParseSolution(
        TextEditorEditContext editContext,
        Key<DotNetSolutionModel> dotNetSolutionModelKey,
        CompilationUnitKind compilationUnitKind)
    {
        var dotNetSolutionState = GetDotNetSolutionState();

        var dotNetSolutionModel = dotNetSolutionState.DotNetSolutionModel;

        if (dotNetSolutionModel is null)
            return false;

        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var progressBarModel = new ProgressBarModel(0, "parsing...")
        {
            OnCancelFunc = () =>
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                return Task.CompletedTask;
            }
        };

        CommonFacts.DispatchProgress(
            $"Parse: {dotNetSolutionModel.AbsolutePath.Name}",
            progressBarModel,
            IdeService.TextEditorService.CommonService,
            TimeSpan.FromMilliseconds(-1));

        StartupControlModel originallyActiveStartupControl = default;
        
        try
        {
            var localStartupControlState = IdeService.GetIdeStartupControlState();
            foreach (var x in localStartupControlState.StartupControlList)
            {
                if (x.StartupProjectAbsolutePath.Value == localStartupControlState.ActiveStartupProjectAbsolutePathValue)
                {
                    originallyActiveStartupControl = x;
                    break;
                }
            }
        
            RegisterStartupControl_Range(dotNetSolutionModel.DotNetProjectList);

            var previousStageProgress = 0.05;
            var dotNetProjectListLength = dotNetSolutionModel.DotNetProjectList.Count;
            var projectsParsedCount = 0;
            foreach (var project in dotNetSolutionModel.DotNetProjectList)
            {
                // foreach project in solution
                //     foreach C# file in project
                //         EnqueueBackgroundTask(async () =>
                //         {
                //             ParseCSharpFile();
                //             UpdateProgressBar();
                //         });
                //
                // Treat every project as an equal weighting with relation to remaining percent to complete
                // on the progress bar.
                //
                // If the project were to be parsed, how much would it move the percent progress completed by?
                //
                // Then, in order to see progress while each C# file in the project gets parsed,
                // multiply the percent progress this project can provide by the proportion
                // of the project's C# files which have been parsed.
                var maximumProgressAvailableToProject = (1 - previousStageProgress) * ((double)1.0 / dotNetProjectListLength);
                var currentProgress = Math.Min(1.0, previousStageProgress + maximumProgressAvailableToProject * projectsParsedCount);

                // This 'SetProgress' is being kept out the throttle, since it sets message 1
                // whereas the per class progress updates set message 2.
                //
                // Otherwise an update to message 2 could result in this message 1 update never being written.
                progressBarModel.SetProgress(
                    currentProgress,
                    project.AbsolutePath.Name);

                cancellationToken.ThrowIfCancellationRequested();

                DiscoverClassesInProject(editContext, project, compilationUnitKind);
                projectsParsedCount++;
            }

            progressBarModel.SetProgress(1, $"Finished parsing: {dotNetSolutionModel.AbsolutePath.Name}", string.Empty);
            progressBarModel.Dispose();

            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine("===========\n");
        
            if (e is OperationCanceledException)
                progressBarModel.IsCancelled = true;

            var currentProgress = progressBarModel.GetProgress();

            progressBarModel.SetProgress(currentProgress, e.ToString());
            progressBarModel.Dispose();

            return false;
        }
        finally
        {
            if (originallyActiveStartupControl.StartupProjectAbsolutePath.Value is not null)
            {
                var localStartupControlState = IdeService.GetIdeStartupControlState();
                
                foreach (var startupControl in localStartupControlState.StartupControlList)
                {
                    if (startupControl.Title == originallyActiveStartupControl.Title  &&
                        startupControl.StartupProjectAbsolutePath.Value == originallyActiveStartupControl.StartupProjectAbsolutePath.Value)
                    {
                        IdeService.Ide_SetActiveStartupControlKey(startupControl.StartupProjectAbsolutePath.Value);
                    }
                }
            }
        }
    }

    private void DiscoverClassesInProject(
        TextEditorEditContext editContext,
        IDotNetProject dotNetProject,
        CompilationUnitKind compilationUnitKind)
    {
        if (!IdeService.TextEditorService.CommonService.FileSystemProvider.File.Exists(dotNetProject.AbsolutePath.Value))
            return; // TODO: This can still cause a race condition exception if the file is removed before the next line runs.

        var parentDirectory = dotNetProject.AbsolutePath.CreateSubstringParentDirectory();
        if (parentDirectory is null)
            return;

        var compilerService = (CSharpCompilerService)IdeService.TextEditorService.GetCompilerService("cs");

        ParseFilesRecursive(parentDirectory, compilerService, editContext, compilationUnitKind);
    }

    /// <summary>
    /// The google AI Overview for "c# enumerate files recursively but exclude certain directories" gave me a near perfect method implementation for this.
    /// 
    /// This entire situation is a huge pain because I'm so strict throughout the codebase with how the formatting of the path is.
    /// When I use DirectoryInfo I get the drive prepended to the path and windows directory separators and it breaks everything.
    /// </summary>
    private void ParseFilesRecursive(
        string currentDirectory,
        CSharpCompilerService compilerService,
        TextEditorEditContext editContext,
        CompilationUnitKind compilationUnitKind)
    {
        // Enumerate files in the current directory
        foreach (string file in Directory.EnumerateFiles(currentDirectory))
        {
            if (file.EndsWith(".cs"))
            {
                var resourceUri = new ResourceUri(file);
                compilerService.TryAddFileAbsolutePath(file);

                compilerService.RegisterResource(
                    resourceUri,
                    shouldTriggerResourceWasModified: false);

                compilerService.FastParse(editContext, resourceUri, IdeService.TextEditorService.CommonService.FileSystemProvider, compilationUnitKind);
            }
        }

        // Enumerate subdirectories
        foreach (string subDirectory in Directory.EnumerateDirectories(currentDirectory))
        {
            // Check if the subdirectory should be excluded
            if (!IFileSystemProvider.IsDirectoryIgnored(subDirectory))
            {
                // Recursively call for non-excluded subdirectories
                ParseFilesRecursive(subDirectory, compilerService, editContext, compilationUnitKind);
            }
        }
    }

    public async ValueTask Do_SetDotNetSolutionTreeView(Key<DotNetSolutionModel> dotNetSolutionModelKey)
    {
        // !!!!!!!!!!!!!
        // This is probably the most important thing to do next.
        //
        // I changed my AppData so the app would auto open a .NET Solution without
        // having to use the input file dialog (which is currently broken).
        // !!!!!!!!!!!!!
        
        var dotNetSolutionState = GetDotNetSolutionState();
        
        var dotNetSolutionModel = dotNetSolutionState.DotNetSolutionModel;
        if (dotNetSolutionModel is null)
            return;
            
        // I said there is an emphasis on tying the TreeViewContainer directly to the UI lifecycle
        // and then DisposeContainer() is invoked when the UI disappears
        // was an opportunity to create more optimized storage of the TreeView
        /// so you could recreate it when the UI later was shown again.
        //
        // I need to just get something on sscreen though so I'm gonna ignore what I said for now.
        //
        // Oh no way my mom might've just gotten home with food totally epic
        
        if (!CommonService.TryGetTreeViewContainer(DotNetSolutionState.TreeViewSolutionExplorerStateKey, out var treeViewContainer))
        {
            treeViewContainer = new SolutionExplorerTreeViewContainer(IdeService, dotNetSolutionModel);
            
            var rootNode = new TreeViewNodeValue
            {
                ParentIndex = -1,
                IndexAmongSiblings = 0,
                ChildListOffset = treeViewContainer.NodeValueList.Count,
                ChildListLength = 0,
                TreeViewNodeValueKind = TreeViewNodeValueKind.b0,
                TraitsIndex = 0,
                IsExpandable = true,
                IsExpanded = true
            };
            treeViewContainer.NodeValueList.Add(rootNode);

            await treeViewContainer.LoadChildListAsync(indexNodeValue: 0).ConfigureAwait(false);

            CommonService.TreeView_RegisterContainerAction(treeViewContainer);
            
            
        }
        
        /*
        var dotNetSolutionState = GetDotNetSolutionState();

        var dotNetSolutionModel = dotNetSolutionState.DotNetSolutionModel;
        if (dotNetSolutionModel is null)
            return;

        

        if (!IdeService.CommonService.TryGetTreeViewContainer(DotNetSolutionState.TreeViewSolutionExplorerStateKey, out var treeViewContainer))
        {
            IdeService.CommonService.TreeView_RegisterContainerAction(new TreeViewContainer(
                DotNetSolutionState.TreeViewSolutionExplorerStateKey,
                rootNode: null,
                selectedNodeList: Array.Empty<TreeViewNoType>()));
        }
        if (IdeService.CommonService.TryGetTreeViewContainer(DotNetSolutionState.TreeViewSolutionExplorerStateKey, out treeViewContainer))
        {
            await rootNode.LoadChildListAsync(treeViewContainer).ConfigureAwait(false);
            
            IdeService.CommonService.TreeView_WithRootNodeAction(DotNetSolutionState.TreeViewSolutionExplorerStateKey, rootNode);
            
            IdeService.CommonService.TreeView_SetActiveNodeAction(
                DotNetSolutionState.TreeViewSolutionExplorerStateKey,
                rootNode,
                true,
                false);
        }
        */
    }

    private void RegisterStartupControl_Range(List<IDotNetProject> projectList)
    {
        var startupControlList = new List<StartupControlModel>(capacity: projectList.Count);
        var startupControlAbsolutePathValueHashSet = new HashSet<string>(capacity: projectList.Count);
        
        foreach (var project in projectList)
        {
            if (startupControlAbsolutePathValueHashSet.Add(project.AbsolutePath.Value))
            {
                startupControlList.Add(new StartupControlModel(
                    project.DisplayName,
                    project.AbsolutePath));
            }
        }
    
        IdeService.Ide_SetStartupControlList(startupControlList);
    }

    private ValueTask Do_Website_AddExistingProjectToSolution(
        Key<DotNetSolutionModel> dotNetSolutionModelKey,
        string projectTemplateShortName,
        string cSharpProjectName,
        AbsolutePath cSharpProjectAbsolutePath)
    {
        return ValueTask.CompletedTask;
    }
    #endregion
    
    public Task StartButtonOnClick(StartupControlModel startupControlModel)
    {
        var ancestorDirectory = startupControlModel.StartupProjectAbsolutePath.CreateSubstringParentDirectory();
        if (ancestorDirectory is null)
        {
            return Task.CompletedTask;
        }

        var formattedCommandValue = DotNetCliCommandFormatter.FormatStartProjectWithoutDebugging(
            startupControlModel.StartupProjectAbsolutePath);

        var terminalCommandRequest = new TerminalCommandRequest(
            formattedCommandValue,
            ancestorDirectory,
            NewDotNetSolutionTerminalCommandRequestKey)
        {
            BeginWithFunc = parsedCommand =>
            {
                ParseOutputEntireDotNetRun(
                    string.Empty,
                    "Run-Project_started");
    
                return Task.CompletedTask;
            },
            ContinueWithFunc = parsedCommand =>
            {
                IdeService.Ide_TriggerStartupControlStateChanged(executingTerminalCommandRequest: null);

                ParseOutputEntireDotNetRun(
                    parsedCommand.OutputCache.ToString(),
                    "Run-Project_completed");

                return Task.CompletedTask;
            }
        };
        
        IdeService.Ide_TriggerStartupControlStateChanged(terminalCommandRequest);
        
        IdeService.GetTerminalState().ExecutionTerminal.EnqueueCommand(terminalCommandRequest);
        return Task.CompletedTask;
    }

    public Task StopButtonOnClick(StartupControlModel startupControlModel)
    {
        IdeService.GetTerminalState().ExecutionTerminal.KillProcess();
        IdeService.Ide_TriggerStartupControlStateChanged(executingTerminalCommandRequest: null);
        return Task.CompletedTask;
    }
}
