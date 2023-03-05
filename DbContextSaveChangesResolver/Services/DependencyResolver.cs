using GraphLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace DbContextSaveChangesResolver.Services
{
    public class DependencyResolver
    {
        private Graph Graph;
        private List<Vertex> _ExecutionOrder;
        public IEnumerable<string> ExecutionOrder { get { return _ExecutionOrder.Select(x => x.Name); } }
        public DependencyResolver(Graph graph)
        {
            this.Graph = graph;
            this._ExecutionOrder = new List<Vertex>();
            ResolveDependencies();
        }

        private void ResolveDependencies()
        {
            foreach (var vertex in Graph.Vertices.OrderBy(x => x.Name))
                DijkstraTraverseAndBuildDependencies(vertex);
        }

        private void DijkstraTraverseAndBuildDependencies(Vertex vertex)
        {
            if (vertex == null)
                return;
            if (!vertex.AdjacentVertices.Any())
                _ExecutionOrder.Add(vertex);
            foreach(var adjVertex in vertex.AdjacentVertices.OrderBy(x => x.Name))
                DijkstraTraverseAndBuildDependencies(adjVertex);
        }
    }
}