using CSMOO.Server.Commands;
using CSMOO.Server.Database;
using LiteDB;

namespace CSMOO.Server.Scripting
{
    /// <summary>
    /// Enhanced script globals for verb execution
    /// </summary>
    public class VerbScriptGlobals : EnhancedScriptGlobals
    {
        /// <summary>
        /// Script helpers for advanced functionality
        /// </summary>
        public new ScriptHelpers? Helpers { get; set; }
        
        /// <summary>
        /// The object this verb is running on
        /// </summary>
        public string ThisObject { get; set; } = string.Empty;
        
        /// <summary>
        /// The complete input string that triggered this verb
        /// </summary>
        public string Input { get; set; } = string.Empty;
        
        /// <summary>
        /// Parsed arguments from the input
        /// </summary>
        public List<string> Args { get; set; } = new List<string>();
        
        /// <summary>
        /// The name of the verb being executed
        /// </summary>
        public string Verb { get; set; } = string.Empty;

        /// <summary>
        /// Named variables extracted from the verb pattern (e.g., {item}, {person})
        /// </summary>
        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Get a property from the current object (this)
        /// </summary>
        public object? GetThisProperty(string propertyName)
        {
            return ObjectManager.GetProperty(ThisObject, propertyName);
        }

        /// <summary>
        /// Set a property on the current object (this)
        /// </summary>
        public void SetThisProperty(string propertyName, object value)
        {
            ObjectManager.SetProperty(ThisObject, propertyName, value);
        }

        /// <summary>
        /// Send a message to a specific player
        /// </summary>
        public new void notify(Database.Player targetPlayer, string message)
        {
            CommandProcessor?.SendToPlayer(message, targetPlayer.SessionGuid);
        }

        /// <summary>
        /// Get a player by name or ID for use with notify()
        /// </summary>
        public Database.Player? GetPlayer(string nameOrId)
        {
            // Try by name first
            var player = GameDatabase.Instance.Players.FindOne(p => 
                p.Name.Equals(nameOrId, StringComparison.OrdinalIgnoreCase));
            
            if (player != null) return player;
            
            // Try by ID
            return GameDatabase.Instance.Players.FindById(nameOrId);
        }

        /// <summary>
        /// Get the current player for use with notify() - returns the actual Database.Player object
        /// </summary>
        public new Database.Player? me => Player;

        /// <summary>
        /// Get the current player for use with notify() - returns the actual Database.Player object
        /// </summary>
        public new Database.Player? player => Player;

        /// <summary>
        /// Send a message to all players in the same room as the current player
        /// </summary>
        public new void SayToRoom(string message, bool includePlayer = false)
        {
            if (Player?.Location == null) return;

            var playersInRoom = Database.PlayerManager.GetOnlinePlayers()
                .Where(p => p.Location == Player.Location)
                .ToList();

            foreach (var otherPlayer in playersInRoom)
            {
                if (!includePlayer && otherPlayer.Id == Player.Id)
                    continue;

                CommandProcessor?.SendToPlayer(message, otherPlayer.SessionGuid);
            }
        }

        /// <summary>
        /// Find an object in the current room by name
        /// </summary>
        public string? FindObjectInRoom(string name)
        {
            if (Player?.Location == null) return null;

            var objects = Database.ObjectManager.GetObjectsInLocation(Player.Location);
            var targetObject = objects.FirstOrDefault(obj =>
            {
                var objName = Database.ObjectManager.GetProperty(obj, "name")?.AsString?.ToLower();
                var shortDesc = Database.ObjectManager.GetProperty(obj, "shortDescription")?.AsString?.ToLower();
                name = name.ToLower();
                return objName?.Contains(name) == true || shortDesc?.Contains(name) == true;
            });

            return targetObject?.Id;
        }

        /// <summary>
        /// Call a verb on an object from within another verb
        /// </summary>
        public new object? CallVerb(string objectRef, string verbName, params object[] args)
        {
            try
            {
                // Prevent calling system programming commands from scripts
                if (objectRef.Equals("system", StringComparison.OrdinalIgnoreCase) && verbName.StartsWith("@"))
                {
                    throw new InvalidOperationException($"Cannot call system programming command '{verbName}' from within a script. Programming commands must be executed directly from the command line.");
                }

                // Resolve the object reference (supports class:Object syntax, #123 DBREFs, etc.)
                var objectId = ResolveObjectFromScript(objectRef);
                if (objectId == null)
                {
                    throw new ArgumentException($"Object '{objectRef}' not found");
                }

                // Find the verb on the object (with inheritance)
                var allVerbsOnObject = VerbResolver.GetAllVerbsOnObject(objectId);
                var verbMatch = allVerbsOnObject.FirstOrDefault(v => 
                    v.verb.Name.Equals(verbName, StringComparison.OrdinalIgnoreCase));

                if (verbMatch.verb == null)
                {
                    throw new ArgumentException($"Verb '{verbName}' not found on object '{objectRef}'");
                }

                // Execute the verb with the provided arguments
                var scriptEngine = new VerbScriptEngine();
                
                // Build input string from arguments
                var inputArgs = args.Select(a => a?.ToString() ?? "").ToArray();
                var input = verbName + (inputArgs.Length > 0 ? " " + string.Join(" ", inputArgs) : "");
                
                if (Player == null || CommandProcessor == null)
                {
                    throw new InvalidOperationException("Cannot call verb without valid player and command processor context");
                }
                
                var result = scriptEngine.ExecuteVerb(verbMatch.verb, input, Player, CommandProcessor, objectId);
                
                // Try to parse result as different types
                if (string.IsNullOrEmpty(result)) return null;
                if (bool.TryParse(result, out bool boolVal)) return boolVal;
                if (int.TryParse(result, out int intVal)) return intVal;
                if (double.TryParse(result, out double doubleVal)) return doubleVal;
                return result; // Return as string if no other type matches
            }
            catch (Exception ex)
            {
                if (Player != null) notify(Player, $"Error calling {objectRef}:{verbName}() - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Resolve object reference from script context (supports all the same syntax as ResolveObject)
        /// </summary>
        private string? ResolveObjectFromScript(string objectRef)
        {
            // Handle special keywords
            switch (objectRef.ToLower())
            {
                case "this":
                    return ThisObject;
                case "me":
                    return Player?.Id;
                case "here":
                    return Player?.Location;
                case "system":
                    return GetSystemObjectId();
            }
            
            // Check if it's a DBREF (starts with # followed by digits)
            if (objectRef.StartsWith("#") && int.TryParse(objectRef.Substring(1), out int dbref))
            {
                var obj = GameDatabase.Instance.GameObjects.FindOne(o => o.DbRef == dbref);
                return obj?.Id;
            }
            
            // Check if it's a class reference
            if (objectRef.StartsWith("class:", StringComparison.OrdinalIgnoreCase))
            {
                var className = objectRef.Substring(6);
                var objectClass = GameDatabase.Instance.ObjectClasses.FindOne(c => 
                    c.Name.Equals(className, StringComparison.OrdinalIgnoreCase));
                return objectClass?.Id;
            }
            
            if (objectRef.EndsWith(".class", StringComparison.OrdinalIgnoreCase))
            {
                var className = objectRef.Substring(0, objectRef.Length - 6);
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
            
            // Try to find by name (simplified version for script context)
            var allObjects = GameDatabase.Instance.GameObjects.FindAll();
            var match = allObjects.FirstOrDefault(obj =>
            {
                var objName = (ObjectManager.GetProperty(obj.Id, "name") as BsonValue)?.AsString;
                return objName?.Equals(objectRef, StringComparison.OrdinalIgnoreCase) == true;
            });
            
            if (match != null) return match.Id;
            
            // Try as class name
            var objectClass2 = GameDatabase.Instance.ObjectClasses.FindOne(c => 
                c.Name.Equals(objectRef, StringComparison.OrdinalIgnoreCase));
            return objectClass2?.Id;
        }

        /// <summary>
        /// Get system object ID (helper method)
        /// </summary>
        private string GetSystemObjectId()
        {
            var allObjects = GameDatabase.Instance.GameObjects.FindAll();
            var systemObj = allObjects.FirstOrDefault(obj => 
                (obj.Properties.ContainsKey("name") && obj.Properties["name"].AsString == "system") ||
                (obj.Properties.ContainsKey("isSystemObject") && obj.Properties["isSystemObject"].AsBoolean == true));
            return systemObj?.Id ?? "";
        }

        /// <summary>
        /// Call a verb on the current object (this)
        /// </summary>
        public object? This(string verbName, params object[] args)
        {
            return CallVerb("this", verbName, args);
        }

        /// <summary>
        /// Call a verb on the player object (me)
        /// </summary>
        public new object? Me(string verbName, params object[] args)
        {
            return CallVerb("me", verbName, args);
        }

        /// <summary>
        /// Call a verb on the current room (here)
        /// </summary>
        public new object? Here(string verbName, params object[] args)
        {
            return CallVerb("here", verbName, args);
        }

        /// <summary>
        /// Call a verb on the system object
        /// </summary>
        public new object? System(string verbName, params object[] args)
        {
            return CallVerb("system", verbName, args);
        }

        /// <summary>
        /// Call a verb on an object by DBREF
        /// </summary>
        public new object? Object(int dbref, string verbName, params object[] args)
        {
            return CallVerb($"#{dbref}", verbName, args);
        }

        /// <summary>
        /// Call a verb on a class
        /// </summary>
        public new object? Class(string className, string verbName, params object[] args)
        {
            return CallVerb($"class:{className}", verbName, args);
        }
    }
}
