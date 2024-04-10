using LocalVpn.Model;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace LocalVpn.Registrtion
{
    internal static class ServerRegistration
    {
        private static Dictionary<string, ServerModel> _servers = new Dictionary<string, ServerModel>();
        private static bool _canServerAddToList {  get; set; } = true;
        private static UdpClient _udpServerClient;

        public static async void Registration(string serverName, UdpClient udpClient, IPEndPoint serverEndPoint, Dictionary<string, ClientModel> clients)
        {
            _udpServerClient = udpClient;
            _canServerAddToList = true;
            foreach (var server in _servers)
            {
                if (server.Key == serverName)
                {
                    _canServerAddToList = false;
                    break;
                }
            }
            if (_canServerAddToList)
            {
                var connectedServer = new ServerModel()
                {
                    Ip = serverEndPoint.Address,
                    Port = serverEndPoint.Port,
                    CanAccess = true
                };
                _servers.Add(serverName, connectedServer);
                Console.WriteLine($"Connected Name: {serverName} - Ip: {serverEndPoint.Address} Port: {serverEndPoint.Port}");
                try
                {
                    await _udpServerClient.SendAsync(Encoding.UTF8.GetBytes($"Welcome, {serverName}"), serverEndPoint);
                }
                catch (Exception ex) 
                {
                    Console.WriteLine(ex.Message);
                }
                NotifyClients(udpClient, serverName, ClientRegistration.GetClient());
            }
            else
            {
                try
                {
                    await _udpServerClient.SendAsync(Encoding.UTF8.GetBytes("This name is occupied"), serverEndPoint);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        public static async void NotifyClients(UdpClient udpClient, string server, Dictionary<string, ClientModel> clients)
        {
            if (clients.Count > 0)
            {
                UdpClient udpNotifyClient = udpClient;
                foreach (var connectedClient in clients)
                {
                    try
                    {
                        await udpNotifyClient.SendAsync(Encoding.UTF8.GetBytes(server),
                            new IPEndPoint(connectedClient.Value.Ip, connectedClient.Value.Port));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        public static Dictionary<string, ServerModel> GetServer()
        {
            return _servers;
        }
    }
}
