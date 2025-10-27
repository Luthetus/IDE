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
        TreeViewContainer:
        NodeValueList = [ROOT, ResultHeap..., FileHeap..., ProjectHeap...]
        
        ResultHeap traits exist in the `SearchResultList`
        FileHeap traits exist in the `SearchResultList`
            - In particular FileHeap traits are the first occurrence of a distinct filename for a SearchResult.
        ProjectHeap traits exist in the `ProjectRespectedList`
        
        Every node's children are a contiguous span of any heap.
        The offsets are always relative to the NodeValueList itself.
        
        The NodeValueList is constructed after pre-calculating the total capacity of
        ResultHeap, FileHeap, and ProjectHeap.
        
        This pre-calculation of capacities is extrapolated from the data itself.
        
        Initially every entry in the NodeValueList is `default`.
        Then you overwrite the respective indices by tracking the offset of each heap.
        */
    
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
        // Plural of "SearchResult" is used rather than "ChildList" or "Children" to ensure variable names
        // are more distinct from eachother.
        //
        // Once the algorithm settles consideration to use ChildList everywhere might be of good use
        // lest you always wonder "well what wording did they use for this variable this time...".
        //
        var projectRespectedList = new List<(string ProjectAbsolutePath, int SearchResultsOffset, int SearchResultsLength)>();
        
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
                // "UPPERCASE_" Avoid confusion with the 'result' named variables.
                var ROOT_ListOffset = 0;
                var ROOT_ListCapacity = 1;
                var ROOT_ListLength = 0;
            
                var resultHeap_Offset = ROOT_ListOffset + ROOT_ListCapacity;
                var resultHeap_Capacity = searchResultList.Count;
                var resultHeap_Length = 0;
                
                var fileHeap_Offset = resultHeap_Offset + resultHeap_Capacity;
                var fileHeap_Capacity = fileCount;
                var fileHeap_Length = 0;
                
                var projectHeap_Offset = fileHeap_Offset + fileHeap_Capacity;
                var projectHeap_Capacity = projectRespectedList.Count;
                var projectHeap_Length = 0;
                
                var i_project = 0;
                
                var nodeValueListInitialCapacity = projectHeap_Offset + projectRespectedList.Count;
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
                    ChildListOffset = projectHeap_Offset,
                    ChildListLength = projectRespectedList.Count,
                    ByteKind = FindAllTreeViewContainer.ByteKind_Aaa,
                    TraitsIndex = 0,
                    IsExpandable = true,
                    IsExpanded = true
                };
                
                // The Length is calculable
                // ...Heap_Offset + ...Heap_Length - ...Node_ChildrenOffset

                // "ChildrenOffset" to avoid naming confusions with "...ListOffset"
                var fileNode_ChildrenOffset = resultHeap_Offset;
                var fileNode_InclusiveMark = findAllTreeViewContainer.SearchResultList[0].ResourceUri.Value;
                
                // "ChildrenOffset" to avoid naming confusions with "...ListOffset"
                var projectNode_ChildrenOffset = fileHeap_Offset;
                // The exclusive `i_searchResult` that marks the end of the project's files.
                // (NOTE: A file is the first distinct occurrence of a filename within the search results.
                //        That relation is why the i_searchResult can indicate this).
                var projectNode_ExclusiveMark = -1;
                
                // Somewhat of a "ranking" pattern?
                // ====================================
                // When using a `projectNode_ChildrenOffset` you use the Heap that is one group "smaller" than the node itself.
                // So `projectNode_ChildrenOffset` uses `fileHeap_...`.
                //
                // I think it makes sense now but when originally reading the code this pattern was hard to read.
                
                // TODO: You can pre-determine that 1 extra node for the misc files exists at the end of the current way the NodeValueList is setup.
                // ... then as you go if there isn't a csproj that claims ownership of the search result then you copy the data
                // to the end of the NodeValueList and the misc files points to those search results you copied to the end of the NodeValueList
                // so then you have to say the misc node itself has children offset...length.
                
                for (int i_searchResult = 0; i_searchResult < findAllTreeViewContainer.SearchResultList.Count; i_searchResult++)
                {
                    var searchResult = findAllTreeViewContainer.SearchResultList[i_searchResult];
                    
                    if (projectRespectedList.Count > 0 && projectNode_ExclusiveMark == -1)
                    {
                        // If there is a search result, there is guaranteed to be a file.
                        // But there is no guarantee of there being a project.
                        if (projectRespectedList[i_project].SearchResultsOffset == i_searchResult)
                        {
                            projectNode_ExclusiveMark = projectRespectedList[i_project].SearchResultsOffset +
                                                        projectRespectedList[i_project].SearchResultsLength;
                        }
                    }
                    
                    if (i_project < projectRespectedList.Count &&
                            (projectNode_ExclusiveMark == projectRespectedList[i_project].SearchResultsLength ||
                            (i_searchResult == findAllTreeViewContainer.SearchResultList.Count - 1 &&
                                 projectNode_ExclusiveMark + 1 == projectRespectedList[i_project].SearchResultsLength)))
                    {
                        // Write out pending
                        findAllTreeViewContainer.NodeValueList[projectHeap_Offset + projectHeap_Length] =
                            new TreeViewNodeValue
                            {
                                ParentIndex = 0,
                                IndexAmongSiblings = 0/*projectLength*/,
                                ChildListOffset = projectNode_ChildrenOffset,
                                ChildListLength = fileHeap_Offset + fileHeap_Length - projectNode_ChildrenOffset,
                                ByteKind = FindAllTreeViewContainer.ByteKind_SearchResultProject,
                                TraitsIndex = i_project,
                                IsExpandable = true,
                                IsExpanded = false
                            };
                        ++projectHeap_Length;
                        ++i_project;
                        
                        projectNode_ChildrenOffset = fileHeap_Offset + fileHeap_Length;
                        projectNode_ExclusiveMark = -1;
                    }
                    
                    if (i_searchResult == projectRespectedList[i_project].SeachResultsOffset)
                    {
                        projectNode_ChildrenOffset = fileHeap_Offset + fileHeap_Length;
                        projectNode_ExclusiveMark = 0;
                    }
                    
                    // SearchResult: Write out pending
                    findAllTreeViewContainer.NodeValueList[resultHeap_Offset + resultHeap_Length] =
                        new TreeViewNodeValue
                        {
                            ParentIndex = fileHeap_Offset + fileHeap_Length,
                            IndexAmongSiblings = 0/*resultLength*/,
                            ChildListOffset = 0,
                            ChildListLength = 0,
                            ByteKind = FindAllTreeViewContainer.ByteKind_SearchResult,
                            TraitsIndex = i_searchResult,
                            IsExpandable = false,
                            IsExpanded = false
                        };
                    ++resultHeap_Length;
                    
                    if (fileNode_InclusiveMark != searchResult.ResourceUri.Value ||
                        i_searchResult == findAllTreeViewContainer.SearchResultList.Count - 1)
                    {
                        // FileGroup: Write out pending
                        findAllTreeViewContainer.NodeValueList[fileHeap_Offset + fileHeap_Length] =
                            new TreeViewNodeValue
                            {
                                ParentIndex = i_project < projectRespectedList.Count
                                    ? projectHeap_Offset + projectHeap_Length
                                    : 0,
                                IndexAmongSiblings = 0/*fileListLength*/,
                                ChildListOffset = fileNode_ChildrenOffset,
                                ChildListLength = resultHeap_Length - fileNode_ChildrenOffset,
                                ByteKind = FindAllTreeViewContainer.ByteKind_SearchResultGroup,
                                TraitsIndex = i_searchResult,
                                IsExpandable = true,
                                IsExpanded = false
                            };
                        ++fileHeap_Length;
                        
                        // FileGroup: Change pending target
                        fileNode_InclusiveMark = searchResult.ResourceUri.Value;
                        fileNode_ChildrenOffset = resultHeap_Offset + resultHeap_Length;
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
                Console.WriteLine($"\tprojectHeap_Offset + projectRespectedList.Count:{projectHeap_Offset + projectRespectedList.Count}");
                Console.WriteLine($"\tprojectRespectedList.Count:{projectRespectedList.Count}");
                Console.WriteLine($"\tresultHeap_Offset:{resultHeap_Offset}");
                Console.WriteLine($"\tresultHeap_Length:{resultHeap_Length}");
                Console.WriteLine($"\tfileHeap_Offset:{fileHeap_Offset}");
                Console.WriteLine($"\tfileHeap_Length:{fileHeap_Length}");
                Console.WriteLine($"\tprojectHeap_Offset:{projectHeap_Offset}");
                Console.WriteLine($"\tprojectHeap_Length:{projectHeap_Length}");
                Console.WriteLine($"\ti_project:{i_project}");
                
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
        List<(string ProjectAbsolutePath, int SearchResultsOffset, int SearchResultsLength)> projectRespectedList,
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
