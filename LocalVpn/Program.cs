﻿using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace LocalVpn
{
    internal class Program
    {

        private static int _connctionPort = 5554;
        private static int _messagePort = 5555;

        private static UdpClient _udpConnectionClient = new UdpClient(_connctionPort);
        private static UdpClient _udpMessageClient = new UdpClient(_messagePort);
        private static UdpClient _udpSendClent;

        private static UdpReceiveResult _receiveConnectionResult;
        private static UdpReceiveResult _receiveMessageResult;

        private static readonly string PATH = @$"{Environment.CurrentDirectory}\log{DateTime.Now.ToShortDateString()}.txt";

        private static StreamWriter _streamWriter = new StreamWriter(PATH, true);

        //name [[ip][port]]
        private static Dictionary<string, string[]> _clients = new Dictionary<string, string[]>();
        private static Dictionary<string, string[]> _clientServer = new Dictionary<string, string[]>();
        private static string _receivedData { get; set; }
        private static string _clientName { get; set; }
        private static IPEndPoint _clientEndPoint { get; set; }
        private static bool _canClientAdd { get; set; } = true;
        private static bool _canServerAdd { get; set; } = true;
        static async Task Main(string[] args)
        {
            _streamWriter.WriteLine($"{DateTime.Now}");
            _streamWriter.Flush();
            _streamWriter.WriteLine("Start\n------------------------------------------------");
            _streamWriter.Flush();
            var conncetionThread = new Thread(() =>
            {
                GetConnection();
            });
            conncetionThread.Name = "GetConnectionThread";
            conncetionThread.Start();
            var messageThread = new Thread(() =>
            {
                GetMessages();
            });
            messageThread.Name = "GetMessageThread";
            messageThread.Start();
            Console.ReadLine();
            _streamWriter.WriteLine("------------------------------------------------\nEnd");
            _streamWriter.Flush();
        }

        private static async void GetConnection()
        {
            try
            {
                while (true)
                {
                    _canClientAdd = true;
                    _canServerAdd = true;
                    _receiveConnectionResult = await _udpConnectionClient.ReceiveAsync();
                    if (_receiveConnectionResult.Buffer != null)
                    {
                        _receivedData = Encoding.UTF8.GetString(_receiveConnectionResult.Buffer);
                        #region Client Reg Region
                        if (_receivedData.Contains("ClientName"))
                        {
                            _clientName = _receivedData.Split(' ')[1];
                            foreach (var o in _clients)
                            {
                                if (o.Key == _clientName)
                                {
                                    _canClientAdd = false;
                                    _streamWriter.WriteLine($"Имя занято! Имя: {_clientName}");
                                    _streamWriter.Flush();
                                    Console.WriteLine($"Имя занято!");
                                }
                            }
                            if(_canClientAdd)
                            {
                                _clients.Add(_clientName, _receiveConnectionResult.RemoteEndPoint.ToString().Split(':'));
                                _streamWriter.WriteLine($"Connected Name: {_clientName} IpPort: {_receiveConnectionResult.RemoteEndPoint}");
                                _streamWriter.Flush();
                                Console.WriteLine($"Connected Name: {_clientName} IpPort: {_receiveConnectionResult.RemoteEndPoint}");
                                await _udpConnectionClient.SendAsync(ObjectToByteArray(_clientServer), _receiveConnectionResult.RemoteEndPoint);
                            }
                        }
                        #endregion
                        #region Server Reg Region
                        else if (_receivedData.Contains("ServerName"))
                        {
                            _clientName = _receivedData.Split(' ')[1];
                            foreach (var o in _clientServer)
                            {
                                if (o.Key == _clientName)
                                {
                                    _canServerAdd = false;
                                    _streamWriter.WriteLine($"Имя занято! Имя: {_clientName}");
                                    _streamWriter.Flush();
                                    Console.WriteLine($"Имя занято!");
                                }
                            }
                            if (_canClientAdd)
                            {
                                _clientServer.Add(_clientName, _receiveConnectionResult.RemoteEndPoint.ToString().Split(':'));
                                _streamWriter.WriteLine($"Connected Name: {_clientName} IpPort: {_receiveConnectionResult.RemoteEndPoint}");
                                _streamWriter.Flush();
                                Console.WriteLine($"Connected Name: {_clientName} IpPort: {_receiveConnectionResult.RemoteEndPoint}");
                            }
                        }
                        #endregion
                        else if (_receivedData != "\0")
                        {
                            foreach(var o in _clientServer)
                            {
                                if (o.Value[0] == _receiveConnectionResult.RemoteEndPoint.ToString().Split(':')[0] & 
                                    o.Value[1] == _receiveConnectionResult.RemoteEndPoint.ToString().Split(':')[1])
                                {
                                    _streamWriter.WriteLine($"Message from {o.Key}: {_receivedData}");
                                    _streamWriter.Flush();
                                    Console.WriteLine($"Message from {o.Key}: {_receivedData}");
                                }
                                if(o.Key == _receivedData)
                                {
                                    _clientEndPoint = _receiveConnectionResult.RemoteEndPoint;
                                    try
                                    {
                                        _streamWriter.WriteLine($"Send READY to {o.Key} {o.Value[0]}:{o.Value[1]}");
                                        _streamWriter.Flush();
                                        await _udpConnectionClient.SendAsync(Encoding.UTF8.GetBytes("Ready"),
                                            new IPEndPoint(IPAddress.Parse(o.Value[0]), int.Parse(o.Value[1])));
                                    }
                                    catch (Exception ex)
                                    {
                                        _streamWriter.WriteLine($"Error while sending READY to {o.Key} {o.Value[0]}:{o.Value[1]}\n{ex.Message}");
                                        _streamWriter.Flush();
                                        Console.WriteLine($"EX: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _streamWriter.WriteLine($"Error while get connection {ex.Message}");
                _streamWriter.Flush();
                Console.WriteLine(ex.Message);
            }
        }

        public static byte[] ObjectToByteArray(Object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        private static async void GetMessages()
        {
            _udpSendClent = _udpConnectionClient;
            try
            {
                while (true)
                {
                    _receiveMessageResult = await _udpMessageClient.ReceiveAsync();
                    if (_receiveMessageResult.Buffer != null)
                    {
                        _streamWriter.WriteLine($"Message: {Encoding.UTF8.GetString(_receiveMessageResult.Buffer)} From {_receiveMessageResult.RemoteEndPoint}");
                        _streamWriter.Flush();
                        Console.WriteLine($"Message: {Encoding.UTF8.GetString(_receiveMessageResult.Buffer)}");
                        await _udpSendClent.SendAsync(_receiveMessageResult.Buffer, _clientEndPoint);
                    }
                }
            }
            catch (Exception ex)
            {
                _streamWriter.WriteLine($"Error {ex.Message} while recieved from {_receiveMessageResult.RemoteEndPoint}");
                _streamWriter.Flush();
                Console.WriteLine(ex.Message);
            }
        }

    }
}
