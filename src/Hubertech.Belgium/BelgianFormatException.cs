namespace Hubertech.Belgium;

/// <summary>
/// The exception thrown by the <c>Parse</c> methods when the input is not valid.
/// </summary>
/// <remarks>
/// It derives from <see cref="FormatException"/>, so existing <c>catch (FormatException)</c>
/// blocks keep working. <see cref="Error"/> gives the stable error code and the details;
/// <see cref="Exception.Message"/> is the localized message of that error, in the UI culture
/// current when the exception was created.
/// </remarks>
public sealed class BelgianFormatException : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BelgianFormatException"/> class.
    /// </summary>
    /// <param name="error">The validation error that caused the exception.</param>
    /// <exception cref="ArgumentException"><paramref name="error"/> does not describe an error.</exception>
    public BelgianFormatException(BelgianValidationError error)
        : base(error.Message)
    {
        if (error.Code == BelgianErrorCode.None)
        {
            throw new ArgumentException("The error must describe a failure.", nameof(error));
        }

        Error = error;
    }

    /// <summary>
    /// Gets the validation error that caused the exception.
    /// </summary>
    public BelgianValidationError Error { get; }
}
