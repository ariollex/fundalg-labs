using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class KaratsubaMultiplier : IMultiplier
{
    private BetterBigInteger KaratsubaMultiply(BetterBigInteger a, BetterBigInteger b)
    {
        if (a == BetterBigInteger.Zero || b == BetterBigInteger.Zero) return BetterBigInteger.Zero;
        
        var digitsA = a.GetDigits();
        var digitsB = b.GetDigits();

        var m = Math.Max(digitsA.Length, digitsB.Length) / 2;
        if (m == 0) return BetterBigInteger.SimpleMultiplier.Multiply(a, b);

        var a0 = new BetterBigInteger(digitsA[..Math.Min(m, digitsA.Length)].ToArray());
        var a1 = new BetterBigInteger(digitsA[Math.Min(m, digitsA.Length)..].ToArray());
        var b0 = new BetterBigInteger(digitsB[..Math.Min(m, digitsB.Length)].ToArray());
        var b1 = new BetterBigInteger(digitsB[Math.Min(m, digitsB.Length)..].ToArray());

        var m0 = KaratsubaMultiply(a0, b0);
        var m2 = KaratsubaMultiply(a1, b1);
        var m1 = KaratsubaMultiply(a0 + a1, b0 + b1) - m2 - m0;

        var shift = m * BetterBigInteger.CountBits;

        return (m2 << (2 * shift)) + (m1 << shift) + m0;
    }

    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b) 
    { 
        var absA = new BetterBigInteger(a.GetDigits().ToArray());
        var absB = new BetterBigInteger(b.GetDigits().ToArray());
        var result = KaratsubaMultiply(absA, absB);
        
        return a.IsNegative != b.IsNegative ? -result : result;
    }
}