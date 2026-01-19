using CSMOO.Core;
using CSMOO.Database;
using CSMOO.Functions;
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
            IDbProvider dbProvider)
            : base(objectManager, verbResolver, functionResolver, dbProvider)
        {
        }

        // Backward compatibility constructor
        public AdminScriptGlobals()
            : base()
        {
        }

        // Global variables and functions for admin scripts
    }
}