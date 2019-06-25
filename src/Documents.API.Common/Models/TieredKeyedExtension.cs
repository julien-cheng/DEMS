namespace Documents.API.Common.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class TieredKeyedExtension
    {
        public static T Search<T>(this IDictionary<string, IDictionary<string, T>> set, string key, string[] tierOrder)
            where T: class
        {
            if (set != null)
                foreach (var level in tierOrder)
                {
                    if (set.ContainsKey(level))
                    {
                        var tier = set[level];
                        if (tier.ContainsKey(key))
                            return tier[key];
                    }

                }
            return null;
        }

        public static IDictionary<string, T> Flatten<T>(this IDictionary<string, IDictionary<string, T>> source, string[] tierOrder)
            where T : class
        {
            if (source == null) return new Dictionary<string, T>();

            var allKeys = new HashSet<string>(source.Keys.SelectMany(s => source[s].Select(v => v.Key)));

            return allKeys.ToDictionary(k => k, k => source.Search(k, tierOrder));
        }

        public static IDictionary<string, IDictionary<string, T>> CopyTo<T>(this IDictionary<string, IDictionary<string, T>> source, IDictionary<string, IDictionary<string, T>> destination, string level)
            where T : class
        {
            if (source == null) return destination;
            if (destination == null)
                destination = new Dictionary<string, IDictionary<string, T>>();

            if (source.ContainsKey(level))
                destination.Add(level, source[level]);

            return destination;
        }
    }
}
