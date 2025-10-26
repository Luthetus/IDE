using System.Text;
using Clair.Common.RazorLib;
using Clair.Common.RazorLib.FileSystems.Models;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.Reactives.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models.Internals;
using Clair.TextEditor.RazorLib.FindAlls.Models;

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

    public void SetStartingDirectoryPath(string startingDirectoryPath, IEnumerable<AbsolutePath> projectList)
    {
        lock (_stateModificationLock)
        {
            _findAllState = _findAllState with
            {
                StartingDirectoryPath = startingDirectoryPath,
                ProjectList = projectList
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
        // var solutionModel = dotNetSolutionState.DotNetSolutionModel;
        
        if (string.IsNullOrWhiteSpace(textEditorFindAllState.SearchQuery) ||
            string.IsNullOrWhiteSpace(textEditorFindAllState.StartingDirectoryPath))
        {
            return Task.CompletedTask;
        }
        
        StreamReaderPooledBuffer? streamReaderPooledBuffer = null;
        StreamReaderPooledBufferWrap streamReaderPooledBufferWrap = new();
        
        var searchResultList = new List<(ResourceUri ResourceUri, TextEditorTextSpan TextSpan)>();
        var projectSeenHashSet = new HashSet<string>();
        
        Exception? exception = null;
        
        try
        {
            var parentDirectory = textEditorFindAllState.StartingDirectoryPath;
            if (parentDirectory is null)
                return Task.CompletedTask;
    
            var utf8Encoding = Encoding.UTF8;
            var utf8_MaxCharCount = utf8Encoding.GetMaxCharCount(StreamReaderPooledBuffer.DefaultBufferSize);
            
            streamReaderPooledBuffer = new StreamReaderPooledBuffer(
                stream: null,
                utf8Encoding,
                byteBuffer: new byte[StreamReaderPooledBuffer.DefaultBufferSize],
                charBuffer: new char[utf8_MaxCharCount]);
            
            var tokenBuilder = new StringBuilder();
            var formattedBuilder = new StringBuilder();
            
            ParseFilesRecursive(tokenBuilder, formattedBuilder, projectSeenHashSet, searchResultList, textEditorFindAllState.SearchQuery, parentDirectory, streamReaderPooledBufferWrap, streamReaderPooledBuffer);
            
            foreach (var projectAbsolutePath in textEditorFindAllState.ProjectList)
            {
                if (projectSeenHashSet.Add(projectAbsolutePath.Value))
                    ParseFilesRecursive(tokenBuilder, formattedBuilder, projectSeenHashSet, searchResultList, textEditorFindAllState.SearchQuery, projectAbsolutePath.CreateSubstringParentDirectory(), streamReaderPooledBufferWrap, streamReaderPooledBuffer);
            }
            
            // Track the .csproj you've seen when recursing from the parent dir of the .NET solution.
            // 
            // Then iterate over every .csproj that the .NET solution specifies in the .sln file.
            //
            // Each iteration will recurse from the parent dir of that .csproj 
            // BUT at the start of each .csproj check whether the .sln recurse step had seen the .csproj file already.
            // IF SO, then skip that iteration.
        }
        catch (Exception e)
        {
            exception = e;
            Console.WriteLine(e);
        }
        finally
        {
            streamReaderPooledBuffer?.Dispose();
            
            var findAllTreeViewContainer = new FindAllTreeViewContainer(this, searchResultList);
            
            var rootNode = new TreeViewNodeValue
            {
                ParentIndex = -1,
                IndexAmongSiblings = 0,
                ChildListOffset = 1,
                ChildListLength = 0,
                ByteKind = FindAllTreeViewContainer.ByteKind_Aaa,
                TraitsIndex = 0,
                IsExpandable = true,
                IsExpanded = true
            };
            findAllTreeViewContainer.NodeValueList.Add(rootNode);

            var groupIndexAmongSiblings = 0;
            
            // you have to iterate once to get the groups,
            // then iterate the groups to get the children of groups
            // because children MUST be contiguous in the NodeValueList.
            
            var previousResourceUri = ResourceUri.Empty;
            
            for (int i = 0; i < findAllTreeViewContainer.SearchResultList.Count; i++)
            {
                var groupByFileSearchResult = findAllTreeViewContainer.SearchResultList[i];
                
                if (previousResourceUri != groupByFileSearchResult.ResourceUri)
                {
                    previousResourceUri = groupByFileSearchResult.ResourceUri;
                    findAllTreeViewContainer.NodeValueList.Add(new TreeViewNodeValue
                    {
                        ParentIndex = 0,
                        IndexAmongSiblings = groupIndexAmongSiblings++,
                        ChildListOffset = 0,
                        ChildListLength = 0,
                        ByteKind = FindAllTreeViewContainer.ByteKind_SearchResultGroup,
                        TraitsIndex = i,
                        IsExpandable = true,
                        IsExpanded = false
                    });
                }
            }
            findAllTreeViewContainer.NodeValueList[0] = findAllTreeViewContainer.NodeValueList[0] with
            {
                ChildListLength = findAllTreeViewContainer.NodeValueList.Count - 1
            };

            for (int outerIndex = findAllTreeViewContainer.NodeValueList[0].ChildListOffset; outerIndex < findAllTreeViewContainer.NodeValueList[0].ChildListOffset + findAllTreeViewContainer.NodeValueList[0].ChildListLength; outerIndex++)
            {
                var groupNodeValue = findAllTreeViewContainer.NodeValueList[outerIndex];
                var groupSearchResult = findAllTreeViewContainer.SearchResultList[groupNodeValue.TraitsIndex];
                var childListOffset = findAllTreeViewContainer.NodeValueList.Count;
                var childListLength = 0;
                
                var indexSearchResult = groupNodeValue.TraitsIndex;
                while (indexSearchResult < findAllTreeViewContainer.SearchResultList.Count)
                {
                    var childSearchResult = findAllTreeViewContainer.SearchResultList[indexSearchResult];
                    if (childSearchResult.ResourceUri == groupSearchResult.ResourceUri)
                    {
                        findAllTreeViewContainer.NodeValueList.Add(new TreeViewNodeValue
                        {
                            ParentIndex = outerIndex,
                            IndexAmongSiblings = childListLength++,
                            ChildListOffset = 0,
                            ChildListLength = 0,
                            ByteKind = FindAllTreeViewContainer.ByteKind_SearchResult,
                            TraitsIndex = indexSearchResult,
                            IsExpandable = false,
                            IsExpanded = false
                        });
                    }
                    else
                    {
                        break;
                    }
                    ++indexSearchResult;
                }
                findAllTreeViewContainer.NodeValueList[outerIndex] = findAllTreeViewContainer.NodeValueList[outerIndex] with
                {
                    ChildListOffset = childListOffset,
                    ChildListLength = childListLength
                };
            }
            
            lock (_stateModificationLock)
            {
                CommonService.TreeView_RegisterContainerAction(findAllTreeViewContainer);
                _findAllState = _findAllState with
                {
                    SearchResultList = searchResultList,
                    Exception = exception
                };
            }
            
            SecondaryChanged?.Invoke(SecondaryChangedKind.FindAllStateChanged);
        }

        return Task.CompletedTask;
    }
    
    /// <summary>
    /// The google AI Overview for "c# enumerate files recursively but exclude certain directories" gave me a near perfect method implementation for this.
    /// 
    /// This entire situation is a huge pain because I'm so strict throughout the codebase with how the formatting of the path is.
    /// When I use DirectoryInfo I get the drive prepended to the path and windows directory separators and it breaks everything.
    /// </summary>
    private void ParseFilesRecursive(
        StringBuilder tokenBuilder,
        StringBuilder formattedBuilder,
        HashSet<string> projectSeenHashSet,
        List<(ResourceUri ResourceUri, TextEditorTextSpan TextSpan)> searchResultList,
        string search,
        string currentDirectory,
        StreamReaderPooledBufferWrap streamReaderPooledBufferWrap,
        StreamReaderPooledBuffer streamReaderPooledBuffer)
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
            if (file.EndsWith(".csproj"))
            {
                Console.WriteLine("file.EndsWith(...):" + file);
                projectSeenHashSet.Add(AbsolutePath.GetFormattedStringOnly(
                    file,
                    isDirectory: false,
                    fileSystemProvider: CommonService.FileSystemProvider,
                    tokenBuilder,
                    formattedBuilder));
            }
            
            var resourceUri = new ResourceUri(file);
            
            MemoryStream memoryStream;
            StreamReaderPooledBuffer sr;
            
            if (TextEditorState._modelMap.TryGetValue(resourceUri, out var textEditorModel))
            {
                // If the formatting of the absolute path is off in any way
                // then the model won't be found.
                //
                // I don't think this conditional branch is getting hit, presumably the
                // absolute path always is slightly different.
                //
                streamReaderPooledBuffer.DiscardBufferedData(
                    new MemoryStream(Encoding.UTF8.GetBytes(textEditorModel.GetAllText())));
            }
            else
            {
                streamReaderPooledBuffer.DiscardBufferedData(
                    new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, StreamReaderPooledBuffer.DefaultFileStreamBufferSize));
            }
            
            streamReaderPooledBufferWrap.ReInitialize(streamReaderPooledBuffer);
            
            var positionInSearch = 0;
            bool fileContainedSearch = false;
            
            while (!streamReaderPooledBufferWrap.IsEof)
            {
                if (streamReaderPooledBufferWrap.CurrentCharacter == search[positionInSearch])
                {
                    var originBytePosition = streamReaderPooledBufferWrap.ByteIndex;;
                    var originCharacterPosition = streamReaderPooledBufferWrap.PositionIndex;
                    _ = streamReaderPooledBufferWrap.ReadCharacter();
                    positionInSearch++;
                    // This is 1 "character" further than the entry point so we can backtrack if it isn't a match.
                    var bytePosition = streamReaderPooledBufferWrap.ByteIndex;
                    var characterPosition = streamReaderPooledBufferWrap.PositionIndex;
                    
                    while (!streamReaderPooledBufferWrap.IsEof)
                    {
                        if (positionInSearch == search.Length)
                        {
                            positionInSearch = 0;
                            fileContainedSearch = true;
                            break;
                        }
                        else if (streamReaderPooledBufferWrap.CurrentCharacter != search[positionInSearch])
                        {
                            positionInSearch = 0;
                            streamReaderPooledBufferWrap.Unsafe_Seek_SeekOriginBegin(
                                bytePosition, characterPosition, characterLength: 0);
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
                        searchResultList.Add(
                            (
                                resourceUri,
                                new TextEditorTextSpan(
                                    startInclusiveIndex: originCharacterPosition,
                                    endExclusiveIndex: streamReaderPooledBufferWrap.PositionIndex,
                                    decorationByte: 0,
                                    byteIndex: originBytePosition)
                            ));
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
                ParseFilesRecursive(tokenBuilder, formattedBuilder, projectSeenHashSet, searchResultList, search, subDirectory, streamReaderPooledBufferWrap, streamReaderPooledBuffer);
            }
        }
    }

    public void Dispose()
    {
        CancelSearch();
    }

    /// <summary>
    /// ProjectList is used for searching .csproj which can't be recursively found by
    /// searching from the .sln's parent dir.
    /// ...
    /// The ProjectList should contain every project whether it is one that
    /// can or cannot be found. If a project can be found,
    /// then it is skipped during the iteration of ProjectList.
    /// </summary>
    public record struct TextEditorFindAllState(
        string SearchQuery,
        string StartingDirectoryPath,
        IEnumerable<AbsolutePath> ProjectList,
        List<(ResourceUri ResourceUri, TextEditorTextSpan TextSpan)> SearchResultList,
        Exception Exception)
    {
        public static readonly Key<TreeViewContainer> TreeViewFindAllContainerKey = Key<TreeViewContainer>.NewKey();

        public TextEditorFindAllState() : this(
            SearchQuery: string.Empty,
            StartingDirectoryPath: string.Empty,
            ProjectList: Enumerable.Empty<AbsolutePath>(),
            SearchResultList: new(),
            Exception: null)
        {
        }
    }
}
