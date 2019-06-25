namespace Documents.Store.Utilities
{
    using System;
    using System.Security.Cryptography;
    using System.Threading.Tasks;

    public static class KeyGenerator
    {
        private const int DefaultKeyLength = 32;

        public static string GenerateKey(int length = DefaultKeyLength)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var data = new byte[length];

                rng.GetBytes(data);
                return ByteArrayToString(data);
            }
        }

        private static string ByteArrayToString(byte[] ba)
        {
            string hex = BitConverter.ToString(ba);
            return hex.Replace("-", "");
        }

        public static async Task<string> GenerateKeyAsync(Func<string, Task<bool>> inUse, int length = DefaultKeyLength)
        {
            int tries = 10;

            while (tries-- > 0)
            {
                var candidate = GenerateKey(length);

                if (!await inUse(candidate))
                    return candidate;
            }
            throw new Exception("Cannot generate unique key");
        }
    }
}
