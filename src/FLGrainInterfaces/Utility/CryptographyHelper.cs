using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FLGrainInterfaces
{
    public static class CryptographyHelper
    {
        public static byte[] GeneratePasswordSalt()
        {
            var result = new byte[32];
            using (var random = new RNGCryptoServiceProvider())
                random.GetNonZeroBytes(result);
            return result;
        }

        public static byte[] HashPassword(byte[] salt, string password)
        {
            using var hash = new SHA512Managed();
            var bytes = new byte[salt.Length + Encoding.UTF8.GetByteCount(password)];
            Array.Copy(salt, 0, bytes, 0, salt.Length);
            Encoding.UTF8.GetBytes(password, 0, password.Length, bytes, salt.Length);
            return hash.ComputeHash(bytes);
        }
    }
}
