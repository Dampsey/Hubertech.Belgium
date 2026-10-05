using System.Diagnostics;
using System.Text.Json;

namespace Hubertech.Belgium.Serialization;

/// <summary>
/// The implementation shared by the JSON converters of the identifier types.
/// </summary>
internal static class BelgianJson
{
    // Longer than any value worth parsing, separators included. A longer string is read on the
    // heap rather than on the stack, and fails to parse anyway unless it is mostly white space.
    private const int StackBufferLength = 64;

    internal static T Read<T>(ref Utf8JsonReader reader)
        where T : struct, IBelgianIdentifier<T>
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            // Without a message, the serializer explains that the value cannot be converted to T
            // and where it is in the document.
            throw new JsonException();
        }

        return Parse<T>(ref reader);
    }

    internal static T ReadAsPropertyName<T>(ref Utf8JsonReader reader)
        where T : struct, IBelgianIdentifier<T> =>
        Parse<T>(ref reader);

    internal static void Write<T>(Utf8JsonWriter writer, T value, string format)
        where T : struct, IBelgianIdentifier<T>
    {
        Span<char> buffer = stackalloc char[StackBufferLength];
        writer.WriteStringValue(Format(value, format, buffer));
    }

    internal static void WriteAsPropertyName<T>(Utf8JsonWriter writer, T value, string format)
        where T : struct, IBelgianIdentifier<T>
    {
        Span<char> buffer = stackalloc char[StackBufferLength];
        writer.WritePropertyName(Format(value, format, buffer));
    }

    private static T Parse<T>(ref Utf8JsonReader reader)
        where T : struct, IBelgianIdentifier<T>
    {
        // The value is in UTF-8, possibly escaped: it never decodes to more characters than it has bytes.
        long length = reader.HasValueSequence ? reader.ValueSequence.Length : reader.ValueSpan.Length;
        Span<char> buffer = length <= StackBufferLength ? stackalloc char[StackBufferLength] : new char[length];
        int charsWritten = reader.CopyString(buffer);

        return T.TryParse(buffer[..charsWritten], out var result, out var error)
            ? result
            : throw new JsonException(error.Message, new BelgianFormatException(error));
    }

    private static ReadOnlySpan<char> Format<T>(T value, string format, Span<char> buffer)
        where T : struct, IBelgianIdentifier<T>
    {
        if (value.IsEmpty)
        {
            throw new JsonException(
                $"An empty {typeof(T).Name} cannot be written: the default value is not a valid {typeof(T).Name}. " +
                $"Declare the member as {typeof(T).Name}? to represent a missing value.");
        }

        bool formatted = value.TryFormat(buffer, out int charsWritten, format);
        Debug.Assert(formatted, "The buffer fits every format.");

        return buffer[..charsWritten];
    }
}
