using DbContextSaveChangesResolver.Models;
using DbContextSaveChangesResolver.Services;
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
        private InitializerService initializerService;

        public SaveChangesResolver(DbContext context)
        {
            this.context = context;
            this.contextTypes = new List<IEntityType>();
            this._graph = new Graph();
            this.initializerService = new InitializerService(Graph, context);
        }

        public void CreateTypeDependencyGraph()
        {
            if (context == null)
                return;
            initializerService.CreateTypeDependencyGraph();
            contextTypes = initializerService.GetContextTypes();
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
}