
namespace Documents.Clients.Manager.Models
{
    using System.Collections.Generic;

    public interface IHaveAttributes
    {
        Dictionary<string, object> Attributes { get; set; }
    }
}
