﻿using DbContextSaveChangesResolver.Models;
using GraphLibrary;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DbContextSaveChangesResolver.Services
{
    public class GraphDependencyBuilderService
    {
        private Graph Graph;
        private DbContext context;
        private IEnumerable<IEntityType> contextTypes;
        private Dictionary<Type, List<string>> _contextPrimaryKeys;
        public Dictionary<Type, List<string>> ContextPrimaryKeys { get { return _contextPrimaryKeys;} }

        public GraphDependencyBuilderService(Graph graph, DbContext context)
        {
            this.Graph = graph;
            this.context = context;
            this._contextPrimaryKeys = new Dictionary<Type, List<string>>();
            CreateTypeDependencyGraph();
        }

        private void CreateTypeDependencyGraph()
        {
            var properties = context.GetType().GetProperties();
            contextTypes = context.Model.GetEntityTypes();
            foreach (var type in contextTypes)
            {
                _contextPrimaryKeys.Add(type.ClrType, new List<string>());
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
            var primaryKeyProperties = entry.Metadata.FindPrimaryKey()?.Properties;
            if(primaryKeyProperties != null && primaryKeyProperties.Any())
                _contextPrimaryKeys[type.ClrType].AddRange(primaryKeyProperties.Select(x => x.Name));
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
            foreach (var FKMetaData in ForeignKeys)
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
            if (foreignKeys != null)
                foreignKeys.ClassType = from;
            if (referencingKeys != null)
                referencingKeys.ClassType = to;
            if (referencedKeys != null)
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
    }
}
