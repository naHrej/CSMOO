using System.Text;
using CSMOO.Database;
using CSMOO.Scripting;
using CSMOO.Logging;
using CSMOO.Verbs;
using CSMOO.Functions;
using CSMOO.Core;
using CSMOO.Object;

namespace CSMOO.Commands;

/// <summary>
/// Handles programming commands for creating and editing verbs
/// </summary>
public class ProgrammingCommands
{
    private readonly CommandProcessor _commandProcessor;
    private readonly Player _player;
    
    // For multi-line programming
    private bool _isInProgrammingMode = false;
    private readonly StringBuilder _currentCode = new StringBuilder();
    private string _currentVerbId = string.Empty;
    private string _currentFunctionId = string.Empty;

    public ProgrammingCommands(CommandProcessor commandProcessor, Player player)
    {
        _commandProcessor = commandProcessor;
        _player = player;
    }

    public bool IsInProgrammingMode => _isInProgrammingMode;

    /// <summary>
    /// Handles programming-related commands
    /// </summary>
    public bool HandleProgrammingCommand(string input)
    {
        // If we're in programming mode, handle code input
        if (_isInProgrammingMode)
        {
            return HandleProgrammingInput(input);
        }

        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;

        var command = parts[0].ToLower();

        return command switch
        {
            "@program" => HandleProgramCommand(parts),
            "@script" => HandleScriptCommand(parts),
            "@verb" => HandleVerbCommand(parts),
            "@list" => HandleListCommand(parts),
            "@edit" => HandleEditCommand(parts),
            "@verbs" => HandleVerbsCommand(parts),
            "@funcs" => HandleFuncsCommand(parts),
            "@rmverb" => HandleRemoveVerbCommand(parts),
            "@flag" => HandleFlagCommand(parts),
            "@flags" => HandleFlagsCommand(parts),
            "@update-permissions" => HandleUpdatePermissionsCommand(parts),
            "@debug" when parts.Length > 1 && parts[1] == "verbs" => HandleDebugVerbsCommand(parts),
            "@fix" when parts.Length > 1 && parts[1] == "verbs" => HandleFixVerbsCommand(parts),
            "@remove" when parts.Length > 1 && parts[1] == "verb" => HandleRemoveVerbByIdCommand(parts),
            "@cleanup" when parts.Length > 1 && parts[1] == "player" => HandleCleanupPlayerCommand(parts),
            "@cleanup" => HandleCleanupCommand(parts),
            "@func" => HandleFuncCommand(parts),
            "@function" => HandleFunctionCommand(parts),
            "@functions" => HandleFunctionsCommand(parts),
            "@funcreload" => HandleFuncReloadCommand(parts),
            "@reload" => HandleReloadCommand(parts),
            "@hotreload" => HandleHotReloadCommand(parts),
            "@corehot" => HandleCoreHotReloadCommand(parts),
            _ => false
        };
    }

    /// <summary>
    /// @program <object>:<verb> or <object>.<function>() - Start programming a verb or function
    /// </summary>
    private bool HandleProgramCommand(string[] parts)
    {
        // Check if player has Programmer flag
        if (!PermissionManager.HasFlag(_player, PermissionManager.Flag.Programmer))
        {
            _commandProcessor.SendToPlayer("You need the Programmer flag to use the @program command.");
            return true;
        }

        if (parts.Length != 2)
        {
            _commandProcessor.SendToPlayer("Usage: @program <object>:<verb> or @program <object>.<function>()");
            _commandProcessor.SendToPlayer("Example: @program here:test or @program me:inventory");
            _commandProcessor.SendToPlayer("Example: @program system.test_params()");
            return true;
        }

        var spec = parts[1];
        
        // Check if this is a function specification (object.function())
        if (spec.Contains(".") && spec.EndsWith("()"))
        {
            return HandleProgramFunctionByDotNotation(spec);
        }
        // Check if this is the old f:object:function format (keep for backward compatibility)
        else if (spec.StartsWith("f:"))
        {
            var functionSpec = spec.Substring(2); // Remove "f:" prefix
            if (!functionSpec.Contains(':'))
            {
                _commandProcessor.SendToPlayer("Function specification must be in format f:<object>:<function>");
                return true;
            }
            return HandleProgramFunctionByName(functionSpec);
        }
        // Legacy support for function:id format
        else if (spec.StartsWith("function:"))
        {
            return HandleProgramFunction(spec.Substring(9)); // Remove "function:" prefix
        }
        // Check if it's a verb specification (object:verb)
        else if (spec.Contains(':'))
        {
            return HandleProgramVerb(spec);
        }
        else
        {
            _commandProcessor.SendToPlayer("Specification must be in format <object>:<verb> or <object>.<function>()");
            return true;
        }
    }

    /// <summary>
    /// Handle programming a verb
    /// </summary>
    private bool HandleProgramVerb(string verbSpec)
    {
        // Handle class syntax (split from right for class:Object:verb)
        var lastColonIndex = verbSpec.LastIndexOf(':');
        var objectName = verbSpec.Substring(0, lastColonIndex);
        var verbName = verbSpec.Substring(lastColonIndex + 1);

        // Resolve object
        var objectId = ResolveObject(objectName);
        if (objectId == null)
        {
            _commandProcessor.SendToPlayer($"Object '{objectName}' not found.");
            return true;
        }

        // Find or create the verb
        var verb = VerbManager.GetVerbsOnObject(objectId)
            .FirstOrDefault(v => v.Name.ToLower() == verbName.ToLower());

        if (verb == null)
        {
            verb = VerbManager.CreateVerb(objectId, verbName, "", "", _player.Name);
            _commandProcessor.SendToPlayer($"Created new verb '{verbName}' on {GetObjectName(objectId)}.");
        }
        else
        {
            _commandProcessor.SendToPlayer($"Editing existing verb '{verbName}' on {GetObjectName(objectId)}.");
        }

        // Enter programming mode
        _isInProgrammingMode = true;
        _currentVerbId = verb.Id;
        _currentFunctionId = string.Empty; // Clear function ID
        _currentCode.Clear(); // Always start with empty code - @program replaces existing code
        
        // if (!string.IsNullOrEmpty(verb.Code))
        // {
        //     _commandProcessor.SendToPlayer("Existing code (will be replaced):");
        //     var lines = verb.Code.Split('\n');
        //     for (int i = 0; i < lines.Length; i++)
        //     {
        //         _commandProcessor.SendToPlayer($"{i + 1}: {lines[i]}");
        //     }
        //     _commandProcessor.SendToPlayer("--- End of existing code ---");
        // }
        // else
        // {
        //     _commandProcessor.SendToPlayer("No existing code.");
        // }

        _commandProcessor.SendToPlayer("Enter your NEW C# code (will replace any existing code).");
        _commandProcessor.SendToPlayer("Type '.' on a line by itself to save, or '.abort' to cancel without changes.");
        _commandProcessor.SendToPlayer("Programming mode active. Available variables:");
        _commandProcessor.SendToPlayer("  Player - the player executing the verb");
        _commandProcessor.SendToPlayer("  ThisObject - ID of the object this verb is on");
        _commandProcessor.SendToPlayer("  Input - the complete command input");
        _commandProcessor.SendToPlayer("  Args - list of arguments");
        _commandProcessor.SendToPlayer("  Notify(player, message) - send message to specific player");
        _commandProcessor.SendToPlayer("  Notify(me, message) - send message to current player");
        _commandProcessor.SendToPlayer("  GetPlayer(name) - get player object by name for notify()");
        _commandProcessor.SendToPlayer("  SayToRoom(message) - send message to all in room");

        return true;
    }

    /// <summary>
    /// Handle programming a function
    /// </summary>
    private bool HandleProgramFunction(string functionId)
    {
        // Find the function
        var functions = GameDatabase.Instance.GetCollection<Function>("functions");
        var function = functions.FindById(functionId);
        
        if (function == null)
        {
            _commandProcessor.SendToPlayer($"Function with ID '{functionId}' not found.");
            return true;
        }

        _commandProcessor.SendToPlayer($"Editing function '{function.Name}' on {GetObjectName(function.ObjectId)}.");
        
        // Show function signature
        var paramString = string.Join(", ", function.ParameterTypes.Zip(function.ParameterNames, (type, name) => $"{type} {name}"));
        _commandProcessor.SendToPlayer($"Function signature: {function.ReturnType} {function.Name}({paramString})");

        // Enter programming mode
        _isInProgrammingMode = true;
        _currentVerbId = string.Empty; // Clear verb ID
        _currentFunctionId = function.Id;
        _currentCode.Clear(); // Always start with empty code - @program replaces existing code
        
        if (!string.IsNullOrEmpty(function.Code))
        {
            _commandProcessor.SendToPlayer("Existing code (will be replaced):");
            var lines = function.Code.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                _commandProcessor.SendToPlayer($"{i + 1}: {lines[i]}");
            }
            _commandProcessor.SendToPlayer("--- End of existing code ---");
        }
        else
        {
            _commandProcessor.SendToPlayer("No existing code.");
        }

        _commandProcessor.SendToPlayer("Enter your NEW C# code (will replace any existing code).");
        _commandProcessor.SendToPlayer("Type '.' on a line by itself to save, or '.abort' to cancel without changes.");
        _commandProcessor.SendToPlayer("Programming mode active. Available variables:");
        
        // Show function parameters as available variables
        for (int i = 0; i < function.ParameterNames.Length; i++)
        {
            _commandProcessor.SendToPlayer($"  {function.ParameterTypes[i]} {function.ParameterNames[i]} - function parameter");
        }
        
        _commandProcessor.SendToPlayer("  Player - the player calling the function");
        _commandProcessor.SendToPlayer("  CallingObjectId - ID of the object that called this function");
        _commandProcessor.SendToPlayer("  Say(message) - send message to calling player");
        _commandProcessor.SendToPlayer("  CallFunction(obj, func, args) - call another function");

        return true;
    }

    /// <summary>
    /// Handle programming a function by object:function name
    /// </summary>
    private bool HandleProgramFunctionByName(string functionSpec)
    {
        // Split from the right to handle class:Object:function syntax  
        var lastColonIndex = functionSpec.LastIndexOf(':');
        if (lastColonIndex == -1)
        {
            _commandProcessor.SendToPlayer("Function specification must be in format <object>:<function>");
            return true;
        }

        var objectName = functionSpec.Substring(0, lastColonIndex);
        var functionName = functionSpec.Substring(lastColonIndex + 1);

        // Resolve object
        var objectId = ResolveObject(objectName);
        if (string.IsNullOrEmpty(objectId))
        {
            _commandProcessor.SendToPlayer($"Object '{objectName}' not found.");
            return true;
        }

        // Find function on object
        var function = FunctionResolver.FindFunction(objectId, functionName);
        if (function == null)
        {
            _commandProcessor.SendToPlayer($"Function '{functionName}' not found on object '{objectName}'.");
            return true;
        }

        // Use the existing HandleProgramFunction method
        return HandleProgramFunction(function.Id);
    }

    /// <summary>
    /// Handle programming a function by object.function() notation
    /// </summary>
    private bool HandleProgramFunctionByDotNotation(string functionSpec)
    {
        // Remove the trailing () 
        if (!functionSpec.EndsWith("()"))
        {
            _commandProcessor.SendToPlayer("Function specification must end with () - e.g., system.display_login()");
            return true;
        }

        var specWithoutParens = functionSpec.Substring(0, functionSpec.Length - 2);
        var dotIndex = specWithoutParens.LastIndexOf('.');
        if (dotIndex == -1)
        {
            _commandProcessor.SendToPlayer("Function specification must be in format <object>.<function>()");
            return true;
        }

        var objectName = specWithoutParens.Substring(0, dotIndex);
        var functionName = specWithoutParens.Substring(dotIndex + 1);

        // Resolve object
        var objectId = ResolveObject(objectName);
        if (string.IsNullOrEmpty(objectId))
        {
            _commandProcessor.SendToPlayer($"Object '{objectName}' not found.");
            return true;
        }

        // Find function on object
        var function = FunctionResolver.FindFunction(objectId, functionName);
        if (function == null)
        {
            _commandProcessor.SendToPlayer($"Function '{functionName}' not found on object '{objectName}'.");
            return true;
        }

        // Use the existing HandleProgramFunction method
        return HandleProgramFunction(function.Id);
    }

    /// <summary>
    /// Handle @script command - multi-line script execution for testing
    /// </summary>
    private bool HandleScriptCommand(string[] parts)
    {
        // Enter programming mode for script testing
        _isInProgrammingMode = true;
        _currentVerbId = string.Empty; // No verb ID for script mode
        _currentFunctionId = string.Empty; // No function ID for script mode
        _currentCode.Clear();
        
        _commandProcessor.SendToPlayer("Enter your C# code for testing:");
        _commandProcessor.SendToPlayer("Type '.' on a line by itself to execute, or '.abort' to cancel.");
        _commandProcessor.SendToPlayer("Script mode active. Available variables:");
        _commandProcessor.SendToPlayer("  player - the current player");
        _commandProcessor.SendToPlayer("  me - alias for player");
        _commandProcessor.SendToPlayer("  here - the current room");
        _commandProcessor.SendToPlayer("  this - the object (same as here for scripts)");
        _commandProcessor.SendToPlayer("  notify(player, message) - send message to specific player");
        _commandProcessor.SendToPlayer("  SayToRoom(message) - send message to all in room");

        return true;
    }

    /// <summary>
    /// Handle input while in programming mode
    /// </summary>
    private bool HandleProgrammingInput(string input)
    {
        if (input.Trim() == ".")
        {
            // Finish programming or execute script
            var code = _currentCode.ToString();
            
            if (!string.IsNullOrEmpty(_currentVerbId))
            {
                // Verb programming mode - save the code
                VerbManager.UpdateVerbCode(_currentVerbId, code);
                // Verify the code was saved using DbProvider
                var savedVerb = DbProvider.Instance.FindById<Verb>("verbs", _currentVerbId);
                if (savedVerb != null)
                {
                    _commandProcessor.SendToPlayer("Verb programming complete.");
                    _commandProcessor.SendToPlayer($"Code saved ({code.Split('\n').Length} lines).");
                    _commandProcessor.SendToPlayer($"Verified: Code length is {savedVerb.Code?.Length ?? 0} characters.");
                }
                else
                {
                    _commandProcessor.SendToPlayer("ERROR: Could not verify that code was saved!");
                }
            }
            else if (!string.IsNullOrEmpty(_currentFunctionId))
            {
                // Function programming mode - save the code
                var function = DbProvider.Instance.FindById<Function>("functions", _currentFunctionId);
                if (function != null)
                {
                    function.Code = code;
                    var updateResult = FunctionManager.UpdateFunction(function);
                    if (updateResult)
                    {
                        _commandProcessor.SendToPlayer("Function programming complete.");
                        _commandProcessor.SendToPlayer($"Code saved ({code.Split('\n').Length} lines).");
                        // Re-fetch to verify
                        var verifyFunction = DbProvider.Instance.FindById<Function>("functions", _currentFunctionId);
                        if (verifyFunction != null)
                        {
                            _commandProcessor.SendToPlayer($"Verified: Code length is {verifyFunction.Code?.Length ?? 0} characters.");
                        }
                    }
                    else
                    {
                        _commandProcessor.SendToPlayer("ERROR: Failed to save function code.");
                    }
                }
                else
                {
                    _commandProcessor.SendToPlayer("ERROR: Could not find function to save code to!");
                }
            }
            else
            {
                // Script mode - execute the code immediately
                try
                {
                    var result = Builtins.ExecuteScript(code, _player, _commandProcessor, _player.Location!, "");
                    
                    _commandProcessor.SendToPlayer("Script executed successfully.");
                    if (!string.IsNullOrEmpty(result) && result != "null")
                    {
                        _commandProcessor.SendToPlayer($"Script result: {result}");
                    }
                }
                catch (Exception ex)
                {
                    _commandProcessor.SendToPlayer($"Script execution failed: {ex.Message}");
                }
            }
            
            _isInProgrammingMode = false;
            _currentCode.Clear();
            _currentVerbId = string.Empty;
            _currentFunctionId = string.Empty;
            return true;
        }

        if (input.Trim().ToLower() == ".abort")
        {
            // Abort programming
            _commandProcessor.SendToPlayer("Programming aborted. No changes saved.");
            
            _isInProgrammingMode = false;
            _currentCode.Clear();
            _currentVerbId = string.Empty;
            _currentFunctionId = string.Empty;
            return true;
        }

        // Add line to current code
        _currentCode.AppendLine(input);
        //_commandProcessor.SendToPlayer($"[{_currentCode.ToString().Split('\n').Length}] "); // Show line number
        return true;
    }

    /// <summary>
    /// @verb <object> <name> [aliases] [pattern] - Create a new verb
    /// </summary>
    private bool HandleVerbCommand(string[] parts)
    {
        // Check if player has Programmer flag
        if (!PermissionManager.HasFlag(_player, PermissionManager.Flag.Programmer))
        {
            _commandProcessor.SendToPlayer("You need the Programmer flag to use the @verb command.");
            return true;
        }

        if (parts.Length < 3)
        {
            _commandProcessor.SendToPlayer("Usage: @verb <object> <name> [aliases] [pattern]");
            _commandProcessor.SendToPlayer("Example: @verb here look 'l examine' '*'");
            return true;
        }

        var objectName = parts[1];
        var verbName = parts[2];
        var aliases = parts.Length > 3 ? parts[3].Trim('"') : "";
        var pattern = parts.Length > 4 ? parts[4].Trim('"') : "";

        var objectId = ResolveObject(objectName);
        if (objectId == null)
        {
            _commandProcessor.SendToPlayer($"Object '{objectName}' not found.");
            return true;
        }

        var verb = VerbManager.CreateVerb(objectId, verbName, pattern, "", _player.Name);
        if (!string.IsNullOrEmpty(aliases))
        {
            var verbCollection = GameDatabase.Instance.GetCollection<Verb>("verbs");
            verb.Aliases = aliases;
            verbCollection.Update(verb);
        }

        _commandProcessor.SendToPlayer($"Created verb '{verbName}' on {GetObjectName(objectId)}.");
        if (!string.IsNullOrEmpty(aliases))
            _commandProcessor.SendToPlayer($"Aliases: {aliases}");
        if (!string.IsNullOrEmpty(pattern))
            _commandProcessor.SendToPlayer($"Pattern: {pattern}");

        return true;
    }

    /// <summary>
    /// @list <object>:<verb> or <object>.<function>() - Show the code for a verb or function
    /// </summary>
    private bool HandleListCommand(string[] parts)
    {
        if (parts.Length != 2)
        {
            _commandProcessor.SendToPlayer("Usage: @list <object>:<verb> or @list <object>.<function>()");
            return true;
        }

        var spec = parts[1];
        
        // Check if this is a function specification (object.function())
        if (spec.Contains(".") && spec.EndsWith("()"))
        {
            return HandleListFunctionByDotNotation(spec);
        }
        // Check if this is the old f:object:function format (keep for backward compatibility)
        else if (spec.StartsWith("f:"))
        {
            var functionSpec = spec.Substring(2); // Remove "f:" prefix
            if (!functionSpec.Contains(':'))
            {
                _commandProcessor.SendToPlayer("Function specification must be in format f:<object>:<function>");
                return true;
            }
            return HandleListFunctionByName(functionSpec);
        }
        // Legacy support for function:id format
        else if (spec.StartsWith("function:"))
        {
            return HandleListFunction(spec.Substring(9)); // Remove "function:" prefix
        }
        // Check if it's a verb specification (object:verb)
        else if (spec.Contains(':'))
        {
            return HandleListVerb(spec);
        }
        else
        {
            _commandProcessor.SendToPlayer("Specification must be in format <object>:<verb> or <object>.<function>()");
            return true;
        }
    }

    /// <summary>
    /// Handle listing a verb
    /// </summary>
    private bool HandleListVerb(string verbSpec)
    {
        const string progStartPrefix = "ProgStart > ";
        const string progDataPrefix = "ProgData > ";
        const string progEditPrefix = "ProgEdit > ";
        const string progEndPrefix = "ProgEnd > ";

        // Split from the right to handle class:Object:verb syntax
        var lastColonIndex = verbSpec.LastIndexOf(':');
        var objectName = verbSpec.Substring(0, lastColonIndex);
        var verbName = verbSpec.Substring(lastColonIndex + 1);

        var objectId = ResolveObject(objectName);

        if (objectId == null)
        {
            // Optionally prepare for creating a new verb
            _commandProcessor.SendToPlayer($"{progStartPrefix}@verb {objectName} {verbName}");
            _commandProcessor.SendToPlayer($"{progEndPrefix}.");
            return true;
        }

        // Retrieve the object using its objectId
        var gameObject = ObjectManager.GetObject(objectId);
        var dbref = gameObject != null ? $"#{gameObject.DbRef}" : objectId;

        if (dbref.StartsWith("class"))
        {
            dbref = $"class:{dbref.Substring(6)}";
        }

        var verb = VerbManager.GetVerbsOnObject(objectId)
            .FirstOrDefault(v => v.Name.ToLower() == verbName.ToLower());

        if (verb == null)
        {
            // Optionally prepare for creating a new verb
            _commandProcessor.SendToPlayer($"{progStartPrefix}@verb {objectName} {verbName}");
            _commandProcessor.SendToPlayer($"{progEndPrefix}.");
            return true;
        }

        // Start of the listing
        // we need to output the verb in the format of class:classname:verbname if it's a class
        // or just object:verbname if it's a regular object
        // This allows the player to copy the verb command directly

        _commandProcessor.SendToPlayer($"{progStartPrefix}@program {dbref}:{verb.Name}");

        // Verb metadata
        _commandProcessor.SendToPlayer($"{progDataPrefix}{GetObjectName(objectId)}:{verb.Name}");
        if (!string.IsNullOrEmpty(verb.Aliases))
            _commandProcessor.SendToPlayer($"{progDataPrefix}Aliases: {verb.Aliases}");
        if (!string.IsNullOrEmpty(verb.Pattern))
            _commandProcessor.SendToPlayer($"{progDataPrefix}Pattern: {verb.Pattern}");
        if (!string.IsNullOrEmpty(verb.Description))
            _commandProcessor.SendToPlayer($"{progDataPrefix}Description: {verb.Description}");

        _commandProcessor.SendToPlayer($"{progDataPrefix}Created by: {verb.CreatedBy} on {verb.CreatedAt:yyyy-MM-dd HH:mm}");

        // Verb code
        if (string.IsNullOrEmpty(verb.Code))
        {
            _commandProcessor.SendToPlayer($"{progEditPrefix}(no code)");
        }
        else
        {
            // Normalize line endings and trim each line
            var normalizedCode = verb.Code.Replace("\r\n", "\n").Replace("\r", "\n");
            var lines = normalizedCode.Split('\n');
            foreach (var line in lines)
            {
                _commandProcessor.SendToPlayer($"{progEditPrefix}{line.TrimEnd()}");
            }
        }

        // End of the listing
        _commandProcessor.SendToPlayer($"{progEndPrefix}.");

        return true;
    }

    /// <summary>
    /// Handle listing a function
    /// </summary>
    private bool HandleListFunction(string functionId)
    {
        const string progStartPrefix = "ProgStart > ";
        const string progDataPrefix = "ProgData > ";
        const string progEditPrefix = "ProgEdit > ";
        const string progEndPrefix = "ProgEnd > ";

        var function = DbProvider.Instance.FindById<Function>("functions", functionId);
        if (function == null)
        {
            _commandProcessor.SendToPlayer($"{progStartPrefix}Function with ID '{functionId}' not found.");
            _commandProcessor.SendToPlayer($"{progEndPrefix}.");
            return true;
        }

        // Get dbref for the object
        var gameObject = ObjectManager.GetObject(function.ObjectId);
        var dbref = gameObject != null ? $"#{gameObject.DbRef}" : function.ObjectId;


        var paramString = string.Join(", ", function.ParameterTypes.Zip(function.ParameterNames, (type, name) => $"{type} {name}"));

        // Start of the listing
        _commandProcessor.SendToPlayer($"{progStartPrefix}@program {dbref}.{function.Name}()");

        // Function metadata
        _commandProcessor.SendToPlayer($"{progDataPrefix}{GetObjectName(function.ObjectId)}.{function.Name}()");
_commandProcessor.SendToPlayer($"{progDataPrefix}Command: @program {dbref}.{function.Name}()");
        _commandProcessor.SendToPlayer($"{progDataPrefix}Signature: {function.ReturnType} {function.Name}({paramString})");
        _commandProcessor.SendToPlayer($"{progDataPrefix}Permissions: {function.Permissions}");
        if (!string.IsNullOrEmpty(function.Description))
            _commandProcessor.SendToPlayer($"{progDataPrefix}Description: {function.Description}");
        _commandProcessor.SendToPlayer($"{progDataPrefix}Created by: {function.CreatedBy} on {function.CreatedAt:yyyy-MM-dd HH:mm}");
        _commandProcessor.SendToPlayer($"{progDataPrefix}Modified: {function.ModifiedAt:yyyy-MM-dd HH:mm}");

        // Function code
        if (string.IsNullOrEmpty(function.Code))
        {
            _commandProcessor.SendToPlayer($"{progEditPrefix}(no code)");
        }
        else
        {
            var normalizedCode = function.Code.Replace("\r\n", "\n").Replace("\r", "\n");
            var lines = normalizedCode.Split('\n');
            foreach (var line in lines)
            {
                _commandProcessor.SendToPlayer($"{progEditPrefix}{line.TrimEnd()}");
            }
        }

        // End of the listing
        _commandProcessor.SendToPlayer($"{progEndPrefix}.");

        return true;
    }

    /// <summary>
    /// Handle listing a function by object:function name
    /// </summary>
    private bool HandleListFunctionByName(string functionSpec)
    {
        // Split from the right to handle class:Object:function syntax  
        var lastColonIndex = functionSpec.LastIndexOf(':');
        if (lastColonIndex == -1)
        {
            _commandProcessor.SendToPlayer("Function specification must be in format <object>:<function>");
            return true;
        }

        var objectName = functionSpec.Substring(0, lastColonIndex);
        var functionName = functionSpec.Substring(lastColonIndex + 1);

        // Resolve object
        var objectId = ResolveObject(objectName);
        if (string.IsNullOrEmpty(objectId))
        {
            _commandProcessor.SendToPlayer($"Object '{objectName}' not found.");
            return true;
        }

        // Find function on object
        var function = FunctionResolver.FindFunction(objectId, functionName);
        if (function == null)
        {
            _commandProcessor.SendToPlayer($"Function '{functionName}' not found on object '{objectName}'.");
            return true;
        }

        // Use the existing HandleListFunction method
        return HandleListFunction(function.Id);
    }

    /// <summary>
    /// Handle listing a function by object.function() notation
    /// </summary>
    private bool HandleListFunctionByDotNotation(string functionSpec)
    {
        // Remove the trailing () 
        if (!functionSpec.EndsWith("()"))
        {
            _commandProcessor.SendToPlayer("Function specification must end with () - e.g., system.display_login()");
            return true;
        }

        var specWithoutParens = functionSpec.Substring(0, functionSpec.Length - 2);
        var dotIndex = specWithoutParens.LastIndexOf('.');
        if (dotIndex == -1)
        {
            _commandProcessor.SendToPlayer("Function specification must be in format <object>.<function>()");
            return true;
        }

        var objectName = specWithoutParens.Substring(0, dotIndex);
        var functionName = specWithoutParens.Substring(dotIndex + 1);

        // Resolve object
        var objectId = ResolveObject(objectName);
        if (string.IsNullOrEmpty(objectId))
        {
            _commandProcessor.SendToPlayer($"Object '{objectName}' not found.");
            return true;
        }

        // Find function on object
        var function = FunctionResolver.FindFunction(objectId, functionName);
        if (function == null)
        {
            _commandProcessor.SendToPlayer($"Function '{functionName}' not found on object '{objectName}'.");
            return true;
        }

        // Use the existing HandleListFunction method
        return HandleListFunction(function.Id);
    }

    /// <summary>
    /// @edit <object>:<verb> - Edit an existing verb
    /// </summary>
    private bool HandleEditCommand(string[] parts)
    {
        // Check if player has Programmer flag
        if (!PermissionManager.HasFlag(_player, PermissionManager.Flag.Programmer))
        {
            _commandProcessor.SendToPlayer("You need the Programmer flag to use the @edit command.");
            return true;
        }

        // Multiline property editor for @edit <object>.<property>
        if (parts.Length != 2 || !parts[1].Contains('.'))
        {
            _commandProcessor.SendToPlayer("Usage: @edit <object>.<property>");
            return true;
        }

        var split = parts[1].Split('.', 2);
        var objectName = split[0];
        var propName = split[1];
        var objectId = ResolveObject(objectName);
        if (objectId == null)
        {
            _commandProcessor.SendToPlayer($"Object '{objectName}' not found.");
            return true;
        }
        var obj = ObjectManager.GetObject(objectId);
        if (obj == null)
        {
            _commandProcessor.SendToPlayer($"Object '{objectName}' not found.");
            return true;
        }

        // Start multiline property edit mode
        _commandProcessor.StartMultilinePropertyEdit(objectId, propName);
        return true;
    }

    /// <summary>
    /// @verbs <object> - List all verbs and functions on an object
    /// </summary>
    private bool HandleVerbsCommand(string[] parts)
    {
        var objectName = parts.Length > 1 ? parts[1] : "here";
        var objectId = ResolveObject(objectName);
        
        if (objectId == null)
        {
            _commandProcessor.SendToPlayer($"Object '{objectName}' not found.");
            return true;
        }

        var allVerbs = VerbResolver.GetAllVerbsOnObject(objectId);
        var allFunctions = FunctionResolver.GetAllFunctionsOnObject(objectId);
        
        _commandProcessor.SendToPlayer($"=== Verbs and Functions on {GetObjectName(objectId)} ===");
        
        // Show verbs
        if (allVerbs.Any())
        {
            _commandProcessor.SendToPlayer("Verbs:");
            foreach (var (verb, source) in allVerbs.OrderBy(v => v.verb.Name))
            {
                var info = $"  {verb.Name}";
                if (!string.IsNullOrEmpty(verb.Aliases))
                    info += $" ({verb.Aliases})";
                if (!string.IsNullOrEmpty(verb.Pattern))
                    info += $" [{verb.Pattern}]";
                if (!string.IsNullOrEmpty(verb.Description))
                    info += $" - {verb.Description}";
                
                // Show where the verb comes from
                if (source != "instance")
                    info += $" (from {source})";
                
                _commandProcessor.SendToPlayer(info);
            }
        }
        
        // Show functions
        if (allFunctions.Any())
        {
            _commandProcessor.SendToPlayer("Functions:");
            foreach (var (function, source) in allFunctions.OrderBy(f => f.function.Name))
            {
                var paramString = string.Join(", ", function.ParameterTypes.Zip(function.ParameterNames, (type, name) => $"{type} {name}"));
                var info = $"  {function.ReturnType} {function.Name}({paramString})";
                
                if (!string.IsNullOrEmpty(function.Description))
                    info += $" - {function.Description}";
                
                if (function.Permissions != "public")
                    info += $" [{function.Permissions}]";
                
                // Show where the function comes from
                if (source != "instance")
                    info += $" (from {source})";
                
                info += $" (ID: {function.Id})";
                
                _commandProcessor.SendToPlayer(info);
            }
        }
        
        if (!allVerbs.Any() && !allFunctions.Any())
        {
            _commandProcessor.SendToPlayer("No verbs or functions defined.");
        }

        return true;
    }

    /// <summary>
    /// @funcs <object> - List all functions on an object
    /// </summary>
    private bool HandleFuncsCommand(string[] parts)
    {
        var objectName = parts.Length > 1 ? parts[1] : "here";
        var objectId = ResolveObject(objectName);
        
        if (objectId == null)
        {
            _commandProcessor.SendToPlayer($"Object '{objectName}' not found.");
            return true;
        }

        var allFunctions = FunctionResolver.GetAllFunctionsOnObject(objectId);
        
        _commandProcessor.SendToPlayer($"=== Functions on {GetObjectName(objectId)} ===");
        
        // Show functions
        if (allFunctions.Any())
        {
            foreach (var (function, source) in allFunctions.OrderBy(f => f.function.Name))
            {
                var paramString = string.Join(", ", function.ParameterTypes.Zip(function.ParameterNames, (type, name) => $"{type} {name}"));
                var info = $"  {function.ReturnType} {function.Name}({paramString})";
                
                if (!string.IsNullOrEmpty(function.Description))
                    info += $" - {function.Description}";
                
                if (function.Permissions != "public")
                    info += $" [{function.Permissions}]";
                
                // Show where the function comes from
                if (source != "instance")
                    info += $" (from {source})";
                
                info += $" (ID: {function.Id})";
                
                _commandProcessor.SendToPlayer(info);
            }
        }
        else
        {
            _commandProcessor.SendToPlayer("No functions defined.");
        }

        return true;
    }

    /// <summary>
    /// @rmverb <object>:<verb> - Remove a verb
    /// </summary>
    private bool HandleRemoveVerbCommand(string[] parts)
    {
        // Check if player has Programmer flag
        if (!PermissionManager.HasFlag(_player, PermissionManager.Flag.Programmer))
        {
            _commandProcessor.SendToPlayer("You need the Programmer flag to use the @rmverb command.");
            return true;
        }

        if (parts.Length != 2)
        {
            _commandProcessor.SendToPlayer("Usage: @rmverb <object>:<verb>");
            return true;
        }

        var verbSpec = parts[1];
        if (!verbSpec.Contains(':'))
        {
            _commandProcessor.SendToPlayer("Verb specification must be in format <object>:<verb>");
            return true;
        }

        // Handle class syntax (split from right for class:Object:verb)
        var lastColonIndex = verbSpec.LastIndexOf(':');
        var objectName = verbSpec.Substring(0, lastColonIndex);
        var verbName = verbSpec.Substring(lastColonIndex + 1);

        var objectId = ResolveObject(objectName);
        if (objectId == null)
        {
            _commandProcessor.SendToPlayer($"Object '{objectName}' not found.");
            return true;
        }

        var verb = VerbManager.GetVerbsOnObject(objectId)
            .FirstOrDefault(v => v.Name.ToLower() == verbName.ToLower());

        if (verb == null)
        {
            _commandProcessor.SendToPlayer($"Verb '{verbName}' not found on {GetObjectName(objectId)}.");
            return true;
        }

        DbProvider.Instance.Delete<Verb>("verbs", verb.Id);
        _commandProcessor.SendToPlayer($"Removed verb '{verbName}' from {GetObjectName(objectId)}.");

        return true;
    }

    /// <summary>
    /// @remove verb <verb-id> - Remove a specific verb by ID
    /// </summary>
    private bool HandleRemoveVerbByIdCommand(string[] parts)
    {
        if (parts.Length < 3)
        {
            _commandProcessor.SendToPlayer("Usage: @remove verb <verb-id>");
            return true;
        }

        var verbId = parts[2];
        var verb = DbProvider.Instance.FindById<Verb>("verbs", verbId);

        if (verb == null)
        {
            _commandProcessor.SendToPlayer($"Verb with ID '{verbId}' not found.");
            return true;
        }

        _commandProcessor.SendToPlayer($"Found verb: '{verb.Name}' (CodeLength: {verb.Code?.Length ?? 0})");
        _commandProcessor.SendToPlayer($"Object ID: {verb.ObjectId}");
        
        if (!string.IsNullOrEmpty(verb.Code) && verb.Code.Length > 0)
        {
            _commandProcessor.SendToPlayer("⚠️ WARNING: This verb has code! Are you sure you want to remove it?");
            _commandProcessor.SendToPlayer("Type 'yes' to confirm, or anything else to cancel:");
            // For now, just proceed - in a real implementation you'd want confirmation
        }

        DbProvider.Instance.Delete<Verb>("verbs", verbId);
        _commandProcessor.SendToPlayer($"Removed verb '{verb.Name}' (ID: {verbId})");
        return true;
    }

    /// <summary>
    /// @function <object> <name> [returnType] - Create a new function
    /// </summary>
    private bool HandleFunctionCommand(string[] parts)
    {
        return HandleFuncCommand(parts);
    }

    /// <summary>
    /// @functions <object> - List functions on an object
    /// </summary>
    private bool HandleFunctionsCommand(string[] parts)
    {
        if (parts.Length != 2)
        {
            _commandProcessor.SendToPlayer("Usage: @functions <object>");
            return true;
        }

        var objectName = parts[1];
        var objectId = ResolveObject(objectName);
        if (objectId == null)
        {
            _commandProcessor.SendToPlayer($"Object '{objectName}' not found.");
            return true;
        }

        var functions = FunctionManager.GetFunctionsOnObject(objectId);
        if (!functions.Any())
        {
            _commandProcessor.SendToPlayer($"No functions found on {GetObjectName(objectId)}.");
            return true;
        }

        _commandProcessor.SendToPlayer($"Functions on {GetObjectName(objectId)}:");
        foreach (var function in functions.OrderBy(f => f.Name))
        {
            var signature = $"{function.ReturnType} {function.Name}({string.Join(", ", function.ParameterTypes)})";
            var info = $"  {signature}";
            
            if (!string.IsNullOrEmpty(function.Description))
                info += $" - {function.Description}";
            
            _commandProcessor.SendToPlayer(info);
        }

        return true;
    }

    /// <summary>
    /// @debug verbs <object> - Show ALL verbs in database for debugging
    /// </summary>
    private bool HandleDebugVerbsCommand(string[] parts)
    {
        if (parts.Length < 3)
        {
            _commandProcessor.SendToPlayer("Usage: @debug verbs <object>");
            return true;
        }

        var objectId = ResolveObject(parts[2]);
        if (string.IsNullOrEmpty(objectId))
        {
            _commandProcessor.SendToPlayer($"Object '{parts[2]}' not found.");
            return true;
        }

        var allVerbs = DbProvider.Instance.FindAll<Verb>("verbs").ToList();
        var objectVerbs = allVerbs.Where(v => v.ObjectId == objectId).ToList();

        _commandProcessor.SendToPlayer("=== COMPREHENSIVE VERB DEBUG ===");
        _commandProcessor.SendToPlayer($"Searching for verbs on object: {objectId}");
        _commandProcessor.SendToPlayer($"Total verbs in database: {allVerbs.Count}");
        _commandProcessor.SendToPlayer($"Verbs on this object: {objectVerbs.Count}");
        _commandProcessor.SendToPlayer("");

        foreach (var verb in objectVerbs.OrderBy(v => v.Name))
        {
            _commandProcessor.SendToPlayer($"Verb ID: {verb.Id}");
            _commandProcessor.SendToPlayer($"  Name: '{verb.Name}'");
            _commandProcessor.SendToPlayer($"  Aliases: '{verb.Aliases}'");
            _commandProcessor.SendToPlayer($"  Code Length: {verb.Code?.Length ?? 0}");
            _commandProcessor.SendToPlayer($"  Object ID: {verb.ObjectId}");
            if (!string.IsNullOrEmpty(verb.Code))
            {
                var preview = verb.Code.Length > 100 ? verb.Code.Substring(0, 100) + "..." : verb.Code;
                _commandProcessor.SendToPlayer($"  Code Preview: {preview}");
            }
            _commandProcessor.SendToPlayer($"  Created By: {verb.CreatedBy}");
            _commandProcessor.SendToPlayer("");
        }

        // Look for verbs with 'ooc' pattern specifically
        var oocVerbs = allVerbs.Where(v => 
            (!string.IsNullOrEmpty(v.Name) && v.Name.ToLower().Contains("ooc")) ||
            (!string.IsNullOrEmpty(v.Code) && v.Code.Contains("OOC"))
        ).ToList();

        if (oocVerbs.Any())
        {
            _commandProcessor.SendToPlayer("=== ALL OOC-RELATED VERBS IN DATABASE ===");
            foreach (var verb in oocVerbs)
            {
                _commandProcessor.SendToPlayer($"ID: {verb.Id}, Name: '{verb.Name}', ObjectId: {verb.ObjectId}, CodeLength: {verb.Code?.Length ?? 0}");
                if (!string.IsNullOrEmpty(verb.Code) && verb.Code.Length > 0)
                {
                    var preview = verb.Code.Length > 50 ? verb.Code.Substring(0, 50) + "..." : verb.Code;
                    _commandProcessor.SendToPlayer($"  Code: {preview}");
                }
            }
        }

        return true;
    }

    /// <summary>
    /// @fix verbs <object> - Remove all duplicate empty verbs, keep only verbs with code
    /// </summary>
    private bool HandleFixVerbsCommand(string[] parts)
    {
        if (parts.Length < 3)
        {
            _commandProcessor.SendToPlayer("Usage: @fix verbs <object>");
            return true;
        }

        var objectId = ResolveObject(parts[2]);
        if (string.IsNullOrEmpty(objectId))
        {
            _commandProcessor.SendToPlayer($"Object '{parts[2]}' not found.");
            return true;
        }

        var objectVerbs = DbProvider.Instance.Find<Verb>("verbs", v => v.ObjectId == objectId).ToList();

        _commandProcessor.SendToPlayer("=== FIXING DUPLICATE VERBS ===");
        
        // Group by name
        var verbGroups = objectVerbs.GroupBy(v => v.Name?.ToLower() ?? "").ToList();
        int removedCount = 0;

        foreach (var group in verbGroups)
        {
            var verbs = group.ToList();
            if (verbs.Count <= 1) continue;

            _commandProcessor.SendToPlayer($"Found {verbs.Count} verbs named '{group.Key}':");
            
            var verbsWithCode = verbs.Where(v => !string.IsNullOrEmpty(v.Code) && v.Code.Length > 0).ToList();
            var emptyVerbs = verbs.Where(v => string.IsNullOrEmpty(v.Code) || v.Code.Length == 0).ToList();

            _commandProcessor.SendToPlayer($"  {verbsWithCode.Count} with code, {emptyVerbs.Count} empty");

            // Remove all empty verbs if there's at least one with code
            if (verbsWithCode.Count > 0)
            {
                foreach (var emptyVerb in emptyVerbs)
                {
                    _commandProcessor.SendToPlayer($"  Removing empty verb: {emptyVerb.Id}");
                    DbProvider.Instance.Delete<Verb>("verbs", emptyVerb.Id);
                    removedCount++;
                }
            }
            // If all are empty, keep only the newest one
            else if (emptyVerbs.Count > 1)
            {
                var verbsToRemove = emptyVerbs.OrderBy(v => v.Id).Take(emptyVerbs.Count - 1);
                foreach (var verb in verbsToRemove)
                {
                    _commandProcessor.SendToPlayer($"  Removing duplicate empty verb: {verb.Id}");
                    DbProvider.Instance.Delete<Verb>("verbs", verb.Id);
                    removedCount++;
                }
            }
        }

        _commandProcessor.SendToPlayer($"Removed {removedCount} duplicate verbs.");
        return true;
    }

    /// <summary>
    /// Resolves object names to object IDs
    /// </summary>
    private string? ResolveObject(string objectName)
    {
        string? result = null;
        
        // Handle special keywords first
        switch (objectName.ToLower())
        {
            case "me":
                result = _player.Id;
                break;
            case "here":
                result = _player.Location?.Id;
                break;
            case "system":
                result = GetSystemObjectId();
                break;
            default:
                // Check if it's a DBREF (starts with # followed by digits)
                if (objectName.StartsWith("#") && int.TryParse(objectName.Substring(1), out int dbref))
                {
                    var obj = DbProvider.Instance.FindOne<GameObject>("gameobjects", o => o.DbRef == dbref);
                    result = obj?.Id;
                }
                // Check if it's a class reference (starts with "class:" or ends with ".class")
                else if (objectName.StartsWith("class:", StringComparison.OrdinalIgnoreCase))
                {
                    var className = objectName.Substring(6); // Remove "class:" prefix
                    var objectClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => 
                        c.Name.Equals(className, StringComparison.OrdinalIgnoreCase));
                    result = objectClass?.Id;
                }
                else if (objectName.EndsWith(".class", StringComparison.OrdinalIgnoreCase))
                {
                    var className = objectName.Substring(0, objectName.Length - 6); // Remove ".class" suffix
                    var objectClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => 
                        c.Name.Equals(className, StringComparison.OrdinalIgnoreCase));
                    result = objectClass?.Id;
                }
                // Check if it's a direct class ID (like "obj_room", "obj_exit", etc.)
                else if (DbProvider.Instance.FindById<ObjectClass>("objectclasses", objectName) != null)
                {
                    result = objectName; // The objectName itself is the class ID
                }
                else
                {
                    // Try to find by name in current location, then globally, then as a class
                    result = FindObjectByName(objectName);
                    
                    // If not found as an object, try as a class name
                    if (result == null)
                    {
                        var objectClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => 
                            c.Name.Equals(objectName, StringComparison.OrdinalIgnoreCase));
                        if (objectClass != null)
                        {
                            result = objectClass.Id;
                            Logger.Debug($"Found class '{objectName}' -> {result}");
                        }
                    }
                }
                break;
        }
        
        Logger.Debug($"Resolved '{objectName}' to: {result ?? "null"}");
        return result;
    }

    /// <summary>
    /// Find an object by name, first in current room, then globally
    /// </summary>
    private string? FindObjectByName(string name)
    {
        name = name.ToLower();
        
        // First, search in current location (most common case)
        if (_player.Location != null)
        {
            var localObjects = ObjectManager.GetObjectsInLocation(_player.Location);
            var localMatch = localObjects.FirstOrDefault(obj =>
            {
                var objName = ObjectManager.GetProperty(obj, "name")?.AsString?.ToLower();
                var shortDesc = ObjectManager.GetProperty(obj, "shortDescription")?.AsString?.ToLower();
                return objName?.Contains(name) == true || shortDesc?.Contains(name) == true;
            });
            
            if (localMatch != null)
            {
                Logger.Debug($"Found '{name}' locally: #{localMatch.DbRef} ({ObjectManager.GetProperty(localMatch, "name")?.AsString})");
                return localMatch.Id;
            }
        }
        
        // If not found locally, search all players (common for targeting players)
        var players = PlayerManager.GetOnlinePlayers();
        var playerMatch = players.FirstOrDefault(p => p.Name.ToLower().Contains(name));
        if (playerMatch != null)
        {
            var playerObj = ObjectManager.GetObject(playerMatch.Id);
            if (playerObj != null)
            {
                Logger.Debug($"Found player '{name}': #{playerObj.DbRef} ({playerMatch.Name})");
                return playerMatch.Id;
            }
        }
        
        // Finally, search globally (for admin/building purposes)
        var allObjects = DbProvider.Instance.FindAll<GameObject>("gameobjects");
        var globalMatch = allObjects.FirstOrDefault(obj =>
        {
            var objName = ObjectManager.GetProperty(obj, "name")?.AsString?.ToLower();
            var shortDesc = ObjectManager.GetProperty(obj, "shortDescription")?.AsString?.ToLower();
            return objName?.Contains(name) == true || shortDesc?.Contains(name) == true;
        });
        
        if (globalMatch != null)
        {
            Logger.Debug($"Found '{name}' globally: #{globalMatch.DbRef} ({ObjectManager.GetProperty(globalMatch, "name")?.AsString})");
            return globalMatch.Id;
        }
        
        Logger.Debug($"Object '{name}' not found anywhere");
        return null;
    }

    /// <summary>
    /// Get the system object ID
    /// </summary>
    private string? GetSystemObjectId()
    {
        // Get all objects and filter in memory (LiteDB doesn't support ContainsKey in expressions)
        var allObjects = DbProvider.Instance.FindAll<GameObject>("gameobjects");
        var systemObj = allObjects.FirstOrDefault(obj => 
            obj.Properties.ContainsKey("isSystemObject") && obj.Properties["isSystemObject"].AsBoolean == true);
        
        if (systemObj == null)
        {
            // System object doesn't exist, create it
            Logger.Debug("System object not found, creating it...");
            // Use Container class instead of abstract Object class
            var containerClass = DbProvider.Instance.FindOne<ObjectClass>("objectclasses", c => c.Name == "Container");
            if (containerClass != null)
            {
                systemObj = ObjectManager.CreateInstance(containerClass.Id);
                ObjectManager.SetProperty(systemObj, "name", "System");
                ObjectManager.SetProperty(systemObj, "shortDescription", "the system object");
                ObjectManager.SetProperty(systemObj, "longDescription", "This is the system object that holds global verbs and functions.");
                ObjectManager.SetProperty(systemObj, "isSystemObject", true);
                ObjectManager.SetProperty(systemObj, "gettable", false); // Don't allow players to pick up the system
                Logger.Debug($"Created system object with ID: {systemObj.Id}");
            }
            else
            {
                Logger.Error("Could not find Container class to create system object!");
                return null;
            }
        }
        
        Logger.Debug($"Resolved 'system' to object ID: {systemObj?.Id}");
        return systemObj?.Id;
    }

    /// <summary>
    /// Get a friendly name for an object
    /// </summary>
    private string GetObjectName(string objectId)
    {
        if (objectId == _player.Id) return "you";
        if (objectId == _player.Location?.Id) return "here";

        // Try as a GameObject first
        var obj = ObjectManager.GetObject(objectId);
        if (obj != null)
        {
            var name = ObjectManager.GetProperty(obj, "name")?.AsString;
            if (!string.IsNullOrEmpty(name))
                return $"{name} (#{obj.DbRef})";
            else
                return $"#{obj.DbRef}";
        }

        // Try as an ObjectClass
        var objectClass = DbProvider.Instance.FindById<ObjectClass>("objectclasses", objectId);
        if (objectClass != null)
        {
            return $"class {objectClass.Name}";
        }

        return $"object #{objectId[..8]}...";
    }

    /// <summary>
    /// @cleanup <object> - Remove duplicate empty verbs from an object
    /// </summary>
    private bool HandleCleanupCommand(string[] parts)
    {
        if (parts.Length < 2)
        {
            _commandProcessor.SendToPlayer("Usage: @cleanup <object>");
            return true;
        }

        var objectId = ResolveObject(parts[1]);
        if (string.IsNullOrEmpty(objectId))
        {
            _commandProcessor.SendToPlayer($"Object '{parts[1]}' not found.");
            return true;
        }

        var verbCollection = GameDatabase.Instance.GetCollection<Verb>("verbs");
        var allVerbs = verbCollection.Find(v => v.ObjectId == objectId).ToList();

        // Group verbs by name to find duplicates
        var verbGroups = allVerbs.GroupBy(v => v.Name?.ToLower() ?? "").Where(g => g.Count() > 1).ToList();

        int removedCount = 0;
        foreach (var group in verbGroups)
        {
            var verbs = group.ToList();
            if (verbs.Count <= 1) continue;

            var verbsWithCode = verbs.Where(v => !string.IsNullOrEmpty(v.Code) && v.Code.Length > 0).ToList();
            var emptyVerbs = verbs.Where(v => string.IsNullOrEmpty(v.Code) || v.Code.Length == 0).ToList();

            // Remove all empty verbs if there's at least one with code
            if (verbsWithCode.Count > 0)
            {
                foreach (var emptyVerb in emptyVerbs)
                {
                    _commandProcessor.SendToPlayer($"  Removing empty verb: {emptyVerb.Id}");
                    verbCollection.Delete(emptyVerb.Id);
                    removedCount++;
                }
            }
            // If all are empty, keep only the newest one
            else if (emptyVerbs.Count > 1)
            {
                var verbsToRemove = emptyVerbs.OrderBy(v => v.Id).Take(emptyVerbs.Count - 1);
                foreach (var verb in verbsToRemove)
                {
                    _commandProcessor.SendToPlayer($"  Removing duplicate empty verb: {verb.Id}");
                    verbCollection.Delete(verb.Id);
                    removedCount++;
                }
            }
        }

        _commandProcessor.SendToPlayer($"Removed {removedCount} duplicate verbs.");
        return true;
    }

    /// <summary>
    /// @cleanup player - Remove duplicate empty verbs from player object specifically
    /// </summary>
    private bool HandleCleanupPlayerCommand(string[] parts)
    {
        // Get the actual player object ID (not the system object)
        var allObjects = DbProvider.Instance.FindAll<GameObject>("gameobjects").ToList();
        var playerObject = allObjects.FirstOrDefault(obj => 
        {
            var playerIdProp = ObjectManager.GetProperty(obj, "playerId");
            return playerIdProp != null && playerIdProp.AsString == _player.Id;
        });

        if (playerObject == null)
        {
            _commandProcessor.SendToPlayer("Could not find your player object.");
            return true;
        }

        var playerVerbs = DbProvider.Instance.FindVerbsByObjectId(playerObject.Id).ToList();

        _commandProcessor.SendToPlayer($"=== CLEANING PLAYER OBJECT {playerObject.Id} ===");
        _commandProcessor.SendToPlayer($"Found {playerVerbs.Count} verbs on your player object:");

        foreach (var verb in playerVerbs)
        {
            _commandProcessor.SendToPlayer($"  ID: {verb.Id}, Name: '{verb.Name}', CodeLength: {verb.Code?.Length ?? 0}");
        }

        // Group by name to find duplicates
        var verbGroups = playerVerbs.GroupBy(v => v.Name?.ToLower() ?? "").ToList();
        int removedCount = 0;

        foreach (var group in verbGroups)
        {
            var verbs = group.ToList();
            if (verbs.Count <= 1) continue;

            var emptyVerbs = verbs.Where(v => string.IsNullOrEmpty(v.Code) || v.Code.Length == 0).ToList();
            
            _commandProcessor.SendToPlayer($"Found {verbs.Count} verbs named '{group.Key}': removing {emptyVerbs.Count} empty ones");

            foreach (var emptyVerb in emptyVerbs)
            {
                _commandProcessor.SendToPlayer($"  Removing empty verb: {emptyVerb.Id}");
                DbProvider.Instance.Delete<Verb>("verbs", emptyVerb.Id);
                removedCount++;
            }
        }

        _commandProcessor.SendToPlayer($"Removed {removedCount} duplicate empty verbs from player object.");
        return true;
    }

    /// <summary>
    /// @flag <player> <flag> - Grant or remove a flag from a player
    /// Usage: @flag <player> +<flag> or @flag <player> -<flag>
    /// </summary>
    private bool HandleFlagCommand(string[] parts)
    {
        if (parts.Length != 3)
        {
            _commandProcessor.SendToPlayer("Usage: @flag <player> <+/-flag>");
            _commandProcessor.SendToPlayer("Available flags: Admin, Programmer, Moderator");
            _commandProcessor.SendToPlayer("Examples: @flag joe +programmer, @flag mary -moderator");
            return true;
        }

        var playerName = parts[1];
        var flagSpec = parts[2];

        if (string.IsNullOrEmpty(flagSpec) || flagSpec.Length < 2 || (flagSpec[0] != '+' && flagSpec[0] != '-'))
        {
            _commandProcessor.SendToPlayer("Flag must start with + (grant) or - (remove)");
            return true;
        }

        var isGranting = flagSpec[0] == '+';
        var flagName = flagSpec.Substring(1);

        // Parse the flag
        if (!Enum.TryParse<PermissionManager.Flag>(flagName, true, out var flag))
        {
            _commandProcessor.SendToPlayer($"Unknown flag: {flagName}");
            _commandProcessor.SendToPlayer("Available flags: Admin, Programmer, Moderator");
            return true;
        }

        // Find the target player
        var targetPlayer = DbProvider.Instance.FindOne<Player>("players", p => 
            p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));
        
        if (targetPlayer == null)
        {
            _commandProcessor.SendToPlayer($"Player '{playerName}' not found.");
            return true;
        }

        bool success;
        if (isGranting)
        {
            success = PermissionManager.GrantFlag(targetPlayer, flag, _player);
            if (success)
            {
                _commandProcessor.SendToPlayer($"Granted {flag} flag to {targetPlayer.Name}.");
            }
            else
            {
                if (!PermissionManager.CanGrantFlag(_player, flag))
                {
                    _commandProcessor.SendToPlayer($"You don't have permission to grant the {flag} flag.");
                }
                else
                {
                    _commandProcessor.SendToPlayer($"{targetPlayer.Name} already has the {flag} flag.");
                }
            }
        }
        else
        {
            success = PermissionManager.RemoveFlag(targetPlayer, flag, _player);
            if (success)
            {
                _commandProcessor.SendToPlayer($"Removed {flag} flag from {targetPlayer.Name}.");
            }
            else
            {
                if (flag == PermissionManager.Flag.Admin && 
                    targetPlayer.Name.Equals(PermissionManager.ORIGINAL_ADMIN_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    _commandProcessor.SendToPlayer("Cannot remove Admin flag from the original admin player.");
                }
                else if (!PermissionManager.CanRemoveFlag(_player, flag))
                {
                    _commandProcessor.SendToPlayer($"You don't have permission to remove the {flag} flag.");
                }
                else
                {
                    _commandProcessor.SendToPlayer($"{targetPlayer.Name} doesn't have the {flag} flag.");
                }
            }
        }

        return true;
    }

    /// <summary>
    /// @flags [player] - Show flags for a player (or yourself if no player specified)
    /// </summary>
    private bool HandleFlagsCommand(string[] parts)
    {
        Player targetPlayer;
        
        if (parts.Length == 1)
        {
            // Show own flags
            targetPlayer = _player;
        }
        else if (parts.Length == 2)
        {
            // Show another player's flags
            var playerName = parts[1];
            var foundPlayer = DbProvider.Instance.FindOne<Player>("players", p => 
                p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));
            if (foundPlayer == null)
            {
                _commandProcessor.SendToPlayer($"Player '{playerName}' not found.");
                return true;
            }
            targetPlayer = foundPlayer;
            
            if (targetPlayer == null)
            {
                _commandProcessor.SendToPlayer($"Player '{playerName}' not found.");
                return true;
            }
        }
        else
        {
            _commandProcessor.SendToPlayer("Usage: @flags [player]");
            return true;
        }

        var flags = PermissionManager.GetPlayerFlags(targetPlayer);
        var flagsString = PermissionManager.GetFlagsString(targetPlayer);
        
        if (targetPlayer == _player)
        {
            _commandProcessor.SendToPlayer($"Your flags: {flagsString}");
        }
        else
        {
            _commandProcessor.SendToPlayer($"{targetPlayer.Name}'s flags: {flagsString}");
        }

        if (flags.Any())
        {
            _commandProcessor.SendToPlayer("Flag details:");
            foreach (var flag in flags)
            {
                var description = flag switch
                {
                    PermissionManager.Flag.Admin => "Full administrative privileges",
                    PermissionManager.Flag.Programmer => "Can use programming commands like @program",
                    PermissionManager.Flag.Moderator => "Moderation privileges",
                    _ => "Unknown flag"
                };
                _commandProcessor.SendToPlayer($"  {flag}: {description}");
            }
        }

        return true;
    }

    /// <summary>
    /// @update-permissions - Update existing player permissions to new flag system (admin only)
    /// </summary>
    private bool HandleUpdatePermissionsCommand(string[] parts)
    {
        // Check if player has Admin flag
        if (!PermissionManager.HasFlag(_player, PermissionManager.Flag.Admin))
        {
            _commandProcessor.SendToPlayer("You need the Admin flag to use this command.");
            return true;
        }

        _commandProcessor.SendToPlayer("Updating existing player permissions to new flag system...");

        var allPlayers = DbProvider.Instance.FindAll<Player>("players").ToList();
        int updatedCount = 0;

        foreach (var player in allPlayers)
        {
            if (player.Permissions?.Any() == true)
            {
                bool needsUpdate = false;
                var currentPermissions = new List<string>(player.Permissions);

                // Convert old permission names to new flags
                if (currentPermissions.Contains("admin"))
                {
                    currentPermissions.Remove("admin");
                    if (!currentPermissions.Contains("admin"))
                        currentPermissions.Add(PermissionManager.Flag.Admin.ToString().ToLower());
                    needsUpdate = true;
                }

                if (currentPermissions.Contains("builder"))
                {
                    currentPermissions.Remove("builder");
                    if (!currentPermissions.Contains("programmer"))
                        currentPermissions.Add(PermissionManager.Flag.Programmer.ToString().ToLower());
                    needsUpdate = true;
                }

                if (needsUpdate)
                {
                    player.Permissions = currentPermissions;
                    DbProvider.Instance.Update<Player>("players", player);
                    updatedCount++;
                    
                    var flags = PermissionManager.GetFlagsString(player);
                    _commandProcessor.SendToPlayer($"Updated {player.Name}: flags = {flags}");
                }
            }
        }

        _commandProcessor.SendToPlayer($"Permission update complete. Updated {updatedCount} players.");
        return true;
    }

    /// <summary>
    /// @reload [verbs|functions|scripts|properties] - Manually trigger hot reload
    /// </summary>
    private bool HandleReloadCommand(string[] parts)
    {
        // Check if player has Admin flag
        if (!PermissionManager.HasFlag(_player, PermissionManager.Flag.Admin))
        {
            _commandProcessor.SendToPlayer("You need the Admin flag to use the @reload command.");
            return true;
        }

        if (parts.Length < 2)
        {
            _commandProcessor.SendToPlayer("Usage: @reload [verbs|functions|scripts|properties|status]");
            _commandProcessor.SendToPlayer("  @reload verbs      - Reload all verb definitions");
            _commandProcessor.SendToPlayer("  @reload functions  - Reload all function definitions");
            _commandProcessor.SendToPlayer("  @reload scripts    - Reload script engine");
            _commandProcessor.SendToPlayer("  @reload properties - Reload all property definitions");
            _commandProcessor.SendToPlayer("  @reload status     - Show hot reload status");
            return true;
        }

        var target = parts[1].ToLower();
        switch (target)
        {
            case "verbs":
                _commandProcessor.SendToPlayer("🔄 Initiating manual verb reload...");
                try
                {
                    HotReloadManager.ManualReloadVerbs();
                    _commandProcessor.SendToPlayer("✅ Verb reload completed successfully!");
                }
                catch (Exception ex)
                {
                    _commandProcessor.SendToPlayer($"❌ Verb reload failed: {ex.Message}");
                    Logger.Error("Manual verb reload failed", ex);
                }
                break;

            case "functions":
            case "funcs":
                _commandProcessor.SendToPlayer("🔄 Initiating manual function reload...");
                try
                {
                    HotReloadManager.ManualReloadFunctions();
                    _commandProcessor.SendToPlayer("✅ Function reload completed successfully!");
                }
                catch (Exception ex)
                {
                    _commandProcessor.SendToPlayer($"❌ Function reload failed: {ex.Message}");
                    Logger.Error("Manual function reload failed", ex);
                }
                break;

            case "scripts":
                _commandProcessor.SendToPlayer("🔄 Initiating manual script reload...");
                try
                {
                    HotReloadManager.ManualReloadScripts();
                    _commandProcessor.SendToPlayer("✅ Script reload completed successfully!");
                }
                catch (Exception ex)
                {
                    _commandProcessor.SendToPlayer($"❌ Script reload failed: {ex.Message}");
                    Logger.Error("Manual script reload failed", ex);
                }
                break;

            case "properties":
            case "props":
                _commandProcessor.SendToPlayer("🔄 Initiating manual property reload...");
                try
                {
                    PropertyInitializer.ReloadProperties();
                    _commandProcessor.SendToPlayer("✅ Property reload completed successfully!");
                }
                catch (Exception ex)
                {
                    _commandProcessor.SendToPlayer($"❌ Property reload failed: {ex.Message}");
                    Logger.Error("Manual property reload failed", ex);
                }
                break;

            case "status":
                var status = HotReloadManager.IsEnabled ? "ENABLED" : "DISABLED";
                _commandProcessor.SendToPlayer($"Hot Reload Status: {status}");
                _commandProcessor.SendToPlayer("Monitored paths:");
                _commandProcessor.SendToPlayer("  • resources/verbs/ (*.json)");
                _commandProcessor.SendToPlayer("  • resources/functions/ (*.json)");
                _commandProcessor.SendToPlayer("  • resources/properties/ (*.json)");
                _commandProcessor.SendToPlayer("  • Scripts/ (*.cs) [if present]");
                break;

            default:
                _commandProcessor.SendToPlayer($"Unknown reload target: {target}");
                _commandProcessor.SendToPlayer("Valid targets: verbs, functions, scripts, properties, status");
                break;
        }

        return true;
    }

    /// <summary>
    /// @hotreload [enable|disable|status] - Control hot reload functionality
    /// </summary>
    private bool HandleHotReloadCommand(string[] parts)
    {
        // Check if player has Admin flag
        if (!PermissionManager.HasFlag(_player, PermissionManager.Flag.Admin))
        {
            _commandProcessor.SendToPlayer("You need the Admin flag to use the @hotreload command.");
            return true;
        }

        if (parts.Length < 2)
        {
            var status = HotReloadManager.IsEnabled ? "ENABLED" : "DISABLED";
            _commandProcessor.SendToPlayer($"Hot Reload Status: {status}");
            _commandProcessor.SendToPlayer("Usage: @hotreload [enable|disable|status]");
            _commandProcessor.SendToPlayer("  @hotreload enable  - Enable automatic hot reloading");
            _commandProcessor.SendToPlayer("  @hotreload disable - Disable automatic hot reloading");
            _commandProcessor.SendToPlayer("  @hotreload status  - Show current status");
            return true;
        }

        var action = parts[1].ToLower();
        switch (action)
        {
            case "enable":
                HotReloadManager.SetEnabled(true);
                _commandProcessor.SendToPlayer("✅ Hot reload ENABLED");
                _commandProcessor.SendToPlayer("File changes will now automatically trigger reloads.");
                break;

            case "disable":
                HotReloadManager.SetEnabled(false);
                _commandProcessor.SendToPlayer("⏸️ Hot reload DISABLED");
                _commandProcessor.SendToPlayer("File changes will no longer trigger automatic reloads.");
                _commandProcessor.SendToPlayer("You can still use @reload commands for manual reloads.");
                break;

            case "status":
                var enabled = HotReloadManager.IsEnabled;
                var statusIcon = enabled ? "✅" : "❌";
                var statusText = enabled ? "ENABLED" : "DISABLED";
                
                _commandProcessor.SendToPlayer($"{statusIcon} Hot Reload Status: {statusText}");
                
                if (enabled)
                {
                    _commandProcessor.SendToPlayer("📁 Monitoring the following directories:");
                    _commandProcessor.SendToPlayer("  • resources/verbs/ (*.json) - Verb definitions");
                    _commandProcessor.SendToPlayer("  • Scripts/ (*.cs) - C# script files [if present]");
                    _commandProcessor.SendToPlayer("🔄 Changes to these files will trigger automatic reloads");
                }
                else
                {
                    _commandProcessor.SendToPlayer("🔇 File monitoring is disabled");
                    _commandProcessor.SendToPlayer("💡 Use '@reload verbs', '@reload functions', or '@reload scripts' for manual reloads");
                }
                break;

            default:
                _commandProcessor.SendToPlayer($"Unknown hotreload action: {action}");
                _commandProcessor.SendToPlayer("Valid actions: enable, disable, status");
                break;
        }

        return true;
    }

    /// <summary>
    /// @corehot [status|test] - Core application hot reload control
    /// </summary>
    private bool HandleCoreHotReloadCommand(string[] parts)
    {
        // Check if player has Admin flag
        if (!PermissionManager.HasFlag(_player, PermissionManager.Flag.Admin))
        {
            _commandProcessor.SendToPlayer("You need the Admin flag to use the @corehot command.");
            return true;
        }

        if (parts.Length < 2)
        {
            _commandProcessor.SendToPlayer("Core Application Hot Reload");
            _commandProcessor.SendToPlayer("Usage: @corehot [status|test]");
            _commandProcessor.SendToPlayer("  @corehot status - Show core hot reload status");
            _commandProcessor.SendToPlayer("  @corehot test   - Test core hot reload notifications");
            _commandProcessor.SendToPlayer("");
            _commandProcessor.SendToPlayer("💡 For automatic core hot reload, run server with:");
            _commandProcessor.SendToPlayer("   dotnet watch run");
            return true;
        }

        var action = parts[1].ToLower();
        switch (action)
        {
            case "status":
                var status = CoreHotReloadManager.GetStatus();
                _commandProcessor.SendToPlayer("🔥 Core Hot Reload Status:");
                _commandProcessor.SendToPlayer("");
                foreach (var line in status.Split('\n'))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        _commandProcessor.SendToPlayer(line);
                }
                
                _commandProcessor.SendToPlayer("");
                _commandProcessor.SendToPlayer("ℹ️ Core hot reload vs Verb hot reload:");
                _commandProcessor.SendToPlayer("• Verb hot reload: Changes to JSON files in resources/verbs/");
                _commandProcessor.SendToPlayer("• Core hot reload: Changes to C# application code files");
                break;

            case "test":
                try
                {
                    _commandProcessor.SendToPlayer("🧪 Triggering test core hot reload notification...");
                    CoreHotReloadManager.TriggerTestNotification();
                    _commandProcessor.SendToPlayer("✅ Test notification sent!");
                }
                catch (Exception ex)
                {
                    _commandProcessor.SendToPlayer($"❌ Test failed: {ex.Message}");
                    Logger.Error("Core hot reload test failed", ex);
                }
                break;

            default:
                _commandProcessor.SendToPlayer($"Unknown action: {action}");
                _commandProcessor.SendToPlayer("Valid actions: status, test");
                break;
        }

        return true;
    }

    /// <summary>
    /// @func <object> <name> [returnType] [param1Type] [param2Type] ... - Create a new function with type checking
    /// </summary>
    private bool HandleFuncCommand(string[] parts)
    {
        if (parts.Length < 2)
        {
            _commandProcessor.SendToPlayer("Usage: @func <object> <name> [returnType] [param1Type] [param2Type] ...");
            _commandProcessor.SendToPlayer("Example: @func system display_login string");
            _commandProcessor.SendToPlayer("Example: @func player calculateDamage int int bool string");
            return true;
        }

        // Parse the command - handle optional public/private modifier
        var commandText = string.Join(" ", parts.Skip(1));
        
        string permissions = "public"; // Default to public
        if (commandText.StartsWith("public "))
        {
            permissions = "public";
            commandText = commandText.Substring(7);
        }
        else if (commandText.StartsWith("private "))
        {
            permissions = "private";
            commandText = commandText.Substring(8);
        }

        // Parse return type, object:function signature
        var signatureParts = commandText.Split(' ', 2);
        if (signatureParts.Length < 2)
        {
            _commandProcessor.SendToPlayer("Usage: @func [public|private] <returnType> <object:functionName>(<type> <name>, ...)");
            return true;
        }

        var returnType = signatureParts[0];
        var functionSignature = signatureParts[1];

        // Parse object:functionName(parameters)
        var colonIndex = functionSignature.IndexOf('.');
        var parenIndex = functionSignature.IndexOf('(');
        var endParenIndex = functionSignature.LastIndexOf(')');

        if (colonIndex == -1 || parenIndex == -1 || endParenIndex == -1 || parenIndex < colonIndex)
        {
            _commandProcessor.SendToPlayer("Invalid syntax. Use: object.functionName(type name, type name, ...)");
            return true;
        }

        var objectName = functionSignature.Substring(0, colonIndex);
        var functionName = functionSignature.Substring(colonIndex + 1, parenIndex - colonIndex - 1);
        var parametersString = functionSignature.Substring(parenIndex + 1, endParenIndex - parenIndex - 1).Trim();

        // Validate function name
        if (!FunctionManager.IsValidFunctionName(functionName))
        {
            _commandProcessor.SendToPlayer($"Invalid function name '{functionName}'. Function names must start with a letter and contain only letters, numbers, and underscores.");
            return true;
        }

        // Validate return type
        if (!FunctionManager.IsValidReturnType(returnType))
        {
            _commandProcessor.SendToPlayer($"Invalid return type '{returnType}'. Valid types: void, string, int, bool, float, double, decimal, object, Player, GameObject, ObjectClass, List<dynamic>, List<GameObject>, List<Player>, List<string>, List<int>");
            return true;
        }

        // Parse parameters
        var parameterTypes = new List<string>();
        var parameterNames = new List<string>();

        if (!string.IsNullOrEmpty(parametersString))
        {
            var parameters = parametersString.Split(',');
            foreach (var param in parameters)
            {
                var paramParts = param.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (paramParts.Length != 2)
                {
                    _commandProcessor.SendToPlayer($"Invalid parameter syntax: '{param.Trim()}'. Use: type name");
                    return true;
                }

                var paramType = paramParts[0];
                var paramName = paramParts[1];

                // Validate parameter type
                if (!FunctionManager.IsValidParameterType(paramType))
                {
                    _commandProcessor.SendToPlayer($"Invalid parameter type '{paramType}'. Valid types: string, int, bool, float, double, decimal, object, Player, GameObject, ObjectClass, List<dynamic>, List<GameObject>, List<Player>, List<string>, List<int>");
                    return true;
                }

                // Validate parameter name
                if (!IsValidVariableName(paramName))
                {
                    _commandProcessor.SendToPlayer($"Invalid parameter name '{paramName}'. Parameter names must start with a letter and contain only letters, numbers, and underscores.");
                    return true;
                }

                parameterTypes.Add(paramType);
                parameterNames.Add(paramName);
            }
        }

        var objectId = ResolveObject(objectName);
        if (objectId == null)
        {
            _commandProcessor.SendToPlayer($"Object '{objectName}' not found.");
            return true;
        }

        // Check if function already exists
        var existingFunction = FunctionManager.FindFunction(objectId, functionName);
        if (existingFunction != null)
        {
            _commandProcessor.SendToPlayer($"Function '{functionName}' already exists on {GetObjectName(objectId)}.");
            return true;
        }

        // Create the function (new signature expects GameObject)
        var gameObject = ObjectManager.GetObject(objectId);
        if (gameObject == null)
        {
            _commandProcessor.SendToPlayer($"Object with ID '{objectId}' not found.");
            return true;
        }

        var function = FunctionManager.CreateFunction(gameObject, functionName, parameterTypes.ToArray(), parameterNames.ToArray(), returnType, "", _player.Name);

        // Set visibility
        function.Permissions = permissions;
        var functions = GameDatabase.Instance.GetCollection<Function>("functions");
        functions.Update(function);

        var paramString = string.Join(", ", parameterTypes.Zip(parameterNames, (type, name) => $"{type} {name}"));
        _commandProcessor.SendToPlayer($"Created {permissions} function '{returnType} {functionName}({paramString})' on {GetObjectName(objectId)}.");
        _commandProcessor.SendToPlayer($"Function ID: {function.Id}");
        _commandProcessor.SendToPlayer($"Use '@program function:{function.Id}' to add code to this function.");

        return true;
    }

    /// <summary>
    /// Validates if a string is a valid variable name
    /// </summary>
    private bool IsValidVariableName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        if (!char.IsLetter(name[0]) && name[0] != '_')
            return false;

        return name.All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    /// <summary>
    /// @funcreload - Hot reload all function definitions from JSON files
    /// </summary>
    private bool HandleFuncReloadCommand(string[] parts)
    {
        // Check if player has admin privileges
        if (!PermissionManager.HasFlag(_player, PermissionManager.Flag.Admin))
        {
            _commandProcessor.SendToPlayer("You need the Admin flag to use this command.");
            return true;
        }

        _commandProcessor.SendToPlayer("Reloading function definitions...");

        try
        {
            FunctionInitializer.ReloadFunctions();
            _commandProcessor.SendToPlayer("Function definitions reloaded successfully!");
        }
        catch (Exception ex)
        {
            _commandProcessor.SendToPlayer($"Error reloading functions: {ex.Message}");
            Logger.Error("Function reload failed", ex);
        }

        return true;
    }

    /// <summary>
    /// Handle input for commands and programming
    /// </summary>
    public bool HandleInput(string input)
    {
        // If in multiline property edit mode, let CommandProcessor handle it
        if (_commandProcessor.IsInMultilinePropertyEditMode())
        {
            return _commandProcessor.HandleMultilinePropertyInput(input);
        }
        // If in programming mode, handle code input
        if (_isInProgrammingMode)
        {
            return HandleProgrammingInput(input);
        }

        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;
        var command = parts[0].ToLower();
        return command switch
        {
            "@program" => HandleProgramCommand(parts),
            "@script" => HandleScriptCommand(parts),
            "@verb" => HandleVerbCommand(parts),
            "@list" => HandleListCommand(parts),
            "@edit" => HandleEditCommand(parts),
            "@verbs" => HandleVerbsCommand(parts),
            "@funcs" => HandleFuncsCommand(parts),
            "@rmverb" => HandleRemoveVerbCommand(parts),
            "@flag" => HandleFlagCommand(parts),
            "@flags" => HandleFlagsCommand(parts),
            "@update-permissions" => HandleUpdatePermissionsCommand(parts),
            "@debug" when parts.Length > 1 && parts[1] == "verbs" => HandleDebugVerbsCommand(parts),
            "@fix" when parts.Length > 1 && parts[1] == "verbs" => HandleFixVerbsCommand(parts),
            "@remove" when parts.Length > 1 && parts[1] == "verb" => HandleRemoveVerbByIdCommand(parts),
            "@cleanup" when parts.Length > 1 && parts[1] == "player" => HandleCleanupPlayerCommand(parts),
            "@cleanup" => HandleCleanupCommand(parts),
            "@func" => HandleFuncCommand(parts),
            "@function" => HandleFunctionCommand(parts),
            "@functions" => HandleFunctionsCommand(parts),
            "@funcreload" => HandleFuncReloadCommand(parts),
            "@reload" => HandleReloadCommand(parts),
            "@hotreload" => HandleHotReloadCommand(parts),
            "@corehot" => HandleCoreHotReloadCommand(parts),
            "@props" => HandlePropsCommand(parts),
            "@propload" => HandlePropLoadCommand(parts),
            _ => false
        };
    }

    /// <summary>
    /// @props <object> - Show all properties on an object
    /// </summary>
    private bool HandlePropsCommand(string[] parts)
    {
        var objectName = parts.Length > 1 ? parts[1] : "here";
        var objectId = ResolveObject(objectName);
        
        if (objectId == null)
        {
            _commandProcessor.SendToPlayer($"Object '{objectName}' not found.");
            return true;
        }

        var obj = ObjectManager.GetObject(objectId);
        if (obj == null)
        {
            _commandProcessor.SendToPlayer($"Object '{objectName}' not found.");
            return true;
        }

        _commandProcessor.SendToPlayer($"=== Properties on {GetObjectName(objectId)} ===");
        
        if (obj.Properties.Any())
        {
            foreach (var prop in obj.Properties.OrderBy(kvp => kvp.Key))
            {
                var value = prop.Value;
                string displayValue;
                
                if (value.IsString)
                {
                    displayValue = $"\"{value.AsString}\"";
                }
                else if (value.IsArray)
                {
                    var lines = value.AsArray.Select(bv => bv.AsString).ToArray();
                    if (lines.Length <= 3)
                    {
                        displayValue = $"[{string.Join(", ", lines.Select(l => $"\"{l}\""))}]";
                    }
                    else
                    {
                        displayValue = $"[{lines.Length} lines: \"{lines[0]}\", \"{lines[1]}\", ...]";
                    }
                }
                else if (value.IsBoolean)
                {
                    displayValue = value.AsBoolean.ToString().ToLower();
                }
                else if (value.IsNumber)
                {
                    displayValue = value.ToString();
                }
                else
                {
                    displayValue = value.ToString();
                }
                
                _commandProcessor.SendToPlayer($"  {prop.Key}: {displayValue}");
            }
        }
        else
        {
            _commandProcessor.SendToPlayer("No properties defined.");
        }

        return true;
    }

    /// <summary>
    /// @propload - Hot reload all property definitions from JSON files
    /// </summary>
    private bool HandlePropLoadCommand(string[] parts)
    {
        // Check if player has admin privileges
        if (!PermissionManager.HasFlag(_player, PermissionManager.Flag.Admin))
        {
            _commandProcessor.SendToPlayer("You need the Admin flag to use this command.");
            return true;
        }

        _commandProcessor.SendToPlayer("Reloading property definitions...");

        try
        {
            PropertyInitializer.ReloadProperties();
            _commandProcessor.SendToPlayer("Property definitions reloaded successfully!");
        }
        catch (Exception ex)
        {
            _commandProcessor.SendToPlayer($"Error reloading properties: {ex.Message}");
            Logger.Error("Property reload failed", ex);
        }

        return true;
    }
}


