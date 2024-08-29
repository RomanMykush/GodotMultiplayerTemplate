using Godot;
using System;
using System.Collections.Generic;

namespace SteampunkDnD.Shared;

public partial class EntityContainer : Node
{
    // Dictionary with keys corresponding to entity id and values to entities which derives from Node or successive type
    private readonly Dictionary<uint, IEntity> Entities = new();

    public IEntity Get(uint entityId) => Entities[entityId];

    public IEnumerable<IEntity> GetAll() => Entities.Values;

    public bool Contains(uint entityId) => Entities.ContainsKey(entityId);

    public void Add(IEntity entity)
    {
        // Validation
        if (Entities.ContainsKey(entity.EntityId))
        {
            Logger.Singleton.Log(LogLevel.Error, $"Tried to add entity, id of which already exist in dictionary");
            return;
        }
        if (entity is not Node node)
        {
            Logger.Singleton.Log(LogLevel.Error, $"Tried to add entity which does not derive from {nameof(Node)}");
            return;
        }

        // Adding
        Entities.Add(entity.EntityId, entity);
        AddChild(node);
    }

    public IEntity Splice(uint entityId)
    {
        if (!Entities.TryGetValue(entityId, out IEntity entity))
        {
            Logger.Singleton.Log(LogLevel.Error, $"Tried to remove entity, id of which was not found in dictionary");
            return null;
        }
        Entities.Remove(entityId);
        return entity;
    }

    public void Clear() => Entities.Clear();

    public void Delete(uint entityId)
    {
        if (Splice(entityId) is Node node)
            node.QueueFree();
    }

    public void DeleteAll()
    {
        foreach (var entity in Entities.Values)
        {
            Entities.Remove(entity.EntityId);
            var node = entity as Node;
            node.QueueFree();
        }
    }
}
