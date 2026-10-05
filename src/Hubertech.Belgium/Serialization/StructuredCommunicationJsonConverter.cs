using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hubertech.Belgium.Serialization;

/// <summary>
/// Converts a structured communication to and from a JSON string.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="StructuredCommunication"/> is annotated with this converter: there is nothing to register,
/// with or without the System.Text.Json source generator.
/// </para>
/// <para>
/// The value is written as <c>123456789002</c>, digits only: JSON is read by programs, which
/// format the value for display themselves. Any string that
/// <see cref="StructuredCommunication.Parse(ReadOnlySpan{char})"/> accepts is read, for example
/// <c>+++123/4567/89002+++</c>.
/// </para>
/// <para>
/// An invalid string throws a <see cref="JsonException"/> whose message is the localized message
/// of the <see cref="BelgianValidationError"/>, and whose inner exception is a
/// <see cref="BelgianFormatException"/> carrying it. A token that is not a string, <c>null</c>
/// included, throws a <see cref="JsonException"/> as well: declare the member as
/// <c>StructuredCommunication?</c> to accept <c>null</c>. The empty
/// <c>default(StructuredCommunication)</c> cannot be written.
/// </para>
/// <para>
/// The value can also be a dictionary key.
/// </para>
/// </remarks>
public sealed class StructuredCommunicationJsonConverter : JsonConverter<StructuredCommunication>
{
    private const string Format = "N";

    /// <inheritdoc/>
    public override StructuredCommunication Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        BelgianJson.Read<StructuredCommunication>(ref reader);

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, StructuredCommunication value, JsonSerializerOptions options) =>
        BelgianJson.Write(writer, value, Format);

    /// <inheritdoc/>
    public override StructuredCommunication ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        BelgianJson.ReadAsPropertyName<StructuredCommunication>(ref reader);

    /// <inheritdoc/>
    public override void WriteAsPropertyName(Utf8JsonWriter writer, StructuredCommunication value, JsonSerializerOptions options) =>
        BelgianJson.WriteAsPropertyName(writer, value, Format);
}
