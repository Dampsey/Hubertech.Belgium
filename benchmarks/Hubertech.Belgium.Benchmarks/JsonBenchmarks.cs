using System.Text.Json;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Hubertech.Belgium.Benchmarks;

/// <summary>
/// The cost of typed identifiers in JSON, compared with the same payment holding plain strings,
/// which nothing validates.
/// </summary>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class JsonBenchmarks
{
    private readonly byte[] _json = """{"Payee":"BE 0202.239.951","Reference":"+++123/4567/89002+++","Account":"BE68 5390 0754 7034"}"""u8.ToArray();
    private readonly Payment _payment = JsonSerializer.Deserialize(
        """{"Payee":"0202239951","Reference":"123456789002","Account":"BE68539007547034"}""", BenchmarkJsonContext.Default.Payment)!;

    private readonly TextPayment _textPayment = new("0202239951", "123456789002", "BE68539007547034");

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Read")]
    public TextPayment? ReadStrings() => JsonSerializer.Deserialize(_json, BenchmarkJsonContext.Default.TextPayment);

    [Benchmark]
    [BenchmarkCategory("Read")]
    public Payment? ReadIdentifiers() => JsonSerializer.Deserialize(_json, BenchmarkJsonContext.Default.Payment);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Write")]
    public byte[] WriteStrings() => JsonSerializer.SerializeToUtf8Bytes(_textPayment, BenchmarkJsonContext.Default.TextPayment);

    [Benchmark]
    [BenchmarkCategory("Write")]
    public byte[] WriteIdentifiers() => JsonSerializer.SerializeToUtf8Bytes(_payment, BenchmarkJsonContext.Default.Payment);
}

public sealed record Payment(EnterpriseNumber Payee, StructuredCommunication Reference, BelgianIban Account);

public sealed record TextPayment(string Payee, string Reference, string Account);

[JsonSerializable(typeof(Payment))]
[JsonSerializable(typeof(TextPayment))]
internal sealed partial class BenchmarkJsonContext : JsonSerializerContext;
