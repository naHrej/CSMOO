using CSMOO.Server;

namespace CSMOO
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Initialize the server and world
            ServerInitializer.Initialize();
            
            ServerTelnet serverTelnet = new ServerTelnet(1701);
            try
            {
                serverTelnet.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            finally
            {
                serverTelnet.Stop();
                ServerInitializer.Shutdown();
                Console.WriteLine("Server has stopped.");
            }
        }
    }
}
