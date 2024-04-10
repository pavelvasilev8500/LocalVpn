using System.Net;
using System.Net.Sockets;
using System.Text;
using LocalVpn.Model;
using LocalVpn.Registrtion;

namespace LocalVpn
{
    internal class Program
    {

        private static int _connctionPort = 5554;
        private static int _messagePort = 5555;

        private static UdpClient _udpClient = new UdpClient(_connctionPort);
        private static UdpClient _udpMessageClient = new UdpClient(_messagePort);

        private static string _buffer{ get; set; }

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

        private static async void GetConnection()
        {
            while (true)
            {
                try
                {
                    var receivedData = await _udpClient.ReceiveAsync();
                    if (receivedData.Buffer != null)
                    {
                        _buffer = Encoding.UTF8.GetString(receivedData.Buffer);
                        if (_buffer.Contains("ServerName"))
                        {
                            ServerRegistration.Registration(_buffer.Split(' ')[1], _udpClient, receivedData.RemoteEndPoint, ClientRegistration.GetClient());
                            foreach (var server in ServerRegistration.GetServer())
                                Console.WriteLine(server.Key + ": " + server.Value.Ip + ":" + server.Value.Port);
                        }
                        else if (_buffer.Contains("ClientName"))
                        {
                            ClientRegistration.Registration(_buffer.Split(' ')[1], _udpClient, receivedData.RemoteEndPoint, ServerRegistration.GetServer());
                            foreach (var client in ClientRegistration.GetClient())
                                Console.WriteLine(client.Key + ": " + client.Value.Ip + ":" + client.Value.Port);
                        }
                        else
                            Console.WriteLine($"Message {_buffer} from Ip: {receivedData.RemoteEndPoint.Address} Port: {receivedData.RemoteEndPoint.Port}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            //try
            //{
            //    while (true)
            //    {
            //        _canClientAdd = true;
            //        _canServerAdd = true;
            //        _receiveConnectionResult = await _udpClient.ReceiveAsync();
            //        if (_receiveConnectionResult.Buffer != null)
            //        {
            //            _receivedData = Encoding.UTF8.GetString(_receiveConnectionResult.Buffer);
            //            Console.WriteLine(_receivedData);
            //            if (_receivedData.Contains("ClientName"))
            //                ClientRegistration(_receivedData, _receiveConnectionResult, _udpClient);
            //            else if (_receivedData.Contains("ServerName"))
            //                ServerRegistration(_receivedData, _receiveConnectionResult, _udpClient);
            //            else
            //                RequestToConnect(_receivedData, _receiveConnectionResult, _udpClient);
            //        }

            //    }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.Message);
            //}
        }


        //private static async void RequestToConnect(string req, UdpReceiveResult udpReceiveResult, UdpClient client)
        //{
        //    UdpClient sendClient = client;
        //    var ipPort = udpReceiveResult.RemoteEndPoint;
        //    Console.WriteLine(ipPort);
        //    foreach (var o in _clientServer)
        //    {
        //        if(o.Key == req)
        //        {
        //            foreach (var c in _clients)
        //            {
        //                if (c.Value[0] == udpReceiveResult.RemoteEndPoint.ToString().Split(':')[0] &
        //                    c.Value[1] == udpReceiveResult.RemoteEndPoint.ToString().Split(':')[1])
        //                {
        //                    Console.WriteLine($"Request {req} from client {c.Key}-{c.Value[0]}:{c.Value[1]}");
        //                    break;
        //                }
        //            }
        //        }
        //        if(o.Key == req & o.Value.CanAccess)
        //        {
        //            try
        //            {
        //                await sendClient.SendAsync(Encoding.UTF8.GetBytes("Ready"), new IPEndPoint(IPAddress.Parse(o.Value.IpPort[0]), int.Parse(o.Value.IpPort[1])));
        //                Console.WriteLine("Send Ready");
        //                Console.WriteLine(ipPort);
        //                //o.Value.CanAccess = false;
        //                Task.Run(() =>
        //                {
        //                    GetMessages(client, udpReceiveResult.RemoteEndPoint);
        //                });
        //                break;
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine(ex.Message);
        //                break;
        //            }
        //        }
        //        else if (o.Key != req && !o.Value.CanAccess)
        //        {
        //            try
        //            {
        //                await sendClient.SendAsync(Encoding.UTF8.GetBytes("Unknown server name or server unavailable"), udpReceiveResult.RemoteEndPoint);
        //                Console.WriteLine("Unknown server name or server unavailable");
        //                break;
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine (ex.Message);
        //                break;
        //            }
        //        }
        //    }
        //}


        //private static async void GetMessages(UdpClient client, IPEndPoint endPoint)
        //{
        //    UdpClient sendMessage = client;
        //    Console.WriteLine(endPoint);
        //    try
        //    {
        //        while (true)
        //        {
        //            _receiveMessageResult = await _udpMessageClient.ReceiveAsync();
        //            if (_receiveMessageResult.Buffer != null)
        //            {
        //                Console.WriteLine($"Message: {Encoding.UTF8.GetString(_receiveMessageResult.Buffer)} to {endPoint}");
        //                await sendMessage.SendAsync(_receiveMessageResult.Buffer, endPoint);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //    }
        //}

    }
}
