namespace Documents.Store.SqlServer.Entities
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("UserAccessIdentifier")]
    public class UserAccessIdentifier
    {
        public long UserAccessIdentifierID { get; set; }

        [StringLength(100)]
        public string Identifier { get; set; }
    }
}