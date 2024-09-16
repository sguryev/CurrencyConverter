using CurrencyConverter.Web.Extensions;
using FluentValidation;
using FluentValidation.Validators;

namespace CurrencyConverter.Web.Validation.Base;

public abstract class PropertyValidatorBase<T, TProperty> : PropertyValidator<T, TProperty>
{
    /// <inheritdoc />
    public override string Name { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="PropertyValidatorBase{T,TProperty}"/>.
    /// </summary>
    protected PropertyValidatorBase()
    {
        Name = GetType().GetCleanName();
    }

    /// <inheritdoc />
    protected override string GetDefaultMessageTemplate(string _) => ValidatorOptions.Global.LanguageManager.GetString(Name);
}