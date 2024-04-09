using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LocalVpn
{
    internal class Program
    {

        private static int _connctionPort = 5554;
        private static int _messagePort = 5555;

        private static UdpClient _udpClient = new UdpClient(_connctionPort);
        private static UdpClient _udpMessageClient = new UdpClient(_messagePort);

        private static UdpReceiveResult _receiveConnectionResult;
        private static UdpReceiveResult _receiveMessageResult;

        private static readonly string PATH = @$"{Environment.CurrentDirectory}\log{DateTime.Now.ToShortDateString()}.txt";

        private static StreamWriter _streamWriter = new StreamWriter(PATH, true);

        //name [[inuse][ip][port]]
        private static Dictionary<string, string[]> _clients = new Dictionary<string, string[]>();
        private static Dictionary<string, ServerModel> _clientServer = new Dictionary<string, ServerModel>();
        private static string _receivedData { get; set; }
        private static string _clientName { get; set; }
        private static IPEndPoint _clientEndPoint { get; set; }
        private static bool _canClientAdd { get; set; } = true;
        private static bool _canServerAdd { get; set; } = true;

        static async Task Main(string[] args)
        {
            var conncetionThread = new Thread(() =>
            {
                GetConnection();
            });
            conncetionThread.Name = "GetConnectionThread";
            conncetionThread.Start();
            Console.ReadLine();
        }

        private static async void ClientRegistration(string data, UdpReceiveResult udpReceiveResult, UdpClient client)
        {
            UdpClient sendClient = client;
            string clientName = data.Split(' ')[1];
            foreach (var o in _clients)
            {
                if (o.Key == clientName)
                {
                    _canClientAdd = false;
                    try
                    {
                        await sendClient.SendAsync(Encoding.UTF8.GetBytes("Имя занято!"), udpReceiveResult.RemoteEndPoint);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    Console.WriteLine($"Имя занято!");
                }
            }
            if (_canClientAdd)
            {
                _clients.Add(clientName, udpReceiveResult.RemoteEndPoint.ToString().Split(':'));
                Console.WriteLine($"Connected Name: {clientName} IpPort: {udpReceiveResult.RemoteEndPoint}");
                if(_clientServer.Count > 0)
                {
                    foreach (var o in _clientServer)
                    {
                        try
                        {
                            await sendClient.SendAsync(Encoding.UTF8.GetBytes(o.Key), udpReceiveResult.RemoteEndPoint);
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
                        await sendClient.SendAsync(Encoding.UTF8.GetBytes("No available servers!"), udpReceiveResult.RemoteEndPoint);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        private static async void ServerRegistration(string data, UdpReceiveResult udpReceiveResult, UdpClient client)
        {
            string serverName = data.Split(' ')[1];
            foreach (var o in _clientServer)
            {
                if (o.Key == serverName)
                {
                    _canServerAdd = false;
                    Console.WriteLine($"Имя занято!");
                }
            }
            if (_canServerAdd)
            {
                var server = new ServerModel()
                {
                    IpPort = udpReceiveResult.RemoteEndPoint.ToString().Split(':'),
                    CanAccess = true
                };
                _clientServer.Add(serverName, server);
                Console.WriteLine($"Connected Name: {serverName} IpPort: {udpReceiveResult.RemoteEndPoint}");
                if(_clients.Count > 0)
                {
                    UdpClient sendClient = client;
                    foreach (var o in _clients)
                    {
                        foreach(var s in _clientServer)
                        {
                            try
                            {
                                await sendClient.SendAsync(Encoding.UTF8.GetBytes(s.Key), new IPEndPoint(IPAddress.Parse(o.Value[0]), int.Parse(o.Value[1])));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }
                    }
                }
            }
        }

        private static async void RequestToConnect(string req, UdpReceiveResult udpReceiveResult, UdpClient client)
        {
            UdpClient sendClient = client;
            foreach(var o in _clientServer)
            {
                if(o.Key == req)
                {
                    foreach (var c in _clients)
                    {
                        if (c.Value[0] == udpReceiveResult.RemoteEndPoint.ToString().Split(':')[0] &
                            c.Value[1] == udpReceiveResult.RemoteEndPoint.ToString().Split(':')[1])
                        {
                            Console.WriteLine($"Request {req} from client {c.Key}-{c.Value[0]}:{c.Value[1]}");
                            break;
                        }
                    }
                }
                if(o.Key == req & o.Value.CanAccess)
                {
                    try
                    {
                        await sendClient.SendAsync(Encoding.UTF8.GetBytes("Ready"), new IPEndPoint(IPAddress.Parse(o.Value.IpPort[0]), int.Parse(o.Value.IpPort[1])));
                        Console.WriteLine("Send Ready");
                        o.Value.CanAccess = false;
                        new Thread(() =>
                        {
                            GetMessages(client, udpReceiveResult);
                        }).Start();
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        break;
                    }
                }
                else if (o.Key != req && !o.Value.CanAccess)
                {
                    try
                    {
                        await sendClient.SendAsync(Encoding.UTF8.GetBytes("Unknown server name or server unavailable"), udpReceiveResult.RemoteEndPoint);
                        Console.WriteLine("Unknown server name or server unavailable");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine (ex.Message);
                        break;
                    }
                }
            }
        }

        private static async void GetConnection()
        {
            try
            {
                while (true)
                {
                    _canClientAdd = true;
                    _canServerAdd = true;
                    _receiveConnectionResult = await _udpClient.ReceiveAsync();
                    if (_receiveConnectionResult.Buffer != null)
                    {
                        _receivedData = Encoding.UTF8.GetString(_receiveConnectionResult.Buffer);
                        Console.WriteLine(_receivedData);
                        if (_receivedData.Contains("ClientName"))
                            ClientRegistration(_receivedData, _receiveConnectionResult, _udpClient);
                        else if (_receivedData.Contains("ServerName"))
                            ServerRegistration(_receivedData, _receiveConnectionResult, _udpClient);
                        else
                            RequestToConnect(_receivedData, _receiveConnectionResult, _udpClient);
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static async void GetMessages(UdpClient client, UdpReceiveResult udpReceiveResult)
        {
            UdpClient sendMessage = client;
            try
            {
                while (true)
                {
                    _receiveMessageResult = await _udpMessageClient.ReceiveAsync();
                    if (_receiveMessageResult.Buffer != null)
                    {
                        Console.WriteLine($"Message: {Encoding.UTF8.GetString(_receiveMessageResult.Buffer)}");
                        await sendMessage.SendAsync(_receiveMessageResult.Buffer, udpReceiveResult.RemoteEndPoint);
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
