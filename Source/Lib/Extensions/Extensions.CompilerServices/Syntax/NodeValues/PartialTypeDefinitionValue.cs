namespace Clair.Extensions.CompilerServices.Syntax.NodeValues;

public struct PartialTypeDefinitionValue
{
    public PartialTypeDefinitionValue(
        int absolutePathId,
        int indexStartGroup,
        int scopeOffset,
        bool isCSharpFile)
    {
        AbsolutePathId = absolutePathId;
        IndexStartGroup = indexStartGroup;
        ScopeSubIndex = scopeOffset;
        IsCSharpFile = isCSharpFile;
    }

    public int AbsolutePathId { get; set; }
    public int IndexStartGroup { get; set; }
    public int ScopeSubIndex { get; set; }
    public bool IsCSharpFile { get; set; }
}
