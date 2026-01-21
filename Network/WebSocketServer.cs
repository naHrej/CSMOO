using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using CSMOO.Commands;
using CSMOO.Logging;
using CSMOO.Sessions;
using Microsoft.Extensions.DependencyInjection;
using CSMOO.Object;
using CSMOO.Verbs;
using CSMOO.Functions;
using CSMOO.Database;
using CSMOO.Scripting;
using CSMOO.Init;

namespace CSMOO.Network;

/// <summary>
/// WebSocket server that provides modern web client support alongside telnet
/// Supports multiple communication channels: text and JSON
/// </summary>
public class WebSocketServer
{
    private readonly HttpListener _httpListener;
    private readonly Dictionary<Guid, WebSocketSession> _sessions;
    private readonly object _sessionsLock = new object();
    private bool _isRunning;
    private readonly int _port;
    private readonly IServiceProvider? _serviceProvider;

    public WebSocketServer(int port)
    {
        _port = port;
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add($"http://localhost:{port}/");
        _sessions = new Dictionary<Guid, WebSocketSession>();
        _serviceProvider = null;
    }

    public WebSocketServer(int port, IServiceProvider serviceProvider)
    {
        _port = port;
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add($"http://localhost:{port}/");
        _sessions = new Dictionary<Guid, WebSocketSession>();
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync()
    {
        try
        {
            _httpListener.Start();
            _isRunning = true;

            Logger.DisplaySectionHeader("WEBSOCKET SERVER");
            Logger.Game($"WebSocket server started on port {_port}...");
            Logger.Info("WebSocket endpoints:");
            Logger.Info($"  ws://localhost:{_port}/ws - Main game connection");
            Logger.Info($"  ws://localhost:{_port}/api - JSON API connection");

            while (_isRunning)
            {
                var context = await _httpListener.GetContextAsync();
                _ = Task.Run(() => HandleConnectionAsync(context));
            }
        }
        catch (Exception ex)
        {
            if (_isRunning) // Only log if we're supposed to be running
            {
                Logger.Error($"WebSocket server error: {ex.Message}");
            }
        }
    }

    private async Task HandleConnectionAsync(HttpListenerContext context)
    {
        try
        {
            if (!context.Request.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
                return;
            }

            var webSocketContext = await context.AcceptWebSocketAsync(null);
            var webSocket = webSocketContext.WebSocket;
            var sessionId = Guid.NewGuid();

            // Determine channel type based on URL path
            var channelType = context.Request.Url?.AbsolutePath switch
            {
                "/ws" => WebSocketChannelType.Text,
                "/api" => WebSocketChannelType.Json,
                _ => WebSocketChannelType.Text
            };

            var session = new WebSocketSession(sessionId, webSocket, channelType);
            var connection = new WebSocketConnection(session);
            
            lock (_sessionsLock)
            {
                _sessions[sessionId] = session;
            }

            // Add to global session handler
            SessionHandler.AddSession(sessionId, connection);


            // Send welcome message based on channel type
            await SendWelcomeMessage(session);

            // Handle the session
            await HandleWebSocketSession(session);
        }
        catch (Exception ex)
        {
            Logger.Error($"WebSocket connection error: {ex.Message}");
        }
    }

    private async Task SendWelcomeMessage(WebSocketSession session)
    {
        try
        {
            if (session.ChannelType == WebSocketChannelType.Text)
            {
                await session.SendTextAsync("Welcome to CSMOO WebSocket server!\r\n");
                await session.SendTextAsync("Type 'help' for assistance.\r\n");
            }
            else if (session.ChannelType == WebSocketChannelType.Json)
            {
                var welcomeMessage = new
                {
                    type = "welcome",
                    message = "Connected to CSMOO WebSocket API",
                    timestamp = DateTime.UtcNow,
                    sessionId = session.SessionId,
                    channels = new[] { "text", "json" },
                    version = "1.0"
                };
                await session.SendJsonAsync(welcomeMessage);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to send welcome message: {ex.Message}");
        }
    }

    private async Task HandleWebSocketSession(WebSocketSession session)
    {
        var connection = new WebSocketConnection(session);
        CommandProcessor commandProcessor;
        if (_serviceProvider != null)
        {
            // Use DI to create CommandProcessor
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
            commandProcessor = new CommandProcessor(session.SessionId, connection, playerManager, verbResolver, permissionManager, objectManager, objectResolver, functionResolver, dbProvider, gameDatabase, logger, roomManager, scriptEngineFactory, verbManager, functionManager, hotReloadManager, coreHotReloadManager, functionInitializer, propertyInitializer);
        }
        else
        {
            // Backward compatibility - use static constructor
            commandProcessor = new CommandProcessor(session.SessionId, connection);
        }
        var buffer = new byte[4096];

        try
        {
            while (session.WebSocket.State == WebSocketState.Open)
            {
                var result = await session.WebSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), 
                    CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await session.WebSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure, 
                        "", 
                        CancellationToken.None);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await HandleMessage(session, message, commandProcessor);
                }
            }
        }
        catch (WebSocketException ex)
        {
            Logger.Error($"WebSocket disconnected: {ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.Error($"WebSocket session error: {ex.Message}");
        }
        finally
        {
            lock (_sessionsLock)
            {
                _sessions.Remove(session.SessionId);
            }
            
            // Clean up session from SessionHandler if it exists
            SessionHandler.RemoveSession(session.SessionId);
        }
    }

    private async Task HandleMessage(WebSocketSession session, string message, CommandProcessor commandProcessor)
    {
        try
        {
            if (session.ChannelType == WebSocketChannelType.Json)
            {
                await HandleJsonMessage(session, message, commandProcessor);
            }
            else
            {
                await HandleTextMessage(session, message, commandProcessor);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error handling WebSocket message: {ex.Message}");
            
            if (session.ChannelType == WebSocketChannelType.Json)
            {
                await session.SendJsonAsync(new
                {
                    type = "error",
                    message = "Failed to process message",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                await session.SendTextAsync($"Error: {ex.Message}\r\n");
            }
        }
    }

    private Task HandleTextMessage(WebSocketSession session, string message, CommandProcessor commandProcessor)
    {
        // Handle text messages similar to telnet
        var lines = message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                commandProcessor.ProcessCommand(line.Trim());
            }
        }
        
        return Task.CompletedTask;
    }

    private async Task HandleJsonMessage(WebSocketSession session, string message, CommandProcessor commandProcessor)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(message);
            var root = jsonDoc.RootElement;

            if (!root.TryGetProperty("type", out var typeElement))
            {
                await session.SendJsonAsync(new
                {
                    type = "error",
                    message = "Missing 'type' property in JSON message",
                    timestamp = DateTime.UtcNow
                });
                return;
            }

            var messageType = typeElement.GetString();

            switch (messageType)
            {
                case "command":
                    await HandleJsonCommand(session, root, commandProcessor);
                    break;
                
                case "ping":
                    await session.SendJsonAsync(new
                    {
                        type = "pong",
                        timestamp = DateTime.UtcNow
                    });
                    break;
                
                case "subscribe":
                    await HandleJsonSubscribe(session, root);
                    break;
                
                case "unsubscribe":
                    await HandleJsonUnsubscribe(session, root);
                    break;
                
                default:
                    await session.SendJsonAsync(new
                    {
                        type = "error",
                        message = $"Unknown message type: {messageType}",
                        timestamp = DateTime.UtcNow
                    });
                    break;
            }
        }
        catch (JsonException ex)
        {
            await session.SendJsonAsync(new
            {
                type = "error",
                message = "Invalid JSON format",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    private async Task HandleJsonCommand(WebSocketSession session, JsonElement root, CommandProcessor commandProcessor)
    {
        if (!root.TryGetProperty("command", out var commandElement))
        {
            await session.SendJsonAsync(new
            {
                type = "error",
                message = "Missing 'command' property",
                timestamp = DateTime.UtcNow
            });
            return;
        }

        var command = commandElement.GetString();
        if (string.IsNullOrWhiteSpace(command))
        {
            await session.SendJsonAsync(new
            {
                type = "error",
                message = "Empty command",
                timestamp = DateTime.UtcNow
            });
            return;
        }

        // Extract request ID for response correlation
        var requestId = root.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;

        // Process the command
        var startTime = DateTime.UtcNow;
        commandProcessor.ProcessCommand(command);
        var endTime = DateTime.UtcNow;

        // Send command acknowledgment
        var response = new
        {
            type = "command_result",
            id = requestId,
            command = command,
            executed = true,
            executionTime = (endTime - startTime).TotalMilliseconds,
            timestamp = DateTime.UtcNow
        };

        await session.SendJsonAsync(response);
    }

    private async Task HandleJsonSubscribe(WebSocketSession session, JsonElement root)
    {
        if (!root.TryGetProperty("channel", out var channelElement))
        {
            await session.SendJsonAsync(new
            {
                type = "error",
                message = "Missing 'channel' property for subscription",
                timestamp = DateTime.UtcNow
            });
            return;
        }

        var channel = channelElement.GetString();
        if (!string.IsNullOrEmpty(channel))
        {
            session.SubscribeToChannel(channel);
        }

        await session.SendJsonAsync(new
        {
            type = "subscribed",
            channel = channel,
            timestamp = DateTime.UtcNow
        });
    }

    private async Task HandleJsonUnsubscribe(WebSocketSession session, JsonElement root)
    {
        if (!root.TryGetProperty("channel", out var channelElement))
        {
            await session.SendJsonAsync(new
            {
                type = "error",
                message = "Missing 'channel' property for unsubscription",
                timestamp = DateTime.UtcNow
            });
            return;
        }

        var channel = channelElement.GetString();
        if (!string.IsNullOrEmpty(channel))
        {
            session.UnsubscribeFromChannel(channel);
        }

        await session.SendJsonAsync(new
        {
            type = "unsubscribed",
            channel = channel,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task BroadcastTextAsync(string message, string? excludeSessionId = null)
    {
        var tasks = new List<Task>();

        lock (_sessionsLock)
        {
            foreach (var session in _sessions.Values)
            {
                if (session.SessionId.ToString() != excludeSessionId && 
                    session.ChannelType == WebSocketChannelType.Text &&
                    session.WebSocket.State == WebSocketState.Open)
                {
                    tasks.Add(session.SendTextAsync(message));
                }
            }
        }

        await Task.WhenAll(tasks);
    }

    public async Task BroadcastJsonAsync(object jsonObject, string? channel = null, string? excludeSessionId = null)
    {
        var tasks = new List<Task>();

        lock (_sessionsLock)
        {
            foreach (var session in _sessions.Values)
            {
                if (session.SessionId.ToString() != excludeSessionId && 
                    session.ChannelType == WebSocketChannelType.Json &&
                    session.WebSocket.State == WebSocketState.Open &&
                    (channel == null || session.IsSubscribedToChannel(channel)))
                {
                    tasks.Add(session.SendJsonAsync(jsonObject));
                }
            }
        }

        await Task.WhenAll(tasks);
    }

    public Task SendToSessionAsync(Guid sessionId, string message)
    {
        lock (_sessionsLock)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                if (session.ChannelType == WebSocketChannelType.Text)
                {
                    _ = Task.Run(() => session.SendTextAsync(message));
                }
                else
                {
                    var jsonMessage = new
                    {
                        type = "message",
                        content = message,
                        timestamp = DateTime.UtcNow
                    };
                    _ = Task.Run(() => session.SendJsonAsync(jsonMessage));
                }
            }
        }
        
        return Task.CompletedTask;
    }

    public void Stop()
    {
        _isRunning = false;
        
        lock (_sessionsLock)
        {
            foreach (var session in _sessions.Values)
            {
                try
                {
                    session.WebSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure, 
                        "Server shutdown", 
                        CancellationToken.None).Wait(1000);
                }
                catch
                {
                    // Ignore errors during shutdown
                }
            }
            _sessions.Clear();
        }

        _httpListener?.Stop();
        Logger.Game("WebSocket server stopped.");
    }

    public int GetActiveSessionCount()
    {
        lock (_sessionsLock)
        {
            return _sessions.Count;
        }
    }

    public List<WebSocketSessionInfo> GetSessionInfo()
    {
        var sessionInfoList = new List<WebSocketSessionInfo>();
        
        lock (_sessionsLock)
        {
            foreach (var session in _sessions.Values)
            {
                sessionInfoList.Add(new WebSocketSessionInfo
                {
                    SessionId = session.SessionId,
                    ChannelType = session.ChannelType,
                    ConnectedAt = session.ConnectedAt,
                    State = session.WebSocket.State,
                    SubscribedChannels = session.GetSubscribedChannels()
                });
            }
        }
        
        return sessionInfoList;
    }
}



