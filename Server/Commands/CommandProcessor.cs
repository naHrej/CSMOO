using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Linq;
using CSMOO.Server.Database;
using CSMOO.Server.Session;
using CSMOO.Server.Scripting;

namespace CSMOO.Server.Commands;

/// <summary>
/// Handles command processing and player interaction
/// </summary>
public class CommandProcessor
{
    private readonly Guid _sessionGuid;
    private readonly TcpClient _client;
    private Player? _player;
    private ProgrammingCommands? _programmingCommands;

    public CommandProcessor(Guid sessionGuid, TcpClient client)
    {
        _sessionGuid = sessionGuid;
        _client = client;
        _player = PlayerManager.GetPlayerBySession(sessionGuid);
        
        if (_player != null)
        {
            _programmingCommands = new ProgrammingCommands(this, _player);
        }
    }

    /// <summary>
    /// Processes a command from the player
    /// </summary>
    public void ProcessCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return;

        input = input.Trim();

        try
        {
            // If player is not logged in, handle login/creation commands
            if (_player == null)
            {
                HandlePreLoginCommand(input);
            }
            else
            {
                HandleGameCommand(input);
            }
        }
        catch (NullReferenceException ex)
        {
            SendToPlayer($"Null reference error: {ex.Message}");
            SendToPlayer($"Stack trace: {ex.StackTrace}");
            Console.WriteLine($"Null reference error in command processing: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        catch (Exception ex)
        {
            SendToPlayer($"Error processing command: {ex.Message}");
            Console.WriteLine($"Error in command processing: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private void HandlePreLoginCommand(string input)
    {
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length == 0)
        {
            SendToPlayer("Please enter a command. Type 'help' for assistance.");
            return;
        }

        var command = parts[0].ToLower();

        switch (command)
        {
            case "login":
                HandleLogin(parts);
                break;
            case "create":
                HandleCreatePlayer(parts);
                break;
            case "help":
                SendPreLoginHelp();
                break;
            case "quit":
            case "exit":
                SendToPlayer("Goodbye!");
                _client.Close();
                break;
            default:
                SendToPlayer("You must login first. Type 'help' for assistance.");
                break;
        }
    }

    private void HandleLogin(string[] parts)
    {
        if (parts.Length != 3)
        {
            SendToPlayer("Usage: login <username> <password>");
            return;
        }

        var username = parts[1];
        var password = parts[2];

        var success = SessionHandler.LoginPlayer(_sessionGuid, username, password);
        if (success)
        {
            _player = PlayerManager.GetPlayerBySession(_sessionGuid);
            _programmingCommands = new ProgrammingCommands(this, _player!);
            SendToPlayer($"Welcome back, {_player?.Name}!");
            SendToPlayer("");
            ShowRoom();
        }
        else
        {
            SendToPlayer("Invalid username or password.");
        }
    }

    private void HandleCreatePlayer(string[] parts)
    {
        if (parts.Length != 4 || parts[1].ToLower() != "player")
        {
            SendToPlayer("Usage: create player <username> <password>");
            return;
        }

        var username = parts[2];
        var password = parts[3];

        try
        {
            var startingRoom = WorldManager.GetStartingRoom();
            var newPlayer = PlayerManager.CreatePlayer(username, password, startingRoom?.Id);
            
            // Auto-login the new player
            PlayerManager.ConnectPlayerToSession(newPlayer.Id, _sessionGuid);
            _player = newPlayer;
            _programmingCommands = new ProgrammingCommands(this, _player);
            
            SendToPlayer($"Welcome to CSMOO, {username}! Your character has been created.");
            SendToPlayer("");
            ShowRoom();
        }
        catch (Exception ex)
        {
            SendToPlayer($"Failed to create player: {ex.Message}");
        }
    }

    private void HandleGameCommand(string input)
    {
        // Ensure player is still valid (might have been updated after login)
        if (_player == null)
        {
            _player = PlayerManager.GetPlayerBySession(_sessionGuid);
            if (_player != null && _programmingCommands == null)
            {
                _programmingCommands = new ProgrammingCommands(this, _player);
            }
        }

        if (_player == null)
        {
            SendToPlayer("Error: Player session not found. Please login again.");
            return;
        }

        // First check if we're in programming mode
        if (_programmingCommands?.IsInProgrammingMode == true)
        {
            _programmingCommands.HandleProgrammingCommand(input);
            return;
        }

        // Check for programming commands
        if (input.StartsWith("@") && _programmingCommands?.HandleProgrammingCommand(input) == true)
        {
            return;
        }

        // Try to execute as a verb first
        if (VerbManager.TryExecuteVerb(input, _player, this))
        {
            return;
        }

        // Fall back to built-in commands
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        var command = parts[0].ToLower();

        switch (command)
        {
            case "look":
            case "l":
                HandleLook(parts);
                break;
            case "go":
            case "move":
                HandleMove(parts);
                break;
            case "north":
            case "n":
                HandleMove(new[] { "go", "north" });
                break;
            case "south":
            case "s":
                HandleMove(new[] { "go", "south" });
                break;
            case "east":
            case "e":
                HandleMove(new[] { "go", "east" });
                break;
            case "west":
            case "w":
                HandleMove(new[] { "go", "west" });
                break;
            case "inventory":
            case "i":
                HandleInventory();
                break;
            case "get":
            case "take":
                HandleGet(parts);
                break;
            case "drop":
                HandleDrop(parts);
                break;
            case "say":
                HandleSay(input);
                break;
            case "who":
                HandleWho();
                break;
            case "script":
                HandleScript(input);
                break;
            case "help":
                SendGameHelp();
                break;
            case "quit":
            case "exit":
                HandleQuit();
                break;
            default:
                SendToPlayer($"Unknown command: {command}. Type 'help' for available commands.");
                break;
        }
    }

    private void HandleLook(string[] parts)
    {
        if (parts.Length == 1)
        {
            ShowRoom();
        }
        else
        {
            // Look at specific object
            var target = string.Join(" ", parts.Skip(1));
            LookAtObject(target);
        }
    }

    private void HandleMove(string[] parts)
    {
        if (parts.Length != 2)
        {
            SendToPlayer("Usage: go <direction>");
            return;
        }

        var direction = parts[1].ToLower();
        if (_player?.Location == null)
        {
            SendToPlayer("You are not in any location.");
            return;
        }

        var exits = WorldManager.GetExitsFromRoom(_player.Location);
        var exit = exits.FirstOrDefault(e => 
            ObjectManager.GetProperty(e, "direction")?.AsString?.ToLower() == direction);

        if (exit == null)
        {
            SendToPlayer($"There is no exit {direction}.");
            return;
        }

        var destination = ObjectManager.GetProperty(exit, "destination")?.AsString;
        if (destination == null)
        {
            SendToPlayer("That exit doesn't lead anywhere.");
            return;
        }

        // Move the player
        ObjectManager.MoveObject(_player.Id, destination);
        _player.Location = destination;
        GameDatabase.Instance.Players.Update(_player);

        SendToPlayer($"You go {direction}.");
        ShowRoom();
    }

    private void HandleInventory()
    {
        if (_player == null) return;

        var playerGameObject = GameDatabase.Instance.GameObjects.FindById(_player.Id);
        if (playerGameObject?.Contents == null || !playerGameObject.Contents.Any())
        {
            SendToPlayer("You are carrying nothing.");
            return;
        }

        SendToPlayer("You are carrying:");
        foreach (var itemId in playerGameObject.Contents)
        {
            var item = GameDatabase.Instance.GameObjects.FindById(itemId);
            if (item != null)
            {
                var name = ObjectManager.GetProperty(item, "shortDescription")?.AsString ?? "something";
                SendToPlayer($"  {name}");
            }
        }
    }

    private void HandleGet(string[] parts)
    {
        if (parts.Length < 2)
        {
            SendToPlayer("Get what?");
            return;
        }

        var itemName = string.Join(" ", parts.Skip(1)).ToLower();
        if (_player?.Location == null) return;

        var roomObjects = ObjectManager.GetObjectsInLocation(_player.Location);
        var item = roomObjects.FirstOrDefault(obj =>
        {
            var name = ObjectManager.GetProperty(obj, "name")?.AsString?.ToLower();
            var shortDesc = ObjectManager.GetProperty(obj, "shortDescription")?.AsString?.ToLower();
            return name?.Contains(itemName) == true || shortDesc?.Contains(itemName) == true;
        });

        if (item == null)
        {
            SendToPlayer("There is no such item here.");
            return;
        }

        var gettable = ObjectManager.GetProperty(item, "gettable")?.AsBoolean ?? false;
        if (!gettable)
        {
            SendToPlayer("You can't take that.");
            return;
        }

        // Move item to player's inventory
        ObjectManager.MoveObject(item.Id, _player.Id);
        var itemDesc = ObjectManager.GetProperty(item, "shortDescription")?.AsString ?? "something";
        SendToPlayer($"You take {itemDesc}.");
    }

    private void HandleDrop(string[] parts)
    {
        if (parts.Length < 2)
        {
            SendToPlayer("Drop what?");
            return;
        }

        var itemName = string.Join(" ", parts.Skip(1)).ToLower();
        if (_player?.Location == null) return;

        var playerGameObject = GameDatabase.Instance.GameObjects.FindById(_player.Id);
        if (playerGameObject?.Contents == null) return;

        var item = playerGameObject.Contents
            .Select(id => GameDatabase.Instance.GameObjects.FindById(id))
            .FirstOrDefault(obj =>
            {
                if (obj == null) return false;
                var name = ObjectManager.GetProperty(obj, "name")?.AsString?.ToLower();
                var shortDesc = ObjectManager.GetProperty(obj, "shortDescription")?.AsString?.ToLower();
                return name?.Contains(itemName) == true || shortDesc?.Contains(itemName) == true;
            });

        if (item == null)
        {
            SendToPlayer("You don't have that item.");
            return;
        }

        // Move item to current room
        ObjectManager.MoveObject(item.Id, _player.Location);
        var itemDesc = ObjectManager.GetProperty(item, "shortDescription")?.AsString ?? "something";
        SendToPlayer($"You drop {itemDesc}.");
    }

    private void HandleSay(string input)
    {
        var message = input.Substring(4).Trim(); // Remove "say "
        if (string.IsNullOrEmpty(message))
        {
            SendToPlayer("Say what?");
            return;
        }

        SendToPlayer($"You say, \"{message}\"");
        
        // Send to other players in the room
        var playersInRoom = GetPlayersInRoom(_player?.Location);
        foreach (var otherPlayer in playersInRoom.Where(p => p.Id != _player?.Id))
        {
            SendToPlayer($"{_player?.Name} says, \"{message}\"", otherPlayer.SessionGuid);
        }
    }

    private void HandleWho()
    {
        var onlinePlayers = PlayerManager.GetOnlinePlayers();
        SendToPlayer("Online players:");
        foreach (var player in onlinePlayers)
        {
            SendToPlayer($"  {player.Name}");
        }
    }

    private void HandleScript(string input)
    {
        // Extract script code (everything after "script ")
        var scriptCode = input.Substring(6).Trim();
        if (string.IsNullOrEmpty(scriptCode))
        {
            SendToPlayer("Usage: script { C# code here }");
            return;
        }

        try
        {
            var scriptEngine = new ScriptEngine();
            var result = scriptEngine.ExecuteScript(scriptCode, _player, this);
            if (!string.IsNullOrEmpty(result))
            {
                SendToPlayer($"Script result: {result}");
            }
        }
        catch (Exception ex)
        {
            SendToPlayer($"Script error: {ex.Message}");
        }
    }

    private void HandleQuit()
    {
        SendToPlayer("Goodbye!");
        if (_player != null)
        {
            PlayerManager.DisconnectPlayer(_player.Id);
        }
        _client.Close();
    }

    private void ShowRoom()
    {
        if (_player?.Location == null)
        {
            SendToPlayer("You are nowhere.");
            return;
        }

        var room = GameDatabase.Instance.GameObjects.FindById(_player.Location);
        if (room == null)
        {
            SendToPlayer("You are in a void.");
            return;
        }

        var name = ObjectManager.GetProperty(room, "name")?.AsString ?? "Unknown Room";
        var longDesc = ObjectManager.GetProperty(room, "longDescription")?.AsString ?? "You see nothing special.";

        SendToPlayer($"=== {name} ===");
        SendToPlayer(longDesc);

        // Show exits
        var exits = WorldManager.GetExitsFromRoom(_player.Location);
        if (exits.Any())
        {
            var exitNames = exits.Select(e => ObjectManager.GetProperty(e, "direction")?.AsString).Where(d => d != null);
            SendToPlayer($"Exits: {string.Join(", ", exitNames)}");
        }

        // Show objects
        var objects = ObjectManager.GetObjectsInLocation(_player.Location)
            .Where(obj => obj.ClassId != GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Exit")?.Id)
            .Where(obj => obj.ClassId != GameDatabase.Instance.ObjectClasses.FindOne(c => c.Name == "Player")?.Id);

        foreach (var obj in objects)
        {
            var visible = ObjectManager.GetProperty(obj, "visible")?.AsBoolean ?? true;
            if (visible)
            {
                var shortDesc = ObjectManager.GetProperty(obj, "shortDescription")?.AsString ?? "something";
                SendToPlayer($"You see {shortDesc} here.");
            }
        }

        // Show other players
        var otherPlayers = GetPlayersInRoom(_player.Location).Where(p => p.Id != _player.Id);
        foreach (var otherPlayer in otherPlayers)
        {
            SendToPlayer($"{otherPlayer.Name} is here.");
        }
    }

    private void LookAtObject(string target)
    {
        if (_player?.Location == null) return;

        target = target.ToLower();
        var objects = ObjectManager.GetObjectsInLocation(_player.Location);
        
        var targetObject = objects.FirstOrDefault(obj =>
        {
            var name = ObjectManager.GetProperty(obj, "name")?.AsString?.ToLower();
            var shortDesc = ObjectManager.GetProperty(obj, "shortDescription")?.AsString?.ToLower();
            return name?.Contains(target) == true || shortDesc?.Contains(target) == true;
        });

        if (targetObject == null)
        {
            SendToPlayer("You don't see that here.");
            return;
        }

        var longDesc = ObjectManager.GetProperty(targetObject, "longDescription")?.AsString ?? "You see nothing special.";
        SendToPlayer(longDesc);
    }

    private List<Player> GetPlayersInRoom(string? roomId)
    {
        if (roomId == null) return new List<Player>();
        
        return PlayerManager.GetOnlinePlayers()
            .Where(p => p.Location == roomId)
            .ToList();
    }

    private void SendPreLoginHelp()
    {
        SendToPlayer("=== CSMOO Login Help ===");
        SendToPlayer("Commands:");
        SendToPlayer("  login <username> <password>  - Login to an existing character");
        SendToPlayer("  create player <username> <password>  - Create a new character");
        SendToPlayer("  help  - Show this help");
        SendToPlayer("  quit  - Exit the game");
    }

    private void SendGameHelp()
    {
        SendToPlayer("=== CSMOO Game Commands ===");
        SendToPlayer("Movement:");
        SendToPlayer("  look, l  - Look around");
        SendToPlayer("  go <direction>, <direction>  - Move in a direction (north, south, east, west)");
        SendToPlayer("Items:");
        SendToPlayer("  inventory, i  - Show your inventory");
        SendToPlayer("  get <item>  - Pick up an item");
        SendToPlayer("  drop <item>  - Drop an item");
        SendToPlayer("Communication:");
        SendToPlayer("  say <message>  - Say something to others in the room");
        SendToPlayer("  who  - List online players");
        SendToPlayer("Scripting:");
        SendToPlayer("  script { C# code }  - Execute C# script");
        SendToPlayer("Programming (LambdaMOO style):");
        SendToPlayer("  @program <object>:<verb>  - Create/edit a verb with multi-line code");
        SendToPlayer("  @verb <object> <name> [aliases] [pattern]  - Create a new verb");
        SendToPlayer("  @list <object>:<verb>  - Show code for a verb");
        SendToPlayer("  @examine <object>  - Show detailed object information");
        SendToPlayer("  @verbs [object]  - List verbs on an object");
        SendToPlayer("  @rmverb <object>:<verb>  - Remove a verb");
        SendToPlayer("Objects: 'me' = you, 'here' = current room, 'system' = global");
        SendToPlayer("         '#123' = object by DBREF number, 'name' = search by name");
        SendToPlayer("         'class:ClassName' or 'ClassName.class' = reference a class definition");
        SendToPlayer("Other:");
        SendToPlayer("  help  - Show this help");
        SendToPlayer("  quit  - Exit the game");
    }

    public void SendToPlayer(string message, Guid? sessionGuid = null)
    {
        var targetSession = sessionGuid ?? _sessionGuid;
        var session = SessionHandler.ActiveSessions.FirstOrDefault(s => s.ClientGuid == targetSession);
        
        if (session?.Client.Connected == true)
        {
            try
            {
                var data = Encoding.UTF8.GetBytes(message + "\r\n");
                session.Client.GetStream().Write(data, 0, data.Length);
            }
            catch
            {
                // Connection lost
            }
        }
    }
}
