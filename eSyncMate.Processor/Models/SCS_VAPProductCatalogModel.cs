namespace eSyncMate.Processor.Models
{
    public class SCS_VAPProductCatalogModel
    {
        public Parent parent { get; set; }
        public List<VCChild>  children { get; set; }

        public SCS_VAPProductCatalogModel()
        {
            this.children = new List<VCChild>();  
        }

        public class Parent
        {
            public string external_id { get; set; }
            public string relationship_type { get; set; }
            public List<ParentField> fields { get; set; }

            public Parent()
            {
                this.fields = new List<ParentField>();
            }
        }

        public class ParentField
        {
            public string name { get; set; }
            public string value { get; set; }
        }

        public class VCChild
        {
            public string external_id { get; set; }
            public string relationship_type { get; set; }
            public List<ChildField> fields { get; set; }

            public VCChild() 
            { 
                this.fields = new List<ChildField>();       
            }
        }

        public class ChildField
        {
            public string name { get; set; }
            public string value { get; set; }
        }

    }
}
