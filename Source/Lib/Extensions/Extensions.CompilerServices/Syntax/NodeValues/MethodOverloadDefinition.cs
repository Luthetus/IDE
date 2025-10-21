namespace Clair.Extensions.CompilerServices.Syntax.NodeValues;

public struct MethodOverloadDefinition
{
    public MethodOverloadDefinition(
        int absolutePathId,
        int offsetGroup,
        int scopeIndexKey)
    {
        AbsolutePathId = absolutePathId;
        OffsetGroup = offsetGroup;
        ScopeIndexKey = scopeIndexKey;
    }

    public int AbsolutePathId { get; set; }
    public int OffsetGroup { get; set; }
    public int ScopeIndexKey { get; set; }
}
