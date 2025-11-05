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
        if (targetGlobalCharacterIndex < 0 || targetGlobalCharacterIndex >= _model.CharCount)
            throw new ClairTextEditorException("if (targetGlobalCharacterIndex >= _model.CharCount)");

        // First iteration will reset position to the first partition, first character.
        // Eventually support for seek origin should be added.
        PartitionIndex = 0;
        RelativeCharacterIndex = 0;
        GlobalCharacterIndex = 0;
        var runningCount = 0;

        for (int i = 0; i < _model.PartitionList.Count; i++)
        {
            // Counts are always greater than indices so this sounds correct,
            // in addition to making the test pass.
            //
            // When checking if the 0th index in a partition... it is the count of the previous partitions.
            //
            // But this if statement seems just plain wrong.
            //
            // You have some available current index then want to determine if the next partition would make
            // the target index available?
            //
            //
            // The if statement is saying if I have a partition of count 0,
            // and the target is 4064 that I'd have space in the second partition...
            if (targetGlobalCharacterIndex < GlobalCharacterIndex + _model.PartitionList[i].Count)
            {
                RelativeCharacterIndex = targetGlobalCharacterIndex - runningCount;
                GlobalCharacterIndex += RelativeCharacterIndex;
                PartitionIndex = i;
                break;
            }
            else
            {
                GlobalCharacterIndex += _model.PartitionList[i].Count;
                runningCount += _model.PartitionList[i].Count;
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
