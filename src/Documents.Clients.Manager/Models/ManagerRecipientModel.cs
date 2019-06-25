namespace Documents.Clients.Manager.Models
{
    using System;
    using System.Collections.Generic;

    public class ManagerRecipientModel : ModelBase, IItemQueryResponse
    {
        public string Key { get; set; }
        public string Name
        {
            get
            {
                return FirstName + ' ' + LasttName;
            }
            set { }
        }
        public string FirstName { get; set; }
        public string LasttName { get; set; }
        public string Email { get; set; }
        public string MagicLink { get; set; }
        public string PasswordHash { get; set; }
        public DateTime ExpirationDate { get; set; }

        public DateTime? Created { get; set; }
        public DateTime? Modified { get; set; }

        public IEnumerable<AllowedOperation> AllowedOperations { get; set; }

        public Dictionary<string, object> DataModel { get; set; }
    }
}