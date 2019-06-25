// todo: move this (and the Manager project I cloned it from to a common assembly)
namespace Documents.Provisioning
{
    using System;
    using System.Security.Cryptography;
    using System.Linq;

    public class PasswordGenerator
    {
        // Testing for collisions across the generator
        //        var set = new HashSet<String>();
        //            for (int i = 0; i< 100000; i++)
        //            {
        //                var randomPW = PasswordGenerator.GetRandomAlphanumericString(8);
        //                if (!set.Contains(randomPW))
        //                {
        //                    set.Add(randomPW);
        //                }
        //                else
        //                {
        //                    throw new Exception("there was a collision");
        //                }
        //                Console.WriteLine(randomPW);
        //            }

        public static byte[] GetRandomBytes(int length)
        {
            var buffer = new byte[length];

            using (var cryptoProvider = new RNGCryptoServiceProvider())
                cryptoProvider.GetBytes(buffer);

            return buffer;
        }

        public static string GetRandomString(int length = 8, string characterSet = null)
        {
            characterSet = characterSet ?? "ABCDEFGHJKLMNPQRSTWXYZabcdefghjkmnpqrstwxyz23456789";
            var characterArray = characterSet.Distinct().ToArray();

            var bytes = new byte[length * 8];
            using(var cryptoProvider = new RNGCryptoServiceProvider())
            {
                cryptoProvider.GetBytes(bytes);
                var result = new char[length];
                for (int i = 0; i < length; i++)
                {
                    ulong value = BitConverter.ToUInt64(bytes, i * 8);
                    result[i] = characterArray[value % (uint)characterArray.Length];
                }
                return new string(result);
            }
        }
    }
}