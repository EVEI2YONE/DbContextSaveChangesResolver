using DbContextSaveChangesResolver;
using DbFirstTestProject.DataLayer.Context;
using DbFirstTestProject.DataLayer.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace SaveChangesResolverTest
{
    public class Tests
    {
        private EntityProjectContext context;
        private SaveChangesResolver resolver;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            DbContextOptionsBuilder<EntityProjectContext> optionsBuilder = new DbContextOptionsBuilder<EntityProjectContext>();
            var connection = new SqliteConnection("Data Source=InMemorySample;Mode=Memory;Cache=Shared");
            connection.Open();
            optionsBuilder.UseSqlite(connection);

            context = new EntityProjectContext(optionsBuilder.Options);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            resolver = new SaveChangesResolver(context);
            resolver.CreateTypeDependencyGraph();
        }

        [Test]
        public void PrintTest1()
        {
            Console.Write(resolver.ToString(false));
        }
        
        [Test]
        public void PrintTest2()
        {
            Console.Write(resolver.ToString());
        }
    }
}