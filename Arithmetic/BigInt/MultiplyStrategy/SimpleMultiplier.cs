using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier
{
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b) 
    {
        ReadOnlySpan<uint> da = a.GetDigits();
        ReadOnlySpan<uint> db = b.GetDigits();

        if (da.Length == 1 && da[0] == 0) return new BetterBigInteger(new uint[] { 0 });
        if (db.Length == 1 && db[0] == 0) return new BetterBigInteger(new uint[] { 0 });

        uint[] res = new uint[da.Length + db.Length];

        for (int i = 0; i < da.Length; i++)
        {
            uint carry = 0; 
            uint ai = da[i]; 

            uint aL = (ai << (sizeof(uint) * 4)) >> (sizeof(uint) * 4);
            uint aH = ai >> (sizeof(uint) * 4);

            for (int j = 0; j < db.Length; j++)
            {
                uint bj = db[j];
                uint bL = (bj << (sizeof(uint) * 4)) >> (sizeof(uint) * 4);
                uint bH = bj >> (sizeof(uint) * 4);

                uint p0 = aL * bL;
                uint p1 = aL * bH;
                uint p2 = aH * bL;
                uint p3 = aH * bH;

                uint resL = (res[i + j] << (sizeof(uint) * 4)) >> (sizeof(uint) * 4);
                uint resH = res[i + j] >> (sizeof(uint) * 4);
                uint carryL = (carry << (sizeof(uint) * 4)) >> (sizeof(uint) * 4);
                uint carryH = carry >> (sizeof(uint) * 4);

                uint low = ((p0 << (sizeof(uint) * 4)) >> (sizeof(uint) * 4)) + resL + carryL;
                uint carryLow = low >> (sizeof(uint) * 4);

                uint mid = (p0 >> (sizeof(uint) * 4)) + ((p1 << (sizeof(uint) * 4)) >> (sizeof(uint) * 4)) + ((p2 << (sizeof(uint) * 4)) >> (sizeof(uint) * 4)) + resH + carryH + carryLow;
                uint carryMid = mid >> (sizeof(uint) * 4);

                uint high = p3 + (p1 >> (sizeof(uint) * 4)) + (p2 >> (sizeof(uint) * 4)) + carryMid;

                res[i + j] = (mid << sizeof(uint) * 4) | (low << (sizeof(uint) * 4)) >> (sizeof(uint) * 4);
                carry = high;
            }
            if (carry > 0)
            {
                res[i + db.Length] += carry;
            }
        }

        return new BetterBigInteger(res, a.IsNegative != b.IsNegative);
    }
}