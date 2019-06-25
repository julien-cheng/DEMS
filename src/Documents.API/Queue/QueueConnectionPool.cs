namespace Documents.API.Queue
{
    using Documents.Queues.Interfaces;
    using Microsoft.Extensions.Caching.Memory;
    using System;

    public static class QueueConnectionPool
    {
        private static readonly MemoryCache MemoryCache = null;
        private static IMessageQueue InternalQueue = null;

        static QueueConnectionPool()
        {
            MemoryCache = new MemoryCache(new MemoryCacheOptions
            {
                
            });
        }

        public static IMessageQueue Queue
        {
            get
            {
                return InternalQueue;
            }
        }

        public static object GetContext(string config, string queueTypeString)
        {
            var configHash = (config + queueTypeString).GetHashCode();

            InternalQueue = MemoryCache.GetOrCreate(queueTypeString, i => {
                return Activator.CreateInstance(Type.GetType(queueTypeString)) as IMessageQueue;
            });

            var context = MemoryCache.GetOrCreate(configHash, i => {
                object ctx = null;

                ctx = InternalQueue.CreateContext(config);
                return ctx;
            });

            return context;
        }
    }
}
