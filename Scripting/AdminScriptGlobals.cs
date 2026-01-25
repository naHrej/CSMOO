using CSMOO.Core;
using CSMOO.Database;
using CSMOO.Functions;
using CSMOO.Logging;
using CSMOO.Object;
using CSMOO.Verbs;

namespace CSMOO.Scripting
{
    public class AdminScriptGlobals : ScriptGlobals
    {
        // Primary constructor with DI dependencies
        public AdminScriptGlobals(
            IObjectManager objectManager,
            IObjectResolver objectResolver,
            IVerbResolver verbResolver,
            IFunctionResolver functionResolver,
            IDbProvider dbProvider,
            IScriptEngineFactory scriptEngineFactory,
            ILogger logger,
            IPlayerManager playerManager)
            : base(objectManager, objectResolver, verbResolver, functionResolver, dbProvider, scriptEngineFactory, logger, playerManager)
        {
        }

        // Global variables and functions for admin scripts
    }
}