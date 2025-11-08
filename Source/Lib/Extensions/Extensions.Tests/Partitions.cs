using Clair.TextEditor.RazorLib.TextEditors.Models;

namespace Clair.Tests.csproj.csproj;

/// <summary>
///
/// Information is split into "Group A" and "Group B".
/// "Group A" is intended to be read first.
/// "Group B" is intended to be read second.
///
/// The classification of the groups is somewhat arbitrary.
/// It is entirely based on where I "feel" something should be.
///
/// # Group A information
/// =====================
///
/// Most importantly, a lot of text editors don't edit text this way.
/// - Rope
/// - Gap buffer
/// - Piece table
/// - ...
///
/// That being said I wrote the partition code without having looked
/// at what other people were doing with respect to the editing.
///
/// And it currently isn't that big of a deal*,
/// and I think you actually can use partitions the entireway through without issues.
///     - *The TextEditorModel when closed needs to be put in a state where
///         the GC is able to collect it, and the various partitions that compose the TextEditorModel's content.
///     - *This currently isn't being done. So the overhead of the partition logic is massive at the moment.
///         - But if I can reliably prove that the GC collects everything eventually after you close the tab,
///             it won't be that big of a deal.
///
/// Partitions have the following interactions:
/// - Seek (find the correct partition)
/// - Multi-byte characters should be maintained within the same partition
/// - Split a partition in half (accounting for odd numbers)
///
/// There are (essentially) two use cases:
/// - Edit()
/// - EditRange()
///
/// These come in the forms of:
/// - Insert()
/// - InsertRange()
/// - RemoveAt()
/// - RemoveRange()
/// - SetDecorationByte()
/// - SetDecorationByteRange()
/// 
/// Listing out the two use cases with their steps:
/// - Edit()
///     - Seek (find the correct partition)
///     - If insertion of a character is found to be on a certain partition:
///         - But this partition is at maximum capacity
///         - Then check if an insertion to the start of the next partition would have an equivalent result.
///         - If NOT equivalent, then you need to perform a split.
///         - I believe the left partition will be set as the current in a split scenario (TODO: Verify this).
///     - Now you have a current partition, and you know it has available space.
///         - Perform the insertion
/// - EditRange()
///     - Seek (find the correct partition)
///     - If insertion of a character is found to be on a certain partition:
///         - But this partition is at maximum capacity
///         - Then check if an insertion to the start of the next partition would have an equivalent result.
///         - If NOT equivalent, then you need to perform a split.
///         - I believe the left partition will be set as the current in a split scenario (TODO: Verify this).
///     - Now you have a current partition, and you know it has available space.
///         - But, in the case of an IEnumerable, you might not know how much text you will insert.
///             - A partition with available space solely means that at least 1 character can be inserted.
///                 - ...maybe it is 2 characters to account for "\r\n", I'm not sure. (TODO: Verify this)
///             - You need to get the "count of available space".
///             - Once you have that you can do something along the lines of 'insert while (countOfAvailableSpace)'
///                 - (TODO: Determine exactly what the code for that 'insert while (countOfAvailableSpace)' looks like).
///             - If you run out of a "count of available" space then you need to start the method over again at the 'Seek' step.
///
/// # Group B information
/// =====================
/// - Garbage Collection overhead:
///     - (links/resources)
///         - Analyze memory usage by using the .NET Object Allocation tool:
///             - https://learn.microsoft.com/en-us/visualstudio/profiling/dotnet-alloc-tool
///         - Fundamentals of garbage collection
///             - https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/fundamentals
///     - The cost of text insertion when using a partition is extremely high.
///         - This is because if I hold down the letter 'j', for example, then
///             each insertion has to recreate whichever partition my cursor resides on.
///         - The worst part of this is specifically the immense GC overhead that comes from
///             allocating large partitions over and over.
///         - The solution is to use SynchronizationContext to create a thread safe context.
///             - Within this thread safe context, you can then make many object allocation optimizations
///                 by pooling the objects (Exchange is a word I use sometimes for single object pools, I might rename these).
///             - I began writing the TextEditorContext without knowing about SynchronizationContext.
///                 - Once I understood more about the SynchronizationContext I didn't change to using it,
///                     because it just isn't a high priority when the TextEditorContext is working
///                     and I have other features to implement still.
///                 
///
///
/// 
/// </summary>
public class Partitions
{
    public TextEditorModel GetTestModel(string content)
    {
        return new TextEditorModel(
            new TextEditor.RazorLib.Lexers.Models.ResourceUri("unittest"),
            DateTime.UtcNow,
            fileExtension: string.Empty,
            content: _content,
            decorationMapper: null,
            compilerService: null,
            textEditorService: null);
    }

    /// <summary>
    /// This test runs (it does nothing currently, but there's no errors or exceptions).
    /// Nothing matters unless the partition logic is written perfectly and performently(?).
    /// So depending on how lazy I am nothing will work for some amount of time.
    /// And I'll just focus on the partition code / not accept the PR of this
    /// in order to keep main "functional".
    /// </summary>
    [Fact]
    public void Seek_FirstPartition_FirstCharacter()
    {
        // What was I doing previously with the runningCount concept???

        //   [ 'a', 'b', 'c', 'd'] ['e', 'f', 'g', 'h' ] ['i', 'j', 'k', 'l' ]
        //      ^

        // TargetGlobalCharacterIndex: 0
        // GlobalCharacterIndex: 0
        // RelativeCharacterIndex: 0
        // PartitionIndex: 0
        // Count: 0
        
        // Was the runningCount the offset for the start of the partition?



        var model = GetTestModel(_content);
        var partitionWalker = new PartitionWalker();
        partitionWalker.ReInitialize(model);

        partitionWalker.Seek(targetGlobalCharacterIndex: 0);
        Assert.Equal(0, partitionWalker.GlobalCharacterIndex);
        Assert.Equal(0, partitionWalker.PartitionIndex);
        Assert.Equal(0, partitionWalker.RelativeCharacterIndex);
    }

    [Fact]
    public void Seek_FirstPartition_IntermediateCharacter()
    {
        //   [ 'a', 'b', 'c', 'd'] ['e', 'f', 'g', 'h' ] ['i', 'j', 'k', 'l' ]
        //           ^

        // TargetGlobalCharacterIndex: 1
        // GlobalCharacterIndex: 1
        // RelativeCharacterIndex: 1
        // PartitionIndex: 0
        // Count: 1

        var model = GetTestModel(_content);
        var partitionWalker = new PartitionWalker();
        partitionWalker.ReInitialize(model);

        partitionWalker.Seek(targetGlobalCharacterIndex: 3);
        Assert.Equal(3, partitionWalker.GlobalCharacterIndex);
        Assert.Equal(0, partitionWalker.PartitionIndex);
        Assert.Equal(3, partitionWalker.RelativeCharacterIndex);
    }
    
    [Fact]
    public void Seek_FirstPartition_LastCharacter()
    {
        //   [ 'a', 'b', 'c', 'd'] ['e', 'f', 'g', 'h' ] ['i', 'j', 'k', 'l' ]
        //                     ^

        // TargetGlobalCharacterIndex: 3
        // GlobalCharacterIndex: 3
        // RelativeCharacterIndex: 3
        // PartitionIndex: 0
        // Count: 3

        var model = GetTestModel(_content);
        var partitionWalker = new PartitionWalker();
        partitionWalker.ReInitialize(model);

        partitionWalker.Seek(targetGlobalCharacterIndex: 4063);
        Assert.Equal(4063, partitionWalker.GlobalCharacterIndex);
        Assert.Equal(0, partitionWalker.PartitionIndex);
        Assert.Equal(4063, partitionWalker.RelativeCharacterIndex);
    }

    [Fact]
    public void Seek_IntermediatePartition_FirstCharacter()
    {
        //   [ 'a', 'b', 'c', 'd'] ['e', 'f', 'g', 'h' ] ['i', 'j', 'k', 'l' ]
        //                           ^

        // TargetGlobalCharacterIndex: 4
        // GlobalCharacterIndex: 4
        // RelativeCharacterIndex: 0
        // PartitionIndex: 1
        // Count: 4

        var model = GetTestModel(_content);
        var partitionWalker = new PartitionWalker();
        partitionWalker.ReInitialize(model);

        partitionWalker.Seek(targetGlobalCharacterIndex: 4064);
        Assert.Equal(4064, partitionWalker.GlobalCharacterIndex);
        Assert.Equal(1, partitionWalker.PartitionIndex);
        Assert.Equal(0, partitionWalker.RelativeCharacterIndex);
    }

