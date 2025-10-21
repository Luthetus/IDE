using Clair.TextEditor.RazorLib.Decorations.Models;

namespace Clair.CompilerServices.DotNetSolution;

public class DotNetSolutionDecorationMapper : IDecorationMapper
{
    public string Map(byte decorationByte)
    {
        var decoration = (DotNetSolutionDecorationKind)decorationByte;

        return decoration switch
        {
            DotNetSolutionDecorationKind.None => string.Empty,
            DotNetSolutionDecorationKind.AttributeName => "ci_attribute-name",
            DotNetSolutionDecorationKind.AttributeValue => "ci_attribute-value",
            DotNetSolutionDecorationKind.AttributeOperator => "ci_attribute-value",
            DotNetSolutionDecorationKind.AttributeDelimiter => "ci_attribute-value",
            DotNetSolutionDecorationKind.AttributeNameInjectedLanguageFragment => "ci_injected-language-fragment",
            DotNetSolutionDecorationKind.AttributeValueInjectedLanguageFragment => "ci_injected-language-fragment",
            DotNetSolutionDecorationKind.AttributeValueInterpolationStart => "ci_attribute-value",
            DotNetSolutionDecorationKind.AttributeValueInterpolationContinue => "ci_attribute-value",
            DotNetSolutionDecorationKind.TagNameNone => "ci_tag-name",
            DotNetSolutionDecorationKind.TagNameOpen => "ci_tag-name",
            DotNetSolutionDecorationKind.TagNameClose => "ci_tag-name",
            DotNetSolutionDecorationKind.TagNameSelf => "ci_tag-name",
            DotNetSolutionDecorationKind.Comment => "ci_comment",
            DotNetSolutionDecorationKind.CustomTagName => "ci_te_custom-tag-name",
            DotNetSolutionDecorationKind.EntityReference => "ci_te_entity-reference",
            DotNetSolutionDecorationKind.HtmlCode => "ci_te_html-code",
            DotNetSolutionDecorationKind.InjectedLanguageFragment => "ci_injected-language-fragment",
            DotNetSolutionDecorationKind.InjectedLanguageComponent => "ci_injected-language-component",
            DotNetSolutionDecorationKind.CSharpMarker => "ci_type",
            DotNetSolutionDecorationKind.Error => "ci_te_error",
            DotNetSolutionDecorationKind.InjectedLanguageCodeBlock => "ci_te_injected-language-code-block",
            DotNetSolutionDecorationKind.InjectedLanguageCodeBlockTag => "ci_te_injected-language-code-block-tag",
            DotNetSolutionDecorationKind.InjectedLanguageKeyword => "ci_keyword",
            DotNetSolutionDecorationKind.InjectedLanguageTagHelperAttribute => "ci_te_injected-language-tag-helper-attribute",
            DotNetSolutionDecorationKind.InjectedLanguageTagHelperElement => "ci_te_injected-language-tag-helper-element",
            DotNetSolutionDecorationKind.InjectedLanguageMethod => "ci_method",
            DotNetSolutionDecorationKind.InjectedLanguageVariable => "ci_variable",
            DotNetSolutionDecorationKind.InjectedLanguageType => "ci_type",
            DotNetSolutionDecorationKind.InjectedLanguageStringLiteral => "ci_string",
            _ => string.Empty,
        };
    }
}
