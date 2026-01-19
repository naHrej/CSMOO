using LiteDB;
using CSMOO.Database;

namespace CSMOO.Object;/// <summary>
/// Player-specific data that extends GameObject
/// </summary>
public class Player : GameObject
{
    [BsonField("passwordhash")]
    private string _passwordHash = string.Empty;

    /// <summary>
    /// Password hash for authentication
    /// Note: Setting this property does NOT automatically update the database.
    /// Use PlayerManager.ChangePassword() or explicitly call DbProvider.Update() after setting.
    /// </summary>
    public string PasswordHash
    {
        get
        {
            return _passwordHash;
        }
        set
        {
            _passwordHash = value;
            // Removed automatic database update to prevent issues during deserialization
            // and to avoid creating multiple database connections. Use PlayerManager.ChangePassword()
            // or explicitly update the database after setting this property.
        }
    }


    /// <summary>
    /// Current session GUID (if online)
    /// </summary>
    public Guid? SessionGuid
    {
        get => Properties.ContainsKey("sessionguid") && Guid.TryParse(Properties["sessionguid"].AsString, out var guid) ? guid : (Guid?)null;
        set => Properties["sessionguid"] = value.HasValue ? new BsonValue(value.Value.ToString()) : BsonValue.Null;
    }

    /// <summary>
    /// Last login time
    /// </summary>
    public DateTime? LastLogin
    {
        get => Properties.ContainsKey("lastlogin") ? Properties["lastlogin"].AsDateTime : (DateTime?)null;
        set => Properties["lastlogin"] = value.HasValue ? new BsonValue(value.Value) : BsonValue.Null;
    }

    /// <summary>
    /// Whether the player is currently online
    /// </summary>
    public bool IsOnline
    {
        get => Properties.ContainsKey("isonline") ? Properties["isonline"].AsBoolean : false;
        set => Properties["isonline"] = new BsonValue(value);
    }

    [BsonIgnore]
    public new readonly GameObject Owner;

    /// <summary>
    /// Ensures legacy fields are synced to Properties after deserialization
    /// Call this after loading a Player from the database for backward compatibility
    /// </summary>
    public void FixupFieldsAfterDeserialization()
    {
        if (!Properties.ContainsKey("passwordhash") && !string.IsNullOrEmpty(_passwordHash))
        {
            Properties["passwordhash"] = new BsonValue(_passwordHash);
        }
    }

    public Player()
    {
        this.Owner = this;
    }
}



