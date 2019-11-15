using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data.Linq;
using System.Data.SQLite;

namespace DashBoardServer
{
    class Server
    {
        const int port = 8888;
        //const string ip = "172.31.197.232";
        const string ip = "127.0.0.1";
        static TcpListener listener;

        static class Data
        {
            public static string[] TestsForStart { get; set; }
        }

        static void Main(string[] args)
        {           
            try
            {
                listener = new TcpListener(IPAddress.Parse(ip), port);
                listener.Start();
                Console.WriteLine("===================================");
                Console.WriteLine("Произведен запуск Asylum!");
                Console.WriteLine("\n");
                Console.WriteLine("Сервер готов принимать подключения");
                Console.WriteLine("==================================");

                try
                {
                    Autostart autostart = new Autostart();
                    Thread autoStartThread = new Thread(new ParameterizedThreadStart(autostart.Init));
                    autoStartThread.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("!!!Отвалился автостарт по причине " + ex.Message + "!!!");
                }

                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();                    
                    ClientObject clientObject = new ClientObject(client);

                    //создаем новый поток для обслуживания нового клиента
                    Thread clientThread = new Thread(new ThreadStart(clientObject.Process));
                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (listener != null)
                    listener.Stop();
            }
        }
    }
}
