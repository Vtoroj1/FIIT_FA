using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class FftMultiplier : IMultiplier
{
    private const uint MOD = 2013265921; //15 × 2^27 + 1
    private const uint PRIMITIVE_ROOT = 31; // 1..MOD-1     

    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        ReadOnlySpan<uint> aDigits = a.GetDigits();
        ReadOnlySpan<uint> bDigits = b.GetDigits();

        if ((aDigits.Length == 1 && aDigits[0] == 0) ||
            (bDigits.Length == 1 && bDigits[0] == 0))
        {
            return new BetterBigInteger([0]);
        }

        uint[] a8 = SplitTo8(aDigits);
        uint[] b8 = SplitTo8(bDigits);

        int neededLength = a8.Length + b8.Length - 1;

        int n = 1;
        while (n < neededLength) 
        {
            n <<= 1;
        }

        uint[] fa = new uint[n];
        uint[] fb = new uint[n]; 

        for (int i = 0; i < a8.Length; i++) fa[i] = a8[i];
        for (int i = 0; i < b8.Length; i++) fb[i] = b8[i];

        ExecuteNtt(fa, false);
        ExecuteNtt(fb, false);

        for (int i = 0; i < n; i++)
        {
            fa[i] = MultiplyMod(fa[i], fb[i], MOD);
        }

        ExecuteNtt(fa, true);

        uint[] res8 = new uint[n];
        uint carry = 0;

        for (int i = 0; i < neededLength; i++)
        {
            uint val = fa[i] + carry; 

            res8[i] = (val << (sizeof(uint) * 6)) >> (sizeof(uint) * 6); 
            carry = val >> (sizeof(uint) * 2);    
        }

        int tailIndex = neededLength;
        while (carry > 0 && tailIndex < n)
        { 
            res8[tailIndex] = (carry << (sizeof(uint) * 6)) >> (sizeof(uint) * 6);
            carry >>= (sizeof(uint) * 2);
            tailIndex++;
        }

        uint[] result32 = MergeTo32(res8, tailIndex);
        bool isNegative = a.IsNegative != b.IsNegative; 
        
        return new BetterBigInteger(result32, isNegative);
    }

    private static void ExecuteNtt(uint[] a, bool invert)
    {
        int n = a.Length;

        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1) j ^= bit;
            j ^= bit;
            
            if (i < j)
            {
                (a[i], a[j]) = (a[j], a[i]);
            }
        }

        for (int len = 2; len <= n; len <<= 1)
        {
            uint wlen = PowerMod(PRIMITIVE_ROOT, (MOD - 1) / (uint)len, MOD);
            
            if (invert)
            {
                wlen = PowerMod(wlen, MOD - 2, MOD);
            }
            
            for (int i = 0; i < n; i += len)
            {
                uint w = 1; 
                int half = len / 2;
                
                for (int j = 0; j < half; j++)
                {
                    uint u = a[i + j];
                    uint v = MultiplyMod(a[i + j + half], w, MOD);
                    
                    a[i + j] = (u + v >= MOD) ? (u + v - MOD) : (u + v);
                    a[i + j + half] = (u < v) ? (u + MOD - v) : (u - v);
                    
                    w = MultiplyMod(w, wlen, MOD);
                }
            }
        }

        if (invert)
        {
            uint nInv = PowerMod((uint)n, MOD - 2, MOD);
            
            for (int i = 0; i < n; i++)
            {
                a[i] = MultiplyMod(a[i], nInv, MOD);
            }
        }
    }

    private static uint MultiplyMod(uint a, uint b, uint mod)
    {
        uint res = 0;
        a %= mod;
        
        while (b > 0)
        {
            if ((b & 1) == 1)
            {
                res += a;
                if (res >= mod) res -= mod;
            }

            a <<= 1;
            if (a >= mod) a -= mod;
            b >>= 1;
        }
        return res;
    }

    private static uint PowerMod(uint baseValue, uint exp, uint mod)
    {
        uint res = 1;
        baseValue %= mod;
        
        while (exp > 0)
        {
            if ((exp & 1) == 1) res = MultiplyMod(res, baseValue, mod);
            baseValue = MultiplyMod(baseValue, baseValue, mod);
            exp >>= 1;
        }
        return res;
    }

    private static uint[] SplitTo8(ReadOnlySpan<uint> digits)
    {
        uint[] result = new uint[digits.Length * 4];
        for (int i = 0; i < digits.Length; i++)
        {
            result[i * 4] = (digits[i] << (sizeof(uint) * 6)) >> (sizeof(uint) * 6);
            result[i * 4 + 1] = (digits[i] << (sizeof(uint) * 4)) >> (sizeof(uint) * 6);
            result[i * 4 + 2] = (digits[i] << (sizeof(uint) * 2)) >> (sizeof(uint) * 6);
            result[i * 4 + 3] = digits[i] >> (sizeof(uint) * 6);
        }
        return result;
    }

    private static uint[] MergeTo32(uint[] digits8, int tailIndex)
    {
        int length32 = (tailIndex + 3) / 4;
        uint[] result = new uint[length32];
        
        for (int i = 0; i < tailIndex; i++)
        {
            result[i / 4] |= (digits8[i] << ((i % 4) * sizeof(uint) * 2));
        }
        
        return result;
    }
}
