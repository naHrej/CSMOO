using System;
using System.Linq;
using CSMOO.Server.Database;
using CSMOO.Server.Database.Models;

namespace CSMOO.Server.Core
{
    /// <summary>
    /// Canonical object resolver for all subsystems (scripting, engine, etc.)
    /// </summary>
    public static class ObjectResolver
    {
        /// <summary>
        /// Resolves an object reference string to an object ID.
        /// Handles: "me", "here", "system", DBREFs, class names, object IDs, and object names.
        /// </summary>
        public static string? ResolveObjectId(string objectRef, string currentPlayerId, string currentRoomId)
        {
            if (string.IsNullOrEmpty(objectRef))
                return null;

            var lowerRef = objectRef.ToLower();

            // Handle special references
            switch (lowerRef)
            {
                case "player":
                case "me":
                    return currentPlayerId;
                case "here":
                case "room":
                    return currentRoomId;
                case "system":
                    return GetSystemObjectId();
            }

            // Handle DBREF format (#123)
            if (objectRef.StartsWith("#") && int.TryParse(objectRef.Substring(1), out var dbref))
            {
                var gameObjects = GameDatabase.Instance.GameObjects;
                var obj = gameObjects.FindOne(o => o.DbRef == dbref);
                return obj?.Id;
            }

            // Handle class references (class:Name)
            if (objectRef.StartsWith("class:", StringComparison.OrdinalIgnoreCase))
            {
                var className = objectRef.Substring(6);
                var objectClass = GameDatabase.Instance.ObjectClasses.FindOne(c =>
                    c.Name.Equals(className, StringComparison.OrdinalIgnoreCase));
                return objectClass?.Id;
            }

            // Check if it's a direct class ID (like "obj_room", "obj_exit", etc.)
            var classById = GameDatabase.Instance.ObjectClasses.FindById(objectRef);
            if (classById != null)
            {
                return classById.Id;
            }

            // Try to find by object ID directly
            var gameObjects2 = GameDatabase.Instance.GameObjects;
            if (gameObjects2.Exists(o => o.Id == objectRef))
                return objectRef;

            // Try to find by name
            var namedObject = gameObjects2.FindOne(o => o.Properties.ContainsKey("name") && o.Properties["name"].AsString.Equals(objectRef, StringComparison.OrdinalIgnoreCase));
            if (namedObject != null) return namedObject.Id;

            // Try as class name
            var classByName = GameDatabase.Instance.ObjectClasses.FindOne(c =>
                c.Name.Equals(objectRef, StringComparison.OrdinalIgnoreCase));
            return classByName?.Id;
        }

        /// <summary>
        /// Resolves an object reference string to a GameObject instance.
        /// </summary>
        public static GameObject? ResolveObject(string objectRef, string currentPlayerId, string currentRoomId)
        {
            var id = ResolveObjectId(objectRef, currentPlayerId, currentRoomId);
            return id != null ? GameDatabase.Instance.GameObjects.FindById(id) : null;
        }

        /// <summary>
        /// Gets the system object ID
        /// </summary>
        private static string? GetSystemObjectId()
        {
            var gameObjects = GameDatabase.Instance.GameObjects;
            var allObjects = gameObjects.FindAll();
            var systemObject = allObjects.FirstOrDefault(obj =>
                (obj.Properties.ContainsKey("name") && obj.Properties["name"].AsString == "system") ||
                (obj.Properties.ContainsKey("isSystemObject") && obj.Properties["isSystemObject"].AsBoolean == true));
            return systemObject?.Id;
        }
    }
}
