using DbContextSaveChangesResolver;
using DbContextSaveChangesResolver.Services;
using DbFirstTestProject.DataLayer.Context;
using DbFirstTestProject.DataLayer.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Reflection;

namespace SaveChangesResolverTest
{
    public class Tests
    {
        private EntityProjectContext context;
        private SaveChangesResolver resolver;
        private bool IntegrationTesting = true;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            if (!IntegrationTesting)
            {
                DbContextOptionsBuilder<EntityProjectContext> optionsBuilder = new DbContextOptionsBuilder<EntityProjectContext>();
                var connection = new SqliteConnection("Data Source=InMemorySample;Mode=Memory;Cache=Shared");
                connection.Open();
                optionsBuilder.UseSqlite(connection);
                context = new EntityProjectContext(optionsBuilder.Options);
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
            }
            else
            {
                context = new EntityProjectContext();
            }
        }

        private void InsertTable<T>(int i, int j)
        {
            if(typeof(T) == typeof(Table1))
                resolver.DeferUpsert(new Table1() { Col1_PK = IntegrationTesting ? 0 : i, Col2 = i.ToString(), Col4 = "" });
            if(typeof(T) == typeof(Table2))
                resolver.DeferUpsert(new Table2() { Col1_PK = IntegrationTesting ? 0 : j, Col2_FK = j, Col3_Value = "", Col4_Extra = "", Col5_Extra = i - j });
            //if(typeof(T) == typeof(Table3))
            //if(typeof(T) == typeof(Table4))
        }

        [SetUp]
        public void SetUp()
        {
            hashSetCount = 0;
            resolver = new SaveChangesResolver(context);
        }

        [Test]
        public void PrintTest1()
        {
            Console.Write(resolver.PrintDependencyGraph(false));
        }
        
        [Test]
        public void PrintTest2()
        {
            Console.Write(resolver.PrintDependencyGraph(true));
        }

        [Test]
        public void PrintDependencyOrder()
        {
            Console.Write(resolver.PrintDependencyOrder());
        }

        private void AddToHashSet(HashSet<object> hashset, object obj)
        {
            int prev = hashset.Count();
            hashset.Add(obj);
            hashSetCount = (prev == hashset.Count()) ? hashSetCount : ++hashSetCount;
        }

        [Test]
        public void PrimaryKeysComparer()
        {
            IDictionary<Type, List<PropertyInfo>> properties = new Dictionary<Type, List<PropertyInfo>>();
            Type type = typeof(Table1);
            properties.Add(type, new List<PropertyInfo>());
            properties[type].Add(type.GetProperty(nameof(Table1.Col1_PK)));

            IEqualityComparer<object> keysComparer = new PrimaryKeysComparer(properties);
            HashSet<object> hashset = new HashSet<object>(keysComparer);

            var a = new Table1() { Col1_PK = 1 };
            var b = new Table1() { Col1_PK = 2 };
            AddToHashSet(hashset, a);
            AddToHashSet(hashset, b);
            Assert.AreEqual(2, hashset.Count()); //only key properties are evaulated

            var c = new Table1() { Col1_PK = 1 };
            AddToHashSet(hashset, c);
            Assert.AreEqual(2, hashset.Count()); //new object isn't added since key properties are compared

            a.Col4 = "test";
            AddToHashSet(hashset, a);
            Assert.AreEqual(2, hashset.Count()); //non-key properties aren't evaluated

            a.Col1_PK = 5;
            AddToHashSet(hashset, a);
            Assert.AreEqual(3, hashset.Count()); //same object, but different key value is evaluated

            reuseableHashSet = hashset;
            reuseableProperties = properties;
        }

        private HashSet<object> reuseableHashSet;
        private IDictionary<Type, List<PropertyInfo>> reuseableProperties;
        private int hashSetCount;
        [Test]
        public void PrimaryKeyComparer_MultipleTypes()
        {
            PrimaryKeysComparer();
            var hashset = reuseableHashSet;
            var properties = reuseableProperties;

            Type type = (typeof(Table2));
            properties.Add(type, new List<PropertyInfo>());
            properties[type].Add(type.GetProperty(nameof(Table2.Col1_PK)));
            properties[type].Add(type.GetProperty(nameof(Table2.Col2_FK)));

            var a = new Table2() { Col1_PK = 1 };
            var b = new Table2() { Col1_PK = 2 };

            Assert.AreEqual(3, hashset.Count());

            AddToHashSet(hashset, a);
            AddToHashSet(hashset, b);
            Assert.AreEqual(5, hashset.Count());
        }

        [Test]
        public void DeferUpsert_Print()
        {
            resolver.DeferUpsert(new Table1() { Col1_PK = 1, Col2 = "1" });
            resolver.DeferUpsert(new Table1() { Col1_PK = 1, Col2 = "2" });
            resolver.DeferUpsert(new Table1() { Col1_PK = 2, Col2 = "2" });
            resolver.DeferUpsert(new Table2() { Col1_PK = 3, Col2_FK = 4 });
            
            Console.WriteLine(resolver.PrintTotalItemsDeferred(false));
            
            Assert.AreEqual(2, resolver.GetTotalItemsDeferredByType(typeof(Table1)));
            Assert.AreEqual(1, resolver.GetTotalItemsDeferredByType(typeof(Table2)));
            
            Console.WriteLine(resolver.PrintTotalItemsDeferred(true));
        }

        [Test]
        public void BulkDeferUpsert_EntitiesTracked()
        {
            Console.WriteLine($"Entities tracked: {context.ChangeTracker.Entries().Count()}");
            BulkDeferUpsert(19000);
            Console.WriteLine($"Entities tracked: {context.ChangeTracker.Entries().Count()}");
        }

        [TestCase(100000, true)]
        public void BulkDeferUpsert(int totalInserts, bool testcase = false)
        {
            int start = (int)context.Table1.Min(x => x.Col1_PK);
            int end = (int)context.Table1.Max(x => x.Col1_PK);
            int j = (int)start+1;
            for (int i = 0; i < totalInserts; i++)
            {
                if (j == end)
                    j = (int)start;
                InsertTable<Table1>(i, j);
                InsertTable<Table2>(i, j);
                j++;
            }

            Console.WriteLine(resolver.PrintTotalItemsDeferred(false));
        }

        [TestCase(1000)]
        [TestCase(10000)]
        [TestCase(100000)]
        [TestCase(230000)]
        public async Task BulkInsertAsync(int bulksize = 0)
        {
            if(bulksize > 0)
            {
                Console.WriteLine("Deferring bulk insert");
                BulkDeferUpsert(bulksize);
            }
            else
            {
                Console.WriteLine("Before bulk insert");
                BulkDeferUpsert(0);
            }
            
            await resolver.BulkUpsertAsync();

            Console.WriteLine("After bulk insert");
            BulkDeferUpsert(0);
        }

        [Test]
        public async Task BulkInsert_EntitiesTracked()
        {
            var entries = context.ChangeTracker.Entries().Count();
            Assert.AreEqual(0, entries);
            Console.WriteLine($"Entities tracked: {entries}");
            
            BulkDeferUpsert(19000);

            Stopwatch sw = Stopwatch.StartNew();
            await BulkInsertAsync(0);
            sw.Stop();

            Console.WriteLine($"Bulk insertion operation time ms: {sw.ElapsedMilliseconds}");

            entries = context.ChangeTracker.Entries().Count();
            Console.WriteLine($"Entities tracked: {entries}");
            Assert.AreEqual(0, entries);
        }

        [TestCase(5004, 1)]
        [TestCase(5004, 5)]
        public async Task BulkInsert_Successive(int bulksize, int iterations)
        {
            Console.WriteLine();
            for(int i = 0; i < iterations; i++)
            {
                Console.WriteLine($"Iteration: {i+1}/{iterations}");
                BulkDeferUpsert(bulksize);
                Console.WriteLine($"START Print bulk set");
                Console.WriteLine(resolver.PrintTotalItemsDeferred(true));
                Console.WriteLine($"END Print bulk set");
                Console.WriteLine($"Deferring additional batch of {bulksize} items");
                await BulkInsertAsync(bulksize);
            }
            Console.WriteLine($"START Print bulk set");
            Console.WriteLine(resolver.PrintTotalItemsDeferred(true));
            Console.WriteLine($"END Print bulk set");
        }

        public async Task BulkUpdate(int bulksize)
        {

        }


        private void InsertTableDbContext<T>(int i, int j) where T : class
        {
            if (typeof(T) == typeof(Table1))
                context.Table1.Add(new Table1() { Col1_PK = IntegrationTesting ? 0 : i, Col2 = i.ToString(), Col4 = "" });
            if (typeof(T) == typeof(Table2))
                context.Table2.Add(new Table2() { Col1_PK = IntegrationTesting ? 0 : j, Col2_FK = j, Col3_Value = "", Col4_Extra = "", Col5_Extra = i - j });
        }

        [TestCase(1000)]
        [TestCase(10000)]
        [TestCase(100000)]
        [TestCase(230000)]
        public async Task DbContext_SaveChanges(int totalInserts)
        {
            int start = (int)context.Table1.Min(x => x.Col1_PK);
            int end = (int)context.Table1.Max(x => x.Col1_PK);
            int j = (int)start + 1;
            Console.WriteLine($"Entities tracked: {context.ChangeTracker.Entries().Count()}");
            for (int i = 0; i < totalInserts; i++)
            {
                if (j == end)
                    j = (int)start;
                InsertTableDbContext<Table1>(i, j);
                InsertTableDbContext<Table2>(i, j);
                j++;
            }
            Console.WriteLine($"Entities tracked: {context.ChangeTracker.Entries().Count()}");
            context.SaveChanges();
            context.ChangeTracker.Clear();
        }
    }
}