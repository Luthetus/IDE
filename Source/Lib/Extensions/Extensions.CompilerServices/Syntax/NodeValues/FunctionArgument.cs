using Clair.Extensions.CompilerServices.Syntax.Enums;
using Clair.Extensions.CompilerServices.Syntax.NodeReferences;

namespace Clair.Extensions.CompilerServices.Syntax.NodeValues;

/// <summary>
/// Used when defining a function.
/// </summary>
public struct FunctionArgument
{
    /// <summary>
    /// The variableDeclarationNode instance is pooled so do NOT store it long term.
    /// This constructor solely reads the properties and copies them.
    /// </summary>
    public FunctionArgument(
        VariableDeclarationNode variableDeclarationNode,
        SyntaxToken optionalCompileTimeConstantToken,
        ArgumentModifierKind argumentModifierKind)
    {
        TypeReference = variableDeclarationNode.TypeReference;
        IdentifierToken = variableDeclarationNode.IdentifierToken;
        VariableKind = variableDeclarationNode.VariableKind;
        
        OptionalCompileTimeConstantToken = optionalCompileTimeConstantToken;
        ArgumentModifierKind = argumentModifierKind;
    }

    public TypeReferenceValue TypeReference { get; }
    public SyntaxToken IdentifierToken { get; }
    public VariableKind VariableKind { get; }
    public SyntaxToken OptionalCompileTimeConstantToken { get; }
    public ArgumentModifierKind ArgumentModifierKind { get; }
}
