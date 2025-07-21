using System;
using System.Linq;
using CSMOO.Server.Database;
using CSMOO.Server.Database.Models;
using System.Collections.Generic;

namespace CSMOO.Server.Core
{
  /// <summary>
  /// Canonical object resolver for all subsystems (scripting, engine, etc.)
  /// </summary>
  /// <summary>
  /// Returns all GameObjects matching the given name, alias, type, and location, as seen by the looker.
  /// </summary>

  public static class ObjectResolver
  {
    /// <summary>
    /// Returns all GameObjects matching the given name, alias, type, and location, as seen by the looker.
    /// If a location is specified, it will be used as if the looker is in that location even if they are not.
    /// The objectType can be used to filter results by class or type.
    /// </summary>
    public static List<GameObject> ResolveObjects(
        string name,
        GameObject looker,
        GameObject? location = null,
        string? objectType = null)
    {
      var results = new List<GameObject>();
      if (string.IsNullOrWhiteSpace(name) || looker == null)
        return results;

      string normName = name.Trim();
      string lowerName = normName.ToLowerInvariant();

      // Determine effective location ONCE.  
      // If no location is provided and the looker has no location, the looker IS the location.
      GameObject? effectiveLocation = location ?? GameDatabase.Instance.GameObjects.FindById(looker.Location);

      if (effectiveLocation == null)
      {
        string? locId = null;
        if (!string.IsNullOrEmpty(looker.Location))
          locId = looker.Location;
        else if (looker.Properties.ContainsKey("location"))
          locId = looker.Properties["location"].AsString;
        if (!string.IsNullOrEmpty(locId))
          effectiveLocation = GameDatabase.Instance.GameObjects.FindById(locId);
      }

      // 1. Keyword resolution
      switch (lowerName)
      {
        case "me":
        case "player":
          results.Add(looker);
          return results;

        case "here":
        case "room":
          if (effectiveLocation != null)
            results.Add(effectiveLocation);
          return results;

        case "system":
          var sysObj = GetSystemObject();
          if (sysObj != null) results.Add(sysObj);
          return results;
        //case "self": // probably should be the object being invoked, but not sure how to determine that yet.       
        //case "this":  //this should refer to the object upon which the script is running
          // and I don't have a good way to determine that yet.
      }

      // 2. DBREF or object ID (global)
      if (normName.StartsWith('#') && int.TryParse(normName.AsSpan(1), out var dbref))
      {
        var obj = GameDatabase.Instance.GameObjects.FindOne(o => o.DbRef == dbref);
        if (obj != null) { results.Add(obj); return results; }
      }
      var byId = GameDatabase.Instance.GameObjects.FindById(normName);
      if (byId != null) { results.Add(byId); return results; }

      // 3. Local/Inventory search
      var localObjs = effectiveLocation != null ? ObjectManager.GetObjectsInLocation(effectiveLocation.Id).ToList() : new List<GameObject>();
      var inventoryObjs = ObjectManager.GetObjectsInLocation(looker.Id).ToList();
      var candidates = localObjs.Concat(inventoryObjs).ToList();

      // 4. Type filter
      if (!string.IsNullOrEmpty(objectType))
        candidates = candidates.Where(obj => obj.ClassId == objectType || (obj.Properties.ContainsKey("type") && obj.Properties["type"].AsString == objectType)).ToList();

      // 5. Name/alias/dynamic alias match
      foreach (var obj in candidates)
      {
        // Name match (case-insensitive)
        // prefer object.Name if available, otherwise use "name" property
        // This allows for objects to have a "name" property that is different from the Name for some reason
        string? objName = !string.IsNullOrEmpty(obj.Name) ? obj.Name : (obj.Properties.ContainsKey("name") ? obj.Properties["name"].AsString : null);
        if (!string.IsNullOrEmpty(objName) && string.Equals(objName, normName, StringComparison.OrdinalIgnoreCase))
        {
          results.Add(obj);
          continue;
        }
        // Alias match (case-insensitive)
        if (obj.Properties.ContainsKey("aliases"))
        {
          var aliasesProp = obj.Properties["aliases"];
          List<string>? aliases = null;
          if (aliasesProp.IsArray)
            aliases = aliasesProp.AsArray.Select(a => a.AsString).ToList();
          else if (aliasesProp.IsString)
            aliases = aliasesProp.AsString.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
          if (aliases != null && aliases.Any(a => string.Equals(a, normName, StringComparison.OrdinalIgnoreCase)))
          {
            results.Add(obj);
            continue;
          }
        }
        // Dynamic alias: capital letters and digits in name, ignoring other chars
        if (!string.IsNullOrEmpty(objName))
        {
          var dynAlias = new string(objName.Where(c => char.IsUpper(c) || char.IsDigit(c)).ToArray());
          if (!string.IsNullOrEmpty(dynAlias) && string.Equals(dynAlias, normName, StringComparison.OrdinalIgnoreCase))
          {
            results.Add(obj);
            continue;
          }
        }
        // Dynamic exit aliases (for exits)
        if (obj.ClassId == "obj_exit" && obj.Properties.ContainsKey("direction"))
        {
          var dir = obj.Properties["direction"].AsString?.ToLowerInvariant();
          if (!string.IsNullOrEmpty(dir))
          {
            if (string.Equals(dir, lowerName, StringComparison.OrdinalIgnoreCase))
            {
              results.Add(obj);
              continue;
            }
            foreach (var alias in GetExitAliases(dir))
            {
              if (string.Equals(alias, lowerName, StringComparison.OrdinalIgnoreCase))
              {
                results.Add(obj);
                break;
              }
            }
          }
        }
      }
      return results;
    }

    // Helper for exit aliases (abbreviations, etc.)
    private static List<string> GetExitAliases(string direction)
    {
      Dictionary<string, List<string>> map = new()
            {
                { "north", new() { "n" } },
                { "south", new() { "s" } },
                { "east", new() { "e" } },
                { "west", new() { "w" } },
                { "northeast", new() { "ne" } },
                { "northwest", new() { "nw" } },
                { "southeast", new() { "se" } },
                { "southwest", new() { "sw" } },
                { "up", new() { "u" } },
                { "down", new() { "d" } },
                { "out", new() { "o" } },
                { "port", new() { "p" } },
                { "starboard", new() { "s", "stbd" } },
                { "forward", new() { "f", "fore" } },
                { "aft", new() { "a" } },
                { "turbolift", new() { "tl" } },
                // Starbase-oriented directions
                { "clockwise", new() { "cw", "clock" } },
                { "counterclockwise", new() { "ccw", "counter", "counter-clockwise", "anticlockwise", "anti-clockwise" } },
                { "hubward", new() { "h","hw","hub", "inward" } },
                { "rimward", new() { "r","rw","rim", "outward" } }
            };
      return map.TryGetValue(direction, out var aliases) ? aliases : new();
    }

    /// <summary>
    /// Returns the first GameObject matching the given name, alias, type, and location, as seen by the looker.
    /// </summary>
    public static GameObject? ResolveObject(
        string name,
        GameObject looker,
        GameObject? location = null,
        string? objectType = null)
    {
      var matches = ResolveObjects(name, looker, location, objectType);
      return matches.FirstOrDefault();
    }

    /// <summary>
    /// Resolves an object reference string to an object ID.
    /// Handles: "me", "here", "system", DBREFs, class names, object IDs, and object names.
    /// </summary>
    [Obsolete("Prefer ResolveObject instead of ResolveObjectId.  Refactor to pass in GameObject looker and location instead of playerId and roomId. ")]
    public static string? ResolveObjectId(string name, string? currentPlayerId = null, string? currentRoomId = null)
    {
      if (string.IsNullOrEmpty(name))
        return null;
      GameObject? looker = currentPlayerId != null ? GameDatabase.Instance.GameObjects.FindById(currentPlayerId) : null;
      GameObject? location = currentRoomId != null ? GameDatabase.Instance.GameObjects.FindById(currentRoomId) : null;
      if (looker == null)
        return null;
      return ResolveObject(name, looker, location)?.Id;
    }

    private static GameObject? GetSystemObject()
    {
      var gameObjects = GameDatabase.Instance.GameObjects;
      var allObjects = gameObjects.FindAll();
      var systemObject = allObjects.FirstOrDefault(obj =>
          (obj.Properties.ContainsKey("name") && obj.Properties["name"].AsString == "system") ||
          (obj.Properties.ContainsKey("isSystemObject") && obj.Properties["isSystemObject"].AsBoolean == true));
      return systemObject;
    }
  }
}
