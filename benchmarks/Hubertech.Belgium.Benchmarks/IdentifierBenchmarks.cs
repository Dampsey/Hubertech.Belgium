using BenchmarkDotNet.Attributes;

namespace Hubertech.Belgium.Benchmarks;

/// <summary>
/// Parsing a valid value, parsing an invalid one, and formatting, for each identifier.
/// </summary>
[MemoryDiagnoser]
public class IdentifierBenchmarks
{
    private readonly char[] _buffer = new char[32];
    private readonly string _enterpriseNumber = "BE 0202.239.951";
    private readonly string _invalidEnterpriseNumber = "0202.239.952";
    private readonly string _structuredCommunication = "+++123/4567/89002+++";
    private readonly string _iban = "BE68 5390 0754 7034";
    private readonly string _invalidIban = "BE68 5390 0754 7035";
    private readonly EnterpriseNumber _parsedEnterpriseNumber = EnterpriseNumber.Parse("0202239951");
    private readonly BelgianIban _parsedIban = BelgianIban.Parse("BE68539007547034");

    [Benchmark]
    public bool ParseEnterpriseNumber() => EnterpriseNumber.TryParse(_enterpriseNumber, out _, out _);

    [Benchmark]
    public bool ParseInvalidEnterpriseNumber() => EnterpriseNumber.TryParse(_invalidEnterpriseNumber, out _, out _);

    [Benchmark]
    public bool FormatEnterpriseNumber() => _parsedEnterpriseNumber.TryFormat(_buffer, out _);

    [Benchmark]
    public bool ParseStructuredCommunication() => StructuredCommunication.TryParse(_structuredCommunication, out _, out _);

    [Benchmark]
    public bool ParseIban() => BelgianIban.TryParse(_iban, out _, out _);

    [Benchmark]
    public bool ParseInvalidIban() => BelgianIban.TryParse(_invalidIban, out _, out _);

    [Benchmark]
    public bool FormatIban() => _parsedIban.TryFormat(_buffer, out _);

    [Benchmark]
    public string IbanToString() => _parsedIban.ToString();
}
