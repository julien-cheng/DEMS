namespace Documents.Clients.Manager.Models.Responses
{
    using System.Collections.Generic;

    public class GridColumnSpecification
    {
        public string KeyName { get; set; }
        public string Label { get; set; }
        public bool IsSortable { get; set; }

        public GridColumnSpecification() { }

        public GridColumnSpecification(string keyName, string label, bool isSortable)
        {
            this.KeyName = keyName;
            this.Label = label;
            this.IsSortable = isSortable;
        }

        public static GridColumnSpecification GetNameColumn()
        {
            return new GridColumnSpecification("name", "Name", true);
        }

        public static GridColumnSpecification GetCustomNameColumn()
        {
            return new GridColumnSpecification("customName", "Description", true);
        }

        public static GridColumnSpecification GetActionsColumn()
        {
            return new GridColumnSpecification("allowedOperations", "Actions", false);
        }

        public static List<GridColumnSpecification> GetStandarSetOfColumns()
        {
            return new List<GridColumnSpecification>() {
                GetNameColumn(),
                new GridColumnSpecification("modified", "Date Modified", true),
                new GridColumnSpecification("viewerType", "Type", true),
                new GridColumnSpecification("lengthForHumans", "Size", true),
                GetActionsColumn(),
            };
        }
    }
}
