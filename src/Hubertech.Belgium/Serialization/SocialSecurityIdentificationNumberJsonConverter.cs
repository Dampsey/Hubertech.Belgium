using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hubertech.Belgium.Serialization;

/// <summary>
/// Converts a social security identification number to and from a JSON string.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SocialSecurityIdentificationNumber"/> is annotated with this converter: there is nothing to register,
/// with or without the System.Text.Json source generator.
/// </para>
/// <para>
/// The value is written in full as its eleven digits, <c>85073003328</c>, unlike
/// <see cref="SocialSecurityIdentificationNumber.ToString()"/>, which masks it: JSON carries the number to programs
/// that need it. Mind where such JSON is logged. Any string that
/// <see cref="SocialSecurityIdentificationNumber.Parse(ReadOnlySpan{char})"/> accepts is read, for example
/// <c>85.07.30-033.28</c>.
/// </para>
/// <para>
/// An invalid string throws a <see cref="JsonException"/> whose message is the localized message
/// of the <see cref="BelgianValidationError"/>, and whose inner exception is a
/// <see cref="BelgianFormatException"/> carrying it. A token that is not a string, <c>null</c>
/// included, throws a <see cref="JsonException"/> as well: declare the member as
/// <c>SocialSecurityIdentificationNumber?</c> to accept <c>null</c>. The empty
/// <c>default(SocialSecurityIdentificationNumber)</c> cannot be written.
/// </para>
/// <para>
/// The value can also be a dictionary key.
/// </para>
/// </remarks>
public sealed class SocialSecurityIdentificationNumberJsonConverter : JsonConverter<SocialSecurityIdentificationNumber>
{
    private const string Format = "N";

    /// <inheritdoc/>
    public override SocialSecurityIdentificationNumber Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        BelgianJson.Read<SocialSecurityIdentificationNumber>(ref reader);

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, SocialSecurityIdentificationNumber value, JsonSerializerOptions options) =>
        BelgianJson.Write(writer, value, Format);

    /// <inheritdoc/>
    public override SocialSecurityIdentificationNumber ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        BelgianJson.ReadAsPropertyName<SocialSecurityIdentificationNumber>(ref reader);

    /// <inheritdoc/>
    public override void WriteAsPropertyName(Utf8JsonWriter writer, SocialSecurityIdentificationNumber value, JsonSerializerOptions options) =>
        BelgianJson.WriteAsPropertyName(writer, value, Format);
}
