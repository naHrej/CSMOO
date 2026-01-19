using LiteDB;
using CSMOO.Object;

namespace CSMOO.Database;

/// <summary>
/// Interface for property management operations
/// </summary>
public interface IPropertyManager
{
    /// <summary>
    /// Gets a property value from an object, checking inheritance chain if not found on instance
    /// </summary>
    BsonValue? GetProperty(GameObject gameObject, string propertyName);
    
    /// <summary>
    /// Sets a property value on an object instance
    /// </summary>
    void SetProperty(GameObject gameObject, string propertyName, BsonValue value);
    
    /// <summary>
    /// Sets a property value by object ID
    /// </summary>
    bool SetProperty(string objectId, string propertyName, BsonValue value);
    
    /// <summary>
    /// Removes a property from an object instance
    /// </summary>
    bool RemoveProperty(GameObject gameObject, string propertyName);
    
    /// <summary>
    /// Removes a property by object ID
    /// </summary>
    bool RemoveProperty(string objectId, string propertyName);
    
    /// <summary>
    /// Checks if an object has a property (including inherited properties)
    /// </summary>
    bool HasProperty(GameObject gameObject, string propertyName);
    
    /// <summary>
    /// Gets all property names from an object (including inherited properties)
    /// </summary>
    string[] GetAllPropertyNames(GameObject gameObject);
    
    /// <summary>
    /// Merges properties from source into target, with source taking priority
    /// </summary>
    void MergeProperties(BsonDocument target, BsonDocument source);
    
    /// <summary>
    /// Copies all properties from one object to another
    /// </summary>
    void CopyProperties(GameObject source, GameObject target, bool overwriteExisting = true);
    
    /// <summary>
    /// Gets the effective value of a property (resolved through inheritance)
    /// </summary>
    T? GetPropertyValue<T>(GameObject gameObject, string propertyName, T? defaultValue = default);
    
    /// <summary>
    /// Sets a strongly-typed property value
    /// </summary>
    void SetPropertyValue<T>(GameObject gameObject, string propertyName, T value);
}
