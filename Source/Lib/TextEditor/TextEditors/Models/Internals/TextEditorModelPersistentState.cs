using Clair.TextEditor.RazorLib.Lines.Models;
using Clair.TextEditor.RazorLib.Decorations.Models;
using Clair.TextEditor.RazorLib.CompilerServices;
using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.TextEditor.RazorLib.Edits.Models;

namespace Clair.TextEditor.RazorLib.TextEditors.Models.Internals;

/// <summary>
/// This type reduces the amount of properties that need to be copied from one TextEditorModel instance to another
/// by chosing to have some of the state shared between instances.
/// </summary>
public sealed class TextEditorModelPersistentState
{
    public TextEditorModelPersistentState(
        TextEditorService textEditorService,
        List<int> viewModelKeyList,
        string fileExtension,
        IDecorationMapper decorationMapper,
        ICompilerService compilerService,
        int partitionSize,
        ResourceUri resourceUri,
        int editBlockIndex,
        List<TextEditorEdit> editBlockList)
    {
        /*
        // 2025-11-04 partition changes
        __LocalLineEndList = textEditorService.__LocalLineEndList;
        __LocalTabPositionList = textEditorService.__LocalTabPositionList;
        __TextEditorViewModelLiason = textEditorService.__TextEditorViewModelLiason;
        */

        ViewModelKeyList = viewModelKeyList;
        
        FileExtension = fileExtension;
        DecorationMapper = decorationMapper;
        CompilerService = compilerService;
        PartitionSize = partitionSize;
        ResourceUri = resourceUri;
        
        EditBlockIndex = editBlockIndex;
        EditBlockList = editBlockList;
    }

    /// <summary>
    /// Do not touch this property, it is used for the 'TextEditorModel.InsertMetadata(...)' method.
    /// </summary>
    public List<LineEnd> __LocalLineEndList { get; }
    /// <summary>
    /// Do not touch this property, it is used for the 'TextEditorModel.InsertMetadata(...)' method.
    /// </summary>
    public List<int> __LocalTabPositionList { get; }
    /// <summary>
    /// Do not touch this property, it is used for the 'TextEditorModel.InsertMetadata(...)' method.
    /// </summary>
    public TextEditorViewModelLiason __TextEditorViewModelLiason { get; }
    public List<int> ViewModelKeyList { get; set; }
    
    public string FileExtension { get; set; }
    public IDecorationMapper DecorationMapper { get; set; }
    public ICompilerService CompilerService { get; set; }
    public int PartitionSize { get; }
    public ResourceUri ResourceUri { get; set; }
    
    public int EditBlockIndex { get; set; }
    public List<TextEditorEdit> EditBlockList { get; set; }
    public bool IsMixedLineEndings { get; set; }
}
