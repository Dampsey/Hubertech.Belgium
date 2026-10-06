namespace Hubertech.Belgium;

/// <summary>
/// The register that assigned a <see cref="SocialSecurityIdentificationNumber"/>.
/// </summary>
/// <remarks>
/// Values are stable: they are never renumbered nor reused, so they can be stored.
/// </remarks>
public enum SocialSecurityIdentificationNumberKind
{
    /// <summary>
    /// No register: the kind of <c>default(SocialSecurityIdentificationNumber)</c>.
    /// </summary>
    None = 0,

    /// <summary>
    /// A national register number, assigned to the persons registered in the National Register
    /// of natural persons.
    /// </summary>
    NationalRegister = 1,

    /// <summary>
    /// A BIS number, assigned by the Crossroads Bank for Social Security to the persons who are
    /// not registered in the National Register but deal with the Belgian social security, such
    /// as cross-border workers. Its month of birth is increased by 20 or 40.
    /// </summary>
    Bis = 2,
}
