using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace FLGrains.Utility
{
    static class InviteCodeHelper
    {
        static readonly uint randomNumberMax = 36u * 36u * 36u * 36u * 36u * 36u;

        public static string GenerateNewCode()
        {
            var bytes = new byte[4];
            RandomNumberGenerator.Fill(bytes);
            var num = BitConverter.ToUInt32(bytes.AsSpan());
            num %= randomNumberMax;
            var chars = new char[6];
            foreach (var i in Enumerable.Range(0, 6))
            {
                var digit = num % 36;
                chars[i] = ToChar(digit);
                num /= 36;
            }
            return new string(chars);
        }

        static char ToChar(uint digit) =>
            digit <= 9 ? (char)('0' + digit) : (char)('A' + digit - 10);
    }
}
