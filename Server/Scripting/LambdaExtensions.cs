using System;
using System.Collections.Generic;
using System.Linq;
using CSMOO.Server.Database;
using CSMOO.Server.Logging;

namespace CSMOO.Server.Scripting;

/// <summary>
/// Extension methods to make lambda expressions work smoothly with dynamic objects and CSMOO types
/// </summary>
public static class LambdaExtensions
{
    /// <summary>
    /// Extension method for filtering dynamic objects with lambdas
    /// Usage: GetObjectsInLocation(roomId).WhereObjects(obj => obj.visible == true)
    /// </summary>
    public static IEnumerable<DynamicGameObject> WhereObjects(this IEnumerable<dynamic> source, Func<DynamicGameObject, bool> predicate)
    {
        return source.Cast<DynamicGameObject>().Where(predicate);
    }

    /// <summary>
    /// Extension method for selecting from dynamic objects with lambdas
    /// Usage: GetObjectsInLocation(roomId).SelectObjects(obj => obj.name)
    /// </summary>
    public static IEnumerable<T> SelectObjects<T>(this IEnumerable<dynamic> source, Func<DynamicGameObject, T> selector)
    {
        return source.Cast<DynamicGameObject>().Select(selector);
    }

    /// <summary>
    /// Extension method for checking if any dynamic object matches condition
    /// Usage: GetObjectsInLocation(roomId).AnyObjects(obj => obj.type == "weapon")
    /// </summary>
    public static bool AnyObjects(this IEnumerable<dynamic> source, Func<DynamicGameObject, bool> predicate)
    {
        return source.Cast<DynamicGameObject>().Any(predicate);
    }

    /// <summary>
    /// Extension method for counting dynamic objects that match condition
    /// Usage: GetObjectsInLocation(roomId).CountObjects(obj => obj.visible == true)
    /// </summary>
    public static int CountObjects(this IEnumerable<dynamic> source, Func<DynamicGameObject, bool> predicate)
    {
        return source.Cast<DynamicGameObject>().Count(predicate);
    }

    /// <summary>
    /// Extension method for finding first dynamic object that matches condition
    /// Usage: GetObjectsInLocation(roomId).FirstObjects(obj => obj.name == "sword")
    /// </summary>
    public static DynamicGameObject? FirstObjects(this IEnumerable<dynamic> source, Func<DynamicGameObject, bool> predicate)
    {
        return source.Cast<DynamicGameObject>().FirstOrDefault(predicate);
    }

    /// <summary>
    /// Extension method for executing action on each dynamic object
    /// Usage: GetObjectsInLocation(roomId).ForEachObjects(obj => { if (obj.broken) obj.broken = false; })
    /// </summary>
    public static void ForEachObjects(this IEnumerable<dynamic> source, Action<DynamicGameObject> action)
    {
        foreach (var obj in source.Cast<DynamicGameObject>())
        {
            try
            {
                action(obj);
            }
            catch (Exception ex)
            {
                var gameObj = obj.GameObject as GameObject;
                Logger.Error($"Error executing action on object {gameObj?.Id}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Extension method for grouping dynamic objects
    /// Usage: GetObjectsInLocation(roomId).GroupByObjects(obj => obj.type)
    /// </summary>
    public static IEnumerable<IGrouping<TKey, DynamicGameObject>> GroupByObjects<TKey>(this IEnumerable<dynamic> source, Func<DynamicGameObject, TKey> keySelector)
    {
        return source.Cast<DynamicGameObject>().GroupBy(keySelector);
    }

    /// <summary>
    /// Extension method for ordering dynamic objects
    /// Usage: GetObjectsInLocation(roomId).OrderByObjects(obj => obj.name)
    /// </summary>
    public static IEnumerable<DynamicGameObject> OrderByObjects<TKey>(this IEnumerable<dynamic> source, Func<DynamicGameObject, TKey> keySelector)
    {
        return source.Cast<DynamicGameObject>().OrderBy(keySelector);
    }

    /// <summary>
    /// Extension method for ordering dynamic objects descending
    /// Usage: GetObjectsInLocation(roomId).OrderByDescendingObjects(obj => obj.value)
    /// </summary>
    public static IEnumerable<DynamicGameObject> OrderByDescendingObjects<TKey>(this IEnumerable<dynamic> source, Func<DynamicGameObject, TKey> keySelector)
    {
        return source.Cast<DynamicGameObject>().OrderByDescending(keySelector);
    }

    /// <summary>
    /// Extension method for taking a specific number of dynamic objects
    /// Usage: GetObjectsInLocation(roomId).WhereObjects(obj => obj.valuable).TakeObjects(5)
    /// </summary>
    public static IEnumerable<DynamicGameObject> TakeObjects(this IEnumerable<dynamic> source, int count)
    {
        return source.Cast<DynamicGameObject>().Take(count);
    }

    /// <summary>
    /// Extension method for skipping a specific number of dynamic objects
    /// Usage: GetObjectsInLocation(roomId).OrderByObjects(obj => obj.name).SkipObjects(5)
    /// </summary>
    public static IEnumerable<DynamicGameObject> SkipObjects(this IEnumerable<dynamic> source, int count)
    {
        return source.Cast<DynamicGameObject>().Skip(count);
    }

    /// <summary>
    /// Extension method to convert back to dynamic list for chaining
    /// Usage: GetObjectsInLocation(roomId).WhereObjects(obj => obj.visible).ToDynamicList()
    /// </summary>
    public static List<dynamic> ToDynamicList(this IEnumerable<DynamicGameObject> source)
    {
        return source.Cast<dynamic>().ToList();
    }

    /// <summary>
    /// Extension method for players with lambdas
    /// Usage: GetAllPlayers().WherePlayers(p => p.IsOnline && IsAdmin(p))
    /// </summary>
    public static IEnumerable<Player> WherePlayers(this IEnumerable<Player> source, Func<Player, bool> predicate)
    {
        return source.Where(predicate);
    }

    /// <summary>
    /// Extension method for executing action on each player
    /// Usage: GetAllPlayers().WherePlayers(p => p.IsOnline).ForEachPlayers(p => Notify(p, "Hello!"))
    /// </summary>
    public static void ForEachPlayers(this IEnumerable<Player> source, Action<Player> action)
    {
        foreach (var player in source)
        {
            try
            {
                action(player);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error executing action on player {player.Name}: {ex.Message}");
            }
        }
    }
}
