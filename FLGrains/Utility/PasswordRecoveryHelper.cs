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

        public static string GenerateNewToken()
        {
            var bytes = new byte[24];
            random.Value.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}
