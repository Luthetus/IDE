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
/// And it currently isn't that big of a deal,
/// and I think you actually can use partitions the entireway through without issues.
///     - The TextEditorModel when closed needs to be put in a state where
///         the GC is able to collect it, and the various partitions that compose the TextEditorModel's content.
///     - This currently isn't being done. So the overhead of the partition logic is massive at the moment.
///         - But if I can reliably prove that the GC collects everything when you close the tab,
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
///     - Analyze memory usage by using the .NET Object Allocation tool:
///         - https://learn.microsoft.com/en-us/visualstudio/profiling/dotnet-alloc-tool
///     - Fundamentals of garbage collection
///         - https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/fundamentals
///
///
///
/// </summary>
public class Partitions
{
    [Fact]
    public void Aaa()
    {
    }
}
