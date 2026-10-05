using BenchmarkDotNet.Attributes;

namespace Hubertech.Belgium.Benchmarks;

/// <summary>
/// Holidays and business days. Counting is linear in the number of days: the ranges go from a
/// month to a century to show where that starts to matter.
/// </summary>
[MemoryDiagnoser]
public class CalendarBenchmarks
{
    private readonly DateOnly _start = new(2026, 1, 1);
    private readonly HolidaySet _set = HolidaySet.Legal | HolidaySet.FederalPublicService;

    [Benchmark]
    public IReadOnlyList<BelgianHoliday> GetHolidays() => BelgianCalendar.GetHolidays(_start.Year, _set);

    [Benchmark]
    public bool IsBusinessDay() => BelgianCalendar.IsBusinessDay(_start, _set);

    [Benchmark]
    public DateOnly AddTwentyBusinessDays() => BelgianCalendar.AddBusinessDays(_start, 20, _set);

    [Benchmark]
    [Arguments(30)]
    [Arguments(365)]
    [Arguments(3_650)]
    [Arguments(36_500)]
    public int CountBusinessDays(int calendarDays) => BelgianCalendar.CountBusinessDays(_start, _start.AddDays(calendarDays), _set);
}
