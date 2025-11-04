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
    [Fact]
    public void Seek_FirstPartition_FirstCharacter()
    {

    }

    [Fact]
    public void Seek_FirstPartition_IntermediateCharacter()
    {
    }
    
    [Fact]
    public void Seek_FirstPartition_LastCharacter()
    {
    }

    [Fact]
    public void Seek_IntermediatePartition_FirstCharacter()
    {
    }

    [Fact]
    public void Seek_IntermediatePartition_IntermediateCharacter()
    {
    }

    [Fact]
    public void Seek_IntermediatePartition_LastCharacter()
    {
    }

    [Fact]
    public void Seek_LastPartition_FirstCharacter()
    {
    }

    [Fact]
    public void Seek_LastPartition_IntermediateCharacter()
    {
    }

    [Fact]
    public void Seek_LastPartition_LastCharacter()
    {
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
    /// Transitioning from n partition to n+1 partition because the data
    /// in partition n was fully enumerated and the next value is
    /// the next partition's first entry.
    /// </summary>
    [Fact]
    public void Enumerate_PartitionOverflow()
    {
    }
}
