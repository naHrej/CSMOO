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
    public static IEnumerable<GameObject> WhereObjects(this IEnumerable<dynamic> source, Func<GameObject, bool> predicate)
    {
        return source.Cast<GameObject>().Where(predicate);
    }

    /// <summary>
    /// Extension method for selecting from dynamic objects with lambdas
    /// Usage: GetObjectsInLocation(roomId).SelectObjects(obj => obj.name)
    /// </summary>
    public static IEnumerable<T> SelectObjects<T>(this IEnumerable<dynamic> source, Func<GameObject, T> selector)
    {
        return source.Cast<GameObject>().Select(selector);
    }

    /// <summary>
    /// Extension method for checking if any dynamic object matches condition
    /// Usage: GetObjectsInLocation(roomId).AnyObjects(obj => obj.type == "weapon")
    /// </summary>
    public static bool AnyObjects(this IEnumerable<dynamic> source, Func<GameObject, bool> predicate)
    {
        return source.Cast<GameObject>().Any(predicate);
    }

    /// <summary>
    /// Extension method for counting dynamic objects that match condition
    /// Usage: GetObjectsInLocation(roomId).CountObjects(obj => obj.visible == true)
    /// </summary>
    public static int CountObjects(this IEnumerable<dynamic> source, Func<GameObject, bool> predicate)
    {
        return source.Cast<GameObject>().Count(predicate);
    }

    /// <summary>
    /// Extension method for finding first dynamic object that matches condition
    /// Usage: GetObjectsInLocation(roomId).FirstObjects(obj => obj.name == "sword")
    /// </summary>
    public static GameObject? FirstObjects(this IEnumerable<dynamic> source, Func<GameObject, bool> predicate)
    {
        return source.Cast<GameObject>().FirstOrDefault(predicate);
    }

    /// <summary>
    /// Extension method for executing action on each dynamic object
    /// Usage: GetObjectsInLocation(roomId).ForEachObjects(obj => { if (obj.broken) obj.broken = false; })
    /// </summary>
    public static void ForEachObjects(this IEnumerable<dynamic> source, Action<GameObject> action)
    {
        foreach (var obj in source.Cast<GameObject>())
        {
            try
            {
                action(obj);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error executing action on object {obj?.Id}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Extension method for grouping dynamic objects
    /// Usage: GetObjectsInLocation(roomId).GroupByObjects(obj => obj.type)
    /// </summary>
    public static IEnumerable<IGrouping<TKey, GameObject>> GroupByObjects<TKey>(this IEnumerable<dynamic> source, Func<GameObject, TKey> keySelector)
    {
        return source.Cast<GameObject>().GroupBy(keySelector);
    }

    /// <summary>
    /// Extension method for ordering dynamic objects
    /// Usage: GetObjectsInLocation(roomId).OrderByObjects(obj => obj.name)
    /// </summary>
    public static IEnumerable<GameObject> OrderByObjects<TKey>(this IEnumerable<dynamic> source, Func<GameObject, TKey> keySelector)
    {
        return source.Cast<GameObject>().OrderBy(keySelector);
    }

    /// <summary>
    /// Extension method for ordering dynamic objects descending
    /// Usage: GetObjectsInLocation(roomId).OrderByDescendingObjects(obj => obj.value)
    /// </summary>
    public static IEnumerable<GameObject> OrderByDescendingObjects<TKey>(this IEnumerable<dynamic> source, Func<GameObject, TKey> keySelector)
    {
        return source.Cast<GameObject>().OrderByDescending(keySelector);
    }

    /// <summary>
    /// Extension method for taking a specific number of dynamic objects
    /// Usage: GetObjectsInLocation(roomId).WhereObjects(obj => obj.valuable).TakeObjects(5)
    /// </summary>
    public static IEnumerable<GameObject> TakeObjects(this IEnumerable<dynamic> source, int count)
    {
        return source.Cast<GameObject>().Take(count);
    }

    /// <summary>
    /// Extension method for skipping a specific number of dynamic objects
    /// Usage: GetObjectsInLocation(roomId).OrderByObjects(obj => obj.name).SkipObjects(5)
    /// </summary>
    public static IEnumerable<GameObject> SkipObjects(this IEnumerable<dynamic> source, int count)
    {
        return source.Cast<GameObject>().Skip(count);
    }

    /// <summary>
    /// Extension method to convert back to dynamic list for chaining
    /// Usage: GetObjectsInLocation(roomId).WhereObjects(obj => obj.visible).ToDynamicList()
    /// </summary>
    public static List<dynamic> ToDynamicList(this IEnumerable<GameObject> source)
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
