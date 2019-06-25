using System;
using System.Collections.Generic;
using System.Text;

namespace Documents.Clients.Manager.Models.eDiscovery.Responses
{
    public class EDiscoveryStatisticsResponse
    {
        public int FilesStaged { get; set; }
        public int FilesPublished { get; set; }
        public int RecipientCount { get; set; }
        public bool IsEDiscoveryActive { get; set; }
    }
}
