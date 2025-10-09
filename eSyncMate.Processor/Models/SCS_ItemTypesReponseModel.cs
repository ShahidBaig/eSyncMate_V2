namespace eSyncMate.Processor.Models
{
    public class SCS_ItemTypesReponseModel
    {
        public Metadata metadata { get; set; }
        public Sections sections { get; set; }

        public class Metadata
        {
            public DateTime date_generated { get; set; }
        }

        public class Sections
        {
            public List<Allowed_Item_Types> allowed_item_types { get; set; }
            public Sections()
            {
                this.allowed_item_types = new List<Allowed_Item_Types>();
            }
        }

        public class Allowed_Item_Types
        {
            public string brand { get; set; }
            public string product_subtype { get; set; }
            public string item_type { get; set; }
            public string item_type_id { get; set; }
            public DateTime last_modified_date { get; set; }
            public float current_referral_rate { get; set; }
            public string item_type_description { get; set; }
        }

    }
}
