using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DashBoardServer
{
    class Autostart
    {
        private Database database = new Database();
        private MySqlCommand command;
        private MySqlDataReader reader;
        private static Logger logger = new Logger();
        private string query = "";
        List<OptionsAutostart> options = new List<OptionsAutostart>();
        Timer timer;
        TimerCallback tm;
        MethodsDB methodsDB = new MethodsDB();

        private Utils _utils;
        public class OptionsAutostart
        {
            public string id { get; set; }
            public string pack { get; set; }
            public string time { get; set; }
            public string days { get; set; }
            public string service { get; set; }
            public string type { get; set; }
            public string status { get; set; }

        }

        public void Init()
        {
            tm = new TimerCallback(CheckTime);
            timer = new Timer(tm, "", 1000, 60000);
        }
        public void CheckTime(object obj)
        {
            _utils = new Utils();
            // БЛОК ПРОВЕРКИ И УСТАНОВКИ ВРЕМЕНИ
            try
            {
                if (DateTime.Now.Hour == 23) _utils.ClearWorkDir();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка установки времени " + ex.Message);
                logger.WriteLog("Ошибка установки времени " + ex.Message, "ERROR");
            }
            try
            {
                options = new List<OptionsAutostart>();
                query = "SELECT * FROM autostart";
                command = new MySqlCommand(query, database.connect);
                database.OpenConnection();
                reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        OptionsAutostart autostart = new OptionsAutostart();

                        autostart.id = reader["id"].ToString();
                        autostart.pack = reader["Packs"].ToString();
                        autostart.time = reader["Time"].ToString();
                        autostart.days = reader["Days"].ToString();
                        autostart.service = reader["Service"].ToString();
                        autostart.type = reader["Type"].ToString();
                        autostart.status = reader["status"].ToString();
                        options.Add(autostart);
                    }
                }
                reader.Close();
                database.CloseConnection();

                try
                {
                    database.connect.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Не смог закрыть connection.Close()! " + ex.Message);
                    logger.WriteLog("Не смог закрыть connection.Close()! " + ex.Message, "ERROR");
                }

                DateTime date = DateTime.Now;
                string nowDay = transformDate(date.DayOfWeek.ToString());

                options.ForEach(autostart =>
                {
                    if (autostart.days.Contains(nowDay))
                    {
                        int time = transform(autostart.time, false);
                        int timeAfter = transform(autostart.time, true);
                        int now = transform(date.Hour.ToString() + ":" + date.Minute.ToString(), false);
                        if (now >= time && now <= timeAfter)
                        {
                            _utils.ClearWorkDir();
                            try
                            {
                                AutoStartTestTask(autostart, database, methodsDB);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Ошибка автозапуска в таске AutoStartTestTask! " + ex.Message);
                                logger.WriteLog("Ошибка автозапуска в таске AutoStartTestTask! " + ex.Message, "ERROR");
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка тайм-чекера автостарта! " + ex.Message);
                logger.WriteLog("Ошибка тайм-чекера автостарта! " + ex.Message, "ERROR");
            }
        }
        static void startAuto(OptionsAutostart autostart, Database database, MethodsDB methodsDB)
        {
            try
            {
                Message mess = JsonConvert.DeserializeObject<Message>(autostart.pack);
                mess.args.Insert(0, autostart.service);

                string query = "UPDATE autostart SET `status` = 'start' WHERE `id` = @id";
                MySqlCommand command = new MySqlCommand(query, database.connect);
                command.Parameters.AddWithValue("@id", autostart.id);
                database.OpenConnection();
                var UpdateTest = command.ExecuteNonQuery();
                database.CloseConnection();
                methodsDB.StartTests(mess);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка = ", ex.Message);
            }
        }
        public string transformDate(string day)
        {
            if (day.Equals("Monday")) return "ПН";
            if (day.Equals("Tuesday")) return "ВТ";
            if (day.Equals("Wednesday")) return "СР";
            if (day.Equals("Thursday")) return "ЧТ";
            if (day.Equals("Friday")) return "ПТ";
            if (day.Equals("Saturday")) return "СБ";
            if (day.Equals("Sunday")) return "ВС";
            return null;
        }
        static async void AutoStartTestTask(OptionsAutostart autostart, Database database, MethodsDB methodsDB)
        {
            await Task.Run(() => startAuto(autostart, database, methodsDB));
        }
        public int transform(string time, bool after)
        {
            int m = (Int32.Parse(time.Split(':')[0]) * 60 + Int32.Parse(time.Split(':')[1])) * 60000;
            return after ? m + 90000 : m;
        }
        static string GetCurTime()
        {
            string result = "";
            try
            {
                WebRequest req = WebRequest.Create("https://time-in.ru/");
                WebResponse resp = req.GetResponse();
                Stream stream = resp.GetResponseStream();
                StreamReader sr = new System.IO.StreamReader(stream);
                string html = sr.ReadToEnd();
                sr.Close();
                Regex myReg = new Regex(@"(G:i:s)...........");
                Match match = myReg.Match(html);
                result = match.Value.Split('>')[1];
                result = result.Split('<')[0];
                result = result.Split(':')[0] + ":" + result.Split(':')[1];
            }
            catch (Exception ex)
            {
                logger.WriteLog("Невозможно получить точное время по причине " + ex.Message, "ERROR");
                Console.WriteLine("Невозможно получить точное время по причине " + ex.Message);
            }
            return result;
        }
    }
}
