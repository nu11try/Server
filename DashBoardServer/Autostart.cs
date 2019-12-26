using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DashBoardServer
{
    class Autostart
    {
        private Database database = new Database();
        private MySqlCommand command;
        private MySqlDataReader reader;
        private MySqlDataReader reader1;
        private Logger logger = new Logger();
        private string query = "";
        List<OptionsAutostart> options = new List<OptionsAutostart>();
        Timer timer;
        TimerCallback tm;
        MethodsDB methodsDB = new MethodsDB();
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

        public void Init(object Obj)
        {
            tm = new TimerCallback(CheckTime);
            timer = new Timer(tm, "", 1000, 60000);
        }
        public void CheckTime(object obj)
        {
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

                DateTime date = DateTime.Now;
                string nowDay = transformDate(date.DayOfWeek.ToString());

                options.ForEach(autostart =>
                {
                    if (autostart.days.Contains(nowDay))
                    {
                        int time = transform(autostart.time, false);
                        int timeAfter = transform(autostart.time, true);
                        int now = transform(date.Hour.ToString() + ":" + date.Minute.ToString(), false);
                        Console.WriteLine(autostart.time + "--" + date.Hour.ToString() + ":" + date.Minute.ToString() + "--" + autostart.time);
                        if (now >= time && now <= timeAfter && autostart.status == "no_start")
                        {
                            Thread StartTests = new Thread(new ParameterizedThreadStart(startAuto));
                            StartTests.Start(autostart);
                        }
                    }
                });
            }
            catch
            {
                Console.WriteLine("Пиздец тайм чекеру пришел!");
            }
        }

        public void startAuto(Object obj)
        {
            try
            {
                OptionsAutostart autostart = (OptionsAutostart)obj;
                Message mess = JsonConvert.DeserializeObject<Message>(autostart.pack);
                mess.args.Insert(0, autostart.service);

                query = "UPDATE autostart SET `status` = 'start' WHERE `id` = @id";
                command = new MySqlCommand(query, database.connect);
                command.Parameters.AddWithValue("@id", autostart.id);
                database.OpenConnection();
                var UpdateTest = command.ExecuteNonQuery();
                database.CloseConnection();
                logger.WriteLog("Обновлен статус автозапуска! Произведен запуск " + autostart.id);
                methodsDB.StartTests(mess);
            }
            catch
            {
                Console.WriteLine("Вот и старт авто наебнулся!");
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

        public int transform(string time, bool after)
        {
            int m = (Int32.Parse(time.Split(':')[0]) * 60 + Int32.Parse(time.Split(':')[1])) * 60000;
            return after ? m + 90000 : m;
        }
    }
}
