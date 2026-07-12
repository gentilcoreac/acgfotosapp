using System;
using System.Security.Cryptography;

namespace AcgFotos.Core.Cryptography
{
    public class EncryptionService : IEncryptionService
    {
        public string CreateSalt()
        {
            var data = new byte[16];
            RandomNumberGenerator.Fill(data);
            return Convert.ToBase64String(data);
        }

        public string Decrypt(string password, string salt)
        {
            return StringCipher.Decrypt(password, salt);
        }

        public string Encrypt(string password, string salt)
        {
            return StringCipher.Encrypt(password, salt);
        }
    }
}
