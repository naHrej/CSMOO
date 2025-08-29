using CSMOO.Configuration;
using CSMOO.Logging;
using CSMOO.Network;
using System.Threading.Tasks;
using CSMOO.Init;

namespace CSMOO;

internal class Program
{
    static void Main(string[] args)
    {
        try
        {
            // Enable Unicode support for console output
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;
            
            // Load configuration
            var config = Config.Instance;
            
            // Initialize logging system (sets up log rotation and directories)
            Logger.Initialize();
            
            // Display startup banner
            Logger.DisplayBanner();
            
            Logger.DisplaySectionHeader("SYSTEM INITIALIZATION");
            Logger.Info("Starting CSMOO Server...");
            Logger.Info($"Server configuration: Port={config.Server.Port}, ShowDebugInConsole={config.Server.ShowDebugInConsole}");
            Logger.Info($"Database files: Game={config.Database.GameDataFile}, Log={config.Database.LogDataFile}");
            
            // Initialize the server and world
            ServerInitializer.Initialize();
            
            // Start both servers
            var telnetServer = new TelnetServer(config.Server.Port);
            var webSocketServer = new WebSocketServer(config.Server.WsPort); // Use next port for WebSocket
            var httpServer = new HttpServer(); // Initialize HTTP server
            
            try
            {
                // Start WebSocket server asynchronously
                _ = Task.Run(async () => await webSocketServer.StartAsync());
                _ = Task.Run(async () => await httpServer.StartAsync());


                // Start Telnet server (blocking call)
                telnetServer.Start();
            }
            catch (Exception ex)
            {
                Logger.Error($"An error occurred during server operation", ex);
            }
            finally
            {
                telnetServer.Stop();
                webSocketServer.Stop();
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

