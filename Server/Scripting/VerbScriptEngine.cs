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
                    "System.Dynamic",
                    "System.Collections.Generic",
                    "System.Linq",
                    "System.Text",
                    "CSMOO.Server.Database",
                    "CSMOO.Server.Commands",
                    "CSMOO.Server.Scripting",
                    "CSMOO.Server.Core"
                );
        }

    /// <summary>
    /// Execute a verb's code with enhanced script globals
    /// </summary>
    public string ExecuteVerb(Database.Models.Verb verb, string input, Database.Player player, 
        CommandProcessor commandProcessor, string? thisObjectId = null, Dictionary<string, string>? variables = null)
    {
        // Use the new unified script engine for consistent behavior
        var unifiedEngine = new UnifiedScriptEngine();
        return unifiedEngine.ExecuteVerb(verb, input, player, commandProcessor, thisObjectId, variables);
    }        private List<string> ParseArguments(string input)
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