    [Fact]
    public void Seek_IntermediatePartition_IntermediateCharacter()
    {
        //   [ 'a', 'b', 'c', 'd'] ['e', 'f', 'g', 'h' ] ['i', 'j', 'k', 'l' ]
        //                                     ^

        // TargetGlobalCharacterIndex: 6
        // GlobalCharacterIndex: 6
        // RelativeCharacterIndex: 2
        // PartitionIndex: 1
        // Count: 6

        // The question is what math tells me the available indices in the partition.
        // I'm gonna forget the running count idea because I have no idea what it was even doing.
        //
        // Partition 0
        // (0, 3)
        //
        // Partition 1
        // (4, 7)
        //
        // Partition 2
        // (8, 11)

        // I start at 0, this doesn't mean that 0 index is available though
        // so I can't say that the comparison is inclusive it has to be a greater than check.
        // 
        // Otherwise with a 0 sized partition I'm saying
        // GlobalCharacterIndex:0 + PartitionCount:0 >= TargetGlobalCharacterIndex:0
        //
        // Probably have to just start with GlobalCharacterIndex == -1



        var model = GetTestModel(_content);
        var partitionWalker = new PartitionWalker();
        partitionWalker.ReInitialize(model);

        partitionWalker.Seek(targetGlobalCharacterIndex: 4067);
        Assert.Equal(4067, partitionWalker.GlobalCharacterIndex);
        Assert.Equal(1, partitionWalker.PartitionIndex);
        Assert.Equal(3, partitionWalker.RelativeCharacterIndex);
    }

    [Fact]
    public void Seek_IntermediatePartition_LastCharacter()
    {
        // I need to stop "shitposting" on youtube and study more lmao
        // I'm supposed to go to bed for work tomorrow but I'm determined
        // to finish this.
        //
        // *sigh* I can still get 8 hours of sleep if I go to bed right now.
        // I'm washed. Coach is telling me to hang up my jersey.
        // I don't blame him.
        // I'm gonna get this done tomorrow, just gotta
        // make sure I get enough sleep tonight.
        //
        // (if I didn't have to bed time I'd have finished this fr fr)

        //   [ 'a', 'b', 'c', 'd'] ['e', 'f', 'g', 'h' ] ['i', 'j', 'k', 'l' ]
        //                                          ^

        // TargetGlobalCharacterIndex: 7
        // GlobalCharacterIndex: 7
        // RelativeCharacterIndex: 3
        // PartitionIndex: 1
        // Count: 7


        /*
            Assert.Equal() Failure: Values differ
            Expected: 4063
            Actual:   4062
         */

        var model = GetTestModel(_content);
        var partitionWalker = new PartitionWalker();
        partitionWalker.ReInitialize(model);

        // Are you sure you have the correct indices the expected assertions????????????
        // Check character at partition index x then flatten partitions and check at global index see if char same

        var partitionList = model.PartitionList;
        var flatList = model.PartitionList.SelectMany(x => x.RichCharacterList).ToList();

        /*
        var x = partitionList[1].RichCharacterList[4063];
        var yl2 = flatList[8124];
        var yl1 = flatList[8125];
        var y = flatList[8126];
        var yh1 = flatList[8127];
        var yh2 = flatList[8128];
        */

        //var count;

        partitionWalker.Seek(targetGlobalCharacterIndex: 8127);
        Assert.Equal(8127, partitionWalker.GlobalCharacterIndex);
        Assert.Equal(1, partitionWalker.PartitionIndex);
        Assert.Equal(4063, partitionWalker.RelativeCharacterIndex);
    }

    [Fact]
    public void Seek_LastPartition_FirstCharacter()
    {
        //   [ 'a', 'b', 'c', 'd'] ['e', 'f', 'g', 'h' ] ['i', 'j', 'k', 'l' ]
        //                                                 ^

        // TargetGlobalCharacterIndex: 8
        // GlobalCharacterIndex: 8
        // RelativeCharacterIndex: 0
        // PartitionIndex: 2
        // Count: 8

        /*
            Assert.Equal() Failure: Values differ
            Expected: 2
            Actual:   1
         */
        var model = GetTestModel(_content);
        var partitionWalker = new PartitionWalker();
        partitionWalker.ReInitialize(model);

        // Are you sure you have the correct indices the expected assertions????????????
        // Check character at partition index x then flatten partitions and check at global index see if char same

        var partitionList = model.PartitionList;
        var flatList = model.PartitionList.SelectMany(x => x.RichCharacterList).ToList();

        /*
        var x = partitionList[1].RichCharacterList[4063];
        var yl2 = flatList[8124];
        var yl1 = flatList[8125];
        var y = flatList[8126];
        var yh1 = flatList[8127];
        var yh2 = flatList[8128];
        */

        // Global index to first character of arbitrary partition is the count of RichCharacter in every partition that came prior
        // 8128 makes complete sense since I am doing 4096 partition size and then during the SetContent in particular
        // the split occurs at partition_size - 32 so you get 4064 count in every partition.
        //
        // The final partition has 2 prior, 4064 * 2 => 8128
        //
        // My asserted indices were wrong lmao

        partitionWalker.Seek(targetGlobalCharacterIndex: 8128);
        Assert.Equal(8128, partitionWalker.GlobalCharacterIndex);
        Assert.Equal(2, partitionWalker.PartitionIndex);
        Assert.Equal(0, partitionWalker.RelativeCharacterIndex);
    }

    [Fact]
    public void Seek_LastPartition_IntermediateCharacter()
    {
        //   [ 'a', 'b', 'c', 'd'] ['e', 'f', 'g', 'h' ] ['i', 'j', 'k', 'l' ]
        //                                                      ^

        // TargetGlobalCharacterIndex: 9
        // GlobalCharacterIndex: 9
        // RelativeCharacterIndex: 1
        // PartitionIndex: 2
        // Count: 9

        /*
            Assert.Equal() Failure: Values differ
            Expected: 3
            Actual:   2
         */

        var model = GetTestModel(_content);
        var partitionWalker = new PartitionWalker();
        partitionWalker.ReInitialize(model);

        // Are you sure you have the correct indices the expected assertions????????????
        // Check character at partition index x then flatten partitions and check at global index see if char same

        var partitionList = model.PartitionList;
        var flatList = model.PartitionList.SelectMany(x => x.RichCharacterList).ToList();

        /*
        var x = partitionList[1].RichCharacterList[4063];
        var yl2 = flatList[8124];
        var yl1 = flatList[8125];
        var y = flatList[8126];
        var yh1 = flatList[8127];
        var yh2 = flatList[8128];
        */

        partitionWalker.Seek(targetGlobalCharacterIndex: 8130);
        Assert.Equal(8130, partitionWalker.GlobalCharacterIndex);
        Assert.Equal(2, partitionWalker.PartitionIndex);
        Assert.Equal(2, partitionWalker.RelativeCharacterIndex);
    }

    [Fact]
    public void Seek_LastPartition_LastCharacter()
    {
        //   [ 'a', 'b', 'c', 'd'] ['e', 'f', 'g', 'h' ] ['i', 'j', 'k', 'l' ]
        //                                                                ^

        // TargetGlobalCharacterIndex: 11
        // GlobalCharacterIndex: 11
        // RelativeCharacterIndex: 3
        // PartitionIndex: 2
        // Count: 11

        /*
            Assert.Equal() Failure: Values differ
            Expected: 3957
            Actual:   3955
         */

        var model = GetTestModel(_content);
        var partitionWalker = new PartitionWalker();
        partitionWalker.ReInitialize(model);

        // Are you sure you have the correct indices the expected assertions????????????
        // Check character at partition index x then flatten partitions and check at global index see if char same

        var partitionList = model.PartitionList;
        var flatList = model.PartitionList.SelectMany(x => x.RichCharacterList).ToList();

        /*
        var x = partitionList[1].RichCharacterList[4063];
        var yl2 = flatList[8124];
        var yl1 = flatList[8125];
        var y = flatList[8126];
        var yh1 = flatList[8127];
        var yh2 = flatList[8128];
        */

        partitionWalker.Seek(targetGlobalCharacterIndex: 11724);
        Assert.Equal(11724, partitionWalker.GlobalCharacterIndex);
        Assert.Equal(2, partitionWalker.PartitionIndex);
        Assert.Equal(3596, partitionWalker.RelativeCharacterIndex);

        /*
        omg the test passed.
        I think I'm so deep in burnout right now that I'm on the edge of insanity.
        I realized looking back I couldn't read the indices correctly lol
        I have money from work so I'm gonna go play wow a bit
         */

        /*
         I tried playing some wow. I got bored tho so I'm back I guess.
         */
    }

