using System;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;
using System.Text;

namespace DashBoardServer
{
    public class ClientObject
    {
        public TcpClient client;
        MethodsDB methodsDB = new MethodsDB();
        FreeRAM freeRAM = new FreeRAM();

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
                /*
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
                builder.Clear();*/

                byte[] fileSizeBytes = new byte[4];
                int bytes = stream.Read(fileSizeBytes, 0, 4);
                int dataLength = BitConverter.ToInt32(fileSizeBytes, 0);
                int bytesLeft = dataLength;
                byte[] data = new byte[dataLength];
                int bufferSize = 1024;
                int bytesRead = 0;
                while (bytesLeft > 0)
                {
                    int curDataSize = Math.Min(bufferSize, bytesLeft);
                    if (client.Available < curDataSize)
                        curDataSize = client.Available; //This saved me
                    bytes = stream.Read(data, bytesRead, curDataSize);
                    bytesRead += curDataSize;
                    bytesLeft -= curDataSize;
                }                
                File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + "\\param.txt", data);                
                string param = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\param.txt").Replace("\n", " ");
                File.Delete(AppDomain.CurrentDomain.BaseDirectory + "param.txt");
                string buf = methodsDB.transformation(param);
                File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + "\\param.txt", Encoding.UTF8.GetBytes(buf));

                data = File.ReadAllBytes(AppDomain.CurrentDomain.BaseDirectory + "\\param.txt");
                File.Delete(AppDomain.CurrentDomain.BaseDirectory + "param.txt");
                byte[] dataLengthResponse = BitConverter.GetBytes(data.Length);
                stream.Write(dataLengthResponse, 0, 4);
                int bytesSent = 0;
                bytesLeft = data.Length;
                while (bytesLeft > 0)
                {
                    int curDataSize = Math.Min(bufferSize, bytesLeft);
                    stream.Write(data, bytesSent, curDataSize);
                    bytesSent += curDataSize;
                    bytesLeft -= curDataSize;
                }

                //data = Encoding.Unicode.GetBytes(buf);
                //stream.Write(data, 0, data.Length);        
                File.Delete(AppDomain.CurrentDomain.BaseDirectory + "param.txt");
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
