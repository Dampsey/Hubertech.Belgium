namespace Hubertech.Belgium;

/// <summary>
/// The BIC assigned to each Belgian bank code, from the list of the National Bank of Belgium
/// generated in <c>BankCodes.Generated.cs</c> by <c>tools/UpdateBankCodes.cs</c>.
/// </summary>
internal static partial class BankCodes
{
    /// <summary>
    /// Finds the BIC assigned to a bank code.
    /// </summary>
    /// <param name="bankCode">The bank code, from 0 to 999.</param>
    /// <returns>The BIC, or <see langword="null"/> when the list gives none for this code.</returns>
    internal static string? FindBic(int bankCode)
    {
        int low = 0;
        int high = Ranges.Length - 1;
        while (low <= high)
        {
            int middle = low + ((high - low) / 2);
            BankCodeRange range = Ranges[middle];
            if (bankCode < range.First)
            {
                high = middle - 1;
            }
            else if (bankCode > range.Last)
            {
                low = middle + 1;
            }
            else
            {
                return range.Bic;
            }
        }

        return null;
    }

    /// <summary>
    /// The bank codes from <paramref name="First"/> to <paramref name="Last"/>, all assigned to
    /// <paramref name="Bic"/>.
    /// </summary>
    internal readonly record struct BankCodeRange(int First, int Last, string Bic);
}
