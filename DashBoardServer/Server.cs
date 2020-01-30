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
using MySql.Data.MySqlClient;
using System.Diagnostics;

namespace DashBoardServer
{
    static class Data
    {
        public static int Port { get; set; }
        public static string IP { get; set; }
        public static string[] TestsForStart { get; set; }
    }
    class Server
    {
        //const int port = 8888;
        //const string ip = "172.31.197.89";
        //const string ip = "172.17.42.40";
        //const string ip = "172.31.197.232";
        //const string ip = "172.17.42.32";
        // const string ip = "127.0.0.1";
        //const string ip = "172.31.191.200";
        static TcpListener listener;        

        static void Main(string[] args)
        {
            try
            {
               // Data.IP = "172.17.42.32";
                Data.IP = "172.31.191.200";                
                Data.Port = 8888;

                listener = new TcpListener(IPAddress.Parse(Data.IP), Data.Port);
                listener.Start();
                Console.WriteLine("===================================");
                Console.WriteLine("Произведен запуск Asylum!");
                Console.WriteLine("\n");
                Console.WriteLine("Сервер готов принимать подключения");
                Console.WriteLine("==================================");                

                try
                {
                    Autostart autostart = new Autostart();
                    AutostartDemon(autostart);                    
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
                    ConnectClient(clientObject);
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
        static async void ConnectClient(ClientObject clientObject)
        {
            await Task.Run(()=> clientObject.Process());
        }
        static async void AutostartDemon(Autostart autostart)
        {
            await Task.Run(() => autostart.Init());
        }
    }
}
