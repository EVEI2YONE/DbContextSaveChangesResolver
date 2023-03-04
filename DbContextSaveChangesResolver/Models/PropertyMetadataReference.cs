using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbContextSaveChangesResolver.Models
{
    public class PropertyMetadataReference
    {
        public PropertyMetadata ForeignKeyProperty { get; set; }
        public Type ReferencingType { get; set; }
        public PropertyMetadata ReferencingProperty { get; set; }
    }
}
