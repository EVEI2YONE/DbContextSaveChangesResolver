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

        public void DeferUpsert(object obj)
        {
            bulkSaveService.DeferUpsert(obj);
        }

        public void BulkUpsert()
        {
            bulkSaveService.BulkUpsert();
        }

        public string PrintTotalItemsDeferred()
        {
            return bulkSaveService.PrintTotalItemsDeferred();
        }

        public string PrintDependencyGraph(bool PrintVertexValues = true)
        {
            return initializerService.PrintDependencyGraph(PrintVertexValues);
        }

        public string PrintDependencyOrder()
        {
            return initializerService.PrintDependencyOrder();
        }


    }
}