using System;
using System.Collections.Generic;
using System.Linq;
using CSMOO.Server.Database;
using LiteDB;

namespace CSMOO.Server.Scripting
{
    /// <summary>
    /// Helper methods to prevent common collection iteration mistakes
    /// </summary>
    public static class CollectionHelpers
    {
        /// <summary>
        /// Extension method to help convert BsonDocument.Values to appropriate types
        /// </summary>
        public static IEnumerable<T> ToTypedValues<T>(this BsonDocument document, Func<BsonValue, T> converter)
        {
            return document.Values.Select(converter);
        }
        
        /// <summary>
        /// Extension method to help iterate over Properties safely
        /// </summary>
        public static IEnumerable<(string Key, BsonValue Value)> ToPropertyPairs(this BsonDocument document)
        {
            return document.Select(kvp => (kvp.Key, kvp.Value));
        }
        
        /// <summary>
        /// Safe method to get GameObjects from Contents
        /// </summary>
        public static List<GameObject> GetGameObjectsFromContents(List<string> contentIds)
        {
            var gameObjects = new List<GameObject>();
            foreach (var id in contentIds)
            {
                var obj = Database.ObjectManager.GetObject(id);
                if (obj != null)
                {
                    gameObjects.Add(obj);
                }
            }
            return gameObjects;
        }
        
        /// <summary>
        /// Extension method to safely convert ID lists to GameObject lists
        /// </summary>
        public static List<GameObject> ToGameObjects(this List<string> ids)
        {
            return GetGameObjectsFromContents(ids);
        }
    }
}
