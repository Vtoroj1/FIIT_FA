using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class KaratsubaMultiplier : IMultiplier
{
    private const int threshold = 32;

    private readonly IMultiplier _simpleMultiplier = new SimpleMultiplier();

    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b) 
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        BetterBigInteger result = MultiplyRecursive(a, b);

        bool isNegative = a.IsNegative != b.IsNegative;

        return isNegative && !(result.GetDigits().Length == 1 && result.GetDigits()[0] == 0) ? -result : result;
    }

    private BetterBigInteger MultiplyRecursive(BetterBigInteger a, BetterBigInteger b)
    {
        ReadOnlySpan<uint> aDigits = a.GetDigits();
        ReadOnlySpan<uint> bDigits = b.GetDigits();

        if (aDigits.Length <= threshold || bDigits.Length <= threshold)
        {
            return _simpleMultiplier.Multiply(a, b);
        }

        int n = Math.Max(aDigits.Length, bDigits.Length);
        int m = n / 2;

        BetterBigInteger x0 = SliceToBigInt(aDigits, 0, m);
        BetterBigInteger x1 = SliceToBigInt(aDigits, m, aDigits.Length - m);

        BetterBigInteger y0 = SliceToBigInt(bDigits, 0, m);
        BetterBigInteger y1 = SliceToBigInt(bDigits, m , bDigits.Length - m);

        BetterBigInteger z0 = MultiplyRecursive(x0, y0);
        BetterBigInteger z2 = MultiplyRecursive(x1, y1);

        BetterBigInteger z1 = MultiplyRecursive(x0 + x1, y0 + y1) - z0 - z2;

        int shift = m * sizeof(uint) * 8;
        
        return (z2 << (shift * 2)) + (z1 << shift) + z0;
    }

    private static BetterBigInteger SliceToBigInt(ReadOnlySpan<uint> span, int start, int length)
    {
        if (start >= span.Length)
        {
            return new BetterBigInteger([0]);
        }

        int actualLen = Math.Min(length, span.Length - start);

        if (actualLen <= 0)
        {
            return new BetterBigInteger([0]);
        }

        return new BetterBigInteger(span.Slice(start, actualLen).ToArray(), false);
    }
}