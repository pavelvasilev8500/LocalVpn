using System.Net;

namespace LocalVpn.Model
{
    internal class ServerModel
    {
        public IPAddress Ip {  get; set; }
        public int Port { get; set; }
        public bool CanAccess { get; set; }
    }
}
