using CSMOO.Server;
using CSMOO.Server.Configuration;
using CSMOO.Server.Logging;

namespace CSMOO
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Load configuration
                var config = Config.Instance;
                
                Logger.Info("Starting CSMOO Server...");
                Logger.Info($"Server configuration: Port={config.Server.Port}, DebugMode={config.Server.DebugMode}");
                Logger.Info($"Database files: Game={config.Database.GameDataFile}, Log={config.Database.LogDataFile}");
                
                // Initialize the server and world
                ServerInitializer.Initialize();
                
                ServerTelnet serverTelnet = new ServerTelnet(config.Server.Port);
                try
                {
                    serverTelnet.Start();
                }
                catch (Exception ex)
                {
                    Logger.Error($"An error occurred during server operation", ex);
                }
                finally
                {
                    serverTelnet.Stop();
                    ServerInitializer.Shutdown();
                    Logger.Info("Server has stopped.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Fatal error during startup", ex);
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
