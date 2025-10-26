namespace Clair.Extensions.CompilerServices.Syntax.Interfaces;

/// <summary>
/// WARNING implementations need to be hardcoded in ParseExpressions.cs:2396
/// until this hack is removed.
/// ...
/// '(List<(int, bool)>)' required the following hack because the CSharpParserContextKind.ForceStatementExpression enum
/// is reset after the first TypeClauseNode in a statement is made, and there was no clear way to set it back again in this situation.;
/// TODO: Don't do this '(List<(int, bool)>)', instead figure out how to have CSharpParserContextKind.ForceStatementExpression live longer in a statement that has many TypeClauseNode(s).
/// </summary>
public interface IGenericParameterNode : IExpressionNode
{
    public SyntaxToken OpenAngleBracketToken { get; set; }
    /// <summary>The default value for this needs to be -1 to indicate that there are no entries in the pooled list.</summary>
    public int OffsetGenericParameterEntryList { get; set; }
    public int LengthGenericParameterEntryList { get; set; }
    public SyntaxToken CloseAngleBracketToken { get; set; }
    
    public bool IsParsingGenericParameters { get; set; }
}
