using System.Net.Sockets;
using System.Net;
using System.Text;

namespace ConnctionClient
{
    internal class Program
    {
        private static string _ip = Resource.ip;
        private static int _connctionPort = int.Parse(Resource.inport);
        private static int _messagePort = int.Parse(Resource.outport);
        private static IPEndPoint _connctionEndPoint = new IPEndPoint(IPAddress.Parse(_ip), _connctionPort);
        private static IPEndPoint _messageEndPoint = new IPEndPoint(IPAddress.Parse(_ip), _messagePort);
        private static UdpClient _udpClient = new UdpClient();
        private static UdpReceiveResult _receiveMessageResult;
        private static string _sendMessage { get; set; }
        private static string _recivedMessage { get; set; }

        static async Task Main(string[] args)
        {
            ConnectToServer();
            Task.Run(() =>
            {
                GetMessages();
            });
            Console.Write("input pc name: ");
            _sendMessage = Console.ReadLine();
            SendMessage(_sendMessage, _connctionEndPoint);
            Console.ReadLine();
        }

        private static async Task ConnectToServer()
        {
            var name = Environment.MachineName.ToLower();
            byte[] pcName = Encoding.UTF8.GetBytes($"ClientName {name}");
            await _udpClient.SendAsync(pcName, _connctionEndPoint);
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
