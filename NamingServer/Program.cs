using System;
using System.Net;
using Nancy.Hosting.Self;
using System.Data.SQLite;

namespace NamingServer
{
    class MainClass
    {

        public static void Main(string[] args)
        {
            NancyHost host = new NancyHost(new Uri("http://localhost:33033"), new Bootstrapper());
            host.Start();
            FileSystem.FillDirsFromDB(FileSystem.root);
            Console.WriteLine("Naming server started.");
            Console.ReadLine();
            FileSystem.db.CloseConnection();
        }
    }
}
