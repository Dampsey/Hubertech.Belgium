using System.Buffers;
using System.Text.Json;
using Hubertech.Belgium.Serialization;

namespace Hubertech.Belgium.Tests;

/// <summary>
/// Checks the promise that parsing and formatting do not allocate, whether the input is valid
/// or not. Only the <c>Message</c> and <c>Expected</c> of an error allocate, when they are read.
/// </summary>
public sealed class AllocationTests
{
    private const int Iterations = 100;

    [Fact]
    public void Enterprise_number_parsing_and_formatting_do_not_allocate()
    {
        Assert.Equal(0, AllocatedBytes(static () =>
        {
            Span<char> destination = stackalloc char[12];

            _ = EnterpriseNumber.TryParse("BE 0202.239.951".AsSpan(), out var number, out _);
            _ = EnterpriseNumber.TryParse("0202.239.952".AsSpan(), out _, out _);
            _ = number.TryFormat(destination, out _, "V");
        }));
    }

    [Fact]
    public void Structured_communication_parsing_and_formatting_do_not_allocate()
    {
        Assert.Equal(0, AllocatedBytes(static () =>
        {
            Span<char> destination = stackalloc char[20];

            _ = StructuredCommunication.TryParse("+++123/4567/89002+++".AsSpan(), out var reference, out _);
            _ = StructuredCommunication.TryParse("+++123/4567/89003+++".AsSpan(), out _, out _);
            _ = reference.TryFormat(destination, out _, "*");
            _ = StructuredCommunication.FromNumber(2_026_000_123).TryFormat(destination, out _);
        }));
    }

    [Fact]
    public void Belgian_iban_parsing_and_formatting_do_not_allocate()
    {
        Assert.Equal(0, AllocatedBytes(static () =>
        {
            Span<char> destination = stackalloc char[19];

            _ = BelgianIban.TryParse("BE68 5390 0754 7034".AsSpan(), out var iban, out _);
            _ = BelgianIban.TryParse("BE68539007547035".AsSpan(), out _, out _);
            _ = BelgianIban.TryParse("NL91ABNA0417164300".AsSpan(), out _, out _);
            _ = iban.TryFormat(destination, out _, "P");
            _ = BelgianIban.TryFromLegacyAccountNumber("539-0075470-34".AsSpan(), out _, out _);
            _ = BelgianIban.TryFromLegacyAccountNumber("539-0075470-35".AsSpan(), out _, out _);
        }));
    }

    [Fact]
    public void Social_security_identification_number_parsing_and_formatting_do_not_allocate()
    {
        Assert.Equal(0, AllocatedBytes(static () =>
        {
            Span<char> destination = stackalloc char[15];

            _ = SocialSecurityIdentificationNumber.TryParse("85.07.30-033.28".AsSpan(), out var number, out _);
            _ = SocialSecurityIdentificationNumber.TryParse("17.07.30-033.84".AsSpan(), out _, out _);
            _ = SocialSecurityIdentificationNumber.TryParse("85.07.30-033.29".AsSpan(), out _, out _);
            _ = number.TryFormat(destination, out _);
            _ = number.TryFormat(destination, out _, "D");
            _ = number.Kind;
            _ = number.BirthDate;
        }));
    }

    [Fact]
    public void Business_day_arithmetic_does_not_allocate()
    {
        Assert.Equal(0, AllocatedBytes(static () =>
        {
            var start = new DateOnly(2026, 4, 2);
            HolidaySet set = HolidaySet.Legal | HolidaySet.FederalPublicService;

            _ = BelgianCalendar.IsHoliday(start, set);
            _ = BelgianCalendar.IsBusinessDay(start, set);
            _ = BelgianCalendar.AddBusinessDays(start, 20, set);
            _ = BelgianCalendar.CountBusinessDays(start, new DateOnly(2026, 5, 29), set);
        }));
    }

    [Fact]
    public void Json_conversion_does_not_allocate()
    {
        var converter = new BelgianIbanJsonConverter();
        var output = new ArrayBufferWriter<byte>(64);
        using var writer = new Utf8JsonWriter(output);

        Assert.Equal(0, AllocatedBytes(() =>
        {
            var reader = new Utf8JsonReader("\"BE68 5390 0754 7034\""u8);
            reader.Read();
            var iban = converter.Read(ref reader, typeof(BelgianIban), JsonSerializerOptions.Default);

            output.ResetWrittenCount();
            writer.Reset();
            converter.Write(writer, iban, JsonSerializerOptions.Default);
            writer.Flush();
        }));
    }

    private static long AllocatedBytes(Action action)
    {
        // The first run pays for JIT compilation and static initialization.
        action();

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < Iterations; i++)
        {
            action();
        }

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
