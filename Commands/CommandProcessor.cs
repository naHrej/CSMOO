using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using CSMOO.Database;
using CSMOO.Sessions;
using CSMOO.Scripting;
using CSMOO.Logging;
using CSMOO.Verbs;
using CSMOO.Functions;
using LiteDB;
using CSMOO.Object;
using CSMOO.Exceptions;
using CSMOO.Core;
using CSMOO.Configuration;
using CSMOO.Init;

namespace CSMOO.Commands;

/// <summary>
/// Handles command processing and player interaction
/// </summary>
public class CommandProcessor
{
    private readonly Guid _sessionGuid;
    private readonly IClientConnection _connection;
    private readonly IPlayerManager _playerManager;
    private readonly IVerbResolver _verbResolver;
    private readonly IPermissionManager _permissionManager;
    private readonly IObjectManager _objectManager;
    private readonly IFunctionResolver _functionResolver;
    private readonly IDbProvider _dbProvider;
    private readonly IGameDatabase _gameDatabase;
    private readonly ILogger _logger;
    private readonly IRoomManager _roomManager;
    private readonly IScriptEngineFactory _scriptEngineFactory;
    private readonly IVerbManager _verbManager;
    private readonly IFunctionManager _functionManager;
    private readonly IHotReloadManager? _hotReloadManager;
    private readonly ICoreHotReloadManager? _coreHotReloadManager;
    private readonly IFunctionInitializer? _functionInitializer;
    private readonly IPropertyInitializer? _propertyInitializer;
    private readonly CSMOO.Scripting.IScriptPrecompiler _scriptPrecompiler;
    private readonly CSMOO.Scripting.ICompilationCache _compilationCache;
    private Player? _player;
    private ProgrammingCommands? _programmingCommands;
    private bool _stylesheetSent = false; // Track if stylesheet has been sent to this session

    // Multiline property editor state
    private string? _editTargetObjectId;
    private string? _editTargetPropName;
    private List<string>? _editBuffer;

    // Constructor for DI - accepts all dependencies
    public CommandProcessor(
        Guid sessionGuid,
        IClientConnection connection,
        IPlayerManager playerManager,
        IVerbResolver verbResolver,
        IPermissionManager permissionManager,
        IObjectManager objectManager,
        IFunctionResolver functionResolver,
        IDbProvider dbProvider,
        IGameDatabase gameDatabase,
        ILogger logger,
        IRoomManager roomManager,
        IScriptEngineFactory scriptEngineFactory,
        IVerbManager verbManager,
        IFunctionManager functionManager,
        CSMOO.Scripting.IScriptPrecompiler scriptPrecompiler,
        CSMOO.Scripting.ICompilationCache compilationCache,
        IHotReloadManager? hotReloadManager = null,
        ICoreHotReloadManager? coreHotReloadManager = null,
        IFunctionInitializer? functionInitializer = null,
        IPropertyInitializer? propertyInitializer = null)
    {
        _sessionGuid = sessionGuid;
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _playerManager = playerManager ?? throw new ArgumentNullException(nameof(playerManager));
        _verbResolver = verbResolver ?? throw new ArgumentNullException(nameof(verbResolver));
        _permissionManager = permissionManager ?? throw new ArgumentNullException(nameof(permissionManager));
        _objectManager = objectManager ?? throw new ArgumentNullException(nameof(objectManager));
        _functionResolver = functionResolver ?? throw new ArgumentNullException(nameof(functionResolver));
        _dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
        _gameDatabase = gameDatabase ?? throw new ArgumentNullException(nameof(gameDatabase));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _roomManager = roomManager ?? throw new ArgumentNullException(nameof(roomManager));
        _scriptEngineFactory = scriptEngineFactory ?? throw new ArgumentNullException(nameof(scriptEngineFactory));
        _verbManager = verbManager ?? throw new ArgumentNullException(nameof(verbManager));
        _functionManager = functionManager ?? throw new ArgumentNullException(nameof(functionManager));
        _scriptPrecompiler = scriptPrecompiler ?? throw new ArgumentNullException(nameof(scriptPrecompiler));
        _compilationCache = compilationCache ?? throw new ArgumentNullException(nameof(compilationCache));
        _hotReloadManager = hotReloadManager;
        _coreHotReloadManager = coreHotReloadManager;
        _functionInitializer = functionInitializer;
        _propertyInitializer = propertyInitializer;
        
        _player = _playerManager.GetPlayerBySession(sessionGuid);
        
        if (_player != null)
        {
            _programmingCommands = new ProgrammingCommands(this, _player, _permissionManager, _verbManager, _functionResolver, _objectManager, _playerManager, _dbProvider, _gameDatabase, _logger, _roomManager, _functionManager, _scriptPrecompiler, _compilationCache, _hotReloadManager, _coreHotReloadManager, _functionInitializer, _propertyInitializer);
        }
    }

    // Backward compatibility constructor for Telnet
    public CommandProcessor(Guid sessionGuid, TcpClient client)
        : this(sessionGuid, new TelnetConnection(sessionGuid, client), 
               CreateDefaultPlayerManager(), CreateDefaultVerbResolver(), CreateDefaultPermissionManager(), 
               CreateDefaultObjectManager(), CreateDefaultFunctionResolver(), CreateDefaultDbProvider(), 
               CreateDefaultGameDatabase(), CreateDefaultLogger(), CreateDefaultRoomManager(), new ScriptEngineFactory(),
               CreateDefaultVerbManager(), CreateDefaultFunctionManager(), CreateDefaultScriptPrecompiler(), CreateDefaultCompilationCache(), CreateDefaultHotReloadManager(), CreateDefaultCoreHotReloadManager(), CreateDefaultFunctionInitializer(), CreateDefaultPropertyInitializer())
    {
    }

    // Backward compatibility constructor for WebSocket
    public CommandProcessor(Guid sessionGuid, IClientConnection connection)
        : this(sessionGuid, connection,
               CreateDefaultPlayerManager(), CreateDefaultVerbResolver(), CreateDefaultPermissionManager(),
               CreateDefaultObjectManager(), CreateDefaultFunctionResolver(), CreateDefaultDbProvider(),
               CreateDefaultGameDatabase(), CreateDefaultLogger(), CreateDefaultRoomManager(), new ScriptEngineFactory(),
               CreateDefaultVerbManager(), CreateDefaultFunctionManager(), CreateDefaultScriptPrecompiler(), CreateDefaultCompilationCache(), CreateDefaultHotReloadManager(), CreateDefaultCoreHotReloadManager(), CreateDefaultFunctionInitializer(), CreateDefaultPropertyInitializer())
    {
    }

    // Helper methods for backward compatibility - create default instances
    private static IPlayerManager CreateDefaultPlayerManager()
    {
        return new PlayerManagerInstance(DbProvider.Instance);
    }

    private static IVerbResolver CreateDefaultVerbResolver()
    {
        return new VerbResolverInstance(DbProvider.Instance, CreateDefaultObjectManager(), CreateDefaultLogger());
    }

    private static IPermissionManager CreateDefaultPermissionManager()
    {
        return new PermissionManagerInstance(DbProvider.Instance, CreateDefaultLogger());
    }

    private static IObjectManager CreateDefaultObjectManager()
    {
        var dbProvider = DbProvider.Instance;
        var logger = new LoggerInstance(Config.Instance);
        var classManager = new ClassManagerInstance(dbProvider, logger);
        return new ObjectManagerInstance(dbProvider, classManager);
    }

    private static IFunctionResolver CreateDefaultFunctionResolver()
    {
        return new FunctionResolverInstance(DbProvider.Instance, CreateDefaultObjectManager());
    }

    private static IDbProvider CreateDefaultDbProvider()
    {
        return DbProvider.Instance;
    }

    private static ILogger CreateDefaultLogger()
    {
        return new LoggerInstance(Config.Instance);
    }

    private static IRoomManager CreateDefaultRoomManager()
    {
        return new RoomManagerInstance(DbProvider.Instance, CreateDefaultLogger(), CreateDefaultObjectManager());
    }

    private static IVerbManager CreateDefaultVerbManager()
    {
        return new VerbManagerInstance(DbProvider.Instance);
    }

    private static IGameDatabase CreateDefaultGameDatabase()
    {
        return GameDatabase.Instance;
    }

    private static IFunctionManager CreateDefaultFunctionManager()
    {
        return new FunctionManagerInstance(new GameDatabase(Config.Instance.Database.GameDataFile));
    }

    private static IHotReloadManager? CreateDefaultHotReloadManager()
    {
        // Create default instance using the same pattern as EnsureInstance
        var config = Config.Instance;
        var logger = new LoggerInstance(config);
        var dbProvider = DbProvider.Instance;
        var classManager = new ClassManagerInstance(dbProvider, logger);
        var objectManager = new ObjectManagerInstance(dbProvider, classManager);
        var playerManager = new PlayerManagerInstance(dbProvider);
        var verbInitializer = new VerbInitializerInstance(dbProvider, logger, objectManager);
        var functionManager = CreateDefaultFunctionManager();
        var functionInitializer = new FunctionInitializerInstance(dbProvider, logger, objectManager, functionManager);
        return new HotReloadManagerInstance(logger, config, verbInitializer, functionInitializer, playerManager);
    }

    private static ICoreHotReloadManager? CreateDefaultCoreHotReloadManager()
    {
        var logger = new LoggerInstance(Config.Instance);
        var playerManager = new PlayerManagerInstance(DbProvider.Instance);
        var permissionManager = new PermissionManagerInstance(DbProvider.Instance, logger);
        return new CoreHotReloadManagerInstance(logger, playerManager, permissionManager);
    }

    private static IFunctionInitializer? CreateDefaultFunctionInitializer()
    {
        var dbProvider = DbProvider.Instance;
        var logger = new LoggerInstance(Config.Instance);
        var classManager = new ClassManagerInstance(dbProvider, logger);
        var objectManager = new ObjectManagerInstance(dbProvider, classManager);
        var functionManager = CreateDefaultFunctionManager();
        return new FunctionInitializerInstance(dbProvider, logger, objectManager, functionManager);
    }

    private static IPropertyInitializer? CreateDefaultPropertyInitializer()
    {
        var dbProvider = DbProvider.Instance;
        var logger = new LoggerInstance(Config.Instance);
        var classManager = new ClassManagerInstance(dbProvider, logger);
        var objectManager = new ObjectManagerInstance(dbProvider, classManager);
        return new PropertyInitializerInstance(dbProvider, logger, objectManager);
    }

    private static CSMOO.Scripting.IScriptPrecompiler CreateDefaultScriptPrecompiler()
    {
        var dbProvider = DbProvider.Instance;
        var logger = new LoggerInstance(Config.Instance);
        var config = Config.Instance;
        var classManager = new ClassManagerInstance(dbProvider, logger);
        var objectManager = new ObjectManagerInstance(dbProvider, classManager);
        var coreClassFactory = new CoreClassFactoryInstance(dbProvider, logger);
        var objectResolver = new ObjectResolverInstance(objectManager, coreClassFactory);
        var verbResolver = new VerbResolverInstance(dbProvider, objectManager, logger);
        var functionResolver = new FunctionResolverInstance(dbProvider, objectManager);
        var playerManager = new PlayerManagerInstance(dbProvider);
        var verbManager = new VerbManagerInstance(dbProvider);
        var roomManager = new RoomManagerInstance(dbProvider, logger, objectManager);
        return new CSMOO.Scripting.ScriptPrecompiler(objectManager, logger, config, objectResolver, verbResolver, functionResolver, dbProvider, playerManager, verbManager, roomManager);
    }

    private static CSMOO.Scripting.ICompilationCache CreateDefaultCompilationCache()
    {
        return new CSMOO.Scripting.CompilationCache();
    }

