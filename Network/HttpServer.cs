using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CSMOO.Configuration;
using CSMOO.Logging;
using CSMOO.Object;
using CSMOO.Database;
using CSMOO.Core;

namespace CSMOO.Network;

public class HttpServer
{
    private readonly IConfig _config;
    private readonly ILogger _logger;
    private readonly IObjectManager _objectManager;
    public readonly HttpListener _listener;
    public string _baseUrl => _listener.Prefixes.Count > 0 ? _listener.Prefixes.First() : "http://localhost:1703/";

    // Primary constructor with DI dependencies
    public HttpServer(IConfig config, ILogger logger, IObjectManager objectManager)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _objectManager = objectManager ?? throw new ArgumentNullException(nameof(objectManager));
        _listener = new HttpListener();

        string prefix = "";
        if (_config.Server.HttpPort > 0)
        {
            prefix = $"http://localhost:{_config.Server.HttpPort}/";
        }
        else if (_config.Server.Port > 0)
        {
            prefix = $"http://localhost:{_config.Server.Port+2}/";
        }
        else
        {
            throw new InvalidOperationException("No valid port configured for HTTP server.");
        }

        _listener.Prefixes.Add(prefix);
    }


    public async Task StartAsync()
    {
        _logger.Info($"HTTP Server started at {_baseUrl}");
        _listener.Start();

        while (_listener.IsListening)
        {
            var context = await _listener.GetContextAsync();

            try
            {
                // Handle the request
                await HandleRequestAsync(context);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error handling request: {ex.Message}");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            finally
            {
                context.Response.Close();
            }
        }


    }
    
    public async Task HandleRequestAsync(HttpListenerContext context)
    {
        var requestPath = context.Request.Url?.AbsolutePath ?? "/";
        
        // Serve stylesheet.less from file system
        if (requestPath.Equals("/stylesheet.less", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.ContentType = "text/css";
            try
            {
                var stylesheet = Html.GetStylesheet();
                if (!string.IsNullOrEmpty(stylesheet))
                {
                    using (var writer = new StreamWriter(context.Response.OutputStream))
                    {
                        await writer.WriteAsync(stylesheet);
                    }
                    _logger.Info($"[HTTP] Served stylesheet.less to {context.Request.RemoteEndPoint}");
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    using (var writer = new StreamWriter(context.Response.OutputStream))
                    {
                        await writer.WriteLineAsync("/* Stylesheet not found */");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"[HTTP] Error serving stylesheet.less: {ex.Message}");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                using (var writer = new StreamWriter(context.Response.OutputStream))
                {
                    await writer.WriteLineAsync($"/* Error loading stylesheet: {ex.Message} */");
                }
            }
            return;
        }
        
        // Legacy endpoint: return system object's less property (for backward compatibility)
        context.Response.ContentType = "text/plain";
        using (var writer = new StreamWriter(context.Response.OutputStream))
        {
            var allObjects = _objectManager.GetAllObjects();
            var systemObject = allObjects.FirstOrDefault(obj =>
                obj.Properties.ContainsKey("isSystemObject"));

            if (systemObject != null)
            {
                string less = "No less information available.";
                
                // Check if the 'less' property exists
                if (systemObject.Properties.ContainsKey("less"))
                {
                    var lessProperty = systemObject.Properties["less"];
                    
                    if (lessProperty.IsString)
                    {
                        less = lessProperty.AsString;
                    }
                    else if (lessProperty.IsArray)
                    {
                        // Convert BsonArray to List<string>
                        var lessLines = ((IEnumerable<dynamic>)lessProperty.AsArray).Select((Func<dynamic, string>)(bv => bv.AsString)).ToList();
                        less = string.Join("\n", lessLines);
                    }
                }
                
                await writer.WriteLineAsync(less);
            }
            else
            {
                await writer.WriteLineAsync("No system object found.");
            }
        }
    }
}