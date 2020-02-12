using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace FLGrains.Utility
{
    static class PasswordRecoveryHelper
    {
        static readonly ThreadLocal<RandomNumberGenerator> random = new ThreadLocal<RandomNumberGenerator>(() => new RNGCryptoServiceProvider());

        static string ToUrlSafeBase64String(byte[] bytes) =>
            Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_');

        public static string GenerateNewToken()
        {
            var bytes = new byte[24];
            random.Value.GetBytes(bytes);
            return ToUrlSafeBase64String(bytes);
        }
    }
}
