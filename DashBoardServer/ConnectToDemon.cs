using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DashBoardServer
{
    class ConnectToDemon
    {
        const int port = 8889;
        const string address = "172.31.197.232";
        //const string address = "127.0.0.1";

        private RequestDemon request = new RequestDemon();
        string bufJSON;

        /// <summary>
        /// Функциия для запуска запроса на коннект к серверу
        /// </summary>
        /// <param name="msg">Сообщение</param>
        /// <param name="service">Сервис</param>
        /// <returns></returns>
        public string StartTestsInDemon(object param)
        {
            request.args = param;
            bufJSON = JsonConvert.SerializeObject(request);
            bufJSON = bufJSON.Replace("{\"args\":{\"args\":[", "{\"args\":[");
            bufJSON = bufJSON.Remove(bufJSON.Length - 1, 1);
            Console.WriteLine(bufJSON);
            return ConnectServer(bufJSON);
        }

        private string ConnectServer(string json)
        {
            TcpClient client = null;
            StringBuilder builder = new StringBuilder();
            string response = "";
            try
            {
                client = new TcpClient(address, port);
                NetworkStream stream = client.GetStream();

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