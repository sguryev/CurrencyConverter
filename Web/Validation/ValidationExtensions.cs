using System.Text.RegularExpressions;
using FluentValidation;

namespace CurrencyConverter.Web.Validation;

public static class ValidationExtensions
{
    public static IRuleBuilderOptions<T, string?> NotOneOf<T>(this IRuleBuilder<T, string?> ruleBuilder, IEnumerable<string> allowedValues) =>
        ruleBuilder.SetValidator(new NotOneOfValidator<T>(allowedValues));

    public static IRuleBuilderOptions<T, string?> NotOneOf<T>(this IRuleBuilder<T, string?> ruleBuilder, params string[] allowedValues) =>
        ruleBuilder.SetValidator(new NotOneOfValidator<T>(allowedValues));

    public static IRuleBuilderOptions<T, string?> ValidCurrencyCode<T>(this IRuleBuilder<T, string?> builder) =>
        builder.ChildRules(rules =>
        {
            rules.When(m => m != null, () =>
            {
                rules.RuleFor(x => x).Length(3).Matches(GeneratedRegexs.CurrencyCode());
            });
        });
}

public static partial class GeneratedRegexs
{
    [GeneratedRegex("^[A-Z]{3}$")] public static partial Regex CurrencyCode();
}