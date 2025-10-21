namespace Clair.Extensions.CompilerServices.Syntax.NodeValues;

public record struct NamespaceGroup
{
    public NamespaceGroup(
        int charIntSum,
        List<NamespaceStatementValue> namespaceStatementValueList)
    {
        CharIntSum = charIntSum;
        NamespaceStatementValueList = namespaceStatementValueList;
    }

    public int CharIntSum { get; }
    public List<NamespaceStatementValue> NamespaceStatementValueList { get; }

    public bool ConstructorWasInvoked => NamespaceStatementValueList is not null;
}
