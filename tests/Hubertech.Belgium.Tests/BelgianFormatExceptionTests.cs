using System.Globalization;

namespace Hubertech.Belgium.Tests;

public sealed class BelgianFormatExceptionTests
{
    [Fact]
    public void Exception_is_a_format_exception_carrying_the_error()
    {
        var error = BelgianValidationError.InvalidFirstDigit("EnterpriseNumber");

        var exception = new BelgianFormatException(error);

        Assert.IsAssignableFrom<FormatException>(exception);
        Assert.Equal(error, exception.Error);
    }

    [Fact]
    public void Exception_message_is_the_error_message_in_the_current_UI_culture()
    {
        var error = BelgianValidationError.InvalidCountryCode("BelgianIban");
        var originalCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-BE");

            var exception = new BelgianFormatException(error);

            Assert.Equal("Seuls les IBAN belges sont acceptés (commençant par BE).", exception.Message);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void Exception_cannot_be_created_without_an_error()
    {
        Assert.Throws<ArgumentException>(() => new BelgianFormatException(default));
    }
}
