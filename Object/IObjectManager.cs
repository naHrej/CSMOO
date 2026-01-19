using LiteDB;

namespace CSMOO.Object;

/// <summary>
/// Interface for object management operations
/// </summary>
public interface IObjectManager
{
    // Cache Management
    void LoadAllObjectsToCache();
    void LoadAllObjectClassesToCache();
    GameObject? CacheGameObject(GameObject obj);
    ObjectClass? CacheObjectClass(ObjectClass objClass);
    
    // Class Management
    ObjectClass CreateClass(string name, string? parentClassId = null, string description = "");
    List<ObjectClass> GetInheritanceChain(string classId);
    bool InheritsFrom(string childClassId, string parentClassId);
    List<ObjectClass> GetSubclasses(string parentClassId, bool recursive = true);
    bool DeleteClass(string classId, bool deleteSubclasses = false);
    bool UpdateClass(ObjectClass objectClass);
    List<ObjectClass> FindClassesByName(string name, bool exactMatch = false);
    ObjectClass? GetClass(string classId);
    ObjectClass? GetClassByName(string className);
    
    // Instance Management
    GameObject CreateInstance(string classId, string? location = null);
    bool DestroyInstance(string objectId);
    bool MoveObject(string objectId, string? newLocationId);
    bool MoveObject(GameObject gameObject, GameObject newLocation);
    List<GameObject> GetObjectsInLocation(string? locationId);
    List<GameObject> GetObjectsInLocation(GameObject? location);
    List<GameObject> FindObjectsByClass(string classId, bool includeSubclasses = true);
    GameObject? FindByDbRef(int dbRef);
    Dictionary<string, int> GetObjectStatistics();
    GameObject? GetObject(string objectId);
    T? GetObject<T>(string objectId) where T : GameObject;
    GameObject? GetObjectByDbRef(int dbRef);
    List<dynamic> GetAllObjects();
    List<ObjectClass> GetAllObjectClasses();
    GameObject? ReloadObject(string objectId);
    
    // Property Management
    BsonValue? GetProperty(GameObject gameObject, string propertyName);
    void SetProperty(GameObject gameObject, string propertyName, BsonValue value);
    bool UpdateObject(GameObject gameObject);
    bool RemoveProperty(GameObject gameObject, string propertyName);
    string[] GetPropertyNames(GameObject gameObject);
    bool HasProperty(GameObject gameObject, string propertyName);
    string[] GetAllPropertyNames(GameObject gameObject);
    T? GetPropertyValue<T>(GameObject gameObject, string propertyName, T? defaultValue = default);
    void SetPropertyValue<T>(GameObject gameObject, string propertyName, T value);
}
