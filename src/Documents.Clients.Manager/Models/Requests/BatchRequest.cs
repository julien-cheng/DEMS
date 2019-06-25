namespace Documents.Clients.Manager.Models.Requests
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class BatchRequest
    {
        [Required]
        public IEnumerable<ModelBase> Operations { get; set; }
    }
}
