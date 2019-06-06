using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NamingServer
{
    public class NameServer
    {
        private IPAddress ipAddress;
        private Socket Server;
        private Socket Client;
        private byte[] buffer=new byte[1024];

        public NameServer(IPAddress address)
        {
            ipAddress = address;
        }

        public void StartServer()
        {
            try
            {
                Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Server.Bind(new IPEndPoint(ipAddress, 33033));
                Server.Listen(5);
                Server.BeginAccept(new AsyncCallback(AcceptCallback), null);
                Console.WriteLine("Naming server started.");
            }
            catch
            {
                Console.WriteLine("Cannot start on specified IP address.");
            }
        }

        private void AcceptCallback(IAsyncResult asyncResult)
        {
            try
            {
                Client = Server.EndAccept(asyncResult);
                Client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), Client);
                Server.BeginAccept(new AsyncCallback(AcceptCallback), null);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }

        private void ReceiveCallback(IAsyncResult asyncResult)
        {
            try
            {
                int received = Client.EndReceive(asyncResult);
                byte[] dataBuf = new byte[received];
                Array.Copy(buffer, dataBuf, received);
                string clientHello = Encoding.UTF8.GetString(dataBuf);
                Console.WriteLine(clientHello);
                Client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }
    }
}
