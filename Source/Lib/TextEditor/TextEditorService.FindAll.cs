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
    
        // I gotta wake up at 5:30 tomorrow and I Gotta take a shower / etc so I'm done for now.
        // It is extremely close it circular points somehow but it all lazily expands.
        // but I Think 0th entry uses the name of the 1st entry idk.
        //
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
        var projectSeenHashSet = new HashSet<string /*ProjectAbsolutePath*/>();
        var projectRespectedList = new List<(string ProjectAbsolutePath, int SeachResult_ChildListOffset, int SeachResult_ChildListLength)>();
        
        int fileCount = 0;
        
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
                csprojDepthMark: -1,
                tokenBuilder,
                formattedBuilder,
                projectSeenHashSet,
                projectRespectedList,
                searchResultList,
                textEditorFindAllState.SearchQuery,
                parentDirectory,
                streamReaderPooledBufferWrap,
                streamReaderPooledBuffer,
                ref fileCount);
            
            foreach (var projectAbsolutePath in textEditorFindAllState.ProjectList)
            {
                if (!projectSeenHashSet.Contains(projectAbsolutePath.Value))
                {
                    ParseFilesRecursive(
                        /*findAllTreeViewContainer, */
                        depth: -1,
                        csprojDepthMark: -1,
                        tokenBuilder,
                        formattedBuilder,
                        projectSeenHashSet,
                        projectRespectedList,
                        searchResultList,
                        textEditorFindAllState.SearchQuery,
                        projectAbsolutePath.CreateSubstringParentDirectory(),
                        streamReaderPooledBufferWrap,
                        streamReaderPooledBuffer,
                        ref fileCount);
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
            
            FindAllTreeViewContainer? findAllTreeViewContainer = null;
            
            if (searchResultList.Count > 0)
            {
                var fixed_searchResultOffset = 1;
                var fluid_searchResultLength = 0;
                
                var fixed_fileGroupOffset = 1 + searchResultList.Count;
                var fluid_fileGroupLength = 0;
                
                var fixed_projectOffset = fixed_fileGroupOffset + fileCount;
                var fluid_projectLength = 0;
                
                var fluid_projectRespectedListIndex = 0;
                
                var nodeValueListInitialCapacity = fixed_projectOffset + projectRespectedList.Count;
                findAllTreeViewContainer = new FindAllTreeViewContainer(
                    this,
                    searchResultList,
                    nodeValueListInitialCapacity,
                    projectRespectedList);
                    
                for (int capacityCounter = 0; capacityCounter < nodeValueListInitialCapacity; capacityCounter++)
                {
                    findAllTreeViewContainer.NodeValueList.Add(default);
                }
                
                findAllTreeViewContainer.NodeValueList[0] = new TreeViewNodeValue
                {
                    ParentIndex = -1,
                    IndexAmongSiblings = 0,
                    ChildListOffset = fixed_projectOffset,
                    ChildListLength = projectRespectedList.Count,
                    ByteKind = FindAllTreeViewContainer.ByteKind_Aaa,
                    TraitsIndex = 0,
                    IsExpandable = true,
                    IsExpanded = true
                };

                var pending_fileGroupChildListOffset = fixed_searchResultOffset;
                var pending_fileGroupChildListLength = 1;
                var pending_fileGroupInclusiveMark = findAllTreeViewContainer.SearchResultList[0].ResourceUri.Value;
                
                var pending_projectChildListOffset = fixed_fileGroupOffset;
                var pending_projectChildListLength = 0;
                var pending_projectExclusiveMark = 0;
                
                // TODO: You can pre-determine that 1 extra node for the misc files exists at the end of the current way the NodeValueList is setup.
                // ... then as you go if there isn't a csproj that claims ownership of the search result then you copy the data
                // to the end of the NodeValueList and the misc files points to those search results you copied to the end of the NodeValueList
                // so then you have to say the misc node itself has children offset...length.
                
                for (int i = 0; i < findAllTreeViewContainer.SearchResultList.Count; i++)
                {
                    var searchResult = findAllTreeViewContainer.SearchResultList[i];
                    
                    if (fluid_projectRespectedListIndex < projectRespectedList.Count &&
                            (pending_projectExclusiveMark == projectRespectedList[fluid_projectRespectedListIndex].SeachResult_ChildListLength ||
                            (i == findAllTreeViewContainer.SearchResultList.Count - 1 &&
                                 pending_projectExclusiveMark + 1 == projectRespectedList[fluid_projectRespectedListIndex].SeachResult_ChildListLength)))
                    {
                        findAllTreeViewContainer.NodeValueList[fixed_projectOffset + fluid_projectLength] =
                            new TreeViewNodeValue
                            {
                                ParentIndex = 0,
                                IndexAmongSiblings = fluid_projectLength,
                                ChildListOffset = pending_projectChildListOffset,
                                ChildListLength = pending_projectChildListLength,
                                ByteKind = FindAllTreeViewContainer.ByteKind_SearchResultProject,
                                TraitsIndex = fluid_projectRespectedListIndex,
                                IsExpandable = true,
                                IsExpanded = false
                            };
                        ++fluid_projectLength;
                        ++fluid_projectRespectedListIndex;
                        
                        pending_projectChildListOffset = fixed_fileGroupOffset + fluid_fileGroupLength;
                        pending_projectChildListLength = 0;
                        pending_projectExclusiveMark = 0;
                    }
                    
                    if (i == projectRespectedList[fluid_projectRespectedListIndex].SeachResult_ChildListOffset)
                    {
                        pending_projectChildListOffset = fixed_fileGroupOffset + fluid_fileGroupLength;
                        pending_projectChildListLength = 0;
                        pending_projectExclusiveMark = 0;
                    }
                    
                    findAllTreeViewContainer.NodeValueList[fixed_searchResultOffset + fluid_searchResultLength] =
                        new TreeViewNodeValue
                        {
                            ParentIndex = fixed_fileGroupOffset + fluid_fileGroupLength,
                            IndexAmongSiblings = fluid_searchResultLength,
                            ChildListOffset = 0,
                            ChildListLength = 0,
                            ByteKind = FindAllTreeViewContainer.ByteKind_SearchResult,
                            TraitsIndex = i,
                            IsExpandable = false,
                            IsExpanded = false
                        };
                    ++fluid_searchResultLength;
                    ++pending_projectExclusiveMark;
                    
                    if (pending_fileGroupInclusiveMark != searchResult.ResourceUri.Value ||
                        i == findAllTreeViewContainer.SearchResultList.Count - 1)
                    {
                        // Write out pending
                        {
                            // WARNING: this code is duplicated after the for loop to write the final entry.
                            findAllTreeViewContainer.NodeValueList[fixed_fileGroupOffset + fluid_fileGroupLength] =
                                new TreeViewNodeValue
                                {
                                    ParentIndex = fluid_projectRespectedListIndex < projectRespectedList.Count
                                        ? fixed_projectOffset + fluid_projectLength
                                        : 0,
                                    IndexAmongSiblings = fluid_fileGroupLength,
                                    ChildListOffset = pending_fileGroupChildListOffset,
                                    ChildListLength = pending_fileGroupChildListLength,
                                    ByteKind = FindAllTreeViewContainer.ByteKind_SearchResultGroup,
                                    TraitsIndex = i,
                                    IsExpandable = true,
                                    IsExpanded = false
                                };
                            ++fluid_fileGroupLength;
                            ++pending_projectChildListLength;
                        }
                        
                        // Change pending target
                        pending_fileGroupInclusiveMark = searchResult.ResourceUri.Value;
                        pending_fileGroupChildListOffset = fixed_searchResultOffset + fluid_searchResultLength;
                        pending_fileGroupChildListLength = 1;
                    }
                    else
                    {
                        ++pending_fileGroupChildListLength;
                    }
                }
                
                // Passing cases:
                // ============================
                // [ R, S_f1, S_f2, F1, F2, P ] // 
                
                // Failing cases:
                // ========================================
                // [ R, S_f1, S_f1, S_f2, S_f2, F1, F2, P ] // 
                
                Console.WriteLine("\n\t==============");
                Console.WriteLine($"\tsearchResultList.Count:{searchResultList.Count}");
                Console.WriteLine($"\tfixed_projectOffset + projectRespectedList.Count:{fixed_projectOffset + projectRespectedList.Count}");
                Console.WriteLine($"\tfluid_projectRespectedList.Count:{projectRespectedList.Count}");
                Console.WriteLine($"\tfixed_searchResultOffset:{fixed_searchResultOffset}");
                Console.WriteLine($"\tfluid_searchResultLength:{fluid_searchResultLength}");
                Console.WriteLine($"\tfixed_fileGroupOffset:{fixed_fileGroupOffset}");
                Console.WriteLine($"\tfluid_fileGroupLength:{fluid_fileGroupLength}");
                Console.WriteLine($"\tfixed_projectOffset:{fixed_projectOffset}");
                Console.WriteLine($"\tfluid_projectLength:{fluid_projectLength}");
                Console.WriteLine($"\tfluid_projectRespectedListIndex:{fluid_projectRespectedListIndex}");
                
                for (int bbb = 0; bbb < findAllTreeViewContainer.NodeValueList.Count; bbb++)
                {
                    var ccc = findAllTreeViewContainer.NodeValueList[bbb];
                    
                    Console.Write($"\t{ccc.ByteKind} {ccc.TraitsIndex} {ccc.ChildListOffset} {ccc.ChildListLength}");
                }
                
                Console.WriteLine("\t==============\n");
            }
            
            lock (_stateModificationLock)
            {
                if (findAllTreeViewContainer is not null)
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
        int csprojDepthMark, // used for the recursion to ignore "recursive" csproj files
        StringBuilder tokenBuilder,
        StringBuilder formattedBuilder,
        HashSet<string /*ProjectAbsolutePath*/> projectSeenHashSet,
        List<(string ProjectAbsolutePath, int SeachResult_ChildListOffset, int SeachResult_ChildListLength)> projectRespectedList,
        List<(ResourceUri ResourceUri, TextEditorTextSpan TextSpan)> searchResultList,
        string search,
        string currentDirectory,
        StreamReaderPooledBufferWrap streamReaderPooledBufferWrap,
        StreamReaderPooledBuffer streamReaderPooledBuffer,
        ref int fileCount)
    {
        var seachResult_csprojChildListOffset = searchResultList.Count;
        
        int projectRespectedListIndex = -1;
        
        var previousFile = string.Empty;
        
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
                var formattedAbsolutePath = AbsolutePath.GetFormattedStringOnly(
                    file,
                    isDirectory: false,
                    fileSystemProvider: CommonService.FileSystemProvider,
                    tokenBuilder,
                    formattedBuilder);
            
                // TODO: Support value tuple named parameters.
                projectSeenHashSet.Add(formattedAbsolutePath);
                
                if (csprojDepthMark == -1)
                {
                    // If anyone has "recursive" csproj files then this code only respects
                    // the first one that was found.
                    csprojDepthMark = depth;
                    
                    projectRespectedListIndex = projectRespectedList.Count;
                    projectRespectedList.Add(
                        (
                            formattedAbsolutePath,
                            seachResult_csprojChildListOffset,
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
                            if (previousFile != file)
                            {
                                previousFile = file;
                                ++fileCount;
                            }
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
                    csprojDepthMark,
                    tokenBuilder,
                    formattedBuilder,
                    projectSeenHashSet,
                    projectRespectedList,
                    searchResultList,
                    search,
                    subDirectory,
                    streamReaderPooledBufferWrap,
                    streamReaderPooledBuffer,
                    ref fileCount);
            }
        }
        
        if (projectRespectedListIndex != -1)
        {
            projectRespectedList[projectRespectedListIndex] =
            (
                projectRespectedList[projectRespectedListIndex].ProjectAbsolutePath,
                seachResult_csprojChildListOffset,
                searchResultList.Count - seachResult_csprojChildListOffset
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
