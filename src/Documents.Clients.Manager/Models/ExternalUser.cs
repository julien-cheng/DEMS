using System;
using System.Collections.Generic;
using System.Text;

namespace Documents.Clients.Manager.Models
{
    public class ExternalUser : ModelBase
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string MagicLink { get; set; }
        public DateTime ExpirationDate { get; set; }
    }
}
