namespace Documents.Store.SqlServer.Entities
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("Privilege")]
    public class Privilege
    {
        public long PrivilegeID { get; set; }

        [StringLength(25)]
        public string Tier { get; set; }

        [StringLength(25)]
        public string Type { get; set; }

        [StringLength(25)]
        public string OverrideKey { get; set; }

        [StringLength(100)]
        public string Identifier { get; set; }
    }
}
