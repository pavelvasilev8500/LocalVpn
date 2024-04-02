using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;

namespace LocalVpn
{
    internal class Program
    {

        private static int _connctionPort = 5555;
        private static int _messagePort = 5554;
        private static UdpClient _udpConnectionClient = new UdpClient(_connctionPort);
        private static UdpClient _udpMessageClient = new UdpClient(_messagePort);
        private static List<ClientModel> _clients = new List<ClientModel>();
        private static UdpReceiveResult _receiveConnectionResult;
        private static UdpReceiveResult _receiveMessageResult;
        private static string _clientName {get;set;}

        static async Task Main(string[] args)
        {
            GetConnection();
            while(true)
            {
                if (_clients.Count == 0)
                    continue;
                else if(_clients.Count > 0)
                {
                    GetMessages();
                    break;
                }
            }
            Console.ReadLine();
        }

        private static async Task GetConnection()
        {
            try
            {
                while (true)
                {
                    _receiveConnectionResult = await _udpConnectionClient.ReceiveAsync();
                    Console.WriteLine($"IpPort: {_receiveConnectionResult.RemoteEndPoint}");
                    if (_receiveConnectionResult.Buffer != null)
                    {
                        _clientName = Encoding.UTF8.GetString(_receiveConnectionResult.Buffer);
                        if (_clientName != "\0")
                        {
                            _clients.Add(new ClientModel() { Name = _clientName, IpAddressPort = _receiveConnectionResult.RemoteEndPoint.ToString().Split(':') });
                            //foreach (var o in _clients)
                            Console.WriteLine($"Connected Name: {_clientName} IpPort: {_receiveConnectionResult.RemoteEndPoint}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static async Task GetMessages()
        {
            try
            {
                while(true)
                {
                    _receiveMessageResult = await _udpMessageClient.ReceiveAsync();
                    if(_receiveMessageResult.Buffer != null)
                    {
                        var message = Encoding.UTF8.GetString(_receiveMessageResult.Buffer);
                        var IpPort = _receiveMessageResult.RemoteEndPoint.ToString().Split(":");
                        foreach(var o in _clients)
                            if(o.Name == message)
                            {
                                await _udpMessageClient.SendAsync(Encoding.UTF8.GetBytes($"{o.IpAddressPort[0]} {o.IpAddressPort[1]}"), 
                                                                        new IPEndPoint(IPAddress.Parse(IpPort[0]), int.Parse(IpPort[1])));
                                //await _udpMessageClient.SendAsync(Encoding.UTF8.GetBytes($"{IpPort[0]} {IpPort[1]}"),
                                //                                        new IPEndPoint(IPAddress.Parse(o.IpAddressPort[0]), int.Parse(o.IpAddressPort[1])));
                            }
                        Console.WriteLine($"Message: {message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

    }
}
