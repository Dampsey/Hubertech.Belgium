using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CsCheck;
using Hubertech.Belgium.Serialization;

namespace Hubertech.Belgium.Tests;

public sealed class JsonSerializationTests
{
    private static readonly Payment Sample = new(
        EnterpriseNumber.Parse("0202.239.951"),
        StructuredCommunication.Parse("+++123/4567/89002+++"),
        BelgianIban.Parse("BE68 5390 0754 7034"));

    private static readonly Gen<Payment> ValidPayment = Gen.Select(
        Gen.UInt[0, 19_999_999].Select(baseNumber =>
            EnterpriseNumber.Parse(string.Create(CultureInfo.InvariantCulture, $"{baseNumber:D8}{97 - (baseNumber % 97):D2}"))),
        Gen.ULong[0, StructuredCommunication.MaxBaseNumber].Select(StructuredCommunication.FromNumber),
        Gen.ULong[1, 9_999_999_999].Select(accountBase =>
        {
            ulong remainder = accountBase % 97;
            ulong account = (accountBase * 100) + (remainder == 0 ? 97 : remainder);

            return BelgianIban.FromLegacyAccountNumber(string.Create(CultureInfo.InvariantCulture, $"{account:D12}"));
        }),
        (payee, reference, account) => new Payment(payee, reference, account));

