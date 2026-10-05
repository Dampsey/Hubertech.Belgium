using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hubertech.Belgium.Serialization;

/// <summary>
/// Converts a Belgian IBAN to and from a JSON string.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="BelgianIban"/> is annotated with this converter: there is nothing to register,
/// with or without the System.Text.Json source generator.
/// </para>
/// <para>
/// The value is written in the electronic format, <c>BE68539007547034</c>, without spaces: JSON
/// is read by programs, which format the value for display themselves. Any string that
/// <see cref="BelgianIban.Parse(ReadOnlySpan{char})"/> accepts is read, for example
/// <c>BE68 5390 0754 7034</c>.
/// </para>
/// <para>
/// An invalid string throws a <see cref="JsonException"/> whose message is the localized message
/// of the <see cref="BelgianValidationError"/>, and whose inner exception is a
/// <see cref="BelgianFormatException"/> carrying it. A token that is not a string, <c>null</c>
/// included, throws a <see cref="JsonException"/> as well: declare the member as
/// <c>BelgianIban?</c> to accept <c>null</c>. The empty <c>default(BelgianIban)</c> cannot be
/// written.
/// </para>
/// <para>
/// The value can also be a dictionary key.
/// </para>
/// </remarks>
public sealed class BelgianIbanJsonConverter : JsonConverter<BelgianIban>
{
    private const string Format = "E";

    /// <inheritdoc/>
    public override BelgianIban Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        BelgianJson.Read<BelgianIban>(ref reader);

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, BelgianIban value, JsonSerializerOptions options) =>
        BelgianJson.Write(writer, value, Format);

    /// <inheritdoc/>
    public override BelgianIban ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        BelgianJson.ReadAsPropertyName<BelgianIban>(ref reader);

    /// <inheritdoc/>
    public override void WriteAsPropertyName(Utf8JsonWriter writer, BelgianIban value, JsonSerializerOptions options) =>
        BelgianJson.WriteAsPropertyName(writer, value, Format);
}
