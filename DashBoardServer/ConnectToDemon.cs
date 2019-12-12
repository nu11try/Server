using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DashBoardServer
{
    class ConnectToDemon
    {
        const int port = 8889;
        string address = "";
        //const string address = "127.0.0.1";

        private RequestDemon request = new RequestDemon();
        string bufJSON;
        string nameText = "";

        byte[] data;
        string param;
        /// <summary>
        /// Функциия для запуска запроса на коннект к серверу
        /// </summary>
        /// <param name="msg">Сообщение</param>
        /// <param name="service">Сервис</param>
        /// <returns></returns>
        public void StartTestsInDemon(object param)
        {
            request.args = param;
            bufJSON = JsonConvert.SerializeObject(request);
            bufJSON = bufJSON.Replace("{\"args\":{\"args\":[", "{\"args\":[");
            bufJSON = bufJSON.Remove(bufJSON.Length - 1, 1);
            Message packs = new Message();
            packs = JsonConvert.DeserializeObject<Message>(bufJSON);
            for (int i = 0; i < packs.args.Count - 1; i += 9) // нужно count-1 и i+=9 так как аргументов у набора 9 и в самом конце добавляется еще 1 ("Start") 
            {
                address = packs.args[i + 3].Split(' ')[2];
                nameText = "\\" + DateTime.Now.ToString("ddMMyyyyhhssmmfff");
                while (true)
                {
                    try
                    {
                        File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + nameText, bufJSON);
                        break;
                    }
                    catch
                    {
                        Task.Delay(1000);
                    }
                }
                ConnectServer(bufJSON, nameText);
            }
        }
        public void StopTestsInDemon(object param)
        {
            bufJSON = (string)param;
            request.args = bufJSON;
            Message packs = new Message();
            packs = JsonConvert.DeserializeObject<Message>(bufJSON);
            address = packs.args[1].Split(' ')[2];
            nameText = "\\" + DateTime.Now.ToString("ddMMyyyymmhhssfff");
            while (true)
            {
                try
                {
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + nameText, bufJSON);
                    break;
                }
                catch
                {
                    Task.Delay(1000);
                }
            }
            ConnectServer(bufJSON, nameText);
        }

        private string ConnectServer(string json, string nameText)
        {
            Console.WriteLine(address + " : " + port);
            TcpClient client = null;
            StringBuilder builder = new StringBuilder();
            string response = "";
            try
            {
                client = new TcpClient(address, port);
                NetworkStream stream = client.GetStream();
                while (true)
                {
                    try
                    {
                        data = File.ReadAllBytes(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + nameText);
                        File.Delete(AppDomain.CurrentDomain.BaseDirectory + nameText);
                        break;
                    }
                    catch
                    {
                        Task.Delay(1000);
                    }
                }

                int bufferSize = 1024;
                byte[] dataLength = BitConverter.GetBytes(data.Length);
                stream.Write(dataLength, 0, 4);
                int bytesSent = 0;
                int bytesLeft = data.Length;
                while (bytesLeft > 0)
                {
                    int curDataSize = Math.Min(bufferSize, bytesLeft);
                    stream.Write(data, bytesSent, curDataSize);
                    bytesSent += curDataSize;
                    bytesLeft -= curDataSize;
                }
                nameText = "\\" + DateTime.Now.ToString("ddyyyyMMhhmmssfff");
                while (true)
                {
                    try
                    {
                        File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + nameText, data);
                        param = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + nameText).Replace("\n", " ");
                        File.Delete(AppDomain.CurrentDomain.BaseDirectory + nameText);
                        break;
                    }
                    catch
                    {
                        Task.Delay(1000);
                    }
                }
                byte[] fileSizeBytes = new byte[4];
                int bytes = stream.Read(fileSizeBytes, 0, 4);
                int dataLengthResponse = BitConverter.ToInt32(fileSizeBytes, 0);
                bytesLeft = dataLengthResponse;
                data = new byte[dataLengthResponse];
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
                nameText = "\\" + DateTime.Now.ToString("MMddyyyyhhmmssfff");
                while (true)
                {
                    try
                    {
                        File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + nameText, data);
                        param = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + nameText).Replace("\n", " ");
                        File.Delete(AppDomain.CurrentDomain.BaseDirectory + nameText);
                        break;
                    }
                    catch
                    {
                        Task.Delay(1000);
                    }
                }
                response = param;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            request = new RequestDemon();
            bufJSON = "";
            return response;
        }
    }

    public class RequestDemon
    {
        public object args { get; set; }
    }
}