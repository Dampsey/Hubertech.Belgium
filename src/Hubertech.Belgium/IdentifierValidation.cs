using System.ComponentModel.DataAnnotations;

namespace Hubertech.Belgium;

/// <summary>
/// The implementation shared by the validation attributes of the identifier types.
/// </summary>
internal static class IdentifierValidation
{
    internal static bool IsValid<T>(ValidationAttribute attribute, object? value)
        where T : struct, IBelgianIdentifier<T> =>
        GetError<T>(attribute, value) is null;

    internal static ValidationResult? GetResult<T>(ValidationAttribute attribute, object? value, ValidationContext validationContext)
        where T : struct, IBelgianIdentifier<T>
    {
        if (GetError<T>(attribute, value) is not { } error)
        {
            return ValidationResult.Success;
        }

        // As with any validation attribute, a message set on the attribute takes precedence.
        string message = attribute.ErrorMessage is null && attribute.ErrorMessageResourceName is null
            ? error.Message
            : attribute.FormatErrorMessage(validationContext.DisplayName);
        string[]? memberNames = validationContext.MemberName is { } memberName ? [memberName] : null;

        return new ValidationResult(message, memberNames);
    }

    private static BelgianValidationError? GetError<T>(ValidationAttribute attribute, object? value)
        where T : struct, IBelgianIdentifier<T> =>
        value switch
        {
            // A missing value is the business of RequiredAttribute, as for RegularExpressionAttribute.
            null or "" => null,
            string s => T.TryParse(s, out _, out var error) ? null : error,
            T identifier => identifier.IsEmpty ? BelgianValidationError.Empty(typeof(T).Name) : null,
            _ => throw new InvalidOperationException(
                $"{attribute.GetType().Name} validates strings and {typeof(T).Name} values, not {value.GetType()} values."),
        };
}
