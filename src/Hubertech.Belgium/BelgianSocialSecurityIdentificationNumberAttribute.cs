using System.ComponentModel.DataAnnotations;

namespace Hubertech.Belgium;

/// <summary>
/// Validates that a member holds a Belgian social security identification number, national
/// register or BIS number, and reports the localized message of the
/// <see cref="BelgianValidationError"/> when it does not.
/// </summary>
/// <remarks>
/// <para>
/// A string is valid when <see cref="SocialSecurityIdentificationNumber.Parse(string)"/> accepts it.
/// A <see cref="SocialSecurityIdentificationNumber"/> is valid unless it is empty, as a non-nullable member
/// left unset is: <see cref="RequiredAttribute"/> cannot tell, since a structure is never <see langword="null"/>.
/// </para>
/// <para>
/// <see langword="null"/> and the empty string are valid: combine with
/// <see cref="RequiredAttribute"/> to require a value. A member of another type is a programming
/// error, and validating it throws an <see cref="InvalidOperationException"/>.
/// </para>
/// <para>
/// The message is <see cref="BelgianValidationError.Message"/>, in the current UI culture. It never
/// quotes the number. Setting <see cref="ValidationAttribute.ErrorMessage"/> or
/// <see cref="ValidationAttribute.ErrorMessageResourceName"/> replaces it.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class BelgianSocialSecurityIdentificationNumberAttribute : ValidationAttribute
{
    /// <inheritdoc/>
    public override bool IsValid(object? value) => IdentifierValidation.IsValid<SocialSecurityIdentificationNumber>(this, value);

    /// <inheritdoc/>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) =>
        IdentifierValidation.GetResult<SocialSecurityIdentificationNumber>(this, value, validationContext);
}
