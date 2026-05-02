using System.Text;
using Arithmetic.BigInt.Interfaces;
using Arithmetic.BigInt.MultiplyStrategy;

namespace Arithmetic.BigInt;

public sealed class BetterBigInteger : IBigInteger
{
    private readonly int _signBit;

    private readonly uint _smallValue; // Если число маленькое, храним его прямо в этом поле, а _data == null.
    private readonly uint[]? _data;

    public bool IsNegative => _signBit == 1;
    
    internal static readonly BetterBigInteger Zero = new([]);
    internal const int CountBits = sizeof(uint) * 8;
    internal static readonly SimpleMultiplier SimpleMultiplier = new();
    
    /// От массива цифр (little endian)
    public BetterBigInteger(uint[] digits, bool isNegative = false)
    {
        ArgumentNullException.ThrowIfNull(digits);
        var len = digits.Length;
        while (len > 0 && digits[len - 1] == 0) --len;
        
        if (len < 2)
        {
            _smallValue = (len == 0 ? 0 : digits[0]);
            _data = null;
        }
        else
        {
            _data = new uint[len];
            Array.Copy(digits, _data, len);
        }
        if (len != 0) _signBit = isNegative ? 1 : 0;
    }
    public BetterBigInteger(IEnumerable<uint> digits, bool isNegative = false): this(digits.ToArray(), isNegative) { }

    public BetterBigInteger(ReadOnlySpan<uint> digits, bool isNegative = false)
    {
        var len = digits.Length;
        while (len > 0 && digits[len - 1] == 0) --len;
        
        if (len < 2)
        {
            _smallValue = (len == 0 ? 0 : digits[0]);
            _data = null;
        }
        else
        {
            _data = digits[..len].ToArray();
        }
        if (len != 0) _signBit = isNegative ? 1 : 0;
    }
    
    public BetterBigInteger(string value, int radix)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        
        if (radix is < 2 or > 36) throw new ArgumentOutOfRangeException(nameof(radix), "radix must be between 2 and 36");
        
        var isNegative = value[0] == '-';
        var start = (value[0] == '-' || value[0] == '+') ? 1 : 0;
        
        if (start == value.Length) throw new ArgumentException("Wrong value");
        
        var current = Zero;
        var bbiRadix = new BetterBigInteger([(uint)radix]);
        
        for (var i = start; i < value.Length; ++i)
        {
            var digit = (value[i] is >= '0' and <= '9') ? value[i] - '0' : value[i] - 'A' + 10;
            if (digit < 0 || digit >= radix) throw new ArgumentException("Wrong value");
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(digit, radix);
            current = current * bbiRadix + new BetterBigInteger([(uint)digit]);
        }
        
        _smallValue = current._smallValue;
        _data = current._data;
        
        _signBit = (current != Zero && isNegative) ? 1 : 0;
    }
    
    public ReadOnlySpan<uint> GetDigits()
    {
        return _data ?? [_smallValue];
    }
    public int CompareTo(IBigInteger? other)
    {
        if (other is null) return 1;
        
        var a = GetDigits();
        var b = other.GetDigits();
        
        var isZeroA = a.Length == 1 && a[0] == 0;
        var isZeroB = b.Length == 1 && b[0] == 0;
        
        var negativeA = !isZeroA && IsNegative;
        var negativeB = !isZeroB && other.IsNegative;
        
        if (negativeA != negativeB) return negativeA ? -1 : 1;
        
        if (a.Length != b.Length) return negativeA ? (a.Length < b.Length ? 1 : -1) : (a.Length < b.Length ? -1 : 1);
        
        for (var i = a.Length - 1; i >= 0; --i)
        {
            if (a[i] != b[i]) return negativeA ? (a[i] < b[i] ? 1 : -1) : (a[i] < b[i] ? -1 : 1);
        }
        
        return 0;
    }
    
    public bool Equals(IBigInteger? other) => CompareTo(other) == 0;
    public override bool Equals(object? obj) => obj is IBigInteger other && Equals(other);
    public override int GetHashCode()
    {
        var hash = new HashCode();
        
        hash.Add(_signBit);
        foreach (var item in GetDigits()) hash.Add(item);
        
        return hash.ToHashCode();
    }


    public static BetterBigInteger operator +(BetterBigInteger a, BetterBigInteger b)
    {
        if (a == Zero) return b;
        if (b == Zero) return a;
        
        if (a.IsNegative != b.IsNegative) return a - -b;
        
        var digitsA = a.GetDigits();
        var digitsB = b.GetDigits();
        
        var n = Math.Max(digitsA.Length, digitsB.Length);
        var digits = new uint[n + 1];
        
        const int halfBits = CountBits / 2;
        const uint halfMask = 0xFFFF;
        uint acc = 0;

        for (var i = 0; i < n; ++i)
        {
            var numA = i < digitsA.Length ? digitsA[i] : 0;
            var numB = i < digitsB.Length ? digitsB[i] : 0;

            uint word = 0;
            
            for (var j = 0; j < 2; ++j)
            {
                acc += (numA & halfMask) + (numB & halfMask);

                numA >>= halfBits;
                numB >>= halfBits;

                word |= (acc & halfMask) << (halfBits * j);

                acc >>= halfBits;
            }

            digits[i] = word;
        }

        digits[n] = acc;
        return new BetterBigInteger(digits, isNegative: false);
    }

    public static BetterBigInteger operator -(BetterBigInteger a, BetterBigInteger b)
    {
        if (a.IsNegative != b.IsNegative) return a + -b;
        
        var cmp = (a.IsNegative ? -a : a).CompareTo(b.IsNegative ? -b : b);
        if (cmp == 0) return Zero;
        var resultNegative = cmp > 0 ? a.IsNegative : !a.IsNegative;
        var topDigits = cmp > 0 ? a.GetDigits() : b.GetDigits();
        var bottomDigits = cmp > 0 ? b.GetDigits() : a.GetDigits();
        
        var n = topDigits.Length;
        var digits = new uint[n];
        uint acc = 0;
        
        for (var i = 0; i < n; ++i)
        {
            var top = topDigits[i];
            var bottom = i < bottomDigits.Length ? bottomDigits[i] : 0;
            
            var borrow = acc == 1 && top == 0 ? 1u : 0u;
            top -= acc;
            acc = borrow;
            
            if (top < bottom) acc = 1; // we need a replacement, but also need to get -number, because we are borrowing 

            digits[i] = top - bottom;
        }

        return new BetterBigInteger(digits, resultNegative);
    }

    public static BetterBigInteger operator -(BetterBigInteger a) => new(a.GetDigits(), !a.IsNegative);

    public static BetterBigInteger operator /(BetterBigInteger a, BetterBigInteger b)
    {
        if (b == Zero) throw new DivideByZeroException();
        if (a == Zero) return Zero;
        
        var absA = a.IsNegative ? -a : a;
        var absB = b.IsNegative ? -b : b;
        
        if (absA < absB) return Zero;
        
        var acc = Zero;
        var quotient = Zero;
        
        var digits = absA.GetDigits();
        
        for (var i = digits.Length - 1; i >= 0; --i)
        {
            acc = (acc << CountBits) + new BetterBigInteger([digits[i]]); // * 2^32 + digit
            uint left = 0;
            var right = uint.MaxValue;
            uint digitQuotient = 0;
            while (left <= right)
            {
                var mid = left + ((right - left) / 2);
                var current = new BetterBigInteger([mid]) * absB;
                if (current <= acc)
                {
                    digitQuotient = mid;
                    if (mid == uint.MaxValue) break;
                    left = mid + 1;
                }
                else
                {
                    if (mid == 0) break;
                    right = mid - 1;
                }
            }
            
            quotient = (quotient << CountBits) + new BetterBigInteger([digitQuotient]);

            if (digitQuotient != 0) acc -= new BetterBigInteger([digitQuotient]) * absB;
        }
        return quotient == Zero ? 
            Zero : new BetterBigInteger(quotient.GetDigits(), a.IsNegative != b.IsNegative);
    }

    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b)
    {
        if (b == Zero) throw new DivideByZeroException();
        if (a == Zero) return Zero;
        
        var absA = a.IsNegative ? -a : a;
        var absB = b.IsNegative ? -b : b;
        
        if (absA < absB) return a;
        
        var acc = Zero;
        
        var digits = absA.GetDigits();
        
        for (var i = digits.Length - 1; i >= 0; --i)
        {
            acc = (acc << CountBits) + new BetterBigInteger([digits[i]]); // * 2^32 + digit
            uint left = 0;
            var right = uint.MaxValue;
            uint digitQuotient = 0;
            while (left <= right)
            {
                var mid = left + ((right - left) / 2);
                var current = new BetterBigInteger([mid]) * absB;
                if (current <= acc)
                {
                    digitQuotient = mid;
                    if (mid == uint.MaxValue) break;
                    left = mid + 1;
                }
                else
                {
                    if (mid == 0) break;
                    right = mid - 1;
                }
            }
            
            if (digitQuotient != 0) acc -= new BetterBigInteger([digitQuotient]) * absB;
        }
        
        return new BetterBigInteger(acc.GetDigits(), a.IsNegative);
    }


    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b)
    {
        return SimpleMultiplier.Multiply(a, b);
    }
    
    #region Helpers
    private uint[] ToTwosComplement(int size)
    {
        var onesComplement = new uint[size];
        var digits = GetDigits();
        
        for (var i = 0; i < digits.Length && i < size; ++i) onesComplement[i] = digits[i];
        
        if (!IsNegative) return onesComplement;
        
        for (var i = 0; i < size; ++i) onesComplement[i] = ~onesComplement[i];
        
        var tmp = new BetterBigInteger(onesComplement) + new BetterBigInteger([1]);
        
        var tmpDigits = tmp.GetDigits();
        var result = new uint[size]; // for fixed size
        
        for (var i = 0; i < tmpDigits.Length && i < size; ++i) result[i] = tmpDigits[i];
        
        return result;
    }
    
    private static BetterBigInteger FromTwosComplement(uint[] digits)
    {
        ArgumentNullException.ThrowIfNull(digits);
        
        if (digits.Length == 0) return Zero;
        
        var isNegative = ((digits[^1] >> 31) & 1) != 0;
        
        if (!isNegative) return new BetterBigInteger(digits);
        
        var inverted = new uint[digits.Length];
        for (var i = 0; i < digits.Length; ++i) inverted[i] = ~digits[i];
        
        var tmp = new BetterBigInteger(inverted) + new BetterBigInteger([1]);
        
        return new BetterBigInteger(tmp.GetDigits(), true);
    }
    #endregion

    public static BetterBigInteger operator ~(BetterBigInteger a)
    {
        var size = a.GetDigits().Length + 1;
        var result = a.ToTwosComplement(size);
        
        for (var i = 0; i < size; ++i) result[i] = ~result[i];
        
        return FromTwosComplement(result);
    }

    public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b)
    {
        var size = Math.Max(a.GetDigits().Length, b.GetDigits().Length) + 1;
        var first = a.ToTwosComplement(size);
        var second = b.ToTwosComplement(size);
        var result = new uint[size];
        
        for (var i = 0; i < size; ++i) result[i] = first[i] & second[i];
        
        return FromTwosComplement(result);
    }

    public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b)
    {
        var size = Math.Max(a.GetDigits().Length, b.GetDigits().Length) + 1;
        var first = a.ToTwosComplement(size);
        var second = b.ToTwosComplement(size);
        var result = new uint[size];
        
        for (var i = 0; i < size; ++i) result[i] = first[i] | second[i];
        
        return FromTwosComplement(result);
    }

    public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b)
    {
        var size = Math.Max(a.GetDigits().Length, b.GetDigits().Length) + 1;
        var first = a.ToTwosComplement(size);
        var second = b.ToTwosComplement(size);
        var result = new uint[size];
        
        for (var i = 0; i < size; ++i) result[i] = first[i] ^ second[i];
        
        return FromTwosComplement(result);
    }

    public static BetterBigInteger operator <<(BetterBigInteger a, int shift)
    {
        if (shift < 0) return a >> -shift;
        if (shift == 0 || a == Zero) return a;
        
        var wordShift = shift / CountBits;
        shift %= CountBits;
        
        var oldDigits = a.GetDigits();
        var digits = new uint[oldDigits.Length + wordShift];
        
        for (var i = 0; i < oldDigits.Length; ++i) digits[i + wordShift] = oldDigits[i];
        
        if (shift == 0) return new BetterBigInteger(digits, a.IsNegative);
        
        var toNextWord = CountBits - shift;
        uint acc = 0;
        
        for (var i = 0; i < digits.Length; ++i)
        {
            var temp = digits[i] >> toNextWord;
            digits[i] <<= shift;
            digits[i] |= acc;
            acc = temp;
        }
        
        if (acc != 0)
        {
            var newDigits = new uint[digits.Length + 1];
            Array.Copy(digits, newDigits, digits.Length);
            newDigits[^1] = acc;
            digits = newDigits;
        }
        
        return new BetterBigInteger(digits, a.IsNegative);
    }
    public static BetterBigInteger operator >> (BetterBigInteger a, int shift)
    {
        if (shift < 0) return a << -shift;
        if (shift == 0 || a == Zero) return a;
        
        var wordShift = shift / CountBits;
        shift %= CountBits;

        var size = a.GetDigits().Length + 1 + shift / CountBits;
        var words = a.ToTwosComplement(size);
        
        var digits = new uint[size];
        var empty = a.IsNegative ? uint.MaxValue : 0; // uint.MaxValue - 111111111..11 (32)
        
        for (var i = 0; i < size; ++i)
        {
            var src = i + wordShift;
            digits[i] = src < size ? words[src] : empty;
        }
        
        if (shift == 0) return FromTwosComplement(digits);
        
        var fromNextWord = CountBits - shift;
        var acc = empty << fromNextWord;
        
        for (var i = digits.Length - 1; i >= 0; --i)
        {
            var temp = digits[i] << fromNextWord;
            digits[i] >>= shift;
            digits[i] |= acc;
            acc = temp;
        }
        
        return FromTwosComplement(digits);
    }

    public static bool operator ==(BetterBigInteger a, BetterBigInteger b) => Equals(a, b);
    public static bool operator !=(BetterBigInteger a, BetterBigInteger b) => !Equals(a, b);
    public static bool operator <(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) < 0;
    public static bool operator >(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) > 0;
    public static bool operator <=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) <= 0;
    public static bool operator >=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) >= 0;

    public override string ToString() => ToString(10);
    public string ToString(int radix)
    {
        if (radix is < 2 or > 36) throw new ArgumentOutOfRangeException(nameof(radix), "radix must be between 2 and 36");
        if (this == Zero) return "0";

        var bbiRadix = new BetterBigInteger([(uint)radix]);
        var number = (this < Zero ? -this : this);
        
        var result = new StringBuilder();
        
        while (number != Zero)
        {
            var digit = (number % bbiRadix).GetDigits()[0];
            result.Append((char)(digit <= 9 ? '0' + digit : 'A' + digit - 10));
            number /= bbiRadix;
        }
        
        if (IsNegative) result.Append('-');
        
        for (var left = 0; left < result.Length / 2; ++left)
        {
            var right = result.Length - 1 - left;
            (result[left], result[right]) = (result[right], result[left]);
        }
        
        return result.ToString();
    }
}