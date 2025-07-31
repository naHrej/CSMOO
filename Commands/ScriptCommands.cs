using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSMOO.Database;
using CSMOO.Database.Models;
using CSMOO.Logging;
using CSMOO.Scripting;

namespace CSMOO.Commands;

    /// <summary>
    /// Handles multi-line script commands
    /// </summary>
    public class ScriptCommands
    {
        private readonly CommandProcessor _commandProcessor;
        private readonly Player _player;
        
        // For multi-line script input
        private bool _isInScriptMode = false;
        private readonly StringBuilder _currentCode = new StringBuilder();

        public ScriptCommands(CommandProcessor commandProcessor, Player player)
        {
            _commandProcessor = commandProcessor;
            _player = player;
        }

        public bool IsInScriptMode => _isInScriptMode;

        /// <summary>
        /// Handles script-related commands
        /// </summary>
        public bool HandleScriptCommand(string input)
        {
            // If we're in script mode, handle code input
            if (_isInScriptMode)
            {
                return HandleScriptInput(input);
            }

            // Check if this is the @script command
            if (input.Trim().ToLower() == "@script")
            {
                return HandleScriptStart();
            }

            return false;
        }

        /// <summary>
        /// Start multi-line script mode
        /// </summary>
        private bool HandleScriptStart()
        {
            _isInScriptMode = true;
            _currentCode.Clear();
            
            _commandProcessor.SendToPlayer("Multi-line script mode active.");
            _commandProcessor.SendToPlayer("Enter your C# code. Available variables:");
            _commandProcessor.SendToPlayer("  player - the current player object");
            _commandProcessor.SendToPlayer("  me - alias for player");
            _commandProcessor.SendToPlayer("  here - the current room");
            _commandProcessor.SendToPlayer("  this - the system object");
            _commandProcessor.SendToPlayer("  Helpers - script helper functions");
            _commandProcessor.SendToPlayer("Type '.' on a line by itself to execute, or '.abort' to cancel.");
            
            return true;
        }

        /// <summary>
        /// Handle input while in script mode
        /// </summary>
        private bool HandleScriptInput(string input)
        {
            if (input.Trim() == ".")
            {
                // Execute the script
                var code = _currentCode.ToString();
                Logger.Debug($"Executing multi-line script. Code length: {code.Length}");
                
                try
                {
                    // Create a temporary verb to execute the script with proper globals
                    var tempVerb = new Verb
                    {
                        Name = "script",
                        Code = code,
                        ObjectId = "system"
                    };
                    
                    // Use the verb script engine for consistent behavior with other verbs
                    var verbEngine = new UnifiedScriptEngine();
                    var result = verbEngine.ExecuteVerb(tempVerb, "@script", _player, _commandProcessor, "system");
                    
                    // Show result
                    if (!string.IsNullOrEmpty(result) && result != "null")
                    {
                        _commandProcessor.SendToPlayer($"Script result: {result}");
                    }
                    else
                    {
                        _commandProcessor.SendToPlayer("Script executed successfully.");
                    }
                }
                catch (Exception ex)
                {
                    _commandProcessor.SendToPlayer($"Script error: {ex.Message}");
                }
                
                _isInScriptMode = false;
                _currentCode.Clear();
                return true;
            }

            if (input.Trim().ToLower() == ".abort")
            {
                // Abort script mode
                _commandProcessor.SendToPlayer("Script mode aborted.");
                
                _isInScriptMode = false;
                _currentCode.Clear();
                return true;
            }

            // Add line to current code
            if (_currentCode.Length > 0)
            {
                _currentCode.AppendLine();
            }
            _currentCode.Append(input);
            
            return true;
        }
    }



