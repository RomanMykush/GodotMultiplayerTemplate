using Godot;
using KellermanSoftware.CompareNetObjects;
using KellermanSoftware.CompareNetObjects.TypeComparers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SteampunkDnD.Shared;

public static class StateSnapshotUtils
{
    private static readonly Dictionary<Type, Dictionary<ushort, PropertyInfo>> PropertiesWithIndex = [];
    private static readonly Dictionary<Type, Dictionary<string, ushort>> PropertiesIndexByName = [];

    private const string DataPropertyName = "Data";

    // Key - data property type, Value - EntityStatePropertyDelta
    private static readonly Dictionary<Type, Type> DeltaPropertyForDataType = [];

    private static readonly Dictionary<Type, PropertyInfo> DataPropertyOfDeltaProperty = [];

    private static readonly CompareLogic DefaultCompareLogic = new(new ComparisonConfig()
    {
        MaxDifferences = int.MaxValue,
        CustomComparers = [new CustomComparer<Vector3, Vector3>((v1, v2) => v1 == v2)],
        MembersToIgnore = [nameof(EntityState.EntityId)]
    });

    static StateSnapshotUtils()
    {
        // Cache state properties reflections
        var stateDerivedClasses = FindAllDerivedTypes<EntityState>();
        foreach (var type in stateDerivedClasses)
        {
            var propertyDictionary = new Dictionary<ushort, PropertyInfo>();
            var propertyIndexDictionary = new Dictionary<string, ushort>();
            ushort index = 0;
            foreach (var property in type.GetProperties())
            {
                propertyDictionary.Add(index, property);
                propertyIndexDictionary.Add(property.Name, index);
                index++;
            }
            PropertiesWithIndex.Add(type, propertyDictionary);
            PropertiesIndexByName.Add(type, propertyIndexDictionary);
        }

        // Cache delta state properties reflections
        var deltaDerivedClasses = FindAllDerivedTypes<EntityStatePropertyDelta>();
        foreach (var type in deltaDerivedClasses)
        {
            var prop = type.GetProperty(DataPropertyName)
                ?? throw new ArgumentException($"Property '{DataPropertyName}' not found");

            DeltaPropertyForDataType.Add(prop.PropertyType, type);
            DataPropertyOfDeltaProperty.Add(type, prop);
        }
    }

    private static List<Type> FindAllDerivedTypes<T>() =>
        FindAllDerivedTypes<T>(Assembly.GetAssembly(typeof(T))!);

    private static List<Type> FindAllDerivedTypes<T>(Assembly assembly)
    {
        var baseType = typeof(T);
        return assembly
            .GetTypes()
            .Where(t => t != baseType && baseType.IsAssignableFrom(t))
            .ToList();
    }

    public static DeltaStateSnapshot DeltaEncode(StateSnapshot baseline, StateSnapshot toBeEncoded)
    {
        var baselineStates = baseline.States.ToDictionary(s => s.EntityId);
        var toBeEncodedStates = toBeEncoded.States.ToDictionary(s => s.EntityId);

        var mergedKeys = new HashSet<uint>(baselineStates.Keys);
        mergedKeys.UnionWith(toBeEncodedStates.Keys);

        // Specifying the initial capacity to avoid unnecessary reallocations when resizing
        var modifications = new Dictionary<uint, ICollection<EntityStatePropertyDelta>>(toBeEncodedStates.Count);
        var toBeRemoved = new HashSet<uint>(baselineStates.Count);
        var toBeAdded = new List<EntityState>(toBeEncodedStates.Count);
        foreach (var key in mergedKeys)
        {
            if (baselineStates.ContainsKey(key))
            {
                if (toBeEncodedStates.TryGetValue(key, out EntityState newState))
                {
                    // Entity changed in next snapshot
                    var baselineState = baselineStates[key];
                    var compResult = DefaultCompareLogic.Compare(baselineState, newState);

                    if (compResult.AreEqual)
                        continue;

                    var propertyIndexDictionary = PropertiesIndexByName[baselineState.GetType()];
                    var deltaProperties = new List<EntityStatePropertyDelta>(compResult.Differences.Count);
                    foreach (var diff in compResult.Differences)
                    {
                        ushort propIndex = propertyIndexDictionary[diff.PropertyName];
                        var deltaPropertyType = DeltaPropertyForDataType[diff.Object2.GetType()];
                        var deltaProperty = Activator.CreateInstance(deltaPropertyType, propIndex, diff.Object2);
                        deltaProperties.Add((EntityStatePropertyDelta)deltaProperty);
                    }
                    modifications.Add(key, deltaProperties);
                }
                else
                {
                    // Entity does not exist in next snapshot
                    toBeRemoved.Add(key);
                }
            }
            else
            {
                // Entity only exist in next snapshot
                toBeAdded.Add(toBeEncodedStates[key]);
            }
        }
        return new(toBeEncoded.Tick, baseline.Tick, toBeAdded, modifications, toBeRemoved, toBeEncoded.MetaData);
    }

    public static StateSnapshot DeltaDecode(StateSnapshot baseline, DeltaStateSnapshot delta)
    {
        // TODO: Add error handling for invalid delta snapshot
        var resultStates = new List<EntityState>(baseline.States.Count + delta.NewEntities.Count - delta.DeletedEntities.Count);
        foreach (var state in baseline.States)
        {
            // Exclude state of deleted entites
            if (delta.DeletedEntities.Contains(state.EntityId))
                continue;

            // Check for changes
            if (!delta.DeltaStates.ContainsKey(state.EntityId))
            {
                // WARN: Avoiding object alocation. Modification of state object in one snapshot can cause effect in multiple ones
                resultStates.Add(state);
                continue;
            }

            var newState = state with { };
            var propertyDictionary = PropertiesWithIndex[newState.GetType()];
            foreach (var modification in delta.DeltaStates[state.EntityId])
            {
                var stateProperty = propertyDictionary[modification.PropertyId];
                var deltaPropertyType = DeltaPropertyForDataType[stateProperty.PropertyType];
                var deltaPropertyDataProperty = DataPropertyOfDeltaProperty[deltaPropertyType];

                // Transfer value using reflection
                object value = deltaPropertyDataProperty.GetValue(modification)!;
                stateProperty.SetValue(newState, value);
            }
            resultStates.Add(newState);
        }
        resultStates.AddRange(delta.NewEntities);
        return new(delta.Tick, resultStates, delta.MetaData);
    }
}
