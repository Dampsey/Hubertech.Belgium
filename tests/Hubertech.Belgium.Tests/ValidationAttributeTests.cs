using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Hubertech.Belgium.Tests;

public sealed class ValidationAttributeTests
{
    private const string MemberName = "Value";

    [Theory]
    [InlineData(nameof(EnterpriseNumber), "BE 0202.239.951")]
    [InlineData(nameof(StructuredCommunication), "+++123/4567/89002+++")]
    [InlineData(nameof(BelgianIban), "BE68 5390 0754 7034")]
    [InlineData(nameof(SocialSecurityIdentificationNumber), "85.07.30-033.28")]
    public void Accepts_a_valid_string(string type, string value)
    {
        var attribute = Attribute(type);

        Assert.True(attribute.IsValid(value));
        Assert.Equal(ValidationResult.Success, attribute.GetValidationResult(value, Context()));
    }

    [Theory]
    [InlineData(nameof(EnterpriseNumber), "0202.239.952")]
    [InlineData(nameof(EnterpriseNumber), "2202.239.951")]
    [InlineData(nameof(StructuredCommunication), "+++123/4567/89003+++")]
    [InlineData(nameof(BelgianIban), "NL91 ABNA 0417 1643 00")]
    [InlineData(nameof(BelgianIban), "BE68 5390 0754 703X")]
    [InlineData(nameof(BelgianIban), "   ")]
    [InlineData(nameof(SocialSecurityIdentificationNumber), "85.07.30-033.29")]
    [InlineData(nameof(SocialSecurityIdentificationNumber), "85.13.30-033.70")]
    public void Rejects_an_invalid_string_with_the_message_of_the_validation_error(string type, string value)
    {
        var attribute = Attribute(type);

        var result = attribute.GetValidationResult(value, Context());

        Assert.False(attribute.IsValid(value));
        Assert.NotNull(result);
        Assert.Equal(ValidationError(type, value).Message, result.ErrorMessage);
        Assert.Equal([MemberName], result.MemberNames);
    }

    [Theory]
    [InlineData(nameof(EnterpriseNumber))]
    [InlineData(nameof(StructuredCommunication))]
    [InlineData(nameof(BelgianIban))]
    [InlineData(nameof(SocialSecurityIdentificationNumber))]
    public void Leaves_a_missing_value_to_the_required_attribute(string type)
    {
        var attribute = Attribute(type);

        Assert.Equal(ValidationResult.Success, attribute.GetValidationResult(null, Context()));
        Assert.Equal(ValidationResult.Success, attribute.GetValidationResult(string.Empty, Context()));
    }

    [Fact]
    public void Accepts_an_identifier_unless_it_is_empty()
    {
        var attribute = new BelgianIbanAttribute();

        var result = attribute.GetValidationResult(default(BelgianIban), Context());

        Assert.True(attribute.IsValid(BelgianIban.Parse("BE68539007547034")));
        Assert.False(attribute.IsValid(default(BelgianIban)));
        Assert.NotNull(result);
        Assert.Equal(BelgianIban.Validate(string.Empty)!.Value.Message, result.ErrorMessage);
    }

    [Fact]
    public void Refuses_to_validate_a_member_of_another_type()
    {
        var attribute = new BelgianStructuredCommunicationAttribute();

        var exception = Assert.Throws<InvalidOperationException>(() => attribute.GetValidationResult(123_456_789_002, Context()));

        Assert.Contains(nameof(BelgianStructuredCommunicationAttribute), exception.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(long).FullName!, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_message_set_on_the_attribute_replaces_the_message_of_the_validation_error()
    {
        var attribute = new BelgianEnterpriseNumberAttribute { ErrorMessage = "{0} is not an enterprise number." };

        var result = attribute.GetValidationResult("0202.239.952", Context());

        Assert.NotNull(result);
        Assert.Equal("Supplier number is not an enterprise number.", result.ErrorMessage);
    }

    [Fact]
    public void Message_follows_the_current_UI_culture()
    {
        var attribute = new BelgianEnterpriseNumberAttribute();
        var originalCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-BE");

            var result = attribute.GetValidationResult("2202.239.951", Context());

            Assert.NotNull(result);
            Assert.Equal("Le numéro d'entreprise doit commencer par 0 ou 1.", result.ErrorMessage);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void Validator_reports_each_invalid_member()
    {
        var form = new SupplierForm
        {
            EnterpriseNumber = null,
            Reference = "+++123/4567/89003+++",
            Account = "BE68 5390 0754 7034",
        };
        var results = new List<ValidationResult>();

        bool valid = Validator.TryValidateObject(form, new ValidationContext(form), results, validateAllProperties: true);

        Assert.False(valid);
        Assert.Equal(
            [
                (nameof(SupplierForm.EnterpriseNumber), "The EnterpriseNumber field is required."),
                (nameof(SupplierForm.Reference), StructuredCommunication.Validate(form.Reference)!.Value.Message),
            ],
            results.Select(result => (result.MemberNames.Single(), result.ErrorMessage)));
    }

    [Fact]
    public void Validator_reports_an_identifier_left_unset()
    {
        var payment = new BoundPayment();
        var results = new List<ValidationResult>();

        bool valid = Validator.TryValidateObject(payment, new ValidationContext(payment), results, validateAllProperties: true);

        Assert.False(valid);
        Assert.Equal(nameof(BoundPayment.Account), Assert.Single(results).MemberNames.Single());
    }

    private static ValidationContext Context() => new(new object())
    {
        MemberName = MemberName,
        DisplayName = "Supplier number",
    };

    private static ValidationAttribute Attribute(string type) => type switch
    {
        nameof(EnterpriseNumber) => new BelgianEnterpriseNumberAttribute(),
        nameof(StructuredCommunication) => new BelgianStructuredCommunicationAttribute(),
        nameof(SocialSecurityIdentificationNumber) => new BelgianSocialSecurityIdentificationNumberAttribute(),
        _ => new BelgianIbanAttribute(),
    };

    private static BelgianValidationError ValidationError(string type, string value) => type switch
    {
        nameof(EnterpriseNumber) => EnterpriseNumber.Validate(value),
        nameof(StructuredCommunication) => StructuredCommunication.Validate(value),
        nameof(SocialSecurityIdentificationNumber) => SocialSecurityIdentificationNumber.Validate(value),
        _ => BelgianIban.Validate(value),
    } ?? throw new ArgumentException("The value is valid.", nameof(value));

    private sealed class SupplierForm
    {
        [Required]
        [BelgianEnterpriseNumber]
        public string? EnterpriseNumber { get; init; }

        [BelgianStructuredCommunication]
        public string? Reference { get; init; }

        [BelgianIban]
        public string? Account { get; init; }
    }

    private sealed class BoundPayment
    {
        [BelgianIban]
        public BelgianIban Account { get; init; }

        [BelgianStructuredCommunication]
        public StructuredCommunication? Reference { get; init; }
    }
}
