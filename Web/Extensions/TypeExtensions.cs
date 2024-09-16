namespace CurrencyConverter.Web.Extensions;

public static class TypeExtensions
{
    /// <summary>
    /// Returns the cleaned type name for Generic types (e.g. "Dictionary" instead of "Dictionary`2").
    /// </summary>
    /// <param name="type">Type to extract the clean name.</param>
    /// <returns>The clean type name.</returns>
    public static string GetCleanName(this Type type)
    {
        var typeName = type.Name;
        var quoteIndex = typeName.IndexOf('`');

        return quoteIndex == -1
            ? typeName
            : typeName[..quoteIndex];
    }
}