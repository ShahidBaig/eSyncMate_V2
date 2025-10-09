namespace eSyncMate.Processor.Models
{
    public class ConnectorModel
    {
        public int Id { get; set; }
    }

    public class ConnectorsDataModel
    {
        public int Id { get; set; }

        public int TypeId { get; set; }

        public string Name { get; set; }

        public string Data { get; set; }
        public string ConnectorType { get; set; }
        public string Party { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
    }

    public class SaveConnectorsDataModel
    {
        public int Id { get; set; }

        public int TypeId { get; set; }

        public string Name { get; set; }

        public string Data { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
    }

    public class UpdateConnectorsDataModel
    {
        public int Id { get; set; }

        public int TypeId { get; set; }

        public string Name { get; set; }

        public string Data { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int ModifiedBy { get; set; }
    }

    public class ConnectorSearchModel
    {
        public string SearchOption { get; set; }
        public string SearchValue { get; set; }
    }
}
