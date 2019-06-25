namespace Documents.Queues.Interfaces
{
    using System;
    using System.Threading.Tasks;

    public interface ISubscription : IDisposable
    {
        Task AckMessageAsync(IMessage message, bool success);
        Task Disconnect();
    }
}