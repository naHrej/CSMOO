using System;
using LiteDB;
using CSMOO.Server.Logging;

namespace CSMOO.Server.Database.Managers;

/// <summary>
/// Manages object properties and their inheritance
/// </summary>
public static class PropertyManager
{
    /// <summary>
    /// Gets a property value from an object, checking inheritance chain if not found on instance
    /// </summary>
    public static BsonValue? GetProperty(GameObject gameObject, string propertyName)
    {
        // First check if the property exists on the instance itself
        if (gameObject.Properties.ContainsKey(propertyName))
            return gameObject.Properties[propertyName];

        // If not found on instance, check the inheritance chain
        var inheritanceChain = ClassManager.GetInheritanceChain(gameObject.ClassId);
        
        // Walk the chain from most specific to least specific (reverse order)
        foreach (var objectClass in inheritanceChain.AsEnumerable().Reverse())
        {
            if (objectClass.Properties.ContainsKey(propertyName))
                return objectClass.Properties[propertyName];
        }

        return null; // Property not found anywhere in the chain
    }

    /// <summary>
    /// Sets a property value on an object instance
    /// </summary>
    public static void SetProperty(GameObject gameObject, string propertyName, BsonValue value)
    {
        gameObject.Properties[propertyName] = value;
        DbProvider.Instance.Update("gameobjects", gameObject);
        Logger.Debug($"Set property '{propertyName}' on object #{gameObject.DbRef}");
    }

    /// <summary>
    /// Sets a property value by object ID
    /// </summary>
    public static bool SetProperty(string objectId, string propertyName, BsonValue value)
    {
        var gameObject = DbProvider.Instance.FindById<GameObject>("gameobjects", objectId);
        if (gameObject == null) return false;

        SetProperty(gameObject, propertyName, value);
        return true;
    }

    /// <summary>
    /// Removes a property from an object instance
    /// </summary>
    public static bool RemoveProperty(GameObject gameObject, string propertyName)
    {
        if (!gameObject.Properties.ContainsKey(propertyName))
            return false;

        gameObject.Properties.Remove(propertyName);
        DbProvider.Instance.Update("gameobjects", gameObject);
        Logger.Debug($"Removed property '{propertyName}' from object #{gameObject.DbRef}");
        return true;
    }

    /// <summary>
    /// Removes a property by object ID
    /// </summary>
    public static bool RemoveProperty(string objectId, string propertyName)
    {
        var gameObject = DbProvider.Instance.FindById<GameObject>("gameobjects", objectId);
        if (gameObject == null) return false;

        return RemoveProperty(gameObject, propertyName);
    }

    /// <summary>
    /// Checks if an object has a property (including inherited properties)
    /// </summary>
    public static bool HasProperty(GameObject gameObject, string propertyName)
    {
        return GetProperty(gameObject, propertyName) != null;
    }

    /// <summary>
    /// Gets all property names from an object (including inherited properties)
    /// </summary>
    public static string[] GetAllPropertyNames(GameObject gameObject)
    {
        var allProperties = new System.Collections.Generic.HashSet<string>();

        // Add instance properties
        foreach (var key in gameObject.Properties.Keys)
        {
            allProperties.Add(key);
        }

        // Add inherited properties
        var inheritanceChain = ClassManager.GetInheritanceChain(gameObject.ClassId);
        foreach (var objectClass in inheritanceChain)
        {
            foreach (var key in objectClass.Properties.Keys)
            {
                allProperties.Add(key);
            }
        }

        return allProperties.ToArray();
    }

    /// <summary>
    /// Merges properties from source into target, with source taking priority
    /// </summary>
    public static void MergeProperties(BsonDocument target, BsonDocument source)
    {
        foreach (var element in source)
        {
            target[element.Key] = element.Value;
        }
    }

    /// <summary>
    /// Copies all properties from one object to another
    /// </summary>
    public static void CopyProperties(GameObject source, GameObject target, bool overwriteExisting = true)
    {
        foreach (var property in source.Properties)
        {
            if (overwriteExisting || !target.Properties.ContainsKey(property.Key))
            {
                target.Properties[property.Key] = property.Value;
            }
        }

        DbProvider.Instance.Update("gameobjects", target);
        Logger.Debug($"Copied properties from object #{source.DbRef} to #{target.DbRef}");
    }

    /// <summary>
    /// Gets the effective value of a property (resolved through inheritance)
    /// </summary>
    public static T? GetPropertyValue<T>(GameObject gameObject, string propertyName, T? defaultValue = default)
    {
        var value = GetProperty(gameObject, propertyName);
        if (value == null) return defaultValue;

        try
        {
            if (typeof(T) == typeof(string))
                return (T)(object)value.AsString;
            if (typeof(T) == typeof(int))
                return (T)(object)value.AsInt32;
            if (typeof(T) == typeof(bool))
                return (T)(object)value.AsBoolean;
            if (typeof(T) == typeof(double))
                return (T)(object)value.AsDouble;
            if (typeof(T) == typeof(DateTime))
                return (T)(object)value.AsDateTime;

            return defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Sets a strongly-typed property value
    /// </summary>
    public static void SetPropertyValue<T>(GameObject gameObject, string propertyName, T value)
    {
        BsonValue bsonValue = value switch
        {
            string s => new BsonValue(s),
            int i => new BsonValue(i),
            bool b => new BsonValue(b),
            double d => new BsonValue(d),
            DateTime dt => new BsonValue(dt),
            _ => new BsonValue(value?.ToString() ?? "")
        };

        SetProperty(gameObject, propertyName, bsonValue);
    }
}
