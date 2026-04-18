using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier
{
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        var zero = new BetterBigInteger([0]);
        if (a == zero || b == zero) return zero;

        var first = a.IsNegative ? -a : a;
        var second = b.IsNegative ? -b : b;

        var result = new BetterBigInteger([0]);

        var digitsA = first.GetDigits();
        var digitsB = second.GetDigits();

        const int halfBits = sizeof(uint) * 8 / 2;
        const uint halfBase = 1 << halfBits;

        for (var j = 0; j < digitsB.Length; ++j)
        {
            for (var i = 0; i < digitsA.Length; ++i)
            {
                var digitA = digitsA[i];
                var digitB = digitsB[j];
                
                var rightA = digitA % halfBase;
                var leftA = digitA / halfBase;

                var rightB = digitB % halfBase;
                var leftB = digitB / halfBase;

                var rr = new BetterBigInteger([rightA * rightB]);
                var lr = new BetterBigInteger([leftA * rightB]) << halfBits;
                var rl = new BetterBigInteger([rightA * leftB]) << halfBits;
                var ll = new BetterBigInteger([leftA * leftB]) << (halfBits + halfBits);

                var digitProduct = rr + lr + rl + ll;

                var fullShift = (i + j) * sizeof(uint) * 8;
                digitProduct <<= fullShift;

                result += digitProduct;
            }
        }

        return a.IsNegative == b.IsNegative ? result : -result;
    }
}