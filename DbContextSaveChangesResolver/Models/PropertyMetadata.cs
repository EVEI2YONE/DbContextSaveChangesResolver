using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbContextSaveChangesResolver.Models
{
    public class PropertyMetadata
    {
        public Type ClassType { get; set; }
        public string PropertyName { get; set; }
        public Type DataType { get; set; }
        public Guid ReferenceID { get; set; }
    }
}
