using CurrencyConverter.Web.Validation.Base;
using FluentValidation;

namespace CurrencyConverter.Web.Validation;

public sealed class NotOneOfValidator<T> : PropertyValidatorBase<T, string?>
{
    /// <summary>
    /// A <see cref="HashSet{T}"/> of except values.
    /// </summary>
    private readonly HashSet<string> _exceptValues;

    /// <summary>
    /// Initializes a new instance of <see cref="NotOneOfValidator{T}"/>.
    /// </summary>
    /// <param name="exceptValues">A set of except values.</param>
    public NotOneOfValidator(IEnumerable<string> exceptValues)
    {
        _exceptValues = new HashSet<string>(exceptValues, StringComparer.InvariantCultureIgnoreCase);
    }

    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return $"Must not be one of the following values: {{{nameof(_exceptValues)}}}.";
    }

    /// <inheritdoc />
    public override bool IsValid(ValidationContext<T> context, string? value)
    {
        if (value == null || !_exceptValues.Contains(value))
            return true;

        context.MessageFormatter.AppendArgument(nameof(_exceptValues), string.Join(", ", _exceptValues));
        return false;
    }
}