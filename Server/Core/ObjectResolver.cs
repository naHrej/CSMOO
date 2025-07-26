using System;
using System.Linq;
using CSMOO.Server.Database;
using CSMOO.Server.Database.Models;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace CSMOO.Server.Core;

/// <summary>
/// Canonical object resolver for all subsystems (scripting, engine, etc.)
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
    Logging.Logger.Debug($"Resolving objects for '{name}' as seen by {looker.Name} (ID: {looker.Id}) in location {location?.Name ?? "none"} with type filter '{objectType}'");

    if (string.IsNullOrWhiteSpace(name) || looker == null)
      return new List<GameObject>();

    string normName = name.Trim();
    string lowerName = normName.ToLowerInvariant();

    // Determine effective location ONCE.  
    GameObject? effectiveLocation = location;
    if (effectiveLocation == null && looker.Location != null)
      effectiveLocation = looker.Location;
    if (effectiveLocation == null)
    {
      if(looker.Location != null)
        effectiveLocation = looker.Location;
    }

    // 1. Keyword resolution
    var keywordResult = MatchKeyword(lowerName, looker, effectiveLocation);
    if (keywordResult != null)
      return [keywordResult];

    // 2. DBREF or object ID (global)
    var dbrefResult = MatchDBref(normName);
    if (dbrefResult != null)
      return [dbrefResult];

    var idResult = MatchId(normName);
    if (idResult != null)
      return [idResult];

    // 3. Local/Inventory search
    var localObjs = effectiveLocation != null ? ObjectManager.GetObjectsInLocation(effectiveLocation.Id).ToList() : new List<GameObject>();
    var inventoryObjs = ObjectManager.GetObjectsInLocation(looker.Id).ToList();
    var candidates = localObjs.Concat(second: inventoryObjs).ToList();

    // 4. Type filter
    if (!string.IsNullOrEmpty(objectType))
      candidates = candidates.Where(obj => obj.ClassId == objectType || (obj.Properties.ContainsKey("type") && obj.Properties["type"].AsString == objectType)).ToList();

    // 5. Name/alias/dynamic alias/exit alias match
    return MatchNameOrAlias(normName, candidates: candidates);
  }



  /// <summary>
  /// Implements keyword matching ("me", "here", "system", etc.).
  /// </summary>
  private static GameObject? MatchKeyword(string name, dynamic looker, dynamic? location = null)
  {
    var effectiveLocation = location;
    if (effectiveLocation == null && looker.Location != null)
      effectiveLocation = looker.Location;
    switch (name.ToLowerInvariant())
    {
      // Keywords that refer to the looker
      case "me":
      case "player":
        return looker;
      case "here":
      case "room":
        return effectiveLocation;
      case "system":
        return GetSystemObject();
      default:
        return null;
    }
  }

  /// <summary>
  /// Implements DBREF matching (e.g., "#123").
  /// </summary>
  private static GameObject? MatchDBref(string dbRef)
  {
    if (dbRef.StartsWith('#') && int.TryParse(dbRef.AsSpan(1), out var dbref))
    {
      return DbProvider.Instance.FindOne<GameObject>("gameobjects", o => o.DbRef == dbref);
    }
    return null;
  }

  /// <summary>
  /// Implements object ID matching (by string ID).
  /// </summary>
  private static GameObject? MatchId(string objectId)
  {
    return DbProvider.Instance.FindById<GameObject>("gameobjects", objectId);
  }

  /// <summary>
  /// Implements name, alias, dynamic alias, and exit alias matching.
  /// </summary>
  /// <remarks>
  /// - Name match: case-insensitive match on object name.
  /// - Alias match: case-insensitive match on aliases property.
  /// - Dynamic alias: capital letters and digits in name.
  /// - Exit alias: direction and abbreviations for exits.
  /// </remarks>
  private static List<GameObject> MatchNameOrAlias(string normName, List<GameObject> candidates)
  {
    string lowerName = normName.ToLowerInvariant();
    var results = new List<GameObject>();
    foreach (var obj in candidates)
    {
      // Name match (case-insensitive)
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
    return map.TryGetValue(direction, out var aliases) ? aliases : [];
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


  private static GameObject? GetSystemObject()
  {
    var allObjects = DbProvider.Instance.FindAll<GameObject>("gameobjects");
    var systemObject = allObjects.FirstOrDefault(obj =>
        (obj.Properties.ContainsKey("name") && obj.Properties["name"].AsString == "system") ||
        (obj.Properties.ContainsKey("isSystemObject") && obj.Properties["isSystemObject"].AsBoolean == true));
    return systemObject;
  }
}

