using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using CSMOO.Server.Commands;
using CSMOO.Server.Database;
using CSMOO.Server.Database.Models;
using CSMOO.Server.Logging;
using System.Text;

namespace CSMOO.Server.Scripting
{
    /// <summary>
    /// Script engine for executing verb code
    /// </summary>
    public class VerbScriptEngine
    {
        private readonly ScriptOptions _scriptOptions;

        public VerbScriptEngine()
        {
            _scriptOptions = ScriptOptions.Default
                .WithReferences(
                    typeof(object).Assembly, // System
                    typeof(System.Collections.Generic.List<>).Assembly, // System.Collections.Generic
                    typeof(System.Linq.Enumerable).Assembly, // System.Linq
                    typeof(GameDatabase).Assembly, // Current assembly
                    typeof(Builtins).Assembly // Builtins assembly
                )
                .WithImports(
                    "System",
                    "System.Collections.Generic",
                    "System.Linq",
                    "System.Text",
                    "CSMOO.Server.Database",
                    "CSMOO.Server.Commands",
                    "CSMOO.Server.Scripting"
                );
        }

        /// <summary>
        /// Execute a verb's code with enhanced script globals
        /// </summary>
        public string ExecuteVerb(Database.Models.Verb verb, string input, Database.Player player, 
            CommandProcessor commandProcessor, string? thisObjectId = null, Dictionary<string, string>? variables = null)
        {
            try
            {
                var globals = new VerbScriptGlobals
                {
                    Player = player,
                    CommandProcessor = commandProcessor,
                    Helpers = new ScriptHelpers(player, commandProcessor),
                    ThisObject = thisObjectId ?? verb.ObjectId,
                    Input = input,
                    Args = ParseArguments(input),
                    Verb = verb.Name,
                    Variables = variables ?? new Dictionary<string, string>()
                };

                // Set the current context for the Builtins class
                Builtins.CurrentContext = globals;

                // Initialize the object factory for enhanced script support
                globals.InitializeObjectFactory();

                // Build the complete script with automatic variable declarations
                var completeScript = BuildScriptWithVariables(verb.Code, variables);

                var script = CSharpScript.Create(completeScript, _scriptOptions, typeof(VerbScriptGlobals));
                var result = script.RunAsync(globals).Result;
                
                return result.ReturnValue?.ToString() ?? "";
            }
            catch (Exception ex)
            {
                // Log the full exception for debugging
                Logger.Error($"Script execution error in verb '{verb.Name}': {ex.Message}");
                if (ex.InnerException != null)
                {
                    Logger.Error($"Inner exception: {ex.InnerException.Message}");
                }
                throw; // Re-throw so VerbResolver can handle it properly
            }
        }

        private List<string> ParseArguments(string input)
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Skip(1).ToList(); // Skip the verb itself
        }

        /// <summary>
        /// Builds a complete script by injecting variable declarations before the main code
        /// </summary>
        private string BuildScriptWithVariables(string originalCode, Dictionary<string, string>? variables)
        {
            if (variables == null || variables.Count == 0)
            {
                return originalCode;
            }

            var scriptBuilder = new StringBuilder();
            
            // Add variable declarations at the beginning
            scriptBuilder.AppendLine("// Auto-generated variable declarations from pattern matching");
            foreach (var kvp in variables)
            {
                // Use simple string escaping for C# string literals
                var escapedValue = kvp.Value
                    .Replace("\\", "\\\\")   // Escape backslashes
                    .Replace("\"", "\\\"")   // Escape double quotes
                    .Replace("\r", "\\r")    // Escape carriage returns
                    .Replace("\n", "\\n")    // Escape newlines
                    .Replace("\t", "\\t");   // Escape tabs
                    
                scriptBuilder.AppendLine($"string {kvp.Key} = \"{escapedValue}\";");
                Logger.Debug($"Auto-declared variable: {kvp.Key} = \"{escapedValue}\"");
            }
            scriptBuilder.AppendLine();
            
            // Add the original verb code
            scriptBuilder.AppendLine("// Original verb code:");
            scriptBuilder.AppendLine(originalCode);
            
            var completeScript = scriptBuilder.ToString();
            Logger.Debug($"Complete generated script:\n{completeScript}");
            
            return completeScript;
        }
    }
}