    /// <summary>
    /// Processes a command from the player
    /// </summary>
    public void ProcessCommand(string input)
    {
        // If in multiline property edit mode, handle input directly
        if (IsInMultilinePropertyEditMode())
        {
            HandleMultilinePropertyInput(input);
            return;
        }

        if (string.IsNullOrWhiteSpace(input))
            return;

        input = input.Trim();

        try
        {
            // If player is not logged in, handle login/creation commands
            if (_player == null)
            {
                _logger.Info($"[PRE-LOGIN] Command: '{input}' (Session: {_sessionGuid})");
                HandlePreLoginCommand(input);
            }
            else
            {
                _logger.Info($"[COMMAND] Player '{_player.Name}' (ID: {_player.Id}): '{input}'");
                HandleGameCommand(input);
            }
        }
        catch (NullReferenceException ex)
        {
            SendToPlayer($"Null reference error: {ex.Message}");
            SendToPlayer($"Stack trace: {ex.StackTrace}");
            _logger.Error($"Null reference error in command processing: {ex.Message}");
            _logger.Error($"Stack trace: {ex.StackTrace}");
        }
        catch (Exception ex)
        {
            SendToPlayer($"Error processing command: {ex.Message}");
            _logger.Error($"Error in command processing: {ex.Message}");
            _logger.Error($"Stack trace: {ex.StackTrace}");
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
            _player = _playerManager.GetPlayerBySession(_sessionGuid);
            if (_player != null)
            {
                _programmingCommands = new ProgrammingCommands(this, _player, _permissionManager, _verbManager, _functionResolver, _objectManager, _playerManager, _dbProvider, _gameDatabase, _logger, _roomManager, _functionManager, _scriptPrecompiler, _compilationCache, _hotReloadManager, _coreHotReloadManager, _functionInitializer, _propertyInitializer);
                
                // Log player connection and check for admin flag
                var hasAdmin = _permissionManager.HasFlag(_player, PermissionManager.Flag.Admin);
                var flags = _permissionManager.GetFlagsString(_player);
                _logger.Info($"[LOGIN] Player '{_player.Name}' (ID: {_player.Id}) connected. Admin flag: {hasAdmin}, Flags: {flags}");
                
                if (hasAdmin)
                {
                    _logger.Info($"[ADMIN] Admin player '{_player.Name}' has logged in with admin privileges.");
                }
            }
            // Reset stylesheet flag so it gets sent after login
            _stylesheetSent = false;
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
            var startingRoom = _roomManager.GetStartingRoom();
            var newPlayer = _playerManager.CreatePlayer(username, password, startingRoom?.Id);
            
            // Auto-login the new player
            _playerManager.ConnectPlayerToSession(newPlayer.Id, _sessionGuid);
            _player = newPlayer;
            _programmingCommands = new ProgrammingCommands(this, _player, _permissionManager, _verbManager, _functionResolver, _objectManager, _playerManager, _dbProvider, _gameDatabase, _logger, _roomManager, _functionManager, _scriptPrecompiler, _compilationCache, _hotReloadManager, _coreHotReloadManager, _functionInitializer, _propertyInitializer);
            
            // Log player creation and check for admin flag
            var hasAdmin = _permissionManager.HasFlag(_player, PermissionManager.Flag.Admin);
            var flags = _permissionManager.GetFlagsString(_player);
            _logger.Info($"[CREATE] New player '{_player.Name}' (ID: {_player.Id}) created. Admin flag: {hasAdmin}, Flags: {flags}");
            
            if (hasAdmin)
            {
                _logger.Info($"[ADMIN] New admin player '{_player.Name}' created with admin privileges.");
            }
            
            // Reset stylesheet flag so it gets sent after login
            _stylesheetSent = false;
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
                _player = _playerManager.GetPlayerBySession(_sessionGuid);
                if (_player != null && _programmingCommands == null)
                {
                    _programmingCommands = new ProgrammingCommands(this, _player, _permissionManager, _verbManager, _functionResolver, _objectManager, _playerManager, _dbProvider, _gameDatabase, _logger, _roomManager, _functionManager, _scriptPrecompiler, _compilationCache, _hotReloadManager, _coreHotReloadManager, _functionInitializer, _propertyInitializer);
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
                _logger.Info($"[HANDLED] Command '{input}' handled by: Programming Mode");
                _programmingCommands.HandleProgrammingCommand(input);
                return;
            }

            // Check for programming commands
            if (input.StartsWith("@") && _programmingCommands?.HandleProgrammingCommand(input) == true)
            {
                _logger.Info($"[HANDLED] Command '{input}' handled by: Programming Command");
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

            // Check for explicit "go" command first (before verb resolution)
            if (input.Equals("go", StringComparison.OrdinalIgnoreCase) || 
                input.StartsWith("go ", StringComparison.OrdinalIgnoreCase))
            {
                if (TryExecuteGoCommand(input))
                {
                    _logger.Info($"[HANDLED] Command '{input}' handled by: Go Command (explicit)");
                    return;
                }
                // If explicit "go" command didn't match an exit, fall through to verb resolution
                // (in case there's a "go" verb defined somewhere)
            }

            // Try to execute as a verb first
            if (_verbResolver.TryExecuteVerb(input, _player, this))
            {
                // Verb logging is done inside VerbResolverInstance with verb name and object details
                return;
            }

            // After verb resolution fails, check for implicit "go" command (direct exit name/abbreviation)
            // Only check if input is NOT an explicit "go" command (already handled above)
            if (!input.Equals("go", StringComparison.OrdinalIgnoreCase) && 
                !input.StartsWith("go ", StringComparison.OrdinalIgnoreCase))
            {
                var exit = FindExitByNameOrAbbreviation(input, _player);
                if (exit != null)
                {
                    if (TryExecuteExit(exit, _player))
                    {
                        _logger.Info($"[HANDLED] Command '{input}' handled by: Exit (implicit)");
                        return;
                    }
                }
            }

            // Fall back to essential built-in commands only
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            var command = parts[0].ToLowerInvariant();

            switch (command)
            {
                case "script":
                    _logger.Info($"[HANDLED] Command '{input}' handled by: Built-in Command (script)");
                    HandleScript(input);
                    break;
                case "@password":
                    _logger.Info($"[HANDLED] Command '{input}' handled by: Built-in Command (@password)");
                    HandlePasswordCommand(parts);
                    break;
                case "@name":
                    _logger.Info($"[HANDLED] Command '{input}' handled by: Built-in Command (@name)");
                    HandleNameCommand(parts);
                    break;
                // case "help":
                //     SendGameHelp();
                //     break;
                case "quit":
                case "exit":
                    _logger.Info($"[HANDLED] Command '{input}' handled by: Built-in Command (quit/exit)");
                    HandleQuit();
                    break;
                default:
                    _logger.Warning($"[UNHANDLED] Command '{input}' not recognized - sending 'Unknown command' message to player");
                    SendToPlayer($"Unknown command: {command}. Type 'help' for available commands.");
                    break;
            }
        }
        catch (Exception ex)
        {
            SendToPlayer($"Error processing command \"{input.ToUpperInvariant()}\": {ex.Message}");
            _logger.Error($"Error in command processing: {ex.Message}");
            _logger.Error($"Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Extracts all capitalized letters and numbers from an exit name to create an abbreviation
    /// </summary>
    private static string ExtractAbbreviation(string exitName)
    {
        if (string.IsNullOrEmpty(exitName))
            return string.Empty;

        var abbreviation = new System.Text.StringBuilder();
        foreach (char c in exitName)
        {
            if (char.IsUpper(c) || char.IsDigit(c))
            {
                abbreviation.Append(c);
            }
        }
        return abbreviation.ToString();
    }

    /// <summary>
    /// Finds an exit in the player's current room matching the input (by exact name or abbreviation)
    /// </summary>
    private GameObject? FindExitByNameOrAbbreviation(string input, Player player)
    {
        if (player?.Location == null || string.IsNullOrWhiteSpace(input))
            return null;

        var exits = _roomManager.GetExits(player.Location.Id);
        if (exits.Count == 0)
            return null;

        var lowerInput = input.Trim().ToLowerInvariant();
        var upperInput = input.Trim().ToUpperInvariant();

        foreach (var exit in exits)
        {
            var exitDirection = _objectManager.GetProperty(exit, "direction")?.AsString;
            if (string.IsNullOrEmpty(exitDirection))
                continue;

            // Check exact name match (case-insensitive)
            if (exitDirection.Equals(input, StringComparison.OrdinalIgnoreCase) ||
                exitDirection.Trim('"').Equals(lowerInput, StringComparison.OrdinalIgnoreCase))
            {
                return exit;
            }

            // Check abbreviation match
            // Extract abbreviation from exit name only, then compare with literal user input (uppercased)
            var exitAbbreviation = ExtractAbbreviation(exitDirection).ToUpperInvariant();
            if (!string.IsNullOrEmpty(exitAbbreviation))
            {
                // Compare the literal user input (uppercased) with the exit abbreviation
                // e.g., user types "n" -> "N" should match "N" from "North"
                if (exitAbbreviation.Equals(upperInput, StringComparison.OrdinalIgnoreCase))
                {
                    return exit;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Attempts to move the player through the matched exit
    /// </summary>
    private bool TryExecuteExit(GameObject exit, Player player)
    {
        if (exit == null || player == null)
            return false;

        var destination = _roomManager.GetExitDestination(exit);
        if (destination == null)
        {
            SendToPlayer("<p class='error' style='color:red'>That exit doesn't lead anywhere.</p>");
            return true; // Exit exists but broken - we handled the command
        }

        // Move the player
        if (_objectManager.MoveObject(player, destination))
        {
            var exitDirection = _objectManager.GetProperty(exit, "direction")?.AsString ?? "that way";
            SendToPlayer($"<p class='success' style='color:dodgerblue'>You go <span class='param' style='color:yellow'>{exitDirection}</span>.</p>");
            
            // Show destination description using the room's Description() function
            try
            {
                // Try to call the Description() function on the destination room
                var scriptEngine = _scriptEngineFactory.Create();
                var descriptionFunction = _functionResolver.FindFunction(destination.Id, "Description");
                if (descriptionFunction != null)
                {
                    var description = scriptEngine.ExecuteFunction(descriptionFunction, Array.Empty<object>(), player, this, destination.Id);
                    if (description != null && !string.IsNullOrEmpty(description.ToString()))
                    {
                        SendToPlayer(description.ToString()!);
                    }
                    else
                    {
                        // Fallback to property access if Description() returns null/empty
                        var destinationName = _objectManager.GetProperty(destination, "name")?.AsString ?? destination.Id;
                        var destinationDesc = _objectManager.GetProperty(destination, "description")?.AsString ?? "You see nothing special.";
                        SendToPlayer($"<h3 style='color:dodgerblue;margin:0;font-weight:bold'>{destinationName}</h3>");
                        if (!string.IsNullOrEmpty(destinationDesc))
                        {
                            SendToPlayer($"<p style='margin:0.5em 0'>{destinationDesc}</p>");
                        }
                    }
                }
                else
                {
                    // No Description() function, use property access as fallback
                    var destinationName = _objectManager.GetProperty(destination, "name")?.AsString ?? destination.Id;
                    var destinationDesc = _objectManager.GetProperty(destination, "description")?.AsString ?? "You see nothing special.";
                    SendToPlayer($"<h3 style='color:dodgerblue;margin:0;font-weight:bold'>{destinationName}</h3>");
                    if (!string.IsNullOrEmpty(destinationDesc))
                    {
                        SendToPlayer($"<p style='margin:0.5em 0'>{destinationDesc}</p>");
                    }
                }
            }
            catch
            {
                // If function execution fails, fall back to property access
                try
                {
                    var name = _objectManager.GetProperty(destination, "name")?.AsString ?? destination.Id;
                    var desc = _objectManager.GetProperty(destination, "description")?.AsString ?? "You see nothing special.";
                    SendToPlayer($"<h3 style='color:dodgerblue;margin:0;font-weight:bold'>{name}</h3>");
                    if (!string.IsNullOrEmpty(desc))
                    {
                        SendToPlayer($"<p style='margin:0.5em 0'>{desc}</p>");
                    }
                }
                catch
                {
                    // Last resort fallback
                    var name = _objectManager.GetProperty(destination, "name")?.AsString ?? destination.Id;
                    SendToPlayer($"<h3>{name}</h3>");
                }
            }
            
            return true;
        }
        else
        {
            SendToPlayer("<p class='error' style='color:red'>You can't go that way.</p>");
            return true; // We handled the command, but movement failed
        }
    }

    /// <summary>
    /// Tries to execute the input as a "go" command for movement
    /// </summary>
    private bool TryExecuteGoCommand(string input)
    {
        if (_player == null) return false;

        // Handle explicit "go" command - extract parameter
        string parameter;
        if (input.Equals("go", StringComparison.OrdinalIgnoreCase))
        {
            // Just "go" with no parameter - show available exits
            if (_player.Location == null) return false;
            var availableExits = _roomManager.GetExits(_player.Location.Id);
            if (availableExits.Count == 0)
            {
                SendToPlayer("<p class='error' style='color:red'>There are no exits from here.</p>");
                return true;
            }

            var exitNames = new List<string>();
            foreach (var exitObj in availableExits)
            {
                var dir = _objectManager.GetProperty(exitObj, "direction")?.AsString;
                if (!string.IsNullOrEmpty(dir))
                {
                    exitNames.Add(dir);
                }
            }
            var exitsList = $"Available exits: <span class='param' style='color:yellow'>{string.Join(", ", exitNames)}</span>";
            SendToPlayer(exitsList);
            SendToPlayer("<p class='usage' style='color:green'>Usage: <span class='command' style='color:yellow'>go <span class='param' style='color:gray'>&lt;direction&gt;</span></span></p>");
            return true;
        }
        else if (input.StartsWith("go ", StringComparison.OrdinalIgnoreCase))
        {
            parameter = input.Substring(3).Trim();
        }
        else
        {
            // Not a "go" command
            return false;
        }

        if (string.IsNullOrWhiteSpace(parameter))
        {
            // "go " with no parameter - show available exits
            if (_player.Location == null) return false;
            var availableExitsList = _roomManager.GetExits(_player.Location.Id);
            if (availableExitsList.Count == 0)
            {
                SendToPlayer("<p class='error' style='color:red'>There are no exits from here.</p>");
                return true;
            }

            var exitNamesList = new List<string>();
            foreach (var exitObj in availableExitsList)
            {
                var dir = _objectManager.GetProperty(exitObj, "direction")?.AsString;
                if (!string.IsNullOrEmpty(dir))
                {
                    exitNamesList.Add(dir);
                }
            }
            var exitsDisplay = $"Available exits: <span class='param' style='color:yellow'>{string.Join(", ", exitNamesList)}</span>";
            SendToPlayer(exitsDisplay);
            SendToPlayer("<p class='usage' style='color:green'>Usage: <span class='command' style='color:yellow'>go <span class='param' style='color:gray'>&lt;direction&gt;</span></span></p>");
            return true;
        }

        // Find exit by name or abbreviation
        var exit = FindExitByNameOrAbbreviation(parameter, _player);
        if (exit == null)
        {
            SendToPlayer("<p class='error' style='color:red'>You can't go that way.</p>");
            return true; // We handled the command, but no exit matched
        }

        return TryExecuteExit(exit, _player);
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
            var scriptEngine = _scriptEngineFactory.Create();
            var result = scriptEngine.ExecuteVerb(
                new Verb { Name = "script", Code = scriptCode, ObjectId = _player?.Id ?? "system" },
                scriptCode, _player!, this);
            if (!string.IsNullOrEmpty(result))
            {
                _logger.Info($"[SCRIPT RESULT] Player '{_player?.Name}' (ID: {_player?.Id}): Script result: {result}");
                SendToPlayer($"Script result: {result}");
            }
            else
            {
                _logger.Info($"[SCRIPT RESULT] Player '{_player?.Name}' (ID: {_player?.Id}): Script executed successfully (no result)");
            }
        }
        catch (Exception ex)
        {
            if (ex is ScriptExecutionException scriptEx)
            {
                // Send the full HTML formatted error to the player
                SendToPlayer(scriptEx.ToString());
            }
            else
            {
                SendToPlayer($"Script error: {ex.Message}");
            }
            
            // Clear the script stack trace in case of unhandled errors
            ScriptStackTrace.Clear();
        }
    }

    private void HandleQuit()
    {
        SendToPlayer("Goodbye!");
        if (_player != null)
        {
            _playerManager.DisconnectPlayer(_player.Id);
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
            _playerManager.ChangePassword(_player.Id, newPassword);
            SendToPlayer("Your password has been changed.");
        }
        else if (parts.Length == 3)
        {
            // Admin changing another player's password
            var targetName = parts[1];
            var newPassword = parts[2];
            if (!_permissionManager.HasFlag(_player, PermissionManager.Flag.Admin))
            {
                SendToPlayer("You do not have permission to change other players' passwords.");
                return;
            }
            var targetPlayer = _playerManager.FindPlayerByName(targetName);
            if (targetPlayer == null)
            {
                SendToPlayer($"Player '{targetName}' not found.");
                return;
            }
            _playerManager.ChangePassword(targetPlayer.Id, newPassword);
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
            _dbProvider.Update("gameobjects", _player);
            _dbProvider.Update("players", _player);
            SendToPlayer($"Your name has been changed to '{newName}'.");
        }
        else if (parts.Length == 3)
        {
            // Admin changing another player's name
            var targetName = parts[1];
            var newName = parts[2];
            if (!_permissionManager.HasFlag(_player, PermissionManager.Flag.Admin))
            {
                SendToPlayer("You do not have permission to change other players' names.");
                return;
            }
            var targetPlayer = _playerManager.FindPlayerByName(targetName);
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
            _dbProvider.Update("gameobjects", targetPlayer);
            _dbProvider.Update("players", targetPlayer);
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
            _logger.Info("DisplayLoginBanner: Starting login banner display");
            
            // send static Stylesheet.less as css to client
            string css = Html.GetStylesheet();
            if (string.IsNullOrEmpty(css))
            {
                css = "/* No CSS available */";
            }
            SendToPlayer($"<style type='text/css'>{css}</style><hr/>");

            // Find all system objects (there might be multiple)
            var allObjects = _objectManager.GetAllObjects();
            _logger.Info($"DisplayLoginBanner: Found {allObjects.Count} total objects");
            
            var systemObjects = allObjects.OfType<GameObject>().Where(obj => 
                (obj.Properties.ContainsKey("name") && obj.Properties["name"].AsString == "system") ||
                (obj.Properties.ContainsKey("isSystemObject") && obj.Properties["isSystemObject"].AsBoolean == true)).ToList();
            
            _logger.Info($"DisplayLoginBanner: Found {systemObjects.Count} system object(s)");
            
            if (systemObjects.Count > 0)
            {
                // Try to find the display_login function on any system object
                Function? function = null;
                GameObject? systemObj = null;
                
                foreach (var sysObj in systemObjects)
                {
                    var objName = sysObj.Properties.ContainsKey("name") ? sysObj.Properties["name"].AsString : "null";
                    _logger.Info($"DisplayLoginBanner: Checking system object {sysObj.Id} (name: {objName})");
                    
                    // Get all functions on this system object for debugging
                    var allFunctions = _functionResolver.GetFunctionsForObject(sysObj.Id, includeSystemFunctions: true);
                    _logger.Info($"DisplayLoginBanner: System object {sysObj.Id} has {allFunctions.Count} function(s): {string.Join(", ", allFunctions.Select(f => f.Name))}");
                    
                    var foundFunction = _functionResolver.FindFunction(sysObj.Id, "display_login");
                    if (foundFunction != null)
                    {
                        _logger.Info($"DisplayLoginBanner: Found display_login function on system object {sysObj.Id}");
                        function = foundFunction;
                        systemObj = sysObj;
                        break;
                    }
                    else
                    {
                        _logger.Warning($"DisplayLoginBanner: display_login function NOT found on system object {sysObj.Id}");
                    }
                }
                
                if (function != null && systemObj != null)
                {
                    _logger.Info($"DisplayLoginBanner: Executing display_login function (ID: {function.Id}, ObjectId: {function.ObjectId})");
                    try
                    {
                        var functionEngine = _scriptEngineFactory.Create();
                        
                        // Create a minimal system player context for login banner
                        var systemPlayer = new Player
                        {
                            Id = systemObj.Id,
                            Name = "System"
                        };
                        
                        _logger.Info("DisplayLoginBanner: Calling ExecuteFunction...");
                        var result = functionEngine.ExecuteFunction(function, new object[0], systemPlayer, this, systemObj.Id);
                        _logger.Info($"DisplayLoginBanner: ExecuteFunction returned: {(result != null ? result.GetType().Name : "null")}");
                        
                        if (result != null)
                        {
                            var output = result.ToString();
                            _logger.Info($"DisplayLoginBanner: Function output length: {output?.Length ?? 0}");
                            if (!string.IsNullOrEmpty(output))
                            {                                
                                SendToPlayer(output);
                                _logger.Info("DisplayLoginBanner: Successfully sent login banner to player");
                                return;
                            }
                            else
                            {
                                _logger.Warning("DisplayLoginBanner: Function returned empty output");
                            }
                        }
                        else
                        {
                            _logger.Warning("DisplayLoginBanner: Function returned null");
                        }
                    }
                    catch (Exception funcEx)
                    {
                        _logger.Error($"DisplayLoginBanner: Error executing display_login function: {funcEx.Message}");
                        _logger.Error($"DisplayLoginBanner: Stack trace: {funcEx.StackTrace}");
                    }
                }
                else
                {
                    _logger.Warning($"DisplayLoginBanner: display_login function not found on any of {systemObjects.Count} system object(s)");
                }
            }
            else
            {
                _logger.Warning("DisplayLoginBanner: No system objects found for login banner");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"DisplayLoginBanner: Error displaying login banner from function: {ex.Message}");
            _logger.Error($"DisplayLoginBanner: Stack trace: {ex.StackTrace}");
        }

        // Fallback to static banner if function fails or doesn't exist
        _logger.Info("DisplayLoginBanner: Falling back to static banner");
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
                // Send stylesheet once per session when player is logged in
                if (_player != null && !_stylesheetSent)
                {
                    try
                    {
                        string css = Html.GetStylesheet();
                        if (!string.IsNullOrEmpty(css))
                        {
                            _ = session.Connection.SendMessageAsync($"<style type='text/css'>{css}</style>\r\n");
                        }
                    }
                    catch
                    {
                        // Stylesheet not available, continue without it
                    }
                    _stylesheetSent = true;
                }
                
                _ = session.Connection.SendMessageAsync(message + "\r\n");
            }
            catch
            {
                // Connection lost
            }
        }
    }

    /// <summary>
    /// Starts multiline property editing mode for @edit <object>.<property>
    /// </summary>
    public void StartMultilinePropertyEdit(string objectId, string propName)
    {
        _editTargetObjectId = objectId;
        _editTargetPropName = propName;
        _editBuffer = new List<string>();
        SendToPlayer($"Editing property '{propName}' on object '{objectId}'. Enter lines, '.' alone to finish, or '.abort' to cancel.");
    }

    // Returns true if currently in multiline property edit mode
    public bool IsInMultilinePropertyEditMode()
    {
        return _editBuffer != null;
    }

    // Make multiline property input handler public
    public bool HandleMultilinePropertyInput(string input)
    {
        if (_editBuffer == null) return false;
        if (input.Trim() == ".abort")
        {
            SendToPlayer("Edit cancelled. No changes saved.");
            _editTargetObjectId = null;
            _editTargetPropName = null;
            _editBuffer = null;
            return true;
        }
        if (input.Trim() == ".")
        {
            var obj = _objectManager.GetObject(_editTargetObjectId!);
            if (obj != null && _editTargetPropName != null)
            {
                obj.Properties[_editTargetPropName] = new BsonArray(_editBuffer.Select(line => new BsonValue(line)));
                _dbProvider.Update("gameobjects", obj);
                SendToPlayer($"Property '{_editTargetPropName}' updated with {_editBuffer.Count} lines.");
            }
            else
            {
                SendToPlayer("Error: Object or property not found.");
            }
            _editTargetObjectId = null;
            _editTargetPropName = null;
            _editBuffer = null;
            return true;
        }
        _editBuffer.Add(input);
        SendToPlayer($"[{_editBuffer.Count}] ");
        return true;
    }
}


