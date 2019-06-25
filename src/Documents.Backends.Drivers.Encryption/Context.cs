namespace Documents.Backends.Drivers.Encryption
{
    using Newtonsoft.Json;
    using System;
    using System.Security.Cryptography;

    public class Context
    {
        public string MasterKey { get; set; }
        public string NextDriverConfigurationJson { get; set; }
        public string NextDriverTypeName { get; set; }

        public bool MD5Enabled { get; set; } = true;
        public bool SHA1Enabled { get; set; } = true;
        public bool SHA256Enabled { get; set; } = true;

        internal SymmetricAlgorithm CryptographyAlgorithm { get; set; }

        public IFileBackend NextDriver { get; set; }
        public object NextDriverContext { get; set; }

        internal void Configure(string json)
        {
            JsonConvert.PopulateObject(json, this);

            CryptographyAlgorithm = Aes.Create();
            CryptographyAlgorithm.Key = Convert.FromBase64String(MasterKey);
            CryptographyAlgorithm.Padding = PaddingMode.PKCS7;
        }
    }
}
