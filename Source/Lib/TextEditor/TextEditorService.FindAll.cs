using Clair.Common.RazorLib;
using Clair.Common.RazorLib.FileSystems.Models;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.Reactives.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models.Internals;

namespace Clair.TextEditor.RazorLib;

public partial class TextEditorService
{
    private readonly object _stateModificationLock = new();

    private CancellationTokenSource _searchCancellationTokenSource = new();
    private TextEditorFindAllState _findAllState = new();

    public TextEditorFindAllState GetFindAllState() => _findAllState;

    public void SetSearchQuery(string searchQuery)
    {
        lock (_stateModificationLock)
        {
            _findAllState = _findAllState with
            {
                SearchQuery = searchQuery
            };
        }

        SecondaryChanged?.Invoke(SecondaryChangedKind.FindAllStateChanged);
    }

    public void SetStartingDirectoryPath(string startingDirectoryPath)
    {
        lock (_stateModificationLock)
        {
            _findAllState = _findAllState with
            {
                StartingDirectoryPath = startingDirectoryPath
            };
        }

        SecondaryChanged?.Invoke(SecondaryChangedKind.FindAllStateChanged);
    }

    public void CancelSearch()
    {
        lock (_stateModificationLock)
        {
            _searchCancellationTokenSource.Cancel();
            _searchCancellationTokenSource = new();
        }

        SecondaryChanged?.Invoke(SecondaryChangedKind.FindAllStateChanged);
    }

    public void SetProgressBarModel(ProgressBarModel progressBarModel)
    {
        /*
        // 2025-10-22 (rewrite TreeViews)
        lock (_stateModificationLock)
        {
            _findAllState = _findAllState with
            {
                ProgressBarModel = progressBarModel,
            };
        }
        */

        SecondaryChanged?.Invoke(SecondaryChangedKind.FindAllStateChanged);
    }

    public void FlushSearchResults(List<(string SourceText, ResourceUri ResourceUri, TextEditorTextSpan TextSpan)> searchResultList)
    {
        /*
        lock (_stateModificationLock)
        {
            var inState = GetFindAllState();

            List<(string SourceText, ResourceUri ResourceUri, TextEditorTextSpan TextSpan)> localSearchResultList;
            lock (_flushSearchResultsLock)
            {
                localSearchResultList = new List<(string SourceText, ResourceUri ResourceUri, TextEditorTextSpan TextSpan)>(inState.SearchResultList);
                localSearchResultList.AddRange(searchResultList);
                searchResultList.Clear();
            }

            _findAllState = inState with
            {
                SearchResultList = localSearchResultList
            };
        }
        */

        SecondaryChanged?.Invoke(SecondaryChangedKind.FindAllStateChanged);
    }

    public void ClearSearch()
    {
        lock (_stateModificationLock)
        {
            var inState = GetFindAllState();

            _findAllState = inState with
            {
                SearchResultList = new()
            };
        }

        SecondaryChanged?.Invoke(SecondaryChangedKind.FindAllStateChanged);
    }

    public Task HandleStartSearchAction()
    {
        CommonService.TreeView_DisposeContainerAction(TextEditorFindAllState.TreeViewFindAllContainerKey, shouldFireStateChangedEvent: false);
        
        var textEditorFindAllState = GetFindAllState();
        var dotNetSolutionState = GetDotNetSolutionState();
        var solutionModel = dotNetSolutionState.DotNetSolutionModel;
        
        if (string.IsNullOrWhiteSpace(textEditorFindAllState.SearchQuery))
            return;
        
        StreamReaderPooledBuffer streamReaderPooledBuffer;
        StreamReaderPooledBufferWrap streamReaderPooledBufferWrap = new();
        
        var searchResultList = new List<(ResourceUri ResourceUri, TextEditorTextSpan TextSpan)>();
        
        try
        {
            var parentDirectory = solutionModel.AbsolutePath.CreateSubstringParentDirectory();
            if (parentDirectory is null)
                return;
    
            var utf8Encoding = Encoding.UTF8;
            var utf8_MaxCharCount = utf8Encoding.GetMaxCharCount(StreamReaderPooledBuffer.DefaultBufferSize);
            
            streamReaderPooledBuffer = new StreamReaderPooledBuffer(
                stream: null,
                utf8Encoding,
                byteBuffer: new byte[StreamReaderPooledBuffer.DefaultBufferSize],
                charBuffer: new char[utf8_MaxCharCount]);
            
            ParseFilesRecursive(searchResultList, textEditorFindAllState.SearchQuery, parentDirectory, streamReaderPooledBufferWrap, streamReaderPooledBuffer));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally
        {
            streamReaderPooledBuffer?.Dispose();
        }

        return Task.CompletedTask;
    }
    
    /// <summary>
    /// The google AI Overview for "c# enumerate files recursively but exclude certain directories" gave me a near perfect method implementation for this.
    /// 
    /// This entire situation is a huge pain because I'm so strict throughout the codebase with how the formatting of the path is.
    /// When I use DirectoryInfo I get the drive prepended to the path and windows directory separators and it breaks everything.
    /// </summary>
    private void ParseFilesRecursive(List<(ResourceUri ResourceUri, TextEditorTextSpan TextSpan)> searchResultList, string search, string currentDirectory, StreamReaderPooledBufferWrap streamReaderPooledBufferWrap, StreamReaderPooledBuffer streamReaderPooledBuffer)
    {
        // Enumerate files in the current directory
        foreach (string file in Directory.EnumerateFiles(currentDirectory))
        {
            // TODO: Don't hardcode file extensions here to avoid searching through them.
            //       Reason being, hardcoding them isn't going to work well as a long term solution.
            //       How does one detect if a file is not text?
            //       |
            //       I seem to get away with opening some non-text files, but I think a gif I opened
            //       had 1 million characters in it? So this takes 2 million bytes in a 2byte char?
            //       I'm not sure exactly what happened, I opened the gif and the app froze,
            //       I saw the character only at a glance. (2024-07-20)
            if (file.EndsWith(".jpg") ||
                file.EndsWith(".png") ||
                file.EndsWith(".pdf") ||
                file.EndsWith(".gif"))
            {
                continue;
            }
            
            var resourceUri = new ResourceUri(file);
            
            MemoryStream memoryStream;
            StreamReaderPooledBuffer sr;
            
            if (TextEditorState._modelMap.TryGetValue(resourceUri, out var textEditorModel))
            {
                streamReaderPooledBuffer.DiscardBufferedData(
                    new MemoryStream(Encoding.UTF8.GetBytes(textEditorModel.GetAllText());
            }
            else
            {
                streamReaderPooledBuffer.DiscardBufferedData(
                    new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, StreamReaderPooledBuffer.DefaultFileStreamBufferSize));
            }
            
            streamReaderPooledBufferWrap.ReInitialize(streamReaderPooledBuffer);
            
            var positionInSearch;
            bool fileContainedSearch = false;
            
            while (!streamReaderPooledBufferWrap.IsEof)
            {
                if (streamReaderPooledBufferWrap.CurrentCharacter == search[positionInSearch])
                {
                    positionInSearch++;
                    
                    while (!streamReaderPooledBufferWrap.IsEof)
                    {
                        if (positionInSearch == positionInSearch)
                        {
                            positionInSearch = 0;
                            fileContainedSearch = true;
                            break;
                        }
                        else if (streamReaderPooledBufferWrap.CurrentCharacter != search[positionInSearch])
                        {
                            positionInSearch = 0;
                            break;
                        }
                        else
                        {
                            positionInSearch++;
                            _ = streamReaderPooledBufferWrap.ReadCharacter();
                        }
                    }
                    
                    if (fileContainedSearch)
                    {
                        searchResultList.Add((resourceUri, default(TextEditorTextSpan)));
                        break;
                    }
                }
            
                _ = streamReaderPooledBufferWrap.ReadCharacter();
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

    private async Task StartSearchTask(
        ProgressBarModel progressBarModel,
        TextEditorFindAllState textEditorFindAllState,
        CancellationToken cancellationToken)
    {
        var filesProcessedCount = 0;
        var textSpanList = new List<(string SourceText, ResourceUri ResourceUri, TextEditorTextSpan TextSpan)>();
        var searchException = (Exception?)null;

        try
        {
            ShowFilesProcessedCountOnUi(0);
            await RecursiveSearch(textEditorFindAllState.StartingDirectoryPath);
        }
        catch (Exception e)
        {
            searchException = e;
        }
        finally
        {
            FlushSearchResults(textSpanList);

            if (searchException is null)
            {
                ShowFilesProcessedCountOnUi(1, true);
            }
            else
            {
                progressBarModel.SetProgress(
                    progressBarModel.DecimalPercentProgress,
                    searchException.ToString());

                progressBarModel.Dispose();
                // The use of '_textEditorFindAllStateWrap.Value' is purposeful.
                ConstructTreeView(GetFindAllState());
                SetProgressBarModel(progressBarModel);
            }
        }

        async Task RecursiveSearch(string directoryPath)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Considering the use a breadth first algorithm

            // Search Files
            {
                var childFileList = await CommonService.FileSystemProvider.Directory
                    .GetFilesAsync(directoryPath)
                    .ConfigureAwait(false);

                foreach (var childFile in childFileList)
                {
                    // TODO: Don't hardcode file extensions here to avoid searching through them.
                    //       Reason being, hardcoding them isn't going to work well as a long term solution.
                    //       How does one detect if a file is not text?
                    //       |
                    //       I seem to get away with opening some non-text files, but I think a gif I opened
                    //       had 1 million characters in it? So this takes 2 million bytes in a 2byte char?
                    //       I'm not sure exactly what happened, I opened the gif and the app froze,
                    //       I saw the character only at a glance. (2024-07-20)
                    if (!childFile.EndsWith(".jpg") &&
                        !childFile.EndsWith(".png") &&
                        !childFile.EndsWith(".pdf") &&
                        !childFile.EndsWith(".gif"))
                    {
                        await PerformSearchFile(childFile).ConfigureAwait(false);
                    }

                    filesProcessedCount++;
                    ShowFilesProcessedCountOnUi(0);
                }
            }

            // Recurse into subdirectories
            {
                var subdirectoryList = await CommonService.FileSystemProvider.Directory
                    .GetDirectoriesAsync(directoryPath)
                    .ConfigureAwait(false);

                foreach (var subdirectory in subdirectoryList)
                {
                    if (IFileSystemProvider.IsDirectoryIgnored(subdirectory))
                        continue;

                    await RecursiveSearch(subdirectory).ConfigureAwait(false);
                }
            }
        }

        async Task PerformSearchFile(string filePath)
        {
            var text = await CommonService.FileSystemProvider.File.ReadAllTextAsync(filePath).ConfigureAwait(false);
            var query = textEditorFindAllState.SearchQuery;

            var matchedTextSpanList = new List<(string SourceText, ResourceUri ResourceUri, TextEditorTextSpan TextSpan)>();

            for (int outerI = 0; outerI < text.Length; outerI++)
            {
                if (outerI + query.Length <= text.Length)
                {
                    int innerI = 0;
                    for (; innerI < query.Length; innerI++)
                    {
                        if (text[outerI + innerI] != query[innerI])
                            break;
                    }

                    if (innerI == query.Length)
                    {
                        // Then the entire query was matched
                        matchedTextSpanList.Add(
                            (
                                text,
                                new ResourceUri(filePath),
                                new TextEditorTextSpan(
                                    outerI,
                                    outerI + innerI,
                                    (byte)FindOverlayDecorationKind.LongestCommonSubsequence)
                            ));
                    }
                }
            }

            foreach (var matchedTextSpan in matchedTextSpanList)
            {
                textSpanList.Add(matchedTextSpan);
            }
        }

        void ShowFilesProcessedCountOnUi(double decimalPercentProgress, bool shouldDisposeProgressBarModel = false)
        {
            /*
            // 2025-10-22 (rewrite TreeViews)
            _throttleUiUpdate.Run(_ =>
            {
                progressBarModel.SetProgress(
                    decimalPercentProgress,
                    $"{filesProcessedCount:N0} files processed");

                if (shouldDisposeProgressBarModel)
                {
                    progressBarModel.Dispose();
                    // The use of 'GetFindAllState()' is purposeful.
                    ConstructTreeView(GetFindAllState());
                    SetProgressBarModel(progressBarModel);
                }

                return Task.CompletedTask;
            });
            */
        }
    }

    private void ConstructTreeView(TextEditorFindAllState textEditorFindAllState)
    {
        var flatListVersion = CommonService.TreeView_GetNextFlatListVersion(TextEditorFindAllState.TreeViewFindAllContainerKey);
        CommonService.TreeView_DisposeContainerAction(TextEditorFindAllState.TreeViewFindAllContainerKey, shouldFireStateChangedEvent: false);
        
        var container = new TreeViewContainer(
    		TextEditorFindAllState.TreeViewFindAllContainerKey,
    		rootNode: null,
    		selectedNodeList: Array.Empty<TreeViewNodeValue>());
    
        var groupedResults = textEditorFindAllState.SearchResultList.GroupBy(x => x.ResourceUri);

        var tokenBuilder = new StringBuilder();
        var formattedBuilder = new StringBuilder();
        
        var treeViewList = groupedResults.Select(group =>
        {
            var absolutePath = new AbsolutePath(
                group.Key.Value,
                false,
                CommonService.FileSystemProvider,
                tokenBuilder,
                formattedBuilder,
                AbsolutePathNameKind.NameWithExtension);

            return (TreeViewNoType)new TreeViewFindAllGroup(
                group.Select(textSpan => new TreeViewFindAllTextSpan(
                    textSpan,
                    absolutePath,
                    false,
                    false)).ToList(),
                absolutePath,
                true,
                false);
        }).ToArray();

        var adhocRoot = TreeViewAdhoc.ConstructTreeViewAdhoc(container, treeViewList);
        var firstNode = treeViewList.FirstOrDefault();

        IReadOnlyList<TreeViewNoType> activeNodes = firstNode is null
            ? Array.Empty<TreeViewNoType>()
            : new List<TreeViewNoType> { firstNode };
        
        container = container with
        {
            RootNode = adhocRoot,
            SelectedNodeList = activeNodes,
            FlatListVersion = flatListVersion
        };
        
        CommonService.TreeView_RegisterContainerAction(
        	container,
        	shouldFireStateChangedEvent: true);
    }

    public void Dispose()
    {
        CancelSearch();
    }

    public record struct TextEditorFindAllState(
        string SearchQuery,
        string StartingDirectoryPath,
        List<(ResourceUri ResourceUri, TextEditorTextSpan TextSpan)> SearchResultList)
    {
        public static readonly Key<TreeViewContainer> TreeViewFindAllContainerKey = Key<TreeViewContainer>.NewKey();

        public TextEditorFindAllState() : this(
            SearchQuery: string.Empty,
            StartingDirectoryPath: string.Empty,
            SearchResultList: new())
        {
        }
    }
}
