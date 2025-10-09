
using System.Runtime.CompilerServices;

namespace eSyncMate.Processor.Models
{
    public class SCS_ProductTypeAttributeReponseModel
    {
        public string taxonomy_id { get; set; }
        public string attribute_id { get; set; }
        public string[] targets { get; set; }
        public bool required { get; set; }
        public int max_selections { get; set; }
        public bool allow_value_requests { get; set; }
        public string id { get; set; }
        public DateTime created { get; set; }
        public DateTime last_modified { get; set; }
        public Taxonomy taxonomy { get; set; }
        public AttributeDetails attribute { get; set; }

        public class Taxonomy
        {
            public string id { get; set; }
            public string name { get; set; }
            public string status { get; set; }
            public string type { get; set; }
            public Breadcrumb[] breadcrumbs { get; set; }
        }

        public class Breadcrumb
        {
            public string id { get; set; }
            public string name { get; set; }
            public string status { get; set; }
            public string type { get; set; }
        }

        public class AttributeDetails
        {
            public string id { get; set; }
            public string name { get; set; }
            public string description { get; set; }
            public string notes { get; set; }
            public string type { get; set; }
            public string attribute_group { get; set; }
            public string attribute_level { get; set; }
            public string mapped_property { get; set; }
        }
    }
}
