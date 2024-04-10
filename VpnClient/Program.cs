using System.Net.Sockets;
using System.Net;
using System.Text;

namespace VpnClient
{
    internal class Program
    {
        //private static string _ip = Resource.ip;
        //private static int _connctionPort = int.Parse(Resource.inport);
        //private static int _messagePort = int.Parse(Resource.outport);
        private static string _ip = "93.84.86.45";
        private static int _connctionPort = 40040;
        private static int _messagePort = 40041;
        private static IPEndPoint _connctionEndPoint = new IPEndPoint(IPAddress.Parse(_ip), _connctionPort);
        private static IPEndPoint _messageEndPoint = new IPEndPoint(IPAddress.Parse(_ip), _messagePort);
        private static UdpClient _udpClient = new UdpClient();
        private static UdpReceiveResult _receiveMessageResult;
        private static string _recivedMessage { get; set; }

        private static string ServerName { get; set; }

        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private static CancellationToken _token = _cancellationTokenSource.Token;

        static async Task Main(string[] args)
        {
            ConnectToServer();
            //Task.Run(() =>
            //{
            //    KeepConnectAlive();
            //});
            Task.Run(() =>
            {
                GetMessages();
            });
            Console.ReadLine();
        }

        private static async Task ConnectToServer()
        {
            ServerName = Console.ReadLine();
            //var name = Environment.MachineName.ToLower();
            byte[] pcName = Encoding.UTF8.GetBytes($"ServerName {ServerName}");
            await _udpClient.SendAsync(pcName, _connctionEndPoint);
        }

        private static async Task KeepConnectAlive()
        {
            try
            {
                while (!_token.IsCancellationRequested)
                {
                    //_udpClient.SendAsync(new byte[] { 0 }, _connctionEndPoint);
                    _udpClient.SendAsync(Encoding.UTF8.GetBytes("Hello From Client"), _connctionEndPoint);
                    Thread.Sleep(20000);
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

        private static async void SendDataMessage(string data, IPEndPoint endPoint)
        {
            while(true)
            {
                await _udpClient.SendAsync(Encoding.UTF8.GetBytes(data), endPoint);
                Thread.Sleep(3000);
            }
        }

        private static async Task GetMessages()
        {
            try
            {
                while (!_token.IsCancellationRequested)
                {
                    _receiveMessageResult = await _udpClient.ReceiveAsync();
                    if (_receiveMessageResult.Buffer != null)
                    {
                        _recivedMessage = Encoding.UTF8.GetString(_receiveMessageResult.Buffer);
                        Console.WriteLine($"Received {_recivedMessage}");
                        if(_recivedMessage == "Ready")
                        {
                            Console.WriteLine(_recivedMessage);
                            _cancellationTokenSource.Cancel();
                            Thread sendData = new Thread(() => 
                            {
                                SendDataMessage($"Hello {ServerName}", _messageEndPoint);
                            });
                            sendData.Start();
                        }
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
