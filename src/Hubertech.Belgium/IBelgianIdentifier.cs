namespace Hubertech.Belgium;

/// <summary>
/// The members shared by the identifier types, so that the integrations are written once.
/// </summary>
/// <remarks>
/// The static members are called through the type parameter, without boxing the identifier.
/// </remarks>
/// <typeparam name="TSelf">The identifier type.</typeparam>
internal interface IBelgianIdentifier<TSelf>
    where TSelf : struct, IBelgianIdentifier<TSelf>
{
    bool IsEmpty { get; }

    static abstract bool TryParse(ReadOnlySpan<char> s, out TSelf result, out BelgianValidationError error);

    bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format);
}
