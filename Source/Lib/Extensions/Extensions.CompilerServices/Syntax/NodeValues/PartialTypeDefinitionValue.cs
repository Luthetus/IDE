namespace Clair.Extensions.CompilerServices.Syntax.NodeValues;

public struct PartialTypeDefinitionValue
{
    public PartialTypeDefinitionValue(
        int absolutePathId,
        int indexStartGroup,
        int scopeOffset)
    {
        AbsolutePathId = absolutePathId;
        IndexStartGroup = indexStartGroup;
        ScopeSubIndex = scopeOffset;
    }

    public int AbsolutePathId { get; set; }
    public int IndexStartGroup { get; set; }
    public int ScopeSubIndex { get; set; }
}