    /// <summary>
    /// Transitioning from n partition to n+1 partition because the data
    /// in partition n was fully enumerated and the next value is
    /// the next partition's first entry.
    /// 
    /// Insert/Remove/Enumerate
    /// 
    /// This is just enumerate to set the decoration bytes.
    /// If I tell the partitionWalker to seek the last character of the intermediate partition
    /// then I can furthermore try to set the decoration bytes of the current and next RichCharacter.
    /// 
    /// The current RichCharacter is on intermediate partition but the next is on the last partition
    /// so I just gotta figure out something for this like a while loop and a for loop while needs to read move next partition.
    /// </summary>
    [Fact]
    public void Enumerate_PartitionOverflow()
    {
        var model = GetTestModel(_content);
        var partitionWalker = new PartitionWalker();
        partitionWalker.ReInitialize(model);

        var partitionList = model.PartitionList;
        var flatList = model.PartitionList.SelectMany(x => x.RichCharacterList).ToList();

        partitionWalker.Seek(targetGlobalCharacterIndex: 8127);
        Assert.Equal(8127, partitionWalker.GlobalCharacterIndex);
        Assert.Equal(1, partitionWalker.PartitionIndex);
        Assert.Equal(4063, partitionWalker.RelativeCharacterIndex);

        var beforeRc1 = partitionList[1].RichCharacterList[4063];
        var beforeRc2 = partitionList[2].RichCharacterList[0];

        var lengthToDecorate = 2;
        while (lengthToDecorate > 0)
        {
            var thisLoopAvailableCharacterCount = partitionWalker.PartitionCurrent.RichCharacterList.Count - partitionWalker.RelativeCharacterIndex;
            if (thisLoopAvailableCharacterCount <= 0)
                break;

            int takeActual = lengthToDecorate < thisLoopAvailableCharacterCount ? lengthToDecorate : thisLoopAvailableCharacterCount;
            for (int i = 0; i < takeActual; i++)
            {
                partitionWalker.PartitionCurrent.RichCharacterList[partitionWalker.RelativeCharacterIndex + i] =
                    partitionWalker.PartitionCurrent.RichCharacterList[partitionWalker.RelativeCharacterIndex + i] with
                    {
                        DecorationByte = 1 // This originally would've defaulted to 0 so anything other than that suffices to see that something changed.
                    };
                --lengthToDecorate;
            }

            if (partitionWalker.PartitionIndex >= partitionWalker.Model.PartitionList.Count - 1)
                break;
            else if (lengthToDecorate <= 0)
                break;
            else
                partitionWalker.MoveToFirstCharacterOfTheNextPartition();
        }

        var afterRc1 = partitionList[1].RichCharacterList[4063];
        var afterRc2 = partitionList[2].RichCharacterList[0];

        // The "subLength" enumeration doesn't actually change the relative index, it just reads
        // all it can/needs to from the current partition
        // and leaves the 'partitionWalker.RelativeCharacterIndex' unchanged.
        //
        // This feels odd.
        // But I can't have the partitionWalker iterate otherwise optimization would be a mess
        // because the edit I'm performing would probably need to be abstracted into something,
        // and that abstraction would be the overhead.

        // Not only am I still exhausted,
        // but I still have cellulitis, what a bummer.
        //
        // In fact I'm scheduled to take an antibiotic right now brb
    }

    /// <summary>
    /// This should be allowed to occur, not because the behavior is desirable,
    /// but because any invoker is intended to start off at a position index which
    /// is provided by the TextEditorModel that is having its partitions walked.
    /// 
    /// Then, this TextEditorModel has the responsibility of not
    /// returning a character position index that resides in a multibyte character.
    /// 
    /// Following that, any edits are presumed to properly reposition the cursor
    /// such that the user's cursor isn't between a multibyte character.
    /// 
    /// Thus, the cost of checking for a multibyte character only needs to be incurred
    /// a single time at the start of a "transaction".
    /// </summary>
    [Fact]
    public void Seek_MiddleMultibyteCharacter()
    {
    }

    /// <summary>
    /// TODO: I'll have to figure out the test text later...
    /// ...I think I changed the code in the TextEditorModel recently
    /// such that you have to use at least a 4,096 partition size.
    /// I need 3 partitions to test First, Intermediate, Last.
    /// Thus this is 3 partitions worth of text.
    /// </summary>
    private static readonly string _content = @"using Microsoft.AspNetCore.Components.Web;
using Clair.Common.RazorLib;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.Dimensions.Models;
using Clair.Common.RazorLib.Dynamics.Models;
using Clair.Common.RazorLib.JavaScriptObjects.Models;
using Clair.Common.RazorLib.Panels.Models;
using Clair.Common.RazorLib.Tooltips.Models;
using Clair.Common.RazorLib.Tabs.Models;
using Clair.Common.RazorLib.BackgroundTasks.Models;
using Clair.TextEditor.RazorLib.JavaScriptObjects.Models;
using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.TextEditor.RazorLib.Decorations.Models;
using Clair.TextEditor.RazorLib.Groups.Models;
using Clair.TextEditor.RazorLib.TextEditors.Displays;

namespace Clair.TextEditor.RazorLib.TextEditors.Models.Internals;

/// <summary>
/// This type reduces the amount of properties that need to be copied from one TextEditorViewModel instance to another
/// by chosing to have some of the state shared between instances.
/// </summary>
public sealed class TextEditorViewModelPersistentState : IDisposable, ITab, IPanelTab, IDialog, IDrag
{
    public TextEditorViewModelPersistentState(
        int viewModelKey,
        ResourceUri resourceUri,
        TextEditorService textEditorService,
        Category category,
        Action<TextEditorModel>? onSaveRequested,
        Func<TextEditorModel, string>? getTabDisplayNameFunc,
        List<Key<TextEditorPresentationModel>> firstPresentationLayerKeysList,
        List<Key<TextEditorPresentationModel>> lastPresentationLayerKeysList,
        bool showFindOverlay,
        string replaceValueInFindOverlay,
        bool showReplaceButtonInFindOverlay,
        string findOverlayValue,
        bool findOverlayValueExternallyChangedMarker,
        MenuKind menuKind,
        ITooltipModel tooltipModel,
        bool shouldRevealCursor,
        TextEditorDimensions textEditorDimensions,
        int scrollLeft,
        int scrollTop,
        int scrollWidth,
        int scrollHeight,
        int marginScrollHeight,
        CharAndLineMeasurements charAndLineMeasurements)
    {
        ViewModelKey = viewModelKey;
        ResourceUri = resourceUri;
        TextEditorService = textEditorService;
        Category = category;
        OnSaveRequested = onSaveRequested;
        GetTabDisplayNameFunc = getTabDisplayNameFunc;
        FirstPresentationLayerKeysList = firstPresentationLayerKeysList;
        LastPresentationLayerKeysList = lastPresentationLayerKeysList;
        
        ShowFindOverlay = showFindOverlay;
        ReplaceValueInFindOverlay = replaceValueInFindOverlay;
        ShowReplaceButtonInFindOverlay = showReplaceButtonInFindOverlay;
        FindOverlayValue = findOverlayValue;
        FindOverlayValueExternallyChangedMarker = findOverlayValueExternallyChangedMarker;
        
        MenuKind = menuKind;
        TooltipModel = tooltipModel;

        ShouldRevealCursor = shouldRevealCursor;
        
        ComponentType = typeof(TextEditorViewModelDisplay);
        ComponentParameterMap = new()
        {
            { nameof(TextEditorViewModelDisplay.TextEditorViewModelKey), ViewModelKey }
        };

        _dragTabComponentType = typeof(Clair.Common.RazorLib.Drags.Displays.DragDisplay);

        DialogFocusPointHtmlElementId = $""ci_dialog-focus-point_{DynamicViewModelKey.Guid}"";
    
        TextEditorDimensions = textEditorDimensions;
        ScrollLeft = scrollLeft;
        ScrollTop = scrollTop;
        ScrollWidth = scrollWidth;
        ScrollHeight = scrollHeight;
        MarginScrollHeight = marginScrollHeight;
        CharAndLineMeasurements = charAndLineMeasurements;
    }

    /// <summary>
    /// The main unique identifier for a <see cref=""TextEditorViewModel""/>, used in many API.
    /// 
    /// 0 indicates 'Empty'
    /// 
    /// As for int max value wrap around resulting in two keys that are equal.
    /// You shouldn't be holding on to a viewmodel long enough to invoke NewViewModelKey int.MaxValue amount of times
    /// while still holding a reference to the first viewmodel with that same key.
    /// 
    /// It isn't that you can't get more than int.MavValue amount of ids, it is that
    /// you shouldn't be holding onto any individual view model for that length of time.
    /// 
    /// If you have long life viewmodels then use GetViewModelKeyLongLife().
    /// 0 remains 'Empty' but during an int wraparound, a check is done
    /// for whether you've landed on an int that was returned from GetViewModelKeyLongLife()
    /// if so then continue to the next key instead.
    /// 
    /// Theoretically you could GetViewModelKeyLongLife() for every valid int value.
    /// But once again, if you do such a thing you are likely storing
    /// the viewmodel object and thus you have bigger problems on your hands from a GarbageCollection perspective.
    /// </summary>
    public int ViewModelKey { get; set; }
    /// <summary>
    /// The unique identifier for a <see cref=""TextEditorModel""/>. The model is to say a representation of the file on a filesystem.
    /// The contents and such. Whereas the viewmodel is to track state regarding a rendered editor for that file, for example the cursor position.
    /// </summary>
    public ResourceUri ResourceUri { get; set; }
    /// <summary>
    /// Most API invocation (if not all) occurs through the <see cref=""ITextEditorService""/>
    /// </summary>
    public TextEditorService TextEditorService { get; set; }
    /// <summary>
    /// <inheritdoc cref=""Models.Category""/>
    /// </summary>
    public Category Category { get; set; }
    /// <summary>
    /// If one hits the keymap { Ctrl + s } when browser focus is within a text editor.
    /// </summary>
    public Action<TextEditorModel>? OnSaveRequested { get; set; }
    /// <summary>
    /// When a view model is rendered within a <see cref=""TextEditorGroup""/>, this Func can be used to render a more friendly tab name, than the resource uri path.
    /// </summary>
    public Func<TextEditorModel, string>? GetTabDisplayNameFunc { get; set; }
    /// <summary>
    /// <see cref=""FirstPresentationLayerKeysList""/> is painted prior to any internal workings of the text editor.<br/><br/>
    /// Therefore the selected text background is rendered after anything in the <see cref=""FirstPresentationLayerKeysList""/>.<br/><br/>
    /// When using the <see cref=""FirstPresentationLayerKeysList""/> one might find their css overriden by for example, text being selected.
    /// </summary>
    public List<Key<TextEditorPresentationModel>> FirstPresentationLayerKeysList { get; set; }
    /// <summary>
    /// <see cref=""LastPresentationLayerKeysList""/> is painted after any internal workings of the text editor.<br/><br/>
    /// Therefore the selected text background is rendered before anything in the <see cref=""LastPresentationLayerKeysList""/>.<br/><br/>
    /// When using the <see cref=""LastPresentationLayerKeysList""/> one might find the selected text background
    /// not being rendered with the text selection css if it were overriden by something in the <see cref=""LastPresentationLayerKeysList""/>.
    /// </summary>
    public List<Key<TextEditorPresentationModel>> LastPresentationLayerKeysList { get; set; }
    
    /// <summary>
    /// The find overlay refers to hitting the keymap { Ctrl + f } when browser focus is within a text editor.
    /// </summary>
    public bool ShowFindOverlay { get; set; }
    public bool ShowReplaceButtonInFindOverlay { get; set; }
    /// <summary>
    /// The find overlay refers to hitting the keymap { Ctrl + f } when browser focus is within a text editor.
    /// This property is what the find overlay input element binds to.
    /// </summary>
    public string FindOverlayValue { get; set; }
    /// <summary>
    /// If the user presses the keybind to show the FindOverlayDisplay while focused on the Text Editor,
    /// check if the user has a text selection.
    ///
    /// If they do have a text selection, then populate the FindOverlayDisplay with their selection.
    ///
    /// The issue arises however, how does one know whether FindOverlayValue changed due to
    /// the input element itself being typed into, versus some 'background action'.
    ///
    /// Because the UI already will update properly if the input element itself is interacted with.
    ///
    /// We only need to solve the case where it was a 'background action'.
    ///
    /// So, if this bool toggles to a different value than what the UI last saw,
    /// then the UI is to set the input element's value equal to the 'FindOverlayValue'
    /// because a 'background action' modified the value.
    /// </summary>
    public bool FindOverlayValueExternallyChangedMarker { get; set; }
    public string ReplaceValueInFindOverlay { get; set; }
    
    /// <summary>
    /// This property determines the menu that is shown in the text editor.
    ///
    /// For example, when this property is <see cref=""MenuKind.AutoCompleteMenu""/>,
    /// then the autocomplete menu is displayed in the text editor.
    /// </summary>
    public MenuKind MenuKind { get; set; }
    /// <summary>
    /// This property determines the tooltip that is shown in the text editor.
    /// </summary>
    public ITooltipModel? TooltipModel { get; set; }
    
    public bool ShouldRevealCursor { get; set; }
    
    private int _seenGutterWidth = -2;
    private string _gutterWidthCssValue;
    
    /// <summary>
    /// This method is not intuitive, because it doesn't make use of 'Changed_GutterWidth'.
    ///
    /// It tracks the int value for the '_gutterWidth' when it does the '.ToString()',
    /// then it checks if the int value had changed.
    ///
    /// This is because if the method were to use 'Changed_GutterWidth',
    /// then it'd presumably want to say 'Changed_GutterWidth = false'
    /// when doing the '.ToString()' so that it re-uses the value.
    ///
    /// But, this would then clobber the functionality of 'TextEditorVirtualizationResult'.
    /// </summary>
    public string GetGutterWidthCssValue()
    {
        if (_seenGutterWidth != _gutterWidth)
        {
            _seenGutterWidth = _gutterWidth;
            _gutterWidthCssValue = GutterWidth.ToString();
        }
        return _gutterWidthCssValue;
    }
    
    private int _seenTextEditorHeight;
    private int _seenLineHeight;
    private string _gutterColumnHeightCssValue;
    
    public void ManuallyPropagateOnContextMenu(MouseEventArgs mouseEventArgs)
    {
        var localHandleTabButtonOnContextMenu = TabCascadingValueBatch?.HandleTabButtonOnContextMenu;
        if (localHandleTabButtonOnContextMenu is null)
            return;

        CommonService.Enqueue(new CommonWorkArgs
        {
            WorkKind = CommonWorkKind.Tab_ManuallyPropagateOnContextMenu,
            HandleTabButtonOnContextMenu = localHandleTabButtonOnContextMenu,
            TabContextMenuEventArgs = new TabContextMenuEventArgs(mouseEventArgs, this, () => Task.CompletedTask),
        });
    }
    
    public async Task CloseTabOnClickAsync()
    {
        var localTabGroup = TabGroup;
        if (localTabGroup is null)
            return;

        await localTabGroup.CloseAsync(this).ConfigureAwait(false);
    }
    
    public void Dispose()
    {
        TextEditorService.WorkerArbitrary.PostUnique(editContext =>
        {
            DisposeComponentData(editContext, ComponentData);
            return ValueTask.CompletedTask;
        });
    }
}
";
}
