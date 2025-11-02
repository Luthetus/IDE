namespace Clair.Tests.csproj.csproj;

/// <summary>
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
/// </summary>
public class Partitions
{
    [Fact]
    public void Aaa()
    {
    }
}
