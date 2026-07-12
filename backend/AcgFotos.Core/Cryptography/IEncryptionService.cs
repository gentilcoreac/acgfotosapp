namespace AcgFotos.Core.Cryptography {
    public interface IEncryptionService {
        /// <summary>
        /// Creates a random salt
        /// </summary>
        /// <returns></returns>
        string CreateSalt();
        /// <summary>
        /// Generates a Hashed password
        /// </summary>
        /// <param name="password"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        string Encrypt(string password, string salt);
        string Decrypt(string password, string salt);
    }
}
