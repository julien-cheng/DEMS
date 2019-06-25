namespace Documents.API.Common.Models.MetadataModels
{
    using System;

    public class AttributeLocator
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public string JsonPathExpression { get; set; }
        public bool IsIndexed { get; set; }
        public bool IsFacet { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsOnDetailView { get; set; }

        public StorageType StorageType { get; set; }

        public string GetTypeNameFromStorageType()
        {
            switch (this.StorageType)
            {
                case StorageType.SystemString:
                    return typeof(string).FullName;
                case StorageType.SystemInt:
                    return typeof(int).FullName;
                case StorageType.SystemObject:
                    return typeof(object).FullName;
                case StorageType.SystemDateTime:
                    return typeof(DateTime).FullName;
                case StorageType.SystemBoolean:
                    return typeof(bool).FullName;
                default:
                    throw (new Exception("There's no mapping for storage type"));
            }
        }
    }

    public enum StorageType : int
    {
        SystemString = 1, 
        SystemInt = 2,
        SystemObject = 3,
        SystemDateTime = 4,
        SystemBoolean = 5,
    }
}