    private static readonly JsonSerializerOptions ReflectionIgnoringDefaults = new(JsonSerializerOptions.Default)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    };

    private static readonly JsonSerializerOptions SourceGenerationIgnoringDefaults = new(TestJsonContext.Default.Options)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    };

    public enum Serializer
    {
        Reflection,
        SourceGeneration,
    }

    [Theory]
    [InlineData(Serializer.Reflection)]
    [InlineData(Serializer.SourceGeneration)]
    public void Writes_digits_only_and_the_iban_in_its_electronic_format(Serializer serializer)
    {
        string json = JsonSerializer.Serialize(Sample, Options(serializer));

        Assert.Equal("""{"Payee":"0202239951","Reference":"123456789002","Account":"BE68539007547034"}""", json);
    }

    [Theory]
    [InlineData(Serializer.Reflection)]
    [InlineData(Serializer.SourceGeneration)]
    public void Reads_any_form_the_parsers_accept(Serializer serializer)
    {
        const string Json = """{"Payee":"be 0202.239.951","Reference":"+++123/4567/89002+++","Account":"be68 5390 0754 7034"}""";

        Assert.Equal(Sample, JsonSerializer.Deserialize<Payment>(Json, Options(serializer)));
    }

    [Theory]
    [InlineData(Serializer.Reflection)]
    [InlineData(Serializer.SourceGeneration)]
    public void Reading_what_was_written_gives_back_the_same_values(Serializer serializer)
    {
        var options = Options(serializer);

        ValidPayment.Sample(payment =>
            Assert.Equal(payment, JsonSerializer.Deserialize<Payment>(JsonSerializer.Serialize(payment, options), options)));
    }

    [Fact]
    public void Reads_escaped_strings()
    {
        Assert.Equal(Sample.Payee, JsonSerializer.Deserialize<EnterpriseNumber>("\"\\u0030202\\u002e239.951\""));
    }

    [Fact]
    public void Reads_strings_longer_than_any_formatted_value()
    {
        string json = $"\"BE{new string(' ', 100)}0202.239.951\"";

        Assert.Equal(Sample.Payee, JsonSerializer.Deserialize<EnterpriseNumber>(json));
    }

    [Fact]
    public void Reads_long_strings_split_across_buffers()
    {
        var converter = new BelgianIbanJsonConverter();
        byte[] start = Encoding.UTF8.GetBytes($"\"BE68 5390 {new string(' ', 100)}");
        var reader = new Utf8JsonReader(Split(start, "0754 7034\""u8.ToArray()));
        reader.Read();

        Assert.True(reader.HasValueSequence);
        Assert.Equal(Sample.Account, converter.Read(ref reader, typeof(BelgianIban), JsonSerializerOptions.Default));
    }

    [Theory]
    [InlineData(Serializer.Reflection)]
    [InlineData(Serializer.SourceGeneration)]
    public void Rejects_an_invalid_value_with_the_validation_error(Serializer serializer)
    {
        const string Json = """{"Payee":"0202.239.951","Reference":"+++123/4567/89003+++","Account":"BE68539007547034"}""";

        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Payment>(Json, Options(serializer)));

        var inner = Assert.IsType<BelgianFormatException>(exception.InnerException);
        Assert.Equal(BelgianErrorCode.InvalidChecksum, inner.Error.Code);
        Assert.Equal(nameof(StructuredCommunication), inner.Error.TypeName);
        Assert.Equal(inner.Error.Message, exception.Message);
        Assert.Equal("$.Reference", exception.Path);
    }

    [Fact]
    public void Rejection_message_follows_the_current_UI_culture()
    {
        var originalCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("nl-BE");

            var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<BelgianIban>("\"NL91ABNA0417164300\""));

            Assert.Equal("Alleen Belgische IBAN-nummers worden aanvaard (beginnend met BE).", exception.Message);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Theory]
    [InlineData("null")]
    [InlineData("202239951")]
    [InlineData("true")]
    [InlineData("{}")]
    [InlineData("[]")]
    public void Rejects_a_token_that_is_not_a_string(string json)
    {
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<EnterpriseNumber>(json));

        Assert.Null(exception.InnerException);
        Assert.Contains(typeof(EnterpriseNumber).FullName!, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(Serializer.Reflection)]
    [InlineData(Serializer.SourceGeneration)]
    public void Nullable_members_read_and_write_null(Serializer serializer)
    {
        const string Json = """{"Payee":null,"Reference":null,"Account":null}""";
        var options = Options(serializer);

        Assert.Equal(new OptionalPayment(null, null, null), JsonSerializer.Deserialize<OptionalPayment>(Json, options));
        Assert.Equal(Json, JsonSerializer.Serialize(new OptionalPayment(null, null, null), options));
    }

    [Theory]
    [InlineData(Serializer.Reflection)]
    [InlineData(Serializer.SourceGeneration)]
    public void Refuses_to_write_an_empty_value(Serializer serializer)
    {
        var payment = Sample with { Account = default };

        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Serialize(payment, Options(serializer)));

        Assert.Contains("BelgianIban?", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(Serializer.Reflection)]
    [InlineData(Serializer.SourceGeneration)]
    public void Empty_values_can_be_left_out_by_ignoring_default_values(Serializer serializer)
    {
        var options = serializer == Serializer.Reflection ? ReflectionIgnoringDefaults : SourceGenerationIgnoringDefaults;

        Assert.Equal("{}", JsonSerializer.Serialize(new Payment(default, default, default), options));
    }

    [Theory]
    [InlineData(Serializer.Reflection)]
    [InlineData(Serializer.SourceGeneration)]
    public void Identifiers_can_be_dictionary_keys(Serializer serializer)
    {
        var options = Options(serializer);
        var totals = new Dictionary<EnterpriseNumber, decimal> { [Sample.Payee] = 12.5m };
        var references = new Dictionary<StructuredCommunication, decimal> { [Sample.Reference] = 12.5m };
        var balances = new Dictionary<BelgianIban, decimal> { [Sample.Account] = 12.5m };

        string totalsJson = JsonSerializer.Serialize(totals, options);
        string referencesJson = JsonSerializer.Serialize(references, options);
        string balancesJson = JsonSerializer.Serialize(balances, options);

        Assert.Equal("""{"0202239951":12.5}""", totalsJson);
        Assert.Equal("""{"123456789002":12.5}""", referencesJson);
        Assert.Equal("""{"BE68539007547034":12.5}""", balancesJson);
        Assert.Equal(totals, JsonSerializer.Deserialize<Dictionary<EnterpriseNumber, decimal>>(totalsJson, options));
        Assert.Equal(references, JsonSerializer.Deserialize<Dictionary<StructuredCommunication, decimal>>(referencesJson, options));
        Assert.Equal(balances, JsonSerializer.Deserialize<Dictionary<BelgianIban, decimal>>(balancesJson, options));
    }

    [Theory]
    [InlineData(Serializer.Reflection)]
    [InlineData(Serializer.SourceGeneration)]
    public void Writes_the_full_social_security_identification_number_although_ToString_masks_it(Serializer serializer)
    {
        var options = Options(serializer);
        var employee = new Employee(SocialSecurityIdentificationNumber.Parse("85.07.30-033.28"));

        string json = JsonSerializer.Serialize(employee, options);

        Assert.Equal("""{"Number":"85073003328"}""", json);
        Assert.Equal(employee, JsonSerializer.Deserialize<Employee>("""{"Number":"85.07.30-033.28"}""", options));
        Assert.Equal("**.**.**-***.28", employee.Number.ToString());
    }

    [Fact]
    public void Rejects_an_invalid_social_security_identification_number_with_the_validation_error()
    {
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Employee>("""{"Number":"85.13.30-033.70"}"""));

        var inner = Assert.IsType<BelgianFormatException>(exception.InnerException);
        Assert.Equal(BelgianErrorCode.InvalidBirthDate, inner.Error.Code);
        Assert.Equal("$.Number", exception.Path);
    }

    [Fact]
    public void Rejects_an_invalid_dictionary_key_with_the_validation_error()
    {
        var exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<Dictionary<EnterpriseNumber, decimal>>("""{"0202.239.952":1}"""));

        var inner = Assert.IsType<BelgianFormatException>(exception.InnerException);
        Assert.Equal(BelgianErrorCode.InvalidChecksum, inner.Error.Code);
    }

    private static JsonSerializerOptions Options(Serializer serializer) => serializer switch
    {
        Serializer.Reflection => JsonSerializerOptions.Default,
        _ => TestJsonContext.Default.Options,
    };

    private static ReadOnlySequence<byte> Split(byte[] first, byte[] second)
    {
        var start = new Segment(first, null);
        var end = new Segment(second, start);

        return new ReadOnlySequence<byte>(start, 0, end, second.Length);
    }

    private sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory, Segment? previous)
        {
            Memory = memory;
            if (previous is not null)
            {
                RunningIndex = previous.RunningIndex + previous.Memory.Length;
                previous.Next = this;
            }
        }
    }
}

public sealed record Payment(EnterpriseNumber Payee, StructuredCommunication Reference, BelgianIban Account);

public sealed record OptionalPayment(EnterpriseNumber? Payee, StructuredCommunication? Reference, BelgianIban? Account);

public sealed record Employee(SocialSecurityIdentificationNumber Number);

[JsonSerializable(typeof(Payment))]
[JsonSerializable(typeof(OptionalPayment))]
[JsonSerializable(typeof(Employee))]
[JsonSerializable(typeof(Dictionary<EnterpriseNumber, decimal>))]
[JsonSerializable(typeof(Dictionary<StructuredCommunication, decimal>))]
[JsonSerializable(typeof(Dictionary<BelgianIban, decimal>))]
internal sealed partial class TestJsonContext : JsonSerializerContext;
