using GraphLibrary;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace DbContextSaveChangesResolver
{
    public class SaveChangesResolver
    {
        private Graph _graph;
        public Graph Graph { get { return _graph; } }
        private DbContext context;
        private IEnumerable<IEntityType> contextTypes;

        public SaveChangesResolver(DbContext context)
        {
            this.context = context;
            contextTypes = new List<IEntityType>();
            _graph = new Graph();
        }

        public void CreateTypeDependencyGraph()
        {
            var properties = context.GetType().GetProperties();
            contextTypes = context.Model.GetEntityTypes();
            foreach (var type in contextTypes)
            {
                EntityMetadata metaData = new EntityMetadata() { Type = type.ClrType };
                Graph.AddVertex(type.ClrType.Name, metaData);
            }

            foreach (var entityType in contextTypes)
                CreateEdges(entityType);
        }

        private void CreateEdges(IEntityType type)
        {
            var entity = Activator.CreateInstance(type.ClrType);
            var entry = context.Entry(entity);
            //IEnumerable<PropertyMetadata> PrimaryKeys = entry.Metadata.FindPrimaryKey()?.Properties.Select(x => new PropertyMetadata() { PropertyName = x.Name, DataType = x.ClrType }) ?? new List<PropertyMetadata>().AsEnumerable();
            IEnumerable<PropertyMetadataReference> ForeignKeys = entry.Metadata.GetForeignKeys().Select(x =>
                new PropertyMetadataReference()
                {
                    ForeignKeyProperty = new PropertyMetadata()
                    {
                        PropertyName = x.Properties.First().Name,
                        DataType = x.Properties.First().ClrType
                    },
                    ReferencingType = x.PrincipalEntityType.ClrType,
                    ReferencingProperty = new PropertyMetadata()
                    {
                        PropertyName = x.PrincipalKey.Properties.First().Name,
                        DataType = x.PrincipalKey.Properties.First().ClrType
                    }
                });
            foreach(var FKMetaData in ForeignKeys)
            {
                var ForeignKey = Graph.AddVertex(type.ClrType.Name); //ClassName
                var Reference = Graph.AddVertex(FKMetaData.ReferencingType.Name); //Vertices holds list of property names and whether they are primary keys
                Type from = type.ClrType; Type to = FKMetaData.ReferencingType;
                InitializeVertex(ForeignKey, from, to, FKMetaData.ForeignKeyProperty, FKMetaData.ReferencingProperty, null, null, out Guid ID);
                InitializeVertex(Reference, from, to, null, null, FKMetaData.ForeignKeyProperty, ID, out Guid _);
                var Edge = Graph.AddEdge(ForeignKey, Reference, DirectionState.AtoB);
            }
        }

        private void InitializeVertex(Vertex vertex, Type from, Type to, PropertyMetadata foreignKeys, PropertyMetadata referencingKeys, PropertyMetadata referencedKeys, Guid? ID, out Guid ReferenceID)
        {
            var metadata = (EntityMetadata?)vertex.Value ?? new EntityMetadata();
            metadata.ForeignKeys = metadata.ForeignKeys ?? new List<PropertyMetadata>();
            metadata.ReferencingKeys = metadata.ReferencingKeys ?? new List<PropertyMetadata>();
            metadata.ReferencedKeys = metadata.ReferencedKeys ?? new List<PropertyMetadata>();
            ReferenceID = ID ?? Guid.NewGuid();
            AddToList(metadata.ForeignKeys, foreignKeys, ReferenceID);
            AddToList(metadata.ReferencingKeys, referencingKeys, ReferenceID);
            AddToList(metadata.ReferencedKeys, referencedKeys, ReferenceID);
            if(foreignKeys != null)
                foreignKeys.ClassType = from;
            if (referencingKeys != null)
                referencingKeys.ClassType = to;
            if(referencedKeys != null)
                referencedKeys.ClassType = from;
            vertex.Value = metadata;
        }

        private void AddToList(List<PropertyMetadata> list, PropertyMetadata item, Guid guid)
        {
            if (item != null)
            {
                item.ReferenceID = guid;
                list.Add(item);
            }
        }

        public string ToString(bool PrintVertexValues = true)
        {
            if (!PrintVertexValues)
                return Graph.ToString();
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var Vertex in Graph.Vertices.OrderBy(x => x.Name))
            {
                stringBuilder.AppendLine(((EntityMetadata)Vertex.Value).ToString()); 
            }
            return stringBuilder.ToString();
        }
    }

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
            foreach(var FK in ForeignKeys)
            {
                var Ref = ReferencingKeys.First(x => x.ReferenceID == FK.ReferenceID);
                stringBuilder.AppendLine($"\t{FK.PropertyName.PadLeft(20)} -> {Ref.ClassType.Name}.{Ref.PropertyName.PadRight(20)}");
            }
            return stringBuilder.ToString();
        }
    }

    public class PropertyMetadataReference
    {
        public PropertyMetadata ForeignKeyProperty {  get; set; }
        public Type ReferencingType { get; set; }
        public PropertyMetadata ReferencingProperty {  get; set; }
    }

    public class PropertyMetadata
    {
        public Type ClassType { get; set; }
        public string PropertyName { get; set; }
        public Type DataType { get; set; }
        public Guid ReferenceID { get; set; }
    }
}