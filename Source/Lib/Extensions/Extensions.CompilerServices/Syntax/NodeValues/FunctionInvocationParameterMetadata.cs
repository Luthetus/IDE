using Clair.Extensions.CompilerServices.Syntax.Enums;

namespace Clair.Extensions.CompilerServices.Syntax.NodeValues;

public record struct FunctionInvocationParameterMetadata(
    int IdentifierStartInclusiveIndex,
    TypeReferenceValue TypeReference,
    ParameterModifierKind ParameterModifierKind);
