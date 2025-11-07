using Clair.Common.RazorLib;
using Clair.TextEditor.RazorLib.Characters.Models;

namespace Clair.TextEditor.RazorLib.TextEditors.Models;

/// <summary>
/// It is undefined behavior to interact with a partition from outside a "TextEditorEditContext".
/// </summary>
public struct TextEditorPartition
{
    public TextEditorPartition(ValueList<RichCharacter> richCharacterList)
    {
        RichCharacterList = richCharacterList;
    }

    public ValueList<RichCharacter> RichCharacterList { get; set; }

    /// <summary>
    /// This is the count of rich characters in THIS particular partition, the <see cref="TextEditorModel.DocumentLength"/>
    /// contains the whole count of rich characters across all partitions.
    /// </summary>
    public int Count => RichCharacterList.Count;

    public TextEditorPartition Insert(
        int relativePositionIndex,
        RichCharacter richCharacter)
    {
        return new(RichCharacterList.New_Insert(relativePositionIndex, richCharacter));
    }

    public TextEditorPartition InsertRange(
        int relativePositionIndex,
        IEnumerable<RichCharacter> richCharacterList)
    {
        // TODO: Do a more optimized InsertRange
        var n = RichCharacterList;
        foreach (var rc in richCharacterList)
        {
            n = n.C_Insert(relativePositionIndex, rc);
        }
        return new(n);
    }

    public TextEditorPartition RemoveAt(int relativePositionIndex)
    {
        return new(RichCharacterList.C_RemoveAt(relativePositionIndex));
    }

    public TextEditorPartition RemoveRange(int relativePositionIndex, int count)
    {
        var n = RichCharacterList;
        for (int i = relativePositionIndex + count - 1; i >= 0; i--)
        {
            n = n.C_RemoveAt(i);
        }
        return new(n);
    }

    public TextEditorPartition AddRange(IEnumerable<RichCharacter> richCharacterList)
    {
        return InsertRange(Count, richCharacterList);
    }

    /// <summary>
    /// If either character or decoration byte are 'null', then the respective
    /// collection will be left unchanged.
    /// 
    /// i.e.: to change ONLY a character value invoke this method with decorationByte set to null,
    ///       and only the <see cref="CharList"/> will be changed.
    /// </summary>
    public TextEditorPartition SetItem(
        int relativePositionIndex,
        RichCharacter richCharacter)
    {
        return new(RichCharacterList.C_SetItem(relativePositionIndex, richCharacter));
    }
}
