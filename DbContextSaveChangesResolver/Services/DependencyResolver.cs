using GraphLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbContextSaveChangesResolver.Services
{
    public class DependencyResolver
    {
        private Graph Graph;
        private List<Vertex> ExecutionOrder;
        public DependencyResolver(Graph graph)
        {
            this.Graph = graph;
            this.ExecutionOrder = new List<Vertex>();
        }

        public void ResolveDependencies()
        {
            foreach (var vertex in Graph.Vertices.OrderBy(x => x.Name))
                DijkstraTraverseAndBuildDependencies(vertex);
        }

        public void DijkstraTraverseAndBuildDependencies(Vertex vertex)
        {
            if (vertex == null)
                return;
            if (!vertex.AdjacentVertices.Any())
                ExecutionOrder.Add(vertex);
            foreach(var adjVertex in vertex.AdjacentVertices.OrderBy(x => x.Name))
                DijkstraTraverseAndBuildDependencies(adjVertex);
        }

        public override string ToString()
        {
            return string.Join(", ", ExecutionOrder);
        }
    }
}