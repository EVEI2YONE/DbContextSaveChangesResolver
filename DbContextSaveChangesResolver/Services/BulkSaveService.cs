using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DbContextSaveChangesResolver.Services
{
    public class BulkSaveService
    {
        private DependencyResolver DependencyResolver;
        private IDictionary<Type, HashSet<object>> DeferredData;
        private IDictionary<Type, HashSet<string>> PrimaryKeys;
        private IDictionary<Type, List<PropertyInfo>> PrimaryKeyProperties;
        public BulkSaveService(DependencyResolver Resolver, IDictionary<Type, List<string>> PKs)
        {
            this.PrimaryKeys = PKs.ToDictionary(x => x.Key, x => new HashSet<string>(PKs[x.Key]));
            PrimaryKeyProperties = PrimaryKeys.ToDictionary(x => x.Key, x => x.Key.GetProperties().Where(pkProp => PrimaryKeys[x.Key].Any(y => y.Equals(pkProp.Name))).ToList());
            this.DependencyResolver = Resolver;
            this.DeferredData = new Dictionary<Type, HashSet<object>>();
            foreach (var key in PKs.Keys)
                this.DeferredData.TryAdd(key, new HashSet<object>(new PrimaryKeysComparer(PrimaryKeyProperties)));
        }

        public void DeferSave<T>(T Entity) where T : class
        {
            Type type = typeof(T);
            if (DeferredData.ContainsKey(type))
                Upsert(Entity);
        }

        private void Upsert<T>(T Entity) where T : class
        {
            Type entityType = typeof(T);
            if (PrimaryKeys.ContainsKey(entityType))
            {
                var other = (T?) FindEntity(DeferredData[entityType], PrimaryKeys[entityType], Entity);
                if(other != null)
                {
                    DeferredData[entityType].Remove(other);
                    Entity = other;
                }
            }
            DeferredData[entityType].Add(Entity);
        }

        private T? FindEntity<T>(HashSet<T> List, HashSet<string> PrimaryKeys, T Entity) where T : class
        {
            DeferredData[typeof(T)].TryGetValue(Entity, out object value);
            return (T?)value;
        }

        public void BulkSave()
        {

        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            long itemsToSave = DeferredData.Sum(x => x.Value.Count);
            stringBuilder.AppendLine($"Total deferred items to save: {itemsToSave}");
            foreach (var Key in DeferredData.Keys)
                stringBuilder.AppendLine($"\t{Key.Name}: {DeferredData[Key].Count()}");
            return stringBuilder.ToString();
        }
    }
}
