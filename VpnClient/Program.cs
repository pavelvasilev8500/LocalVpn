using System.Net.Sockets;
using System.Net;
using System.Text;

namespace VpnClient
{
    internal class Program
    {

        //private static string _ip = "192.168.1.140";
        //private static int _connctionPort = 5555;
        //private static int _messagePort = 5554;
        private static string _ip = "93.84.86.45";
        private static int _connctionPort = 40041;
        private static int _messagePort = 40040;
        private static string _messageForUser = "Hello From Client";
        private static IPEndPoint _connctionEndPoint = new IPEndPoint(IPAddress.Parse(_ip), _connctionPort);
        private static IPEndPoint _messageEndPoint = new IPEndPoint(IPAddress.Parse(_ip), _messagePort);
        private static UdpClient _udpClient = new UdpClient();
        private static UdpReceiveResult _receiveMessageResult;
        private static string _sendMessage { get; set; }
        private static string _recivedMessage { get; set; }

        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private static CancellationToken _token = _cancellationTokenSource.Token;

        static async Task Main(string[] args)
        {
            ConnectToServer();
            Task.Run(() =>
            {
                KeepConnectAlive();
            });
            Task.Run(() =>
            {
                GetMessages();
            });
            Console.WriteLine($"{_udpClient.Client.LocalEndPoint}");
            Console.Write("input pc name: ");
            _sendMessage = Console.ReadLine();
            SendMessage(_sendMessage, _messageEndPoint);
            while (_recivedMessage == null)
                continue;
            var IpPort = _recivedMessage.Split(' ');
            Console.WriteLine($"{IpPort[0]} {IpPort[1]} Form Main");
            _cancellationTokenSource?.Cancel();
            try
            {
                _udpClient.Connect(new IPEndPoint(IPAddress.Parse(IpPort[0]), int.Parse(IpPort[1])));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            while (true)
                if (Console.ReadLine() == "send")
                {
                    Console.WriteLine($"{_udpClient.Client.LocalEndPoint}");
                    await SendMessage("Hello");
                }
        }

        private static async Task ConnectToServer()
        {
            var name = Environment.MachineName.ToLower();
            byte[] pcName = Encoding.UTF8.GetBytes(name);
            await _udpClient.SendAsync(pcName, _connctionEndPoint);
        }

        private static async Task KeepConnectAlive()
        {
            try
            {
                while (!_token.IsCancellationRequested)
                {
                    _udpClient.SendAsync(new byte[] { 0 }, _connctionEndPoint);
                    Thread.Sleep(3000);
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static async Task SendMessage(string message)
        {
            await _udpClient.SendAsync(Encoding.UTF8.GetBytes(message));
        }

        private static async Task SendMessage(string message, IPEndPoint endpoint)
        {
            await _udpClient.SendAsync(Encoding.UTF8.GetBytes(message), endpoint);
        }

        private static async Task GetMessages()
        {
            try
            {
                while (true)
                {
                    _receiveMessageResult = await _udpClient.ReceiveAsync();
                    if (_receiveMessageResult.Buffer != null)
                    {
                        _recivedMessage = Encoding.UTF8.GetString(_receiveMessageResult.Buffer);
                        Console.WriteLine(_recivedMessage);
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
