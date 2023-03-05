using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Contracts;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DbContextSaveChangesResolver.Services
{
    public class BulkSaveService
    {
        private IEnumerable<Type> ExecutionOrder;
        private List<IDictionary<Type, HashSet<object>>> DeferredData;
        private List<IDictionary<Type, HashSet<object>>> DeferredDataBuffer;
        private object BufferSwapLock = new object();
        private object BulkOperations = new object();
        private IDictionary<Type, HashSet<string>> PrimaryKeys;
        private IDictionary<Type, List<PropertyInfo>> PrimaryKeyProperties;
        private DbContext Context;
        private static ConcurrentDictionary<Type, dynamic> GenericDbSets = new ConcurrentDictionary<Type, dynamic>();
        public BulkSaveService(IEnumerable<string> ExecutionOrder, IDictionary<Type, List<string>> PKs, DbContext Context)
        {
            this.PrimaryKeys = PKs.ToDictionary(x => x.Key, x => new HashSet<string>(PKs[x.Key]));
            PrimaryKeyProperties = PrimaryKeys.ToDictionary(x => x.Key, x => x.Key.GetProperties().Where(pkProp => PrimaryKeys[x.Key].Any(y => y.Equals(pkProp.Name))).ToList());
            this.ExecutionOrder = PKs.Keys.Where(x => ExecutionOrder.Contains(x.Name));
            this.DeferredDataBuffer = InitializeDeferredDataList();
            this.DeferredData = InitializeDeferredDataList();
            this.Context = Context;
        }

        private List<IDictionary<Type, HashSet<object>>> InitializeDeferredDataList()
        {
            var list = new List<IDictionary<Type, HashSet<object>>>();
            list.Add(CreateDeferredDataBulkSet());
            return list;
        }

        public IDictionary<Type, HashSet<object>> CreateDeferredDataBulkSet()
        {
            var subset = new Dictionary<Type, HashSet<object>>();
            foreach (var type in ExecutionOrder)
                subset.TryAdd(type, new HashSet<object>(new PrimaryKeysComparer(PrimaryKeyProperties)));
            return subset;
        }

        public void DeferUpsert<T>(T Entity) where T : class
        {
            Type type = typeof(T);
            lock (BufferSwapLock)
            {
                if (DeferredData.Any(x => x.ContainsKey(type)))
                    Upsert(Entity);
            }
        }

        private void Upsert<T>(T Entity) where T : class
        {
            Type entityType = typeof(T);
            IDictionary<Type, HashSet<object>> subset = DeferredData.First();
            bool found = false;
            if (PrimaryKeys.ContainsKey(entityType))
            {
                foreach(var dataset in DeferredData)
                {
                    var other = (T?) FindEntity(dataset[entityType], PrimaryKeys[entityType], Entity);
                    if(other != null)
                    {
                        dataset[entityType].Remove(other);
                        subset = dataset;
                        found = true;
                        break;
                    }
                }
            }
            if(!found && GetTotalItemsDeferredByType(entityType, subset) >= bulkThreshold) //new item, but batchsize limit reached
            {
                subset = DeferredData.FirstOrDefault(x => GetTotalItemsDeferredByType(entityType, x) < bulkThreshold); //find batch under batchsize or create new
                if(subset == null)
                {
                    subset = CreateDeferredDataBulkSet();
                    DeferredData.Add(subset);
                }
            }
            subset[entityType].Add(Entity);
        }

        private T? FindEntity<T>(HashSet<object> dataset, HashSet<string> PrimaryKeys, T Entity) where T : class
        {
            dataset.TryGetValue(Entity, out object value);
            return (T?)value;
        }

        private int bulkThreshold = 10000;
        public async Task BulkUpsertAsync()
        {
            lock (BulkOperations)
            {
                lock (BufferSwapLock)
                {
                    var temp = DeferredData;
                    DeferredData = DeferredDataBuffer;
                    DeferredDataBuffer = temp;
                }
            }
            var prevCascadeDeleteOption = Context.ChangeTracker.CascadeDeleteTiming;
            var prevDeleteOrphansOption = Context.ChangeTracker.DeleteOrphansTiming;
            Context.ChangeTracker.CascadeDeleteTiming = CascadeTiming.OnSaveChanges;
            Context.ChangeTracker.DeleteOrphansTiming = CascadeTiming.OnSaveChanges;
            ParallelOptions options = new ParallelOptions() { MaxDegreeOfParallelism = 5 };
            foreach (var type in ExecutionOrder)
            {
                dynamic dbset;
                if(!GenericDbSets.TryGetValue(type, out dbset))
                {
                    Type myType = typeof(InternalDbSet<>).MakeGenericType(type);
                    //instance is only used to spoof the binding
                    dynamic instance = Activator.CreateInstance(myType, Context, type.Name);
                    dbset = FetchContextDbSet(instance);
                    GenericDbSets.TryAdd(type, dbset);
                }

                //i already know the type and dependency order, I can just use a custom sqldataread to upload the data based on properties in this loop
                foreach (var dictionary in DeferredDataBuffer)
                {
                    var dataset = dictionary[type];
                    try
                    {
                        Context.ChangeTracker.AutoDetectChangesEnabled = false;
                        if (dataset.Any())
                            await AddRangeAsync(Context, dataset);
                    }
                    catch (Exception ex)
                    {

                    }
                    finally
                    {
                        Context.ChangeTracker.AutoDetectChangesEnabled = true;
                    }
                    if (dataset.Any())
                    {
                        await Context.SaveChangesAsync();
                        foreach (var item in dataset)
                            Detach(dbset, Convert.ChangeType(item, type));
                    }
                }
            }
            Context.ChangeTracker.CascadeDeleteTiming = prevCascadeDeleteOption;
            Context.ChangeTracker.DeleteOrphansTiming = prevDeleteOrphansOption;
            DeferredDataBuffer.ForEach(dataset =>
            {
                foreach (var key in dataset.Keys)
                    dataset[key].Clear();
            });
            if (DeferredDataBuffer.Count > 1)
                DeferredDataBuffer.RemoveRange(1, DeferredDataBuffer.Count - 1);
        }

        private DbSet<T> FetchContextDbSet<T>(DbSet<T> _) where T : class
            => Context.Set<T>();

        private async Task AddRangeAsync(dynamic context, HashSet<object> items)
            => await context.AddRangeAsync(items);

        private void Detach<T>(InternalDbSet<T> _, object item) where T : class
            => Context.Set<T>().Entry((T)((object)item)).State = EntityState.Detached;

        public int GetTotalItemsDeferredByType(Type type)
        {
            lock(BufferSwapLock)
                return DeferredData.Sum(x => GetTotalItemsDeferredByType(type, x));
        }
        public int GetTotalItemsDeferredByType(Type type, IDictionary<Type, HashSet<object>> dataset)
        => dataset.Where(y => y.Key == type).Sum(y => y.Value.Count);

        public string PrintTotalItemsDeferred(bool PrintBulkSets = false)
        {
            StringBuilder stringBuilder = new StringBuilder();
            long itemsToSave = DeferredData.Sum(x => x.Sum(y => y.Value.Count));
            stringBuilder.AppendLine($"Total deferred items to save: {itemsToSave}");
            if (PrintBulkSets)
            {
                foreach (var dataset in DeferredData)
                {
                    foreach (var type in ExecutionOrder)
                        stringBuilder.AppendLine($"\t{type.Name}: {GetTotalItemsDeferredByType(type, dataset)}");
                    stringBuilder.AppendLine();
                }
            }
            else
            {
                foreach (var type in ExecutionOrder)
                    stringBuilder.AppendLine($"\t{type.Name}: {GetTotalItemsDeferredByType(type)}");
            }
            return stringBuilder.ToString();
        }
    }
}
