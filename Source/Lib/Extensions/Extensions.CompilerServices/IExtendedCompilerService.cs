using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.TextEditor.RazorLib.CompilerServices;
using Clair.Extensions.CompilerServices.Syntax;
using Clair.Extensions.CompilerServices.Syntax.Interfaces;
using Clair.Extensions.CompilerServices.Syntax.NodeValues;
using Clair.Extensions.CompilerServices.Syntax.Enums;

namespace Clair.Extensions.CompilerServices;

public interface IExtendedCompilerService : ICompilerService
{
    /// <summary>
    /// TextEditorEditContext thread safety is required when invoking this.
    ///
    /// 0 implies null
    /// 1 implies empty
    /// 
    /// </summary>
    public int TryAddFileAbsolutePath(string fileAbsolutePath);

    /// <summary>
    /// `0` will be returned if `_fileAbsolutePathToIntMap` did not contain the key.
    /// 
    /// 0 implies null
    /// 1 implies empty
    /// 
    /// </summary>
    public int TryGetFileAbsolutePathToInt(string fileAbsolutePath);

    /// <summary>
    /// `null` is returned if `_intToFileAbsolutePathMap` did not contain the key.
    /// 
    /// 0 implies null
    /// 1 implies empty
    /// 
    /// </summary>
    public string? TryGetIntToFileAbsolutePathMap(int intId);

    public ICompilerServiceResource? GetResourceByAbsolutePathId(int absolutePathId);

    /// <summary>
    /// unsafe vs safe are duplicates of the same code
    /// Safe implies the "TextEditorEditContext"
    /// </summary>
    public string? UnsafeGetText(int absolutePathId, TextEditorTextSpan textSpan);
    /// <summary>
    /// unsafe vs safe are duplicates of the same code
    /// Safe implies the "TextEditorEditContext"
    /// </summary>
    public string? UnsafeGetText(string absolutePath, TextEditorTextSpan textSpan);
    /// <summary>
    /// unsafe vs safe are duplicates of the same code
    /// Safe implies the "TextEditorEditContext"
    /// </summary>
    public string? SafeGetText(int absolutePathId, TextEditorTextSpan textSpan);

    public IReadOnlyList<GenericParameter> GenericParameterEntryList { get; }
    public IReadOnlyList<FunctionParameter> FunctionParameterEntryList { get; }
    public IReadOnlyList<FunctionArgument> FunctionArgumentEntryList { get; }
    
    public IReadOnlyList<TypeDefinitionTraits> TypeDefinitionTraitsList { get; }
    public IReadOnlyList<FunctionDefinitionTraits> FunctionDefinitionTraitsList { get; }
    public IReadOnlyList<VariableDeclarationTraits> VariableDeclarationTraitsList { get; }
    public IReadOnlyList<ConstructorDefinitionTraits> ConstructorDefinitionTraitsList { get; }

    public SyntaxNodeValue GetSyntaxNode(int positionIndex, ResourceUri resourceUri, ICompilerServiceResource? compilerServiceResource);
    public SyntaxNodeValue GetDefinitionNodeValue(TextEditorTextSpan textSpan, int absolutePathId, ICompilerServiceResource compilerServiceResource, Symbol? symbol = null);
    public (Scope Scope, SyntaxNodeValue CodeBlockOwner) GetCodeBlockTupleByPositionIndex(int absolutePathId, int positionIndex);
    public string GetIdentifierText(ISyntaxNode node, int absolutePathId);
}
