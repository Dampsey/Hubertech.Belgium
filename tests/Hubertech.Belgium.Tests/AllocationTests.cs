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
