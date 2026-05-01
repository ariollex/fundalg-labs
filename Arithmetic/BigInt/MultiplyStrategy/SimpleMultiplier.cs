using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier
{
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        if (a == BetterBigInteger.Zero || b == BetterBigInteger.Zero) return BetterBigInteger.Zero;

        var result = new BetterBigInteger([]);

        var digitsA = a.GetDigits();
        var digitsB = b.GetDigits();

        const int halfBits = BetterBigInteger.CountBits / 2;
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