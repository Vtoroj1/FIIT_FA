using System.ComponentModel.DataAnnotations;
using Arithmetic.BigInt.Interfaces;
using Arithmetic.BigInt.MultiplyStrategy;

namespace Arithmetic.BigInt;

public sealed class BetterBigInteger : IBigInteger
{
    private int _signBit;
    
    private uint _smallValue; // Если число маленькое, храним его прямо в этом поле, а _data == null.
    private uint[]? _data;
    
    public bool IsNegative => _signBit == 1;
    
    /// От массива цифр (little endian)
    public BetterBigInteger(uint[] digits, bool isNegative = false)
    {
        if (digits == null) throw new ArgumentNullException(nameof(digits));

        int length = digits.Length;
        while (length > 0 && digits[length - 1] == 0) length--;

        if (length == 0)
        {
            _signBit = 0;
            _smallValue = 0;
            _data = null;
        }
        else if (length == 1)
        {
            _signBit = isNegative ? 1 : 0;
            _smallValue = digits[0];
            _data = null;
        } else {
            _signBit = isNegative ? 1 : 0; 
            _smallValue = 0;
            _data = new uint[length];
            Array.Copy(digits, _data, length);
        }
    }
    
    public BetterBigInteger(IEnumerable<uint> digits, bool isNegative = false)
        : this(digits?.ToArray() ?? throw new ArgumentNullException(nameof(digits)), isNegative)
    {
    }  
    
    public BetterBigInteger(string value, int radix)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Строка не может быть пустой", nameof(value));
        }
        if (radix < 2 || radix > 36)
        {
            throw new ArgumentException("Основание системы счисления должно быть от 2 до 36", nameof(radix));
        }

        bool isNeg = value[0] == '-';
        int startIndex = (isNeg || value[0] == '+') ? 1 : 0;

        if (startIndex >= value.Length)
        {
            throw new FormatException("Строка не содержит цифр");
        }

        uint[] currentDigits = [];

        for (int i = startIndex; i < value.Length; i++)
        {
            char c = value[i];
            uint digit = ParseCharToDigit(c);

            if (digit >= radix)
            {
                throw new FormatException($"Символ '{c}' недопустим для системы счисления {radix}");
            }

            currentDigits = MultiplyByAndAdd(currentDigits, (uint)radix, digit);
        }

        var final = new BetterBigInteger(currentDigits, isNeg);
        _smallValue = final._smallValue;
        _data = final._data;
        _signBit = final._signBit;
    }

    private static uint ParseCharToDigit(char c)
    {
        uint digitValue;
        if (c >= '0' && c <= '9')
        {
            digitValue = (uint)(c - '0');
        }
        else if (c >= 'a' && c <= 'z')
        {
            digitValue = (uint)(c - 'a' + 10);
        }
        else if (c >= 'A' && c <= 'Z')
        {
            digitValue = (uint)(c - 'A' + 10);
        } else {
            throw new FormatException($"Недопустимый символ: {c}");
        }

        return digitValue;
    }

    private static uint[] MultiplyByAndAdd(uint[] digits, uint multiplier, uint addend)
    {
        if (digits.Length == 0) return addend == 0 ? [] : [addend];

        uint[] result = new uint[digits.Length + 1];
        uint carry = addend;

        for (int i = 0; i < digits.Length; i++)
        {
            uint a = digits[i];

            uint aL = a & 0xFFFF;
            uint aH = a >> 16;
            uint bL = multiplier & 0xFFFF;
            uint bH = multiplier >> 16;

            uint p0 = aL * bL;
            uint p1 = aL * bH;
            uint p2 = aH * bL;
            uint p3 = aH * bH;

            uint low = (p0 & 0xFFFF) + (carry & 0xFFFF);
            uint carryLow = low >> 16;

            uint mid = (p0 >> 16) + (p1 & 0xFFFF) + (p2 & 0xFFFF) + (carry >> 16) + carryLow;
            uint carryMid = mid >> 16;

            uint high = p3 + (p1 >> 16) + (p2 >> 16) + carryMid;

            result[i] = (mid << 16) | (low & 0xFFFF);
            carry = high;
        }

        result[digits.Length] = carry;
        return result;
    }

    public ReadOnlySpan<uint> GetDigits()
    {
        return _data != null ? new ReadOnlySpan<uint>(_data) : new uint[] { _smallValue };
    }
    
    public int CompareTo(IBigInteger? other)
    {
        if (other is null) return 1;

        if (!this.IsNegative && other.IsNegative) return 1;
        if (this.IsNegative && !other.IsNegative) return -1;

        ReadOnlySpan<uint> thisDigits = this.GetDigits();
        ReadOnlySpan<uint> otherDigits = other.GetDigits();

        int lengthcomparison = thisDigits.Length.CompareTo(otherDigits.Length);

        if (lengthcomparison != 0)
        {
            return this.IsNegative ? -lengthcomparison : lengthcomparison;
        }

        for (int i = thisDigits.Length -1; i >= 0; i--)
        {
            int digitComparison = thisDigits[i].CompareTo(otherDigits[i]);
            
            if (digitComparison != 0)
            {
                return this.IsNegative ? -digitComparison : digitComparison;
            }
        }

        return 0;
    }

    public bool Equals(IBigInteger? other)
    {
        if (other is null) return false;
        if (this.IsNegative != other.IsNegative) return false;

        ReadOnlySpan<uint> thisDigits = this.GetDigits();
        ReadOnlySpan<uint> otherDigits = other.GetDigits();

        if (thisDigits.Length != otherDigits.Length) return false;

        return thisDigits.SequenceEqual(otherDigits);
    }

    public override bool Equals(object? obj) => obj is IBigInteger other && Equals(other);

    public override int GetHashCode()
    {
        HashCode hashCode = new HashCode();
        hashCode.Add(_signBit);

        foreach (uint digit in GetDigits())
        {
            hashCode.Add(digit);
        }

        return hashCode.ToHashCode();
    }
    
    
    public static BetterBigInteger operator +(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        if (a.IsNegative == b.IsNegative)
        {
            uint[] result = AddMags(a.GetDigits(), b.GetDigits());
            return new BetterBigInteger(result, a.IsNegative);
        } else {
            int cmp = CompareMags(a.GetDigits(), b.GetDigits());

            if (cmp == 0) return new BetterBigInteger(new uint[] { 0 });

            if (cmp > 0)
            {
                uint[] result = SubtractMags(a.GetDigits(), b.GetDigits());
                return new BetterBigInteger(result, a.IsNegative);
            } else {
                uint[] result = SubtractMags(b.GetDigits(), a.GetDigits());
                return new BetterBigInteger(result, b.IsNegative);
            }
        }
    }

    public static BetterBigInteger operator -(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        return a + (-b);
    }

    public static BetterBigInteger operator -(BetterBigInteger a)
    {
        ArgumentNullException.ThrowIfNull(a);

        ReadOnlySpan<uint> digits = a.GetDigits();
        if (digits.Length == 1 && digits[0] == 0)
        {
            return a;
        }
        return new BetterBigInteger(digits.ToArray(), !a.IsNegative);
    }

    public static BetterBigInteger operator /(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        DivMod(a, b, out BetterBigInteger quotient, out _);
        return quotient;
    }

    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        DivMod(a, b, out _, out BetterBigInteger remainder);
        return remainder;
    }
    
    
    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b)
    {
        int lenA = a.GetDigits().Length;
        int lenB = b.GetDigits().Length;
        int maxLen = Math.Max(lenA, lenB);

        IMultiplier multiplier;
        if (maxLen <= 32)
        {
            multiplier = new SimpleMultiplier();
        }
        else if (maxLen <= 256)
        {
            multiplier = new KaratsubaMultiplier();
        } else {
            multiplier = new FftMultiplier();
        }

        return multiplier.Multiply(a, b);     
    }
    
    public static BetterBigInteger operator ~(BetterBigInteger a)
    {
        ArgumentNullException.ThrowIfNull(a);
        return -a - new BetterBigInteger(new uint[] { 1 });
    }

    public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return PerformBitwise(a, b, '&');
    }

    public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return PerformBitwise(a, b, '|');
    }

    public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return PerformBitwise(a, b, '^');
    }

    private static BetterBigInteger PerformBitwise(BetterBigInteger a, BetterBigInteger b, char operation)
    {
        bool aSign = a.IsNegative;
        bool bSign = b.IsNegative;
        
        bool resultSign = operation switch
        {
            '&' => aSign & bSign,
            '|' => aSign | bSign,
            '^' => aSign ^ bSign,
            _ => throw new ArgumentException("Неизвестная операция")
        };

        ReadOnlySpan<uint> aDigits = a.GetDigits();
        ReadOnlySpan<uint> bDigits = b.GetDigits();
        int maxLength = Math.Max(aDigits.Length, bDigits.Length);

        uint[] result = new uint[maxLength + 1]; 

        bool carryA = true; 
        bool carryB = true;
        bool carryR = true; 

        for (int i = 0; i <= maxLength; i++)
        {
            uint wordA = i < aDigits.Length ? aDigits[i] : 0;
            uint wordB = i < bDigits.Length ? bDigits[i] : 0;

            if (aSign)
            {
                if (i >= aDigits.Length) wordA = 0xFFFFFFFF;
                else {
                    wordA = ~wordA;
                    if (carryA) { if (wordA == uint.MaxValue) wordA = 0; else { wordA++; carryA = false; } }
                }
            }

            if (bSign)
            {
                if (i >= bDigits.Length) wordB = 0xFFFFFFFF;
                else {
                    wordB = ~wordB;
                    if (carryB) { if (wordB == uint.MaxValue) wordB = 0; else { wordB++; carryB = false; } }
                }
            }

            uint wordR = operation switch
            {
                '&' => wordA & wordB,
                '|' => wordA | wordB,
                '^' => wordA ^ wordB,
                _ => 0
            };

            if (resultSign)
            {
                wordR = ~wordR;
                if (carryR) { if (wordR == uint.MaxValue) wordR = 0; else { wordR++; carryR = false; } }
            }

            result[i] = wordR;
        }

        return new BetterBigInteger(result, resultSign);
    }

    public static BetterBigInteger operator <<(BetterBigInteger a, int shift)
    {
        ArgumentNullException.ThrowIfNull(a);

        if (shift < 0) return a >> -shift;
        if (shift == 0 || (a.GetDigits().Length == 1 && a.GetDigits()[0] == 0)) return a;

        int wordShift = shift / (sizeof(uint) * 8);
        int bitShift = shift % (sizeof(uint) * 8);

        ReadOnlySpan<uint> digits = a.GetDigits();
        uint[] result = new uint[digits.Length + wordShift + 1];

        uint carry = 0;
        int carShift = 32 - bitShift;

        for (int i = 0; i < digits.Length; i++)
        {
            uint current = digits[i];

            if (bitShift == 0)
            {
                result[i + wordShift] = current;
                carry = 0;
            } else {
                result[i + wordShift] = (current << bitShift) | carry;
                carry = current >> carShift;
            }
        }

        if (carry > 0)
        {
            result[digits.Length + wordShift] = carry;
        }

        return new BetterBigInteger(result, a.IsNegative);        
    }
    public static BetterBigInteger operator >> (BetterBigInteger a, int shift)
    {
        ArgumentNullException.ThrowIfNull(a);
        
        if (shift < 0) return a << -shift;
        if (shift == 0) return a;

        int wordShift = shift / (sizeof(uint) * 8);
        int bitShift = shift % (sizeof(uint) * 8);

        ReadOnlySpan<uint> digits = a.GetDigits();
        
        bool droppedBits = false;
        if (a.IsNegative)
        {
            if (wordShift >= digits.Length) 
            {
                droppedBits = true;
            } else {
                for (int i = 0; i < wordShift; i++)
                {
                    if (digits[i] != 0) 
                    {
                        droppedBits = true;
                        break;
                    }
                }

                if (!droppedBits && bitShift > 0)
                {
                    uint mask = (1u << bitShift) - 1u;
                    if ((digits[wordShift] & mask) != 0)
                    {
                        droppedBits = true;
                    }
                }
            }
        }

        if (wordShift >= digits.Length) 
        {
            return droppedBits ? new BetterBigInteger(new uint[] { 1 }, true) : new BetterBigInteger(new uint[] { 0 });
        }

        uint[] result = new uint[digits.Length - wordShift];

        uint carry = 0;
        int carBitShift = 32 - bitShift;
        
        for (int i = digits.Length - 1; i >= wordShift; i--)
        {
            uint current = digits[i];
            
            if (bitShift == 0)
            {
                result[i - wordShift] = current;
                carry = 0;
            } else {
                result[i - wordShift] = (current >> bitShift) | carry;
                carry = current << carBitShift;
            }
        }

        if (droppedBits)
        {
            result = AddMags(result, new uint[] { 1u });
        }

        return new BetterBigInteger(result, a.IsNegative);        
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
        if (radix < 2 || radix > 36)
        {
            throw new ArgumentException("Основание системы счисления должно быть в диапазоне от 2 до 36", nameof(radix));
        }

        ReadOnlySpan<uint> digits = GetDigits();
        if (digits.Length == 1 && digits[0] == 0)
        {
            return "0";
        }

        BetterBigInteger current = new BetterBigInteger(digits.ToArray(), false);
        BetterBigInteger radixBigInt = new BetterBigInteger(new uint[] { (uint)radix });
        BetterBigInteger zero = new BetterBigInteger(new uint[] { 0 });

        ReadOnlySpan<char> chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".AsSpan();
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        while (current > zero)
        {
            DivMod(current, radixBigInt, out BetterBigInteger quotient, out BetterBigInteger remainder);
            uint remValue = remainder.GetDigits()[0]; 
            sb.Append(chars[(int)remValue]);
            current = quotient;
        }

        if (this.IsNegative)
        {
            sb.Append('-');
        }

        char[] resultChars = sb.ToString().ToCharArray();
        Array.Reverse(resultChars);

        return new string(resultChars);        
    }

    private static uint[] AddMags(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        int maxLength = Math.Max(a.Length, b.Length);
        uint[] result = new uint[maxLength + 1];

        uint carry = 0;

        for (int i = 0; i < maxLength; i++)
        {
            uint uA = i < a.Length ? a[i] : 0;
            uint uB = i < b.Length ? b[i] : 0;

            uint aLow = uA & 0xFFFF;
            uint bLow = uB & 0xFFFF; 

            uint aHigh = uA >> 16;
            uint bHigh = uB >> 16;

            uint sumLow = aLow + bLow + carry;
            uint resLow = sumLow & 0xFFFF;

            uint carryLow = sumLow >> 16;

            uint sumHigh = aHigh + bHigh + carryLow;
            uint resHigh = sumHigh & 0xFFFF;

            carry = sumHigh >> 16;

            result[i] = resLow | (resHigh << 16);
        }

        if (carry > 0)
        {
            result[maxLength] = carry;
        }

        return result;
    }

    private static uint[] SubtractMags(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        uint[] result = new uint[a.Length];

        uint acc = 0;

        for (int i = 0; i < a.Length; i++)
        {
            uint uA = a[i];
            uint uB = i < b.Length ? b[i] : 0;

            uint aLow = uA & 0xFFFF;
            uint aHigh = uA >> 16;

            uint bLow = uB & 0xFFFF;
            uint bHigh = uB >> 16;

            uint bLowTotal = bLow + acc;
            uint resLow;

            if (aLow < bLowTotal) {
                uint diff = bLowTotal - aLow; 
                resLow = (~diff + 1) & 0xFFFF;
                acc = 1;
            } else {
                resLow = aLow - bLowTotal;
                acc = 0;
            }

            uint bHighTotal = bHigh + acc;
            uint resHigh;

            if (aHigh < bHighTotal)
            {
                uint diff = bHighTotal - aHigh;
                resHigh = (~diff + 1) & 0xFFFF;
                acc = 1;
            } else {
                resHigh = aHigh - bHighTotal;
                acc = 0;
            }

            result[i] = resLow | (resHigh << 16);
        }

        return result;
    }

    private static int CompareMags(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        if (a.Length != b.Length) return a.Length.CompareTo(b.Length);

        for (int i = a.Length - 1; i >= 0; i--)
        {
            if (a[i] != b[i]) return a[i].CompareTo(b[i]);
        }

        return 0;
    }

    private static int GetBitLength(ReadOnlySpan<uint> digits)
    {
        if (digits.Length == 1 && digits[0] == 0) return 0;
        int length = (digits.Length - 1) * 32;
        uint last = digits[digits.Length - 1]; 
        while (last > 0)
        {
            length++;
            last >>= 1;
        }
        return length;
    }

    private static void DivMod(BetterBigInteger dividend, BetterBigInteger divisor, out BetterBigInteger quotient, out BetterBigInteger remainder)
    {
        ReadOnlySpan<uint> divDigits = divisor.GetDigits();

        if (divDigits.Length == 1 && divDigits[0] == 0)
        {
            throw new DivideByZeroException("Деление на ноль недопустимо");
        }

        int cmp = CompareMags(dividend.GetDigits(), divisor.GetDigits());

        if (cmp < 0)
        {
            quotient = new BetterBigInteger(new uint[] { 0 });
            remainder = dividend;
            return;
        }
        if (cmp == 0)
        {
            quotient = new BetterBigInteger(new uint[] { 1 }, dividend.IsNegative != divisor.IsNegative);
            remainder = new BetterBigInteger(new uint[] { 0 });
            return;
        }

        BetterBigInteger currentDividend = new BetterBigInteger(dividend.GetDigits().ToArray(), false);
        BetterBigInteger currentDivisor = new BetterBigInteger(divisor.GetDigits().ToArray(), false);
        BetterBigInteger currentQuotient = new BetterBigInteger(new uint[] { 0 });

        int dividendBits = GetBitLength(currentDividend.GetDigits());
        int divisorBits = GetBitLength(currentDivisor.GetDigits());
        int shift = dividendBits - divisorBits;

        currentDivisor <<= shift;

        for (int i = 0; i <= shift; i++)
        {
            currentQuotient <<= 1;

            if (CompareMags(currentDividend.GetDigits(), currentDivisor.GetDigits()) >= 0)
            {
                currentDividend -= currentDivisor;
                currentQuotient += new BetterBigInteger(new uint[] { 1 });
            }

            currentDivisor >>= 1;
        }

        bool qIsNegative = dividend.IsNegative != divisor.IsNegative;
        quotient = new BetterBigInteger(currentQuotient.GetDigits().ToArray(), qIsNegative);
        remainder = new BetterBigInteger(currentDividend.GetDigits().ToArray(), dividend.IsNegative);
    }
}