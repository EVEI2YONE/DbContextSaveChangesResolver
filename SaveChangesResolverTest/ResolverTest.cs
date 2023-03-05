using DbContextSaveChangesResolver;
using DbContextSaveChangesResolver.Services;
using DbFirstTestProject.DataLayer.Context;
using DbFirstTestProject.DataLayer.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;

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
            hashSetCount = 0;
            resolver = new SaveChangesResolver(context);
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

        [Test]
        public void PrintDependencyOrder()
        {
            Console.Write(resolver.PrintDependencyOrder());
        }
        
        [Test]
        public void DeferSave()
        {
            resolver.BulkSaveService.DeferSave(new Table1() { Col1_PK = 1, Col2 = "1" });
            resolver.BulkSaveService.DeferSave(new Table1() { Col1_PK = 1, Col2 = "2" });
            Console.Write(resolver.BulkSaveService.ToString());
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
        public void BulkSave()
        {
            DeferSave();
            resolver.BulkSaveService.BulkSave();

        }
    }
}