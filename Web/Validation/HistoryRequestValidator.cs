using FluentValidation;

namespace CurrencyConverter.Web.Validation;

public class HistoryRequestValidator : AbstractValidator<HistoryRequest>
{
    public HistoryRequestValidator()
    {
        RuleFor(x => x.Code).NotNull().ValidCurrencyCode();
    }
}