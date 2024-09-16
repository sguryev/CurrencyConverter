using FluentValidation;

namespace CurrencyConverter.Web.Validation;

public class ConvertRequestValidator : AbstractValidator<ConvertRequest>
{
    private static readonly string[] _exceptCurrencies = ["TRY", "PLN", "THB", "MXN"];

    public ConvertRequestValidator()
    {
        RuleFor(x => x.BaseCode).NotNull().ValidCurrencyCode().NotOneOf(_exceptCurrencies);
        RuleFor(x => x.TargetCode).NotNull().ValidCurrencyCode().NotOneOf(_exceptCurrencies);
        RuleFor(x => x.Amount).InclusiveBetween(0.001m, decimal.MaxValue);
    }
}