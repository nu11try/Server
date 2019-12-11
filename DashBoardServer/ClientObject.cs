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

        string nameText = "";
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
                nameText = "\\" + DateTime.Now.ToString("ddMMyyyyhhmmssfff");
                File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + nameText, data);                
                string param = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + nameText).Replace("\n", " ");
                File.Delete(AppDomain.CurrentDomain.BaseDirectory + nameText);
                string buf = methodsDB.transformation(param);

                nameText = "\\" + DateTime.Now.ToString("ddMMyyyyhhmmfffss");
                File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + nameText, Encoding.UTF8.GetBytes(buf));
                data = File.ReadAllBytes(AppDomain.CurrentDomain.BaseDirectory + nameText);
                File.Delete(AppDomain.CurrentDomain.BaseDirectory + nameText);
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
 
                File.Delete(AppDomain.CurrentDomain.BaseDirectory + nameText);
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
