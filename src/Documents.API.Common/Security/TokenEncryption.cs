namespace Documents.API.Common.Security
{
    using System;
    using System.IO;
    using System.Security.Cryptography;

    public class TokenEncryption
    {
        /// <summary>
        /// This will return a string delimted with @ symbol for IV and cipher text. 
        ///  IV + @ + CipherText
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="passphrase">This assumes that the passphrase is exactly 32bytes worth of a password, base64 encoded</param>
        /// <returns></returns>
        public static string Encrypt(string plainText, string passphrase)
        {
            // comment
            using (Aes myAes = Aes.Create())
            {
                myAes.GenerateIV();
                // Encrypt the string to an array of bytes.
                byte[] encrypted = EncryptStringToBytes_Aes(plainText, Convert.FromBase64String(passphrase), myAes.IV);

                return $"{Convert.ToBase64String(myAes.IV)}@{Convert.ToBase64String(encrypted)}";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="enc64cipherText"></param>
        /// <param name="passphrase">This assumes that the passphrase is exactly 32bytes worth of a password, base64 encoded</param>
        /// <returns></returns>
        public static string Decrypt(string enc64cipherText, string passphrase)
        {

            // This IV is tacked on at the start of the cipher text. split by @ symbol.  
            var IvAndCipher = enc64cipherText.Split('@');
            // First we need to remove the base 64 encoding.
            var IV = Convert.FromBase64String(IvAndCipher[0]);

            var cipherText = Convert.FromBase64String(IvAndCipher[1]);

            // Encrypt the string to an array of bytes.
            var decryptedString = DecryptStringFromBytes_Aes(cipherText, Convert.FromBase64String(passphrase), IV);

            return decryptedString;
        }

        static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key,byte[] IV)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            byte[] encrypted;
            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(Key,IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            // Return the encrypted bytes from the memory stream.
            return encrypted;
        }

        static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(Key, IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }

            return plaintext;

        }
    }
}
