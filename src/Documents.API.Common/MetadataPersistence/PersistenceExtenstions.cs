namespace Documents.API.Common.MetadataPersistence
{
    using Documents.API.Common.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using System;
    using System.Collections.Generic;

    public static class PersistenceExtenstions
    {
        public static T Read<T>(this FileModel model, string key, bool withTypeName = false, T defaultValue = default(T))
            => Read<T>(model.MetadataFlattened, key, withTypeName, defaultValue);

        public static object Read(this FileModel model, Type type, string key)
             => Read(model.MetadataFlattened,type, key);

        public static FileModel Write<T>(this FileModel model, string key, T value, bool withTypeName = false)
        {
            model.FileMetadata.MetadataForWriting(FileModel.Tier)
                .Write(key, value, withTypeName);
            return model;
        }

        public static FileModel RemoveMetadata(this FileModel model, string key)
        {
            model.FileMetadata.MetadataForWriting(FileModel.Tier).Remove(key);
            return model;
        }

        public static T Read<T>(this FolderModel model, string key, bool withTypeName = false, T defaultValue = default(T))
            => Read<T>(model.MetadataFlattened, key, withTypeName, defaultValue);
        public static void Write<T>(this FolderModel model, string key, T value, bool withTypeName = false)
            => model.FolderMetadata.MetadataForWriting(FolderModel.Tier)
                .Write(key, value, withTypeName);
        public static void WriteForFile<T>(this FolderModel model, string key, T value, bool withTypeName = false)
            => model.FileMetadata.MetadataForWriting(FolderModel.Tier)
                .Write(key, value, withTypeName);

        public static T Read<T>(this OrganizationModel model, string key, bool withTypeName = false, T defaultValue = default(T))
            => Read<T>(model.MetadataFlattened, key, withTypeName, defaultValue);
        public static void Write<T>(this OrganizationModel model, string key, T value, bool withTypeName = false)
            => model.OrganizationMetadata.MetadataForWriting(OrganizationModel.Tier)
                .Write(key, value, withTypeName);
        public static void WriteForFolder<T>(this OrganizationModel model, string key, T value, bool withTypeName = false)
            => model.FolderMetadata.MetadataForWriting(OrganizationModel.Tier)
                .Write(key, value, withTypeName);
        public static void WriteForFile<T>(this OrganizationModel model, string key, T value, bool withTypeName = false)
            => model.FileMetadata.MetadataForWriting(OrganizationModel.Tier)
                .Write(key, value, withTypeName);

        private static IDictionary<string, string> MetadataForWriting(this IDictionary<string, IDictionary<string, string>> tiers, string tier)
        {
            if (tiers == null)
                throw new Exception("Model not loaded with Metadata. Check Population directives. If you're creating a new model, you can use model.InitializeEmptyMetadata().");

            IDictionary<string, string> metadata;
            if (tiers.ContainsKey(tier))
                metadata = tiers[tier];
            else
            {
                metadata = new Dictionary<string, string>();
                tiers.Add(tier, metadata);
            }

            return metadata;
        }

        public static string ReadRaw(this IDictionary<string, string> metadata, string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("key", nameof(key));

            return metadata.ContainsKey(key.ToLower())
                ? metadata[key.ToLower()]
                : null;
        }

        public static T Read<T>(this IDictionary<string, string> metadata, string key, bool withTypeName = false, T defaultValue = default(T))
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("key", nameof(key));

            var value = metadata.ReadRaw(key);
            if (!string.IsNullOrEmpty(value))
                return JsonConvert.DeserializeObject<T>(value, JsonSettings(withTypeName: withTypeName));
            else
                return defaultValue;
        }

        public static object Read(this IDictionary<string, string> metadata, Type type, string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("key", nameof(key));

            var value = metadata.ReadRaw(key);
            if (!string.IsNullOrEmpty(value))
                return JsonConvert.DeserializeObject(value, type, JsonSettings(withTypeName: false));

            return null;
        }

        public static void WriteRaw(this IDictionary<string, string> metadata, string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("key", nameof(key));

            // null to delete
            if (value == null)
            {
                if (metadata.ContainsKey(key.ToLower()))
                    metadata.Remove(key.ToLower());
            }
            else
            {
                if (metadata.ContainsKey(key.ToLower()))
                    metadata[key.ToLower()] = value;
                else
                    metadata.Add(key.ToLower(), value);
            }
        }

        public static void Write<T>(this IDictionary<string, string> metadata, string key, T value, bool withTypeName = false)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("message", nameof(key));

            if (value == null)
                metadata.WriteRaw(key, null);
            else
                metadata.WriteRaw(key, 
                    JsonConvert.SerializeObject(
                        value, JsonSettings(withTypeName: withTypeName)
                    )
                );
        }

        private static JsonSerializerSettings JsonSettings(bool withTypeName = false)
        {
            return new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = withTypeName ? TypeNameHandling.Objects : TypeNameHandling.None,
                NullValueHandling = NullValueHandling.Ignore,
                /*ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                }*/
            };
        }
    }
}