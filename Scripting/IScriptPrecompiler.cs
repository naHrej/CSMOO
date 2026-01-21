namespace CSMOO.Scripting;

/// <summary>
/// Interface for precompiling scripts before execution
/// </summary>
public interface IScriptPrecompiler
{
    /// <summary>
    /// Precompiles verb code to check for errors and cache compiled script
    /// </summary>
    /// <param name="code">Source code to compile</param>
    /// <param name="objectId">Optional object ID for context</param>
    /// <param name="pattern">Optional verb pattern to extract variables from (e.g., "examine {targetName}")</param>
    /// <param name="variables">Optional variables to inject into script</param>
    /// <returns>Compilation result with diagnostics and compiled script</returns>
    CompilationResult PrecompileVerb(string code, string? objectId = null, string? pattern = null, Dictionary<string, string>? variables = null);
    
    /// <summary>
    /// Precompiles function code to check for errors and cache compiled script
    /// </summary>
    /// <param name="code">Source code to compile</param>
    /// <param name="objectId">Optional object ID for context</param>
    /// <param name="parameterTypes">Parameter types for function signature</param>
    /// <param name="parameterNames">Parameter names for function signature (must match parameterTypes length)</param>
    /// <param name="returnType">Return type of the function</param>
    /// <returns>Compilation result with diagnostics and compiled script</returns>
    CompilationResult PrecompileFunction(string code, string? objectId = null, string[]? parameterTypes = null, string[]? parameterNames = null, string returnType = "void");
    
    /// <summary>
    /// Precompiles generic script code (for @script command)
    /// </summary>
    /// <param name="code">Source code to compile</param>
    /// <returns>Compilation result with diagnostics and compiled script</returns>
    CompilationResult PrecompileScript(string code);
    
    /// <summary>
    /// Computes SHA256 hash of code for cache invalidation
    /// </summary>
    /// <param name="code">Source code to hash</param>
    /// <returns>SHA256 hash as hex string</returns>
    string ComputeCodeHash(string code);
}
