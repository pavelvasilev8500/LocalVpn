using LocalVpn.Model;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LocalVpn.Registrtion
{
    internal class ClientRegistration
    {

        private static Dictionary<string, ClientModel> _clients = new Dictionary<string, ClientModel>();
        private static bool _canClientAddToList { get; set; } = true;
        private static UdpClient _clientUdpClient;

        public static async void Registration(string clientName, UdpClient udpClient, IPEndPoint clientEndPoit, Dictionary<string, ServerModel> servers)
        {
            _clientUdpClient = udpClient;
            _canClientAddToList = true;
            foreach (var client in _clients)
            {
                if (client.Key == clientName)
                {
                    _canClientAddToList = false;
                    break;
                }
            }
            if (_canClientAddToList)
            {
                var connectedClient = new ClientModel()
                {
                    Ip = clientEndPoit.Address,
                    Port = clientEndPoit.Port
                };
                _clients.Add(clientName, connectedClient);
                Console.WriteLine($"Connected Name: {clientName} - Ip: {clientEndPoit.Address} Port: {clientEndPoit.Port}");
                try
                {
                    await _clientUdpClient.SendAsync(Encoding.UTF8.GetBytes($"Welcome, {clientName}"), clientEndPoit);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                if (servers.Count > 0)
                {
                    foreach (var server in servers)
                    {
                        try
                        {
                            await _clientUdpClient.SendAsync(Encoding.UTF8.GetBytes(server.Key), clientEndPoit);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
                else
                {
                    try
                    {
                        await _clientUdpClient.SendAsync(Encoding.UTF8.GetBytes("No available servers!"), clientEndPoit);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            else
            {
                try
                {
                    await _clientUdpClient.SendAsync(Encoding.UTF8.GetBytes("This name is occupied"), clientEndPoit);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public static Dictionary<string, ClientModel> GetClient()
        {
            return _clients;
        }
    }
}
