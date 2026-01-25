using CSMOO.Object;
using CSMOO.Database;
using CSMOO.Configuration;
using CSMOO.Logging;
using LiteDB;

namespace CSMOO.Scripting;

/// <summary>
/// Script-safe wrapper for ObjectManager
/// </summary>
public class ScriptObjectManager
{
    private readonly IObjectManager _objectManager;

    // Primary constructor with DI dependencies
    public ScriptObjectManager(IObjectManager objectManager)
    {
        _objectManager = objectManager ?? throw new ArgumentNullException(nameof(objectManager));
    }


    public object? GetProperty(string objectId, string propertyName)
    {
        var obj = _objectManager.GetObject(objectId);
        if (obj == null) return null;
        return _objectManager.GetProperty(obj, propertyName)?.RawValue;
    }

    public void SetProperty(string objectId, string propertyName, object value)
    {
        var obj = _objectManager.GetObject(objectId);
        if (obj != null)
        {
            _objectManager.SetProperty(obj, propertyName, new BsonValue(value));
        }
    }

    public void MoveObject(string objectId, string? newLocation)
    {
        _objectManager.MoveObject(objectId, newLocation);
    }

    public List<string> GetObjectsInLocation(string locationId)
    {
        return [.. _objectManager.GetObjectsInLocation(locationId).Select(obj => obj.Id)];
    }
}



