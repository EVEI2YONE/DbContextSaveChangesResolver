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
        private DbContext context;
        private InitializerService initializerService;
        private BulkSaveService bulkSaveService;

        public SaveChangesResolver(DbContext context)
        {
            this.context = context;
            this.initializerService = new InitializerService(context);
            this.bulkSaveService = initializerService.GetBulkSaveService();
            
        }

        public Graph GetGraph()
            => initializerService.Graph;

        public void DeferUpsert<T>(T obj) where T : class
            => bulkSaveService.DeferUpsert(obj);

        public Task BulkUpsert()
            => bulkSaveService.BulkUpsert();

        public string PrintTotalItemsDeferred(bool PrintBulkSets = false)
            => bulkSaveService.PrintTotalItemsDeferred(PrintBulkSets);

        public int GetTotalItemsDeferredByType(Type type)
            => bulkSaveService.GetTotalItemsDeferredByType(type);

        public string PrintDependencyGraph(bool PrintVertexValues = true)
            => initializerService.PrintDependencyGraph(PrintVertexValues);

        public string PrintDependencyOrder()
            => initializerService.PrintDependencyOrder();
    }
}