using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using CSMOO.Commands;
using CSMOO.Object;

namespace CSMOO.Scripting;

/// <summary>
/// Executes C# scripts in a sandboxed environment with access to game objects
/// </summary>
public class ScriptEngine
{
    private readonly ScriptOptions _scriptOptions;

    public ScriptEngine()
    {
        // Set up script options with necessary references
        _scriptOptions = ScriptOptions.Default
            .WithReferences(
                typeof(object).Assembly,                    // System.Object
                typeof(Console).Assembly,                   // System.Console
                typeof(Enumerable).Assembly,                // System.Linq
                typeof(GameObject).Assembly,                // Our game objects
                typeof(ObjectManager).Assembly,             // Our managers
                typeof(HtmlAgilityPack.HtmlDocument).Assembly, // HtmlAgilityPack
                Assembly.GetExecutingAssembly()             // Current assembly
            )
            .WithImports(
                "System",
                "System.Dynamic",
                "System.Linq",
                "System.Collections.Generic",
                "System.Text",
                "CSMOO.Commands",
                "CSMOO.Object",
                "CSMOO.Scripting",
                "HtmlAgilityPack"
            );
    }

    /// <summary>
    /// Executes a C# script in the context of a player and command processor
    /// </summary>
    public string ExecuteScript(string code, Player? player, CommandProcessor commandProcessor)
    {
        try
        {
            // Create script globals that provide access to game systems
            var globals = new UnifiedScriptGlobals
            {
                Player = player != null ? ObjectManager.GetObject(player.Id) : null,
                This = player != null ? ObjectManager.GetObject(player.Id) : null, // For test scripts, 'this' refers to the player
                CommandProcessor = commandProcessor,
                ObjectManager = new ScriptObjectManager(),
                WorldManager = new ScriptWorldManager(),
                PlayerManager = new ScriptPlayerManager(),
                Helpers = player != null ? new ScriptHelpers(player, commandProcessor) : null
            };

            // Initialize the object factory for natural syntax support
            if (player != null)
            {
                globals.InitializeObjectFactory();
            }

            // Remove curly braces if present (for "script { code }" syntax)
            if (code.StartsWith("{") && code.EndsWith("}"))
            {
                code = code.Substring(1, code.Length - 2).Trim();
            }

            // Preprocess the code to handle natural syntax
            code = ScriptPreprocessor.Preprocess(code);

            // Execute the script
            var script = CSharpScript.Create(code, _scriptOptions, typeof(UnifiedScriptGlobals));
            var result = script.RunAsync(globals).GetAwaiter().GetResult();

            return result.ReturnValue?.ToString() ?? "";
        }
        catch (CompilationErrorException ex)
        {
            return $"Compilation error: {string.Join(", ", ex.Diagnostics)}";
        }
        catch (Exception ex)
        {
            return $"Runtime error: {ex.Message}";
        }
    }
}



