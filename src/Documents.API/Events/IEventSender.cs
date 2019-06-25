namespace Documents.API.Events
{
    using System.Threading.Tasks;
    using Documents.API.Common.Events;

    public interface IEventSender
    {
        Task SendAsync(EventBase eventObject);
    }
}