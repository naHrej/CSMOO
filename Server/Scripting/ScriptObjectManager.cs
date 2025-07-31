using System.Collections.Generic;
using System.Linq;
using CSMOO.Server.Database;
using LiteDB;

namespace CSMOO.Server.Scripting;

/// <summary>
/// Script-safe wrapper for ObjectManager
/// </summary>
public class ScriptObjectManager
{
    public object? GetProperty(string objectId, string propertyName)
    {
        var obj = ObjectManager.GetObject(objectId);
        if (obj == null) return null;
        return Database.ObjectManager.GetProperty(obj, propertyName)?.RawValue;
    }

    public void SetProperty(string objectId, string propertyName, object value)
    {
        var obj = ObjectManager.GetObject(objectId);
        if (obj != null)
        {
            Database.ObjectManager.SetProperty(obj, propertyName, new BsonValue(value));
        }
    }

    public void MoveObject(string objectId, string? newLocation)
    {
        Database.ObjectManager.MoveObject(objectId, newLocation);
    }

    public List<string> GetObjectsInLocation(string locationId)
    {
        return [.. Database.ObjectManager.GetObjectsInLocation(locationId).Select(obj => obj.Id)];
    }
}
