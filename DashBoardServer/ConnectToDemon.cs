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
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "param.txt", bufJSON);
                ConnectServer(bufJSON);
            }
        }
        public void StopTestsInDemon(object param)
        {
            bufJSON = (string)param;
            request.args = bufJSON;
            Message packs = new Message();
            packs = JsonConvert.DeserializeObject<Message>(p);
            address = packs.args[1].Split(' ')[2];
            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "param.txt", bufJSON);
            ConnectServer(bufJSON);
        }

        private string ConnectServer(string json)
        {
            Console.WriteLine(address + " : " + port);
            TcpClient client = null;
            StringBuilder builder = new StringBuilder();
            string response = "";
            try
            {
                client = new TcpClient(address, port);
                NetworkStream stream = client.GetStream();

                byte[] data = File.ReadAllBytes(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\param.txt");
                /*
                // преобразуем сообщение в массив байтов
                byte[] data = new byte[] { };
                data = Encoding.Unicode.GetBytes(json);

                // отправка сообщения
                stream.Write(data, 0, data.Length);

                // получаем ответ
                data = new byte[9999999]; // буфер для получаемых данных

                int bytes = 0;

                bytes = stream.Read(data, 0, data.Length);
                builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                response = builder.ToString();

                builder.Clear();
                stream.Close();
                client.Close();
                */
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
                File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + "param.txt", data);
                string param = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "param.txt").Replace("\n", " ");

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
                File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + "param.txt", data);
                param = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "param.txt").Replace("\n", " ");
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