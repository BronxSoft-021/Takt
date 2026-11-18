namespace Takt.Merenje;

/// <summary>
/// Compares names the way a person counts: "jezgro 2" before "jezgro 10". Plain text order would put
/// 1, 10, 11, 12 before 2, and a list of twelve cores would read like a shuffled deck.
/// </summary>
public sealed class Prirodno : IComparer<string>
{
    public static readonly Prirodno Poredak = new();

    public int Compare(string? a, string? b)
    {
        if (ReferenceEquals(a, b)) return 0;
        if (a is null) return -1;
        if (b is null) return 1;

        int i = 0, j = 0;
        while (i < a.Length && j < b.Length)
        {
            if (char.IsAsciiDigit(a[i]) && char.IsAsciiDigit(b[j]))
            {
                int pocetakA = i, pocetakB = j;
                while (i < a.Length && char.IsAsciiDigit(a[i])) i++;
                while (j < b.Length && char.IsAsciiDigit(b[j])) j++;

                var brojA = a.AsSpan(pocetakA, i - pocetakA).TrimStart('0');
                var brojB = b.AsSpan(pocetakB, j - pocetakB).TrimStart('0');

                // A longer number (without leading zeros) is the larger one; same length compares digit by digit.
                if (brojA.Length != brojB.Length) return brojA.Length.CompareTo(brojB.Length);
                int cifre = brojA.SequenceCompareTo(brojB);
                if (cifre != 0) return cifre;
                continue;
            }

            int znak = string.Compare(a, i, b, j, 1, StringComparison.Ordinal);
            if (znak != 0) return znak;
            i++;
            j++;
        }

        return (a.Length - i).CompareTo(b.Length - j);
    }
}
