using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using CSMOO.Server.Commands;
using CSMOO.Server.Database;
using CSMOO.Server.Database.Models;
using CSMOO.Server.Logging;

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
            CommandProcessor commandProcessor, string? thisObjectId = null)
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
                    Verb = verb.Name
                };

                // Set the current context for the Builtins class
                Builtins.CurrentContext = globals;

                // Initialize the object factory for enhanced script support
                globals.InitializeObjectFactory();

                var script = CSharpScript.Create(verb.Code, _scriptOptions, typeof(VerbScriptGlobals));
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
    }
}
