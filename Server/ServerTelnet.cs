using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using CSMOO.Server.Session;
using CSMOO.Server.Commands;
using CSMOO.Server.Logging;

namespace CSMOO.Server
{
    internal class ServerTelnet
    {
        private TcpListener _listener;
        private bool _isRunning;
        
        public ServerTelnet(int port)
        {
            _listener = new TcpListener(System.Net.IPAddress.Any, port);
            _isRunning = false;
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
            Logger.Debug("Client connected.");
            
            var clientGuid = Guid.NewGuid();
            SessionHandler.AddSession(clientGuid, client);
            
            var commandProcessor = new CommandProcessor(clientGuid, client);
            
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
                        else if (c >= 32) // Printable characters
                        {
                            inputBuffer.Append(c);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug($"Client disconnected with error: {ex.Message}");
            }
            finally
            {
                SessionHandler.RemoveSession(clientGuid);
                client.Close();
                Logger.Debug("Client disconnected.");
            }
        }
        
        public void Stop()
        {
            _isRunning = false;
            _listener.Stop();
            Logger.Game("Telnet server stopped.");
        }
    }
}
