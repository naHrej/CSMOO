using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CSMOO.Configuration;
using CSMOO.Logging;
using CSMOO.Object;
namespace CSMOO.Network;

public class HttpServer
{
    public readonly HttpListener _listener;
    public string _baseUrl => _listener.Prefixes.Count > 0 ? _listener.Prefixes.First() : "http://localhost:1703/";

    public HttpServer()
    {
        _listener = new HttpListener();

        string prefix = "";
        var config = Config.Instance;
        if (config.Server.HttpPort > 0)
        {
            prefix = $"http://localhost:{config.Server.HttpPort}/";
        }
        else if (config.Server.Port > 0)
        {
            prefix = $"http://localhost:{config.Server.Port}/";
        }
        else
        {
            throw new InvalidOperationException("No valid port configured for HTTP server.");
        }


        _listener.Prefixes.Add(prefix);

    }

    public async Task StartAsync()
    {
        Logger.Info($"HTTP Server started at {_baseUrl}");
        _listener.Start();

        while (_listener.IsListening)
        {
            var context = await _listener.GetContextAsync();
            Logger.Debug($"Received request: {context.Request.HttpMethod} {context.Request.Url}");

            try
            {
                // Handle the request
                await HandleRequestAsync(context);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error handling request: {ex.Message}");
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
        // Example: Just return a simple response
        context.Response.ContentType = "text/plain";
        using (var writer = new StreamWriter(context.Response.OutputStream))
        {
            var allObjects = ObjectManager.GetAllObjects();
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
                        var lessLines = lessProperty.AsArray.Select(bv => bv.AsString).ToList();
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