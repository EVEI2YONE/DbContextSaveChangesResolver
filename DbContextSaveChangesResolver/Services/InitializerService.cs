using DbContextSaveChangesResolver.Models;
using GraphLibrary;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbContextSaveChangesResolver.Services
{
    internal class InitializerService
    {
        private Graph _graph;
        public Graph Graph { get { return _graph; } }
        private GraphDependencyBuilderService graphDependencyBuilderService;
        private DependencyResolver dependencyResolver;
        private BulkSaveService bulkSaveService;
        private DbContext context;

        public InitializerService(DbContext context)
        {
            this._graph = new Graph();
            this.graphDependencyBuilderService = new GraphDependencyBuilderService(Graph, context);
            this.dependencyResolver = new DependencyResolver(Graph);
            this.bulkSaveService = new BulkSaveService(dependencyResolver.ExecutionOrder, graphDependencyBuilderService.ContextPrimaryKeys, context);
        }

        public BulkSaveService GetBulkSaveService()
        {
            return this.bulkSaveService;
        }

        public string PrintDependencyGraph(bool PrintVertexValues = true)
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

        public string PrintDependencyOrder()
        {
            return string.Join(", ",  dependencyResolver.ExecutionOrder);
        }
    }
}
