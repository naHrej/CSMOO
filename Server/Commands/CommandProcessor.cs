using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Linq;
using CSMOO.Server.Database;
using CSMOO.Server.Session;
using CSMOO.Server.Scripting;
using CSMOO.Server.Logging;
using LiteDB;

namespace CSMOO.Server.Commands;

/// <summary>
/// Handles command processing and player interaction
/// </summary>
public class CommandProcessor
{
    private readonly Guid _sessionGuid;
    private readonly IClientConnection _connection;
    private Player? _player;
    private ProgrammingCommands? _programmingCommands;

    public CommandProcessor(Guid sessionGuid, TcpClient client)
    {
        _sessionGuid = sessionGuid;
        _connection = new TelnetConnection(sessionGuid, client);
        _player = PlayerManager.GetPlayerBySession(sessionGuid);
        
        if (_player != null)
        {
            _programmingCommands = new ProgrammingCommands(this, _player);
        }
    }

    public CommandProcessor(Guid sessionGuid, IClientConnection connection)
    {
        _sessionGuid = sessionGuid;
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
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
            Logger.Error($"Null reference error in command processing: {ex.Message}");
            Logger.Error($"Stack trace: {ex.StackTrace}");
        }
        catch (Exception ex)
        {
            SendToPlayer($"Error processing command: {ex.Message}");
            Logger.Error($"Error in command processing: {ex.Message}");
            Logger.Error($"Stack trace: {ex.StackTrace}");
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
            case "con":
            case "connect":
            case "login":
                HandleLogin(parts);
                break;
            case "create":
                HandleCreatePlayer(parts);
                break;
            // case "help":
            //     SendPreLoginHelp();
            //     break;
            case "quit":
            case "exit":
                SendToPlayer("Goodbye!");
                _connection.Disconnect();
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
            SendToPlayer("Type 'look' to see your surroundings.");
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
            SendToPlayer("Type 'look' to see your surroundings.");
        }
        catch (Exception ex)
        {
            SendToPlayer($"Failed to create player: {ex.Message}");
        }
    }

    private void HandleGameCommand(string input)
    {
        try
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

            // Insert space after special command characters if at the start
            if (!string.IsNullOrEmpty(input))
            {
                char[] specialChars = [';', '\'', '"', ':', '!'];
                char firstChar = input[0];
                if (specialChars.Contains(firstChar) && input.Length > 1 && !char.IsWhiteSpace(input[1]))
                {
                    input = $"{firstChar} {input.Substring(1)}";
                }
            }

            // Try to execute as a verb first
            if (VerbResolver.TryExecuteVerb(input, _player, this))
            {
                return;
            }

            // Fall back to essential built-in commands only
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            var command = parts[0].ToLowerInvariant();

            switch (command)
            {
                case "script":
                    HandleScript(input);
                    break;
                case "@password":
                    HandlePasswordCommand(parts);
                    break;
                case "@name":
                    HandleNameCommand(parts);
                    break;
                // case "help":
                //     SendGameHelp();
                //     break;
                case "quit":
                case "exit":
                    HandleQuit();
                    break;
                default:
                    SendToPlayer($"Unknown command: {command}. Type 'help' for available commands.");
                    break;
            }
        }
        catch (Exception ex)
        {
            SendToPlayer($"Error processing command \"{input.ToUpperInvariant()}\": {ex.Message}");
            Logger.Error($"Error in command processing: {ex.Message}");
            Logger.Error($"Stack trace: {ex.StackTrace}");
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
        _connection.Disconnect();
    }

    private void HandlePasswordCommand(string[] parts)
    {
        if (_player == null)
        {
            SendToPlayer("You must be logged in to change your password.");
            return;
        }
        if (parts.Length == 2)
        {
            // Change own password
            var newPassword = parts[1];
            PlayerManager.ChangePassword(_player.Id, newPassword);
            SendToPlayer("Your password has been changed.");
        }
        else if (parts.Length == 3)
        {
            // Admin changing another player's password
            var targetName = parts[1];
            var newPassword = parts[2];
            if (!PermissionManager.HasFlag(_player, PermissionManager.Flag.Admin))
            {
                SendToPlayer("You do not have permission to change other players' passwords.");
                return;
            }
            var targetPlayer = PlayerManager.FindPlayerByName(targetName);
            if (targetPlayer == null)
            {
                SendToPlayer($"Player '{targetName}' not found.");
                return;
            }
            PlayerManager.ChangePassword(targetPlayer.Id, newPassword);
            SendToPlayer($"Password for '{targetName}' has been changed.");
        }
        else
        {
            SendToPlayer("Usage: @password <newpassword> OR @password <playername> <newpassword> (admin only)");
        }
    }

    private void HandleNameCommand(string[] parts)
    {
        if (_player == null)
        {
            SendToPlayer("You must be logged in to change your name.");
            return;
        }
        if (parts.Length == 2)
        {
            // Change own name
            var newName = parts[1];
            if (string.IsNullOrWhiteSpace(newName))
            {
                SendToPlayer("Name cannot be empty.");
                return;
            }
            _player.Name = newName;
            _player.Properties["name"] = new BsonValue(newName);
            DbProvider.Instance.Update("gameobjects", _player);
            DbProvider.Instance.Update("players", _player);
            SendToPlayer($"Your name has been changed to '{newName}'.");
        }
        else if (parts.Length == 3)
        {
            // Admin changing another player's name
            var targetName = parts[1];
            var newName = parts[2];
            if (!PermissionManager.HasFlag(_player, PermissionManager.Flag.Admin))
            {
                SendToPlayer("You do not have permission to change other players' names.");
                return;
            }
            var targetPlayer = PlayerManager.FindPlayerByName(targetName);
            if (targetPlayer == null)
            {
                SendToPlayer($"Player '{targetName}' not found.");
                return;
            }
            if (string.IsNullOrWhiteSpace(newName))
            {
                SendToPlayer("Name cannot be empty.");
                return;
            }
            targetPlayer.Name = newName;
            DbProvider.Instance.Update("gameobjects", targetPlayer);
            DbProvider.Instance.Update("players", targetPlayer);
            SendToPlayer($"Name for '{targetName}' has been changed to '{newName}'.");
        }
        else
        {
            SendToPlayer("Usage: @name <newname> OR @name <playername> <newname> (admin only)");
        }
    }

    /// <summary>
    /// Displays the login banner by executing the system:display_login function
    /// </summary>
    public void DisplayLoginBanner()
    {
        try
        {
            // send static Stylesheet.less as css to client
            string css = Html.GetStylesheet();
            if (string.IsNullOrEmpty(css))
            {
                css = "/* No CSS available */";
            }
            Logger.Debug($"Sending login banner CSS to player {_player?.Name}: {css}");
            SendToPlayer($"<style type='text/css'>{css}</style><hr/>");
            Logger.Debug($"Login banner CSS sent to player {_player?.Name}\n<style type='text/css'>{css}</style><hr/>");

            // Find the system object first
            var allObjects = DbProvider.Instance.FindAll<GameObject>("gameobjects").ToList();
            var systemObj = allObjects.FirstOrDefault(obj => 
                (obj.Properties.ContainsKey("name") && obj.Properties["name"].AsString == "system") ||
                (obj.Properties.ContainsKey("isSystemObject") && obj.Properties["isSystemObject"].AsBoolean == true));
            
            if (systemObj != null)
            {
                // Try to find and execute the system:display_login function
                var function = Scripting.FunctionResolver.FindFunction(systemObj.Id, "display_login");
                if (function != null)
                {
                    try
                    {
                        var functionEngine = new Scripting.UnifiedScriptEngine();
                        
                        // Create a minimal system player context for login banner
                        var systemPlayer = new Database.Player
                        {
                            Id = systemObj.Id,
                            Name = "System"
                        };
                        
                        var result = functionEngine.ExecuteFunction(function, new object[0], systemPlayer, this, systemObj.Id);
                        if (result != null)
                        {
                            var output = result.ToString();
                            if (!string.IsNullOrEmpty(output))
                            {                                
                                SendToPlayer(output);
                                return;
                            }
                        }
                    }
                    catch (Exception funcEx)
                    {
                        Logger.Warning($"Error executing display_login function: {funcEx.Message}");
                    }
                }
                else
                {
                    Logger.Warning("display_login function not found on system object");
                }
            }
            else
            {
                Logger.Warning("System object not found for login banner");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error displaying login banner from function: {ex.Message}");
        }

        // Fallback to static banner if function fails or doesn't exist
        SendToPlayer("=== Welcome to CSMOO ===");
        SendToPlayer("A Multi-User Shared Object-Oriented Environment");
        SendToPlayer("");
        SendToPlayer("Please login or create a new character.");
        SendToPlayer("Type 'help' for assistance.");
        SendToPlayer("");
    }

    public void SendToPlayer(string message, Guid? sessionGuid = null)
    {
        var targetSession = sessionGuid ?? _sessionGuid;
        var session = SessionHandler.ActiveSessions.FirstOrDefault(s => s.ClientGuid == targetSession);
        
        if (session?.Connection.IsConnected == true)
        {
            try
            {
                _ = session.Connection.SendMessageAsync(message + "\r\n");
            }
            catch
            {
                // Connection lost
            }
        }
    }
}
