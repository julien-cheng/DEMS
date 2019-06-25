using System.Collections.Generic;

namespace Documents.API.Common
{
    public interface IProvideMetadata
    {
        string Metadata(string key);
        IDictionary<string, string> MetadataFlattened { get; }
        void Write(string key, object value, bool withTypeName = false);
    }
}
