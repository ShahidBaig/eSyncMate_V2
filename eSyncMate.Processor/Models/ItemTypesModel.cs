namespace eSyncMate.Processor.Models
{
    public class ItemTypesModel
    {
    }

    public class ItemTypesDataModel
    {
        public int ID { get; set; }
        public string Brand { get; set; }
        public string Product_Subtype { get; set; }
        public string Item_Type { get; set; }
        public string Item_Type_Id { get; set; }
        public string Item_Type_Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int ModifiedBy { get; set; }

    }
}
