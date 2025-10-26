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
        /*
        project grouping is a "late" brace matching algorithm, you are enumerating so you'll see files that are non .csproj
        prior to the .csproj but once you do see the .csproj you mark that directory and any recursive to be owned
        by that respective .csproj.
        
        Then you "brace match" by tracking directory depth until you've returned to the parent dir, then unset the owning csproj.
        Anything that isn't owned by a csproj is marked under the "misc files" but the issue here is that I think they'll
        be fragmented under these conditions so I gotta figure that out.
        
        Anything that is owned by a csproj isn't fragmented,
        but the misc you can either duplicate or move data around in the NodeValueList
        OR you can store a separate List that contains all the misc entries then move them to the NodeValueList at the end.
        
        The csharp projects as well need to be added last since they would otherwise fragment the children of the root.
        
        When you've seen a .csproj file you add to projectSeenMap so you can change this to a List,
        entry can be a value tuple of (string ProjectAbsolutePath, int ChildListOffset, int ChildListLength)
        
        Then you'd have to write the search through the List but this may or may not be an issue
        I don't think people would have thousands of projects in their .sln.
        
        Groups need not be fragmented too though...
        
        You know the amount of SearchResults and the amount of C# projects
        
        You can determine the amount of filename grouped keys by tracking this during the enumeration of files step.
        
        So you predetermine which contiguous indices will correspond to the SearchResult nodes.
        
        Then predetermine the filename group nodes...
        and the C# project nodes...
        
        And you write out the data with gaps between each section.
        
        ----
        
        prellocate space in the shared list for the groups by counting distinct filenames during enumeration,
        iterate the search-results to write out the group nodes,
        
        then write the csproj nodes to point to the group nodes by changing the Dictionary's value tuple
            to contain the offset and length of the groups instead of the files. (you have to do this AFTER you
            initially have the value tuple signify the search result offset and length that corresponds to the csproj).
        */
    
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
        var projectSeenMap = new Dictionary<string /*ProjectAbsolutePath*/, (int ChildListOffset, int ChildListLength)>();
        
        int fileCount = 0;
        int respectedProjectCount = 0;
        
        /*
        var findAllTreeViewContainer = new FindAllTreeViewContainer(this, searchResultList);
        findAllTreeViewContainer.NodeValueList.Add(new TreeViewNodeValue
        {
            ParentIndex = -1,
            IndexAmongSiblings = 0,
            ChildListOffset = 1,
            ChildListLength = 0,
            ByteKind = FindAllTreeViewContainer.ByteKind_Aaa,
            TraitsIndex = 0,
            IsExpandable = true,
            IsExpanded = true
        });
        */
        
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
            
            ParseFilesRecursive(
                /*findAllTreeViewContainer, */
                depth: 0,
                csprojMark: (-1, string.Empty),
                tokenBuilder,
                formattedBuilder,
                projectSeenMap,
                searchResultList,
                textEditorFindAllState.SearchQuery,
                parentDirectory,
                streamReaderPooledBufferWrap,
                streamReaderPooledBuffer,
                ref fileCount,
                ref respectedProjectCount);
            
            foreach (var projectAbsolutePath in textEditorFindAllState.ProjectList)
            {
                if (!projectSeenMap.ContainsKey(projectAbsolutePath.Value))
                {
                    ParseFilesRecursive(
                        /*findAllTreeViewContainer, */
                        depth: -1,
                        csprojMark: (-1, string.Empty),
                        tokenBuilder,
                        formattedBuilder,
                        projectSeenMap,
                        searchResultList,
                        textEditorFindAllState.SearchQuery,
                        projectAbsolutePath.CreateSubstringParentDirectory(),
                        streamReaderPooledBufferWrap,
                        streamReaderPooledBuffer,
                        ref fileCount,
                        ref respectedProjectCount);
                }
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
            
            var searchResultOffset = 1;
            var searchResultIndexAmongSiblings = 0;
            
            var fileGroupOffset = 1 + searchResultList.Count;
            var fileGroupIndexAmongSiblings = 0;
            
            var csprojOffset = fileGroupOffset + fileCount;
            var csprojIndexAmongSiblings = 0;
            
            var findAllTreeViewContainer = new FindAllTreeViewContainer(
                this,
                searchResultList,
                nodeValueListInitialCapacity: csprojOffset + respectedProjectCount);
            
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
        //FindAllTreeViewContainer container,
        int depth,
        (int Depth, string FormattedAbsolutePath) csprojMark,
        StringBuilder tokenBuilder,
        StringBuilder formattedBuilder,
        Dictionary<string /*ProjectAbsolutePath*/, (int ChildListOffset, int ChildListLength)> projectSeenMap,
        List<(ResourceUri ResourceUri, TextEditorTextSpan TextSpan)> searchResultList,
        string search,
        string currentDirectory,
        StreamReaderPooledBufferWrap streamReaderPooledBufferWrap,
        StreamReaderPooledBuffer streamReaderPooledBuffer,
        ref int fileCount,
        ref int respectedProjectCount)
    {
        var csprojChildListOffset = searchResultList.Count + 1 /* '+ 1' is the root node */;
        var countUponEntry = searchResultList.Count;
        
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
            
            ++fileCount;
            
            if (file.EndsWith(".csproj"))
            {
                var formattedAbsolutePath = AbsolutePath.GetFormattedStringOnly(
                    file,
                    isDirectory: false,
                    fileSystemProvider: CommonService.FileSystemProvider,
                    tokenBuilder,
                    formattedBuilder);
            
                if (csprojMark.Depth == -1)
                {
                    // If anyone has "recursive" csproj files then this code only respects
                    // the first one that was found.
                    ++respectedProjectCount;
                    csprojMark = (depth, formattedAbsolutePath);
                    
                    projectSeenMap.Add(
                        formattedAbsolutePath,
                        (
                            csprojChildListOffset,
                            -1
                        ));
                }
                else
                {
                    // TODO: Support value tuple named parameters.
                    projectSeenMap.Add(
                        formattedAbsolutePath,
                        (
                            -1,
                            -1
                        ));
                }
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
            
            while (!streamReaderPooledBufferWrap.IsEof)
            {
                continue_outer_while_loop:
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
                            searchResultList.Add(
                            (
                                resourceUri,
                                new TextEditorTextSpan(
                                    startInclusiveIndex: originCharacterPosition,
                                    endExclusiveIndex: streamReaderPooledBufferWrap.PositionIndex,
                                    decorationByte: 0,
                                    byteIndex: originBytePosition)
                            ));
                            
                            positionInSearch = 0;
                            streamReaderPooledBufferWrap.Unsafe_Seek_SeekOriginBegin(
                                bytePosition, characterPosition, characterLength: 0);
                            
                            goto continue_outer_while_loop;
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
                ParseFilesRecursive(
                    /*container, */
                    depth + 1,
                    csprojMark,
                    tokenBuilder,
                    formattedBuilder,
                    projectSeenMap,
                    searchResultList,
                    search,
                    subDirectory,
                    streamReaderPooledBufferWrap,
                    streamReaderPooledBuffer,
                    ref fileCount,
                    ref respectedProjectCount);
            }
        }
        
        if (csprojMark.Depth == depth)
        {
            projectSeenMap[csprojMark.FormattedAbsolutePath] =
            (
                csprojChildListOffset,
                searchResultList.Count - countUponEntry
            );
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
