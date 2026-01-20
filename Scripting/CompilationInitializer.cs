using CSMOO.Database;
using CSMOO.Logging;
using CSMOO.Verbs;
using CSMOO.Functions;

namespace CSMOO.Scripting;

/// <summary>
/// Initializes compilation cache on server startup by recompiling all verbs and functions
/// </summary>
public class CompilationInitializer : ICompilationInitializer
{
    private readonly IScriptPrecompiler _precompiler;
    private readonly ICompilationCache _cache;
    private readonly IVerbManager _verbManager;
    private readonly IFunctionManager _functionManager;
    private readonly ILogger _logger;
    private readonly IDbProvider _dbProvider;
    
    private CompilationStatistics _statistics = new CompilationStatistics();

    public CompilationInitializer(
        IScriptPrecompiler precompiler,
        ICompilationCache cache,
        IVerbManager verbManager,
        IFunctionManager functionManager,
        ILogger logger,
        IDbProvider dbProvider)
    {
        _precompiler = precompiler ?? throw new ArgumentNullException(nameof(precompiler));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _verbManager = verbManager ?? throw new ArgumentNullException(nameof(verbManager));
        _functionManager = functionManager ?? throw new ArgumentNullException(nameof(functionManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
    }

    public async Task RecompileAllAsync()
    {
        _logger.Info("Starting compilation cache initialization...");
        _statistics = new CompilationStatistics();

        try
        {
            // Recompile all verbs
            await RecompileAllVerbsAsync();

            // Recompile all functions
            await RecompileAllFunctionsAsync();

            _logger.Info($"Compilation cache initialization complete. Compiled {_statistics.VerbsCompiled} verbs, {_statistics.FunctionsCompiled} functions. {_statistics.VerbsFailed} verbs failed, {_statistics.FunctionsFailed} functions failed.");
        }
        catch (Exception ex)
        {
            _logger.Error("Error during compilation cache initialization", ex);
        }
    }

    private async Task RecompileAllVerbsAsync()
    {
        var verbs = _verbManager.GetAllVerbs();
        _logger.Info($"Recompiling {verbs.Count} verbs...");

        foreach (var verb in verbs)
        {
            try
            {
                if (string.IsNullOrEmpty(verb.Code))
                {
                    continue; // Skip verbs with no code
                }

                // Pass the verb pattern so variables can be extracted for precompilation
                var result = _precompiler.PrecompileVerb(verb.Code, verb.ObjectId, verb.Pattern);

                if (result.Success && result.CompiledScript != null)
                {
                    _cache.SetVerb(verb.Id, result.CompiledScript, result.CodeHash);
                    _statistics.VerbsCompiled++;
                }
                else
                {
                    _statistics.VerbsFailed++;
                    _logger.Warning($"Failed to compile verb '{verb.Name}' (ID: {verb.Id}): {result.Errors.FirstOrDefault()?.Message ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                _statistics.VerbsFailed++;
                _logger.Warning($"Exception compiling verb '{verb.Name}' (ID: {verb.Id}): {ex.Message}");
            }

            // Yield to allow other tasks to run
            await Task.Yield();
        }

        _logger.Info($"Verb recompilation complete: {_statistics.VerbsCompiled} compiled, {_statistics.VerbsFailed} failed");
    }

    private async Task RecompileAllFunctionsAsync()
    {
        var functions = _functionManager.GetAllFunctions();
        _logger.Info($"Recompiling {functions.Count} functions...");

        foreach (var function in functions)
        {
            try
            {
                if (string.IsNullOrEmpty(function.Code))
                {
                    continue; // Skip functions with no code
                }

                var result = _precompiler.PrecompileFunction(function.Code, function.ObjectId, function.ParameterTypes, function.ReturnType);

                if (result.Success && result.CompiledScript != null)
                {
                    _cache.SetFunction(function.Id, result.CompiledScript, result.CodeHash);
                    _statistics.FunctionsCompiled++;
                }
                else
                {
                    _statistics.FunctionsFailed++;
                    _logger.Warning($"Failed to compile function '{function.Name}' (ID: {function.Id}): {result.Errors.FirstOrDefault()?.Message ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                _statistics.FunctionsFailed++;
                _logger.Warning($"Exception compiling function '{function.Name}' (ID: {function.Id}): {ex.Message}");
            }

            // Yield to allow other tasks to run
            await Task.Yield();
        }

        _logger.Info($"Function recompilation complete: {_statistics.FunctionsCompiled} compiled, {_statistics.FunctionsFailed} failed");
    }

    public CompilationStatistics GetStatistics()
    {
        return _statistics;
    }
}
