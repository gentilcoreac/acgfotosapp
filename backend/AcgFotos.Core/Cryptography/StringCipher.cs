using System;
using System.Security.Cryptography;
using System.Text;

namespace AcgFotos.Core.Cryptography
{
    // AES-GCM con nonce random de 12 bytes y tag de 16 bytes (authenticated encryption).
    // Reemplaza el AES-CBC sin HMAC previo, que era vulnerable a padding-oracle.
    // Formato del ciphertext: base64( [nonce(12)] [tag(16)] [ciphertext(N)] ).
    // El keyString se deriva determinísticamente a 32 bytes (AES-256) vía SHA-256.
    public static class StringCipher
    {
        public static string Encrypt(string plaintext, string keyString)
        {
            var key = SHA256.HashData(Encoding.UTF8.GetBytes(keyString));

            var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
            RandomNumberGenerator.Fill(nonce);

            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var ciphertextBytes = new byte[plaintextBytes.Length];
            var tag = new byte[AesGcm.TagByteSizes.MaxSize];

            using (var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize))
            {
                aes.Encrypt(nonce, plaintextBytes, ciphertextBytes, tag);
            }

            var output = new byte[nonce.Length + tag.Length + ciphertextBytes.Length];
            Buffer.BlockCopy(nonce, 0, output, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, output, nonce.Length, tag.Length);
            Buffer.BlockCopy(ciphertextBytes, 0, output, nonce.Length + tag.Length, ciphertextBytes.Length);

            return Convert.ToBase64String(output);
        }

        public static string Decrypt(string ciphertext, string keyString)
        {
            var key = SHA256.HashData(Encoding.UTF8.GetBytes(keyString));
            var fullCipher = Convert.FromBase64String(ciphertext);

            var nonceSize = AesGcm.NonceByteSizes.MaxSize;
            var tagSize = AesGcm.TagByteSizes.MaxSize;

            var nonce = new byte[nonceSize];
            var tag = new byte[tagSize];
            var ciphertextBytes = new byte[fullCipher.Length - nonceSize - tagSize];

            Buffer.BlockCopy(fullCipher, 0, nonce, 0, nonceSize);
            Buffer.BlockCopy(fullCipher, nonceSize, tag, 0, tagSize);
            Buffer.BlockCopy(fullCipher, nonceSize + tagSize, ciphertextBytes, 0, ciphertextBytes.Length);

            var plaintextBytes = new byte[ciphertextBytes.Length];
            using (var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize))
            {
                aes.Decrypt(nonce, ciphertextBytes, tag, plaintextBytes);
            }

            return Encoding.UTF8.GetString(plaintextBytes);
        }
    }
}
