// Copyright 2018 Ulf Adams. Distributed under the Boost Software License, Version 1.0.
// Adapted from Dogwei/RyuCsharp. See NOTICE.txt and LICENSE-Boost.txt.
using System;

namespace Ryu
{
    internal static partial class RyuSingle
    {
        internal static int Format(float value, Span<char> output)
        {
            uint bits = (uint)BitConverter.SingleToInt32Bits(value);
            uint mantissa = bits & ((1u << FLOAT_MANTISSA_BITS) - 1);
            uint exponent = (bits >> FLOAT_MANTISSA_BITS) & 255;
            if (exponent == 255)
            {
                var specialValue = "NaN";
                if (mantissa == 0)
                    specialValue = (bits >> 31) != 0
                        ? "-Infinity"
                        : "Infinity";

                specialValue.AsSpan().CopyTo(output);
                return specialValue.Length;
            }

            if ((bits & 0x7fffffff) == 0)
            {
                output[0] = '0';
                return 1;
            }

            floating_decimal_32 number = f2d(mantissa, exponent);
            Span<char> digits = stackalloc char[9];
            int firstDigit = digits.Length;
            uint remaining = number.mantissa;
            do
            {
                digits[--firstDigit] = (char)('0' + remaining % 10);
                remaining /= 10;
            }
            while (remaining != 0);

            int digitCount = digits.Length - firstDigit;
            int scientificExponent = number.exponent + digitCount - 1;
            int length = 0;
            if ((bits >> 31) != 0)
            {
                output[length++] = '-';
            }

            if (scientificExponent >= -4 && scientificExponent < 9)
            {
                int decimalPosition = scientificExponent + 1;
                if (decimalPosition <= 0)
                {
                    output[length++] = '0';
                    output[length++] = '.';
                    for (int i = 0; i < -decimalPosition; ++i)
                    {
                        output[length++] = '0';
                    }
                }

                for (int i = 0; i < digitCount; ++i)
                {
                    if (i > 0 && i == decimalPosition)
                    {
                        output[length++] = '.';
                    }

                    output[length++] = digits[firstDigit + i];
                }

                for (int i = digitCount; i < decimalPosition; ++i)
                {
                    output[length++] = '0';
                }
            }
            else
            {
                output[length++] = digits[firstDigit];
                if (digitCount > 1)
                {
                    output[length++] = '.';
                    for (int i = 1; i < digitCount; ++i)
                    {
                        output[length++] = digits[firstDigit + i];
                    }
                }

                output[length++] = 'E';
                output[length++] = scientificExponent < 0 ? '-' : '+';
                int magnitude = Math.Abs(scientificExponent);
                output[length++] = (char)('0' + magnitude / 10);
                output[length++] = (char)('0' + magnitude % 10);
            }
            return length;
        }

        const int FLOAT_MANTISSA_BITS = 23;
        const int FLOAT_EXPONENT_BITS = 8;
        const int FLOAT_BIAS = 127;

        static uint pow5factor_32(uint value)
        {
            uint count = 0;
            for (; ; )
            {
                assert(value != 0);
                uint q = value / 5;
                uint r = value % 5;
                if (r != 0)
                {
                    break;
                }
                value = q;
                ++count;
            }
            return count;
        }

        // Returns true if value is divisible by 5^p.
        static bool multipleOfPowerOf5_32(uint value, uint p)
        {
            return pow5factor_32(value) >= p;
        }

        // Returns true if value is divisible by 2^p.
        static bool multipleOfPowerOf2_32(uint value, uint p)
        {
            // __builtin_ctz doesn't appear to be faster here.
            return (value & ((1u << (int)p) - 1)) == 0;
        }

        // It seems to be slightly faster to avoid uint128_t here, although the
        // generated code for uint128_t looks slightly nicer.
        static uint mulShift32(uint m, ulong factor, int shift)
        {
            assert(shift > 32);

            // The casts here help MSVC to avoid calls to the __allmul library
            // function.
            uint factorLo = (uint)(factor);
            uint factorHi = (uint)(factor >> 32);
            ulong bits0 = (ulong)m * factorLo;
            ulong bits1 = (ulong)m * factorHi;

            ulong sum = (bits0 >> 32) + bits1;
            ulong shiftedSum = sum >> (shift - 32);
            assert(shiftedSum <= /*UINT32_MAX*/uint.MaxValue);
            return (uint)shiftedSum;
        }

        static uint mulPow5InvDivPow2(uint m, uint q, int j)
        {
            return mulShift32(m, FLOAT_POW5_INV_SPLIT[q], j);
        }

        static uint mulPow5divPow2(uint m, uint i, int j)
        {
            return mulShift32(m, FLOAT_POW5_SPLIT[i], j);
        }

        static floating_decimal_32 f2d(uint ieeeMantissa, uint ieeeExponent)
        {
            int e2;
            uint m2;
            if (ieeeExponent == 0)
            {
                // We subtract 2 so that the bounds computation has 2 additional bits.
                e2 = 1 - FLOAT_BIAS - FLOAT_MANTISSA_BITS - 2;
                m2 = ieeeMantissa;
            }
            else
            {
                e2 = (int)ieeeExponent - FLOAT_BIAS - FLOAT_MANTISSA_BITS - 2;
                m2 = (1u << FLOAT_MANTISSA_BITS) | ieeeMantissa;
            }
            bool even = (m2 & 1) == 0;
            bool acceptBounds = even;


            // Step 2: Determine the interval of valid decimal representations.
            uint mv = 4 * m2;
            uint mp = 4 * m2 + 2;
            // Implicit bool -> int conversion. True is 1, false is 0.
            uint mmShift = (ieeeMantissa != 0 || ieeeExponent <= 1) ? 1U : 0;
            uint mm = 4 * m2 - 1 - mmShift;

            // Step 3: Convert to a decimal power base using 64-bit arithmetic.
            uint vr, vp, vm;
            int e10;
            bool vmIsTrailingZeros = false;
            bool vrIsTrailingZeros = false;
            byte lastRemovedDigit = 0;
            if (e2 >= 0)
            {
                uint q = log10Pow2(e2);
                e10 = (int)q;
                int k = FLOAT_POW5_INV_BITCOUNT + pow5bits((int)q) - 1;
                int i = -e2 + (int)q + k;
                vr = mulPow5InvDivPow2(mv, q, i);
                vp = mulPow5InvDivPow2(mp, q, i);
                vm = mulPow5InvDivPow2(mm, q, i);

                if (q != 0 && (vp - 1) / 10 <= vm / 10)
                {
                    // We need to know one removed digit even if we are not going to loop below. We could use
                    // q = X - 1 above, except that would require 33 bits for the result, and we've found that
                    // 32-bit arithmetic is faster even on 64-bit machines.
                    int l = FLOAT_POW5_INV_BITCOUNT + pow5bits((int)(q - 1)) - 1;
                    lastRemovedDigit = (byte)(mulPow5InvDivPow2(mv, q - 1, -e2 + (int)q - 1 + l) % 10);
                }
                if (q <= 9)
                {
                    // The largest power of 5 that fits in 24 bits is 5^10, but q <= 9 seems to be safe as well.
                    // Only one of mp, mv, and mm can be a multiple of 5, if any.
                    if (mv % 5 == 0)
                    {
                        vrIsTrailingZeros = multipleOfPowerOf5_32(mv, q);
                    }
                    else if (acceptBounds)
                    {
                        vmIsTrailingZeros = multipleOfPowerOf5_32(mm, q);
                    }
                    else
                    {
                        if (multipleOfPowerOf5_32(mp, q))
                        {
                            --vp;
                        }
                    }
                }
            }
            else
            {
                uint q = log10Pow5(-e2);
                e10 = (int)q + e2;
                int i = -e2 - (int)q;
                int k = pow5bits(i) - FLOAT_POW5_BITCOUNT;
                int j = (int)q - k;
                vr = mulPow5divPow2(mv, (uint)i, j);
                vp = mulPow5divPow2(mp, (uint)i, j);
                vm = mulPow5divPow2(mm, (uint)i, j);

                if (q != 0 && (vp - 1) / 10 <= vm / 10)
                {
                    j = (int)q - 1 - (pow5bits(i + 1) - FLOAT_POW5_BITCOUNT);
                    lastRemovedDigit = (byte)(mulPow5divPow2(mv, (uint)(i + 1), j) % 10);
                }
                if (q <= 1)
                {
                    // {vr,vp,vm} is trailing zeros if {mv,mp,mm} has at least q trailing 0 bits.
                    // mv = 4 * m2, so it always has at least two trailing 0 bits.
                    vrIsTrailingZeros = true;
                    if (acceptBounds)
                    {
                        // mm = mv - 1 - mmShift, so it has 1 trailing 0 bit iff mmShift == 1.
                        vmIsTrailingZeros = mmShift == 1;
                    }
                    else
                    {
                        // mp = mv + 2, so it always has at least one trailing 0 bit.
                        --vp;
                    }
                }
                else if (q < 31)
                {
                    vrIsTrailingZeros = multipleOfPowerOf2_32(mv, q - 1);

                }
            }


            // Step 4: Find the shortest decimal representation in the interval of valid representations.
            int removed = 0;
            uint output;
            if (vmIsTrailingZeros || vrIsTrailingZeros)
            {
                // General case, which happens rarely (~4.0%).
                while (vp / 10 > vm / 10)
                {
                    vmIsTrailingZeros &= vm % 10 == 0;
                    vrIsTrailingZeros &= lastRemovedDigit == 0;
                    lastRemovedDigit = (byte)(vr % 10);
                    vr /= 10;
                    vp /= 10;
                    vm /= 10;
                    ++removed;
                }

                if (vmIsTrailingZeros)
                {
                    while (vm % 10 == 0)
                    {
                        vrIsTrailingZeros &= lastRemovedDigit == 0;
                        lastRemovedDigit = (byte)(vr % 10);
                        vr /= 10;
                        vp /= 10;
                        vm /= 10;
                        ++removed;
                    }
                }

                if (vrIsTrailingZeros && lastRemovedDigit == 5 && vr % 2 == 0)
                {
                    // Round even if the exact number is .....50..0.
                    lastRemovedDigit = 4;
                }
                // We need to take vr + 1 if vr is outside bounds or we need to round up.
                output = vr;
                if ((vr == vm && (!acceptBounds || !vmIsTrailingZeros)) || lastRemovedDigit >= 5)
                {
                    ++output;
                }
            }
            else
            {
                // Specialized for the common case (~96.0%). Percentages below are relative to this.
                // Loop iterations below (approximately):
                // 0: 13.6%, 1: 70.7%, 2: 14.1%, 3: 1.39%, 4: 0.14%, 5+: 0.01%
                while (vp / 10 > vm / 10)
                {
                    lastRemovedDigit = (byte)(vr % 10);
                    vr /= 10;
                    vp /= 10;
                    vm /= 10;
                    ++removed;
                }

                // We need to take vr + 1 if vr is outside bounds or we need to round up.
                output = vr;
                if (vr == vm || lastRemovedDigit >= 5)
                {
                    ++output;
                }
            }
            int exp = e10 + removed;



            floating_decimal_32 fd = default;
            fd.exponent = exp;
            fd.mantissa = output;
            return fd;
        }


        private struct floating_decimal_32
        {
            public uint mantissa;
            public int exponent;
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private static void assert(bool condition)
        {
            System.Diagnostics.Debug.Assert(condition);
        }

        private static int pow5bits(int e) => (int)(((uint)e * 1217359) >> 19) + 1;
        private static uint log10Pow2(int e) => ((uint)e * 78913) >> 18;
        private static uint log10Pow5(int e) => ((uint)e * 732923) >> 20;
    }
}
