using CSMOO.Object;
using LiteDB;

namespace CSMOO.Scripting;

/// <summary>
/// Script-safe wrapper for ObjectManager
/// </summary>
public class ScriptObjectManager
{
    public object? GetProperty(string objectId, string propertyName)
    {
        var obj = ObjectManager.GetObject(objectId);
        if (obj == null) return null;
        return ObjectManager.GetProperty(obj, propertyName)?.RawValue;
    }

    public void SetProperty(string objectId, string propertyName, object value)
    {
        var obj = ObjectManager.GetObject(objectId);
        if (obj != null)
        {
            ObjectManager.SetProperty(obj, propertyName, new BsonValue(value));
        }
    }

    public void MoveObject(string objectId, string? newLocation)
    {
        ObjectManager.MoveObject(objectId, newLocation);
    }

    public List<string> GetObjectsInLocation(string locationId)
    {
        return [.. ObjectManager.GetObjectsInLocation(locationId).Select(obj => obj.Id)];
    }
}



