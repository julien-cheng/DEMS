namespace Documents.Backends.Drivers.Encryption
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Org.BouncyCastle.Crypto.Digests;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public class SequentialState
    {
        public string NextDriverState { get; set; }
        public string IVBase64 { get; set; }

        private static JsonSerializerSettings settings = new JsonSerializerSettings
        {
            ContractResolver = new DigestContractResolver()
        };

        public MD5Digest MD5 { get; set; }
        public Sha1Digest SHA1 { get; set; }
        public Sha256Digest SHA256 { get; set; }

        public byte[] IV()
        {
            return IVBase64 != null
                ? Convert.FromBase64String(IVBase64)
                : null;
        }

        public SequentialState(string state = null)
        {
            if (state != null
                && state != "BEGIN")
                JsonConvert.PopulateObject(state, this, settings);
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, settings);
        }

        // using newtonsoft to serialize these hash calc algos
        // they just have a bunch of longs inside for state.
        // Only doubts are about the size of the serialization output
        // might need something more compact because there are a LOT of longs.
        public class DigestContractResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.CanWrite)
                    .Select(p => base.CreateProperty(p, memberSerialization))
                    .Union(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Select(f => base.CreateProperty(f, memberSerialization)))
                    .ToList();
                props.ForEach(p => { p.Writable = true; p.Readable = true; });

                if (type.BaseType != null && !type.BaseType.Equals(typeof(object)))
                    props.AddRange(CreateProperties(type.BaseType, memberSerialization));

                return props;
            }
        }
    }
}
