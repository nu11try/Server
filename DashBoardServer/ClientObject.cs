using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Reflection;

namespace DashBoardServer
{
    public class ClientObject
    {
        public TcpClient client;
        MethodsDB methodsDB = new MethodsDB();
        FreeRAM freeRAM = new FreeRAM();

        byte[] data;
        public ClientObject(TcpClient tcpClient)
        {
            client = tcpClient;
        }

        public void Process()
        {
            NetworkStream stream = null;            
            try
            {
                stream = client.GetStream();
                data = new byte[9999999]; // буфер для получаемых данных
                                              // получаем сообщение
                StringBuilder builder = new StringBuilder();
                int bytes = 0;
                do
                {
                    bytes = stream.Read(data, 0, data.Length);
                    builder.Append(Encoding.Unicode.GetString(data, 0, bytes));                    
                }
                while (stream.DataAvailable);
                string buf = methodsDB.transformation(builder.ToString());
                data = Encoding.Unicode.GetBytes(buf);
                stream.Write(data, 0, data.Length);
                builder.Clear();                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (stream != null)
                    stream.Close();                  
                if (client != null)
                    client.Close();

                freeRAM.Free();
            }
        }
    }
}
