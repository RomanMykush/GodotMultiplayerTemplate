using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SteampunkDnD.Shared;

public class EntityStateIdComparer : IEqualityComparer<EntityState>
{
    public bool Equals(EntityState x, EntityState y) => x.EntityId == y.EntityId;

    public int GetHashCode([DisallowNull] EntityState obj) => (int)obj.EntityId;
}
