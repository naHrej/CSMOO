using System.Text;
using System.Net.Sockets;
using CSMOO.Sessions;
using CSMOO.Commands;
using CSMOO.Logging;
using Microsoft.Extensions.DependencyInjection;
using CSMOO.Object;
using CSMOO.Verbs;
using CSMOO.Functions;
using CSMOO.Database;
using CSMOO.Scripting;
using CSMOO.Init;

namespace CSMOO.Network;

internal class TelnetServer
{
    private TcpListener _listener;
    private bool _isRunning;
    private readonly IServiceProvider? _serviceProvider;
    
    public TelnetServer(int port)
    {
        _listener = new TcpListener(System.Net.IPAddress.Any, port);
        _isRunning = false;
        _serviceProvider = null;
    }

    public TelnetServer(int port, IServiceProvider serviceProvider)
    {
        _listener = new TcpListener(System.Net.IPAddress.Any, port);
        _isRunning = false;
        _serviceProvider = serviceProvider;
    }
    
    public void Start()
    {
        _listener.Start();
        _isRunning = true;
        
        Logger.DisplaySectionHeader("TELNET SERVER");
        Logger.Game("Telnet server started...");
        
        while (_isRunning)
        {
            var client = _listener.AcceptTcpClient();
            HandleClient(client);
        }
    }
    
    private async void HandleClient(TcpClient client)
    {
        
        var clientGuid = Guid.NewGuid();
        SessionHandler.AddSession(clientGuid, client);
        
        CommandProcessor commandProcessor;
        if (_serviceProvider != null)
        {
            // Use DI to create CommandProcessor
            var connection = new TelnetConnection(clientGuid, client);
            var playerManager = _serviceProvider.GetRequiredService<IPlayerManager>();
            var verbResolver = _serviceProvider.GetRequiredService<IVerbResolver>();
            var permissionManager = _serviceProvider.GetRequiredService<IPermissionManager>();
            var objectManager = _serviceProvider.GetRequiredService<IObjectManager>();
            var functionResolver = _serviceProvider.GetRequiredService<IFunctionResolver>();
            var dbProvider = _serviceProvider.GetRequiredService<IDbProvider>();
            var gameDatabase = _serviceProvider.GetRequiredService<IGameDatabase>();
            var logger = _serviceProvider.GetRequiredService<ILogger>();
            var roomManager = _serviceProvider.GetRequiredService<IRoomManager>();
            var scriptEngineFactory = _serviceProvider.GetRequiredService<IScriptEngineFactory>();
            var verbManager = _serviceProvider.GetRequiredService<IVerbManager>();
            var functionManager = _serviceProvider.GetRequiredService<IFunctionManager>();
            var objectResolver = _serviceProvider.GetRequiredService<CSMOO.Core.IObjectResolver>();
            var hotReloadManager = _serviceProvider.GetService<IHotReloadManager>();
            var coreHotReloadManager = _serviceProvider.GetService<ICoreHotReloadManager>();
            var functionInitializer = _serviceProvider.GetService<IFunctionInitializer>();
            var propertyInitializer = _serviceProvider.GetService<IPropertyInitializer>();
            commandProcessor = new CommandProcessor(clientGuid, connection, playerManager, verbResolver, permissionManager, objectManager, objectResolver, functionResolver, dbProvider, gameDatabase, logger, roomManager, scriptEngineFactory, verbManager, functionManager, hotReloadManager, coreHotReloadManager, functionInitializer, propertyInitializer);
        }
        else
        {
            // Backward compatibility - use static constructor
            commandProcessor = new CommandProcessor(clientGuid, client);
        }
        
        // Send dynamic login banner
        commandProcessor.DisplayLoginBanner();

        try
        {
            using var stream = client.GetStream();
            var buffer = new byte[1024];
            var inputBuffer = new StringBuilder();
            
            while (client.Connected)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    SessionHandler.RemoveSession(clientGuid);
                    break;
                }

                var data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                
                foreach (char c in data)
                {
                    if (c == '\r' || c == '\n')
                    {
                        if (inputBuffer.Length > 0)
                        {
                            var command = inputBuffer.ToString();
                            inputBuffer.Clear();
                            
                            // Process the command
                            commandProcessor.ProcessCommand(command);
                        }
                    }
                    else if (c == '\b' || c == 127) // Backspace
                    {
                        if (inputBuffer.Length > 0)
                        {
                            inputBuffer.Remove(inputBuffer.Length - 1, 1);
                        }
                    }
                    else if (char.IsControl(c) == false) // Accept all non-control characters (includes Unicode)
                    {
                        inputBuffer.Append(c);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Client disconnected with error: {ex.Message}");
        }
        finally
        {
            SessionHandler.RemoveSession(clientGuid);
            client.Close();
        }
    }
    
    public void Stop()
    {
        _isRunning = false;
        _listener.Stop();
        Logger.Game("Telnet server stopped.");
    }
}


