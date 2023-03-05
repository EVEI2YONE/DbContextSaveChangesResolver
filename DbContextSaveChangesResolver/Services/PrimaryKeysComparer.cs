using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DbContextSaveChangesResolver.Services
{
    public class PrimaryKeysComparer : IEqualityComparer<object>
    {
        private IDictionary<Type, List<PropertyInfo>> PrimaryKeyProperties;

        public PrimaryKeysComparer(IDictionary<Type, List<PropertyInfo>> PKProperties)
        {
            this.PrimaryKeyProperties = PKProperties;
        }

        public bool Equals(object? x, object? y)
        {
            return x == y || GetHashCode(x) == GetHashCode(y);
        }

        public int GetHashCode([DisallowNull] object obj)
        {
            int hash = 17;
            if (PrimaryKeyProperties.ContainsKey(obj.GetType()))
                hash = CalculateHash(obj.GetType(), hash, obj);
            else
                hash = CalculateHash(obj.GetType().UnderlyingSystemType, hash, obj);
            return hash;
        }

        private int CalculateHash(Type type, int hash, object obj)
        {
            foreach (var property in PrimaryKeyProperties[type])
            {
                var val = property.GetValue(obj);
                if (int.TryParse(val.ToString(), out int PK_Default) && PK_Default == 0)
                    return obj.GetHashCode();
                else
                    hash = hash * 23 + val.GetHashCode();
            }
            return hash;
        }
    }
}
