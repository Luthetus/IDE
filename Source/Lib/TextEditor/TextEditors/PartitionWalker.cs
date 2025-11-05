using Clair.TextEditor.RazorLib.Characters.Models;
using Clair.TextEditor.RazorLib.Exceptions;
using Clair.TextEditor.RazorLib.TextEditors.Models;

namespace Clair.TextEditor.RazorLib.TextEditors;

/// <summary>
/// # TOC
/// =====
/// - Goal of class
/// - IEnumerable vs ...
/// - Move PartitionWalker properties native to the TextEditorModel itself
///       rather than having this separate type?
/// - Long Term usage
/// - Edits to a TextEditorModel resulting in an invalid PartitionWalker
/// - Scuffed code markers for regression
/// 
/// 
/// 
/// # Goal of class
/// ===============
/// Abstract enumeration of RichCharacter "partitions" by
/// internally managing state to allow a pseudo "flat list" of RichCharacter.
/// 
/// 
/// 
/// # IEnumerable vs ...
/// ====================
/// I'm considering an IEnumerable but I'd want to think about it in great detail
/// due to how much overhead could easily be incurred with doing that.
/// 
/// Versus if I have specialized methods that do things I want it to.
/// 
/// 
/// 
/// # Move PartitionWalker properties native to the TextEditorModel itself
/// # rather than having this separate type?
/// ======================================================================
/// I say no because the model instance can be accessed by multiple threads.
/// PartitionWalker ensures a text editor model can be "queried" concurrently.
/// 
/// 
/// 
/// # Long Term usage
/// =================
/// All text editor model state that relates to the RichCharacters
/// should be stored by partition.
/// 
/// This type will then provide the abstraction that displays
/// all the information as if it were a flat list.
/// 
/// i.e.: the List of tab character positions should be stored by partition.
/// 
/// This normalizes the amount of shifting in a large List of tab character positions
/// where you are inserting at a position that isn't the end of the list.
/// 
/// 
/// 
/// # Edits to a TextEditorModel resulting in an invalid PartitionWalker
/// ====================================================================
/// This shouldn't happen because the edits to a TextEditorModel
/// are performed within the <see cref="TextEditorEditContext"/>.
/// 
/// As well prior to the edit, a clone of the object is made
/// and the edits are made to the clone.
/// 
/// The class named <see cref="TextEditorViewModelLiason"/> is weird
/// I wanted to avoid the models having a direct reference to
/// the <see cref="TextEditorService"/> because
/// I was worried all the cloning of models with a direct reference to
/// a dependency injectable service could somehow be problematic.
/// 
/// Maybe it would be I'm not sure.
/// But I added an indirect reference to the <see cref="TextEditorService"/>
/// via the <see cref="TextEditorViewModelLiason"/>
/// for this reason. I have no proof for this being the case
/// and in fact I've added an extra object allocation to the app as whole
/// by doing this... as well other worsening things... I don't know what I'm doing.
/// 
/// 
/// 
/// # Scuffed code markers for regression
/// =====================================
/// The text: "2025-11-04 partition changes"
/// will be commented above anything that I recklessly comment out
/// in order to run the tests without build errors.
/// </summary>
public class PartitionWalker
{
    private TextEditorModel _model;

    public void ReInitialize(TextEditorModel model)
    {
        _model = model;
    }

    public int PartitionIndex { get; set; }
    /// <summary>
    /// Relative to the current position
    /// </summary>
    public int RelativeCharacterIndex { get; set; }
    /// <summary>
    /// If the list of partitions were to be flattened
    /// </summary>
    public int GlobalCharacterIndex { get; set; }
    /// <summary>
    /// ...RunningCount is probably calculable and if so remove it / expression bound property it...
    /// It is but you'd have to loop over any partitions before you
    /// because the counts aren't always the same.
    /// 
    /// ... isn't the running count the global position + 1????
    /// </summary>
    public int RunningCount => GlobalCharacterIndex + 1;
    public TextEditorPartition PartitionCurrent => _model.PartitionList[PartitionIndex];

    /// <summary>
    /// Updates the <see cref="PartitionIndex"/>, and <see cref="PartitionCurrent"/> properties
    /// to be the partition in which the globalIndex resides.
    /// </summary>
    /// 
    /// <remarks>
    /// The seek origin should be "dynamic" and "internally automated".
    /// 
    /// If the current position of the PartitionWalker is closer to the next desired globalIndex,
    /// then move from there.
    /// 
    /// Else if the first partition, 0th RichCharacter is closest to the next desired globalIndex,
    /// then start there.
    /// 
    /// Else if the last partition, ??? RichCharacter is closest to the next desired globalIndex,
    /// then start there.
    /// </remarks>
    public void Seek(int targetGlobalCharacterIndex)
    {
        // First iteration will reset position to the first partition, first character.
        // Eventually support for seek origin should be added.
        PartitionIndex = 0;
        RelativeCharacterIndex = 0;
        GlobalCharacterIndex = 0;

        // runningCount has to start at 0 and then equal the count iterations beyond the first because of 0 indexing

        // runningCount = 0;
        // GlobalCharacterIndex = -1;
        // RelativeCharacterIndex = -1;
        //
        //
        //   [ 'a', 'b', 'c', 'd'] ['e', 'f', 'g', 'h' ]
        // ^

        // runningCount = 0;
        // GlobalCharacterIndex = 0;
        // RelativeCharacterIndex = 0;
        //
        // 
        //   [ 'a', 'b', 'c', 'd'] ['e', 'f', 'g', 'h' ]
        //      ^

        // Actually I think it is the diagram above this comment.
        // Whether you've moved to the first element or not.

        // I don't think I want to even deal with the runningCount it is just kind of annoying to read.
        // Cause I'm dealing with indices and then runningCount isn't an index it just is a headache.
        // Unless I need it I'm gonna get rid of it until then.

        // BUT you also have to consider what you said, the first partition is 0 based
        // but... so you gotta diagram the transition between them.
        // If the numbers off by 1 it ain't gonna work so I should just figure this out now.

        // runningCount = 3;
        // GlobalCharacterIndex = 3;
        // RelativeCharacterIndex = 3;
        //
        //
        //   [ 'a', 'b', 'c', 'd'] ['e', 'f', 'g', 'h' ]
        //                     ^

        // runningCount = 4;
        // GlobalCharacterIndex = 4;
        // RelativeCharacterIndex = 0;
        //
        // 
        //   [ 'a', 'b', 'c', 'd'] ['e', 'f', 'g', 'h' ]
        //                           ^

        for (int i = 0; i < _model.PartitionList.Count; i++)
        {
            if (RunningCount + PartitionCurrent.Count >= targetGlobalCharacterIndex)
            {
                RelativeCharacterIndex = targetGlobalCharacterIndex - RunningCount;
                PartitionIndex = i;
                GlobalCharacterIndex += RelativeCharacterIndex + 1;
                break;
            }
            else
            {
                GlobalCharacterIndex += PartitionCurrent.Count;
            }
        }

        if (PartitionIndex == -1)
            throw new ClairTextEditorException("if (indexOfPartitionWithAvailableSpace == -1)");

        if (RelativeCharacterIndex == -1)
            throw new ClairTextEditorException("if (relativePositionIndex == -1)");

        /*
        if (partition.Count >= _model.PersistentState.PartitionSize)
        {
            _model.__SplitIntoTwoPartitions(i);
            i--;
            continue;
        }
         */
    }
}
