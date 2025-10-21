using Clair.Extensions.CompilerServices.Syntax.Enums;

namespace Clair.Extensions.CompilerServices.Syntax.NodeValues;

/// <summary>
/// Used when invoking a function.
/// </summary>
public struct FunctionParameter
{
    public FunctionParameter(ParameterModifierKind parameterModifierKind)
    {
        ParameterModifierKind = parameterModifierKind;
    }

    public ParameterModifierKind ParameterModifierKind { get; }
}
