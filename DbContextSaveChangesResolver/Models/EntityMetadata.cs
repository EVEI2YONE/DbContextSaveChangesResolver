using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbContextSaveChangesResolver.Models
{
    public class EntityMetadata
    {
        public Type Type { get; set; }
        public List<PropertyMetadata> ForeignKeys { get; set; }
        public List<PropertyMetadata> ReferencingKeys { get; set; }
        public List<PropertyMetadata> ReferencedKeys { get; set; }
        public string DependencyOrder { get; set; }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"{Type.Name}:");
            foreach (var FK in ForeignKeys)
            {
                var Ref = ReferencingKeys.First(x => x.ReferenceID == FK.ReferenceID);
                stringBuilder.AppendLine($"\t{FK.PropertyName.PadLeft(20)} -> {Ref.ClassType.Name}.{Ref.PropertyName.PadRight(50)}");
            }
            return stringBuilder.ToString();
        }
    }
}
