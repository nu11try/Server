using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Net;
using Atlassian.Jira;

namespace DashBoardServer
{
    class MethodsDB
    {
        private Database database = new Database();
        private SQLiteCommand command;
        private Logger logger = new Logger();
        private string query = "";
        private static Message res = new Message();

        public string transformation(string param)
        {
            Message mess = JsonConvert.DeserializeObject<Message>(param);
            Type type = typeof(MethodsDB);
            object o = Activator.CreateInstance(type);
            MethodInfo info = type.GetMethod(mess.args[0]);
            mess.args.RemoveAt(0);
            if (!mess.args[1].Equals(""))
            {
                Message paramFun = JsonConvert.DeserializeObject<Message>(mess.args[1]);
                if (!mess.args[0].Equals("")) paramFun.args.Insert(0, mess.args[0]);
                info.Invoke(o, new object[] { paramFun });
            }
            else info.Invoke(o, new object[] { mess });

            string resS = JsonConvert.SerializeObject(res);
            res = new Message();
            return resS;
        }
        public void Auth(Message mess)
        {
            string login = mess.args[0];
            string password = mess.args[1];
            Message message = new Message();

            var token = Encoding.UTF8.GetBytes(login + password);

            query = "SELECT * FROM user WHERE `login` = @login AND `password` = @password";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@login", login);
            command.Parameters.AddWithValue("@password", password);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();
            if (SelectResult.HasRows)
            {
                while (SelectResult.Read())
                {
                    query = "SELECT `full_name` FROM service WHERE `name` = @name";
                    command = new SQLiteCommand(query, database.connect);
                    Message serviceName = JsonConvert.DeserializeObject<Message>(SelectResult["projects"].ToString());
                    foreach (var elService in serviceName.args)
                    {
                        command.Parameters.AddWithValue("@name", elService);
                        database.OpenConnection();
                        SQLiteDataReader SelectResult1 = command.ExecuteReader();
                        if (SelectResult1.HasRows)
                        {
                            while (SelectResult1.Read())
                            {
                                message.Add(SelectResult1["full_name"].ToString());
                            }
                        }
                        SelectResult1.Close();
                    }
                    res.Add(Convert.ToBase64String(token), SelectResult["sec_level"].ToString(),
                        SelectResult["projects"].ToString(), JsonConvert.SerializeObject(message), SelectResult["name"].ToString());
                }
            }
            else res.Add("no");
            SelectResult.Close();
            database.CloseConnection();
        }
        //-------------------------------------------------------------------------------------
        // ФУНКЦИИ ПОЛУЧЕНИЯ
        //-------------------------------------------------------------------------------------
        /// <summary>
        /// Функция получения пути для сервиса (для тестов)
        /// </summary>
        /// <param name="nameService"> Имя сервиса </param>
        /// <returns></returns>
        private string GetPathService(Message mess)
        {
            query = "SELECT path FROM service WHERE `name` = @service";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();
            string result = "";

            if (SelectResult.HasRows) while (SelectResult.Read()) result = SelectResult["path"].ToString();

            SelectResult.Close();
            database.CloseConnection();
            return result;
        }
        /// <summary>
        /// Функция получения название папок тестов (имя самих тестов) 
        /// </summary>
        /// <param name="service"> Сервис, на котором мы сидим </param>
        /// <returns></returns>
        public void GetTestsPath(Message mess)
        {
            string path = "";
            List<string> dirs = new List<string>();

            path = GetPathService(mess);
            //if (service == "ai") path = "Z:\\DEG_AI\\Tests";
            DirectoryInfo dir = new DirectoryInfo(path);

            foreach (var item in dir.GetDirectories()) dirs.Add(item.ToString());

            query = "SELECT * FROM tests WHERE `service` = @service";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();

            if (SelectResult.HasRows)
            {
                while (SelectResult.Read())
                {
                    try { if (dirs.Count > 0) dirs.Remove(SelectResult["id"].ToString()); }
                    catch { }
                }
            }
            SelectResult.Close();
            if (dirs.Count > 0)
            {

                query = "INSERT INTO tests (`id`, `name`, `service`) VALUES (@id, @name, @service)";
                command = new SQLiteCommand(query, database.connect);
                foreach (var item in dirs)
                {
                    string query1 = "SELECT path FROM service WHERE `name` = @service";
                    SQLiteCommand command1 = new SQLiteCommand(query1, database.connect);
                    command1.Parameters.AddWithValue("@service", mess.args[0]);
                    database.OpenConnection();
                    SQLiteDataReader SelectResult1 = command1.ExecuteReader();
                    string direct = "";
                    if (SelectResult1.HasRows) while (SelectResult1.Read()) direct = SelectResult1["path"].ToString();
                    SelectResult1.Close();
                    database.CloseConnection();

                    string query2 = "INSERT INTO dirs (`test`, `service`, `path`) VALUES (@id, @service, @path)";
                    SQLiteCommand command2 = new SQLiteCommand(query2, database.connect);
                    command2.Parameters.AddWithValue("@id", item.ToString());
                    command2.Parameters.AddWithValue("@service", mess.args[0]);
                    command2.Parameters.AddWithValue("@path", direct);
                    database.OpenConnection();
                    var InsertTesult2 = command2.ExecuteNonQuery();
                    database.CloseConnection();
                    command.Parameters.AddWithValue("@id", item.ToString());
                    command.Parameters.AddWithValue("@name", item.ToString());
                    command.Parameters.AddWithValue("@service", mess.args[0]);
                    database.OpenConnection();
                    var InsertTesult = command.ExecuteNonQuery();
                    database.CloseConnection();
                }
                Console.WriteLine("{0} add tests ", dirs.Count().ToString());
                Console.WriteLine("{0} tests for return ", res.args.Count().ToString());
            }
            database.CloseConnection();
        }
        /// <summary>
        /// Функция получения ФИО автора
        /// </summary>
        /// <param name="service"> Сервис (так же будут выводиться авторы, у которых есть доступ ко всему) </param>
        /// <returns></returns>
        public void GetAuthor(Message mess)
        {
            query = "SELECT `name` FROM authors WHERE `service` = @service OR `service` = 'all'";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();

            if (SelectResult.HasRows)
            {
                while (SelectResult.Read()) res.Add(SelectResult["name"].ToString());
            }
            SelectResult.Close();
            database.CloseConnection();
        }
        public void GetStends(Message mess)
        {
            query = "SELECT `url` FROM stends";
            command = new SQLiteCommand(query, database.connect);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();

            if (SelectResult.HasRows)
            {
                while (SelectResult.Read()) res.Add(SelectResult["url"].ToString());
            }
            SelectResult.Close();
            database.CloseConnection();
        }
        public void GetVersion(Message mess)
        {
            query = "SELECT `version`, `data` FROM stends WHERE `service` = @service";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();

            if (SelectResult.HasRows)
            {
                while (SelectResult.Read()) res.Add(SelectResult["version"].ToString(), SelectResult["data"].ToString());
            }
            SelectResult.Close();
            database.CloseConnection();
        }
        /// <summary>
        /// Функция получения тестов определенного сервиса для отображения их в ListView
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public void GetTests(Message mess)
        {
            GetTestsPath(mess);
            query = "SELECT * FROM tests WHERE `service` = @service AND `status` = @status";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            if (mess.args[1].Equals("no_add")) command.Parameters.AddWithValue("@status", "no_add");
            else command.Parameters.AddWithValue("@status", "add");
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();
            if (SelectResult.HasRows)
            {
                while (SelectResult.Read())
                {
                    res.Add(SelectResult["id"].ToString());
                    res.Add(SelectResult["name"].ToString());
                    res.Add(SelectResult["author"].ToString());
                    string query1 = "SELECT * FROM kp WHERE `service` = @service AND `test` = @test";
                    SQLiteCommand command1 = new SQLiteCommand(query1, database.connect);
                    command1.Parameters.AddWithValue("@service", mess.args[0]);
                    command1.Parameters.AddWithValue("@test", mess.args[1]);
                    SQLiteDataReader SelectResult1 = command1.ExecuteReader();
                    if (SelectResult1.HasRows)
                    {
                        SelectResult1.Read();
                        res.Add(SelectResult1["id"].ToString());
                    }
                    SelectResult1.Close();
                }
            }
            SelectResult.Close();
            database.CloseConnection();
        }
        /// <summary>
        /// Функция получения тестов набора
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public void GetTestsForPack(Message mess)
        {
            query = "SELECT * FROM tests WHERE `service` = @service AND `status` = 'add'";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();
            if (SelectResult.HasRows)
            {
                while (SelectResult.Read())
                {
                    res.Add(SelectResult["id"].ToString(), SelectResult["name"].ToString(), SelectResult["author"].ToString());
                }
            }
            else
            {
                res.Add("no_tests_for_pack");
            }
            SelectResult.Close();
            database.CloseConnection();

        }
        /// <summary>
        /// Функция получения IP адреса демона
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public void GetIPPc(Message mess)
        {
            query = "SELECT * FROM demons";
            command = new SQLiteCommand(query, database.connect);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();
            if (SelectResult.HasRows)
            {
                while (SelectResult.Read()) res.Add(
                    SelectResult["name"].ToString(), SelectResult["ip"].ToString());
            }
            SelectResult.Close();
            database.CloseConnection();
        }
        /// <summary>
        /// Функция получения наборов для последующего вывода в ListView
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public void GetPacksForList(Message mess)
        {
            query = "SELECT * FROM packs WHERE `service` = @service";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();
            if (SelectResult.HasRows)
            {
                while (SelectResult.Read()) res.Add(
                    SelectResult["id"].ToString(), SelectResult["name"].ToString()
                    , SelectResult["tests"].ToString(), SelectResult["time"].ToString()
                    , SelectResult["count_restart"].ToString()
                    , SelectResult["ip"].ToString()
                    , SelectResult["status"].ToString());
            }
            else
            {
                res.Add("no_packs");
            }
            SelectResult.Close();
            database.CloseConnection();

        }
        /// <summary>
        /// Функция получения данных по определенному набору
        /// </summary>
        /// <param name="service"></param>
        /// <param name="ID"></param>
        /// <returns></returns>
        public void GetPackChange(Message mess)
        {
            query = "SELECT * FROM packs WHERE `service` = @service AND `id` = @id";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            command.Parameters.AddWithValue("@id", mess.args[1]);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();
            if (SelectResult.HasRows)
            {
                while (SelectResult.Read()) res.Add(SelectResult["id"].ToString(), SelectResult["name"].ToString(),
                    SelectResult["ip"].ToString(), SelectResult["time"].ToString(), SelectResult["count_restart"].ToString(),
                    SelectResult["tests"].ToString(), SelectResult["browser"].ToString(), SelectResult["stend"].ToString());

            }
            else
            {
                res.Add("error");
            }
            SelectResult.Close();
            database.CloseConnection();
        }
        /// <summary>
        /// Функция получения тестов для обпереденного набора
        /// </summary>
        /// <param name="service"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public void GetTestsThisPack(Message mess)
        {
            Tests testsPack = new Tests();
            SQLiteDataReader SelectResult;
            query = "SELECT `tests` FROM packs WHERE `service` = @service AND `id` = @id";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            command.Parameters.AddWithValue("@id", mess.args[1]);
            database.OpenConnection();
            SelectResult = command.ExecuteReader();
            if (SelectResult.HasRows)
            {
                while (SelectResult.Read())
                {
                    testsPack = JsonConvert.DeserializeObject<Tests>(SelectResult["tests"].ToString());
                }
            }
            SelectResult.Close();
            database.CloseConnection();
            for (int i = 0; i < testsPack.id.Count; i++)
            {
                string id = "";
                if (testsPack.duplicate[i] != "not") id = testsPack.duplicate[i];
                else id = testsPack.id[i];
                query = "SELECT * FROM tests WHERE `service` = @service AND `id` = @id";
                command = new SQLiteCommand(query, database.connect);
                command.Parameters.AddWithValue("@service", mess.args[0]);
                command.Parameters.AddWithValue("@id", id);
                database.OpenConnection();
                SelectResult = command.ExecuteReader();
                if (SelectResult.HasRows)
                {
                    while (SelectResult.Read())
                    {
                        res.Add(testsPack.id[i], SelectResult["name"].ToString(), testsPack.time[i], testsPack.restart[i], testsPack.browser[i], testsPack.dependon[i], testsPack.duplicate[i]);
                    }
                }
                else
                {
                    res.Add("no_tests");
                }
                SelectResult.Close();
                database.CloseConnection();
                

                query = "SELECT * FROM statistic WHERE `service` = @service AND `id` = @id";
                string result = "";
                string time = "";
                command = new SQLiteCommand(query, database.connect);
                command.Parameters.AddWithValue("@service", mess.args[0]);
                command.Parameters.AddWithValue("@id", testsPack.id[i]);
                database.OpenConnection();
                SelectResult = command.ExecuteReader();
                if (SelectResult.HasRows)
                {
                    while (SelectResult.Read())
                    {
                        result = SelectResult["result"].ToString();
                        time = SelectResult["time_end"].ToString();
                    }
                    res.Add(time, result);
                }
                else
                {
                    res.Add("Нет данных", "Нет данных");
                }
                SelectResult.Close();
                database.CloseConnection();
                
            }
            
        }
        /// <summary>
        /// Функция получения дополнительных параметров о тесте
        /// </summary>
        /// <param name="service"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public void GetTestPerform(Message mess)
        {
            string id = mess.args[1];
            string id_pack = mess.args[2];
            Message args = new Message();
            //  string argsS = "";
            query = "SELECT * FROM packs WHERE `service` = @service AND `id` = @id";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            command.Parameters.AddWithValue("@id", id_pack);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();
            if (SelectResult.HasRows)
            {
                while (SelectResult.Read())
                {
                    Tests te = JsonConvert.DeserializeObject<Tests>(SelectResult["tests"].ToString());
                    for (int j = 0; j < te.id.Count; j++)
                    {
                        if (te.id[j].Equals(id))
                            res.Add(te.id[j], te.start[j], te.time[j], te.restart[j], te.browser[j]);
                        // args.Add(te.id[j]);

                    }
                }
                // argsS = JsonConvert.SerializeObject(args);
            }
            else
            {
                res.Add("error");
            }
            // Console.WriteLine(argsS);
            SelectResult.Close();
            database.CloseConnection();
            // res.Add(argsS);
        }
        /// <summary>
        /// Функция получения результатов теста
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        /// 
        public void GetErrors(Message mess)
        {
            Jira jira = Jira.CreateRestClient("https://job-jira.otr.ru", "suhorukov.anton", "g8kyto648W");
            query = "SELECT * FROM jira where `test` = @test ";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@test", mess.args[1]);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();
            if (SelectResult.HasRows)
            {
                while (SelectResult.Read())
                {
                    var issues = from i in jira.Issues.Queryable
                                 where i.Key == SelectResult["link"].ToString()
                                 select i;

                    query = "UPDATE jira SET `status` = @status,`name` = @name, `type` = @type, `executor` = @executor, data = @data " +
                                   "WHERE `link` = @link";
                    command = new SQLiteCommand(query, database.connect);
                    command.Parameters.AddWithValue("@link", SelectResult["link"].ToString());
                    command.Parameters.AddWithValue("@status", issues.First().Status.Name);
                    command.Parameters.AddWithValue("@name", issues.First().Summary);
                    command.Parameters.AddWithValue("@type", issues.First().Type.Name);
                    command.Parameters.AddWithValue("@executor", issues.First().Reporter);
                    command.Parameters.AddWithValue("@data", issues.First().Created.Value.ToString());
                    database.OpenConnection();
                    var UpdateTest = command.ExecuteNonQuery();
                }
            }

            SelectResult.Close();
            database.CloseConnection();


            query = "SELECT * FROM jira where `test` = @test ";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@test", mess.args[1]);
            database.OpenConnection();
            SelectResult = command.ExecuteReader();
            if (SelectResult.HasRows)
            {
                while (SelectResult.Read())
                    res.Add(SelectResult["name"].ToString(), SelectResult["link"].ToString(), SelectResult["type"].ToString(),SelectResult["data"].ToString(), SelectResult["executor"].ToString(), SelectResult["status"].ToString());
            }
            SelectResult.Close();
            database.CloseConnection();
        }
        public void CheckErrors(Message mess)
        {
            Jira jira = Jira.CreateRestClient("https://job-jira.otr.ru", "suhorukov.anton", "g8kyto648W");
            query = "SELECT * FROM jira where `test` = @test ";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@test", mess.args[1]);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();
            if (SelectResult.HasRows)
            {
                while (SelectResult.Read())
                {
                    var issues = from i in jira.Issues.Queryable
                            where i.Key == SelectResult["link"].ToString()
                            select i;
                    
                    query = "UPDATE jira SET `status` = @status,`name` = @name, `type` = @type, `executor` = @executor, data = @data " +
                                   "WHERE `link` = @link";
                    command = new SQLiteCommand(query, database.connect);
                    command.Parameters.AddWithValue("@link", SelectResult["link"].ToString());
                    command.Parameters.AddWithValue("@status", issues.First().Status.Name);
                    command.Parameters.AddWithValue("@name", issues.First().Summary);
                    command.Parameters.AddWithValue("@type", issues.First().Type.Name);
                    command.Parameters.AddWithValue("@executor", issues.First().Reporter);
                    command.Parameters.AddWithValue("@data", issues.First().Created.Value.ToString());
                    database.OpenConnection();
                    var UpdateTest = command.ExecuteNonQuery();
                }
            }

            SelectResult.Close();
            database.CloseConnection();
            query = "SELECT * FROM jira where `test` = @test and (`type` = 'Ошибка' or `type` = 'Доработка' or `type` = 'Компонентная доработка') and `status` <> 'Закрыто' and `status` <> 'Протестировано' and `status` <> 'Отклонено' and `status` <> 'Авторская приемка'and `status` <> 'Archive'";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@test", mess.args[1]);
            database.OpenConnection();
            SelectResult = command.ExecuteReader();
            if (SelectResult.HasRows)
            {
                res.Add("errors");
            }
            else
            {
                SelectResult.Close();
                database.CloseConnection();
                query = "SELECT * FROM jira where `test` = @test and `type` = 'Задача' and `status` <> 'Закрыто' and `status` <> 'Протестировано' and `status` <> 'Отклонено' and `status` <> 'Авторская приемка'and `status` <> 'Archive'";
                command = new SQLiteCommand(query, database.connect);
                command.Parameters.AddWithValue("@test", mess.args[1]);
                database.OpenConnection();
                SelectResult = command.ExecuteReader();
                if (SelectResult.HasRows)
                {
                    res.Add("issue");
                }
                else
                {
                    res.Add("no issue");
                }
            }
            SelectResult.Close();
            database.CloseConnection();

        }
        public void GetTestResult(Message mess)
        {            
            // хз на сколько это правильно, но это блять работает
            query = "SELECT * FROM statistic LEFT JOIN tests ON statistic.id = tests.id WHERE statistic.service = @service AND statistic.stend = @stend ORDER BY number DESC";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            command.Parameters.AddWithValue("@stend", mess.args[1]);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();
            if (SelectResult.HasRows)
            {
                while (SelectResult.Read())
                {
                    res.Add( SelectResult["name"].ToString(), SelectResult["result"].ToString(),
                        SelectResult["time_step"].ToString(), SelectResult["steps"].ToString(), SelectResult["version"].ToString());
                    if (SelectResult["author"].ToString() == "")
                    {
                        query = "SELECT * FROM tests where id = @id and service = @service ";
                        SQLiteCommand command1 = new SQLiteCommand(query, database.connect);
                        command1.Parameters.AddWithValue("@service", mess.args[0]);
                        command1.Parameters.AddWithValue("@id", SelectResult["id"].ToString().Split('(')[0]);
                        database.OpenConnection();
                        SQLiteDataReader SelectResult1 = command1.ExecuteReader();
                        if (SelectResult1.HasRows)
                        {
                            SelectResult1.Read();
                                res.Add(SelectResult1["author"].ToString());
                        }
                        SelectResult1.Close();
                    }
                    else
                    {
                        res.Add(SelectResult["author"].ToString());
                    }
                    res.Add(SelectResult["id"].ToString());
                }
            }            
            else
            {
                res.Add("no_result");
            }
            SelectResult.Close();
            database.CloseConnection();

        }
        public void GetTestResultVersion(Message mess)
        {
            query = "SELECT * FROM statistic where `service` = @service and `version` = @version and `id` = @id and (`result`= 'Passed' or `result` = 'Warning')";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            command.Parameters.AddWithValue("@version", mess.args[1]);
            command.Parameters.AddWithValue("@id", mess.args[2]);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();
            if (SelectResult.HasRows)
            {
                while (SelectResult.Read())
                {
                    res.Add(SelectResult["result"].ToString());     
                }
            }
        }
        /// <summary>
        /// Функция получение подробной информации по результату теста
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public void GetTestResultInfo(Message mess)
        {
            query = "SELECT * FROM statistic WHERE `service` = @service AND `stend` = @stend ORDER BY number DESC";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            command.Parameters.AddWithValue("@stend", mess.args[1]);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();
            if (SelectResult.HasRows)
            {
                while (SelectResult.Read())
                {
                    res.Add(SelectResult["date"].ToString(), SelectResult["result"].ToString(), 
                        SelectResult["version"].ToString(), SelectResult["time_end"].ToString(), SelectResult["id"].ToString(), SelectResult["version"].ToString());
                }
            }
            else
            {
                res.Add("no_result");
            }
            SelectResult.Close();
            database.CloseConnection();
        }
        /// <summary>
        /// Функция получения документа
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public void GetDocument(Message mess)
        {
            query = "SELECT * FROM doc WHERE `service` = @service";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();
            if (SelectResult.HasRows)
            {
                while (SelectResult.Read())
                {
                    res.Add(SelectResult["id"].ToString(), SelectResult["pim"].ToString(),
                        SelectResult["date"].ToString());
                }
            }
            else
            {
                res.Add("no_doc");
            }
            SelectResult.Close();
            database.CloseConnection();
        }
        /// <summary>
        /// Функция получения информации по документу
        /// </summary>
        /// <param name="service"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public void GetDocInfo(Message mess)
        {
            query = "SELECT * FROM doc WHERE `service` = @service AND `id` = @id";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            command.Parameters.AddWithValue("@id", mess.args[1]);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();
            if (SelectResult.HasRows)
            {
                while (SelectResult.Read()) res.Add(
                    SelectResult["pim"].ToString(), SelectResult["date"].ToString());
            }
            else
            {
                res.Add("error");
            }
            SelectResult.Close();
            database.CloseConnection();

        }
        /// <summary>
        /// Функция получения КП для документа
        /// </summary>
        /// <param name="service"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public void GetKPForDoc(Message mess)
        {
            query = "SELECT * FROM kp WHERE `service` = @service AND `id_doc` = @id";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            command.Parameters.AddWithValue("@id", mess.args[1]);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();
            if (SelectResult.HasRows)
            {
                while (SelectResult.Read()) res.Add(
                    SelectResult["id"].ToString(), SelectResult["name"].ToString(),
                    SelectResult["steps"].ToString(), SelectResult["author"].ToString(),
                    SelectResult["date"].ToString(), SelectResult["test"].ToString());
            }
            else
            {
                res.Add("no_kp");
            }
            SelectResult.Close();
            database.CloseConnection();
        }
        /// <summary>
        /// Функция получения информации по КП
        /// </summary>
        /// <param name="service"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public void GetKPInfo(Message mess)
        {
            string id_doc = mess.args[1].Equals("") ? "" : "AND `id_doc` = @id_doc ";
            string id_kp = mess.args[2].Equals("") ? "" : "AND `id` = @id_kp ";
            query = "SELECT * FROM kp WHERE `service` = @service " + id_doc + id_kp;
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            command.Parameters.AddWithValue("@id_doc", mess.args[1]);
            command.Parameters.AddWithValue("@id_kp", mess.args[2]);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();
            if (SelectResult.HasRows)
            {
                while (SelectResult.Read())
                {
                    Message tests = JsonConvert.DeserializeObject<Message>(SelectResult["test"].ToString());
                    if (tests.args.Contains(mess.args[3]) || mess.args[3].Equals("")) {
                        string query1 = "SELECT * FROM doc WHERE `service` = @service AND `id` = @id";
                        SQLiteCommand command1 = new SQLiteCommand(query1, database.connect);
                        command1.Parameters.AddWithValue("@service", mess.args[0]);
                        command1.Parameters.AddWithValue("@id", SelectResult["id_doc"].ToString());
                        database.OpenConnection();
                        SQLiteDataReader SelectResult1 = command1.ExecuteReader();
                        SelectResult1.Read();
                        res.Add(SelectResult["id"].ToString(), SelectResult["name"].ToString(), SelectResult["date"].ToString(), SelectResult1["id"].ToString());
                    }
                }
            }
            SelectResult.Close();
            
            if (res.args.Count == 0) res.Add("error");
            database.CloseConnection();
        }
        /// <summary>
        /// Функция получения автостарта
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public void GetAutostart(Message mess)
        {
            ;
            query = "SELECT * FROM autostart WHERE `service` = @service";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();
            if (SelectResult.HasRows)
            {
                while (SelectResult.Read()) res.Add(SelectResult["id"].ToString(),
                    SelectResult["name"].ToString(), SelectResult["days"].ToString(),
                    SelectResult["time"].ToString(), SelectResult["packs"].ToString(),
                    SelectResult["type"].ToString(), SelectResult["status"].ToString());
            }
            else
            {
                res.Add("error");
            }
            SelectResult.Close();
            database.CloseConnection();
        }
        /// <summary>
        /// Функция получения информации по автостарту
        /// </summary>
        /// <param name="service"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public void GetAutostartInfo(Message mess)
        {
            query = "SELECT * FROM autostart WHERE `service` = @service AND `id` = @id";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            command.Parameters.AddWithValue("@id", mess.args[1]);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();
            if (SelectResult.HasRows)
            {
                while (SelectResult.Read()) res.Add(SelectResult["id"].ToString(),
                    SelectResult["name"].ToString(), SelectResult["days"].ToString(),
                    SelectResult["time"].ToString(), SelectResult["packs"].ToString(),
                    SelectResult["type"].ToString(), SelectResult["status"].ToString());
            }
            else
            {
                res.Add("error");
            }
            SelectResult.Close();
            database.CloseConnection();
        }
        /// <summary>
        /// Функция получения комментария
        /// </summary>
        /// <param name="service"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public void GetCommnents(Message mess)
        {
            query = "SELECT `comments` FROM tests WHERE `service` = @service AND `id` = @id_test";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            command.Parameters.AddWithValue("@id_test", mess.args[1]);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();
            if (SelectResult.HasRows)
            {
                while (SelectResult.Read()) res.Add(SelectResult["comments"].ToString());
            }
            else
            {
                res.Add("error");
            }
            SelectResult.Close();
            database.CloseConnection();
        }
        //-------------------------------------------------------------------------------------
        // ФУНКЦИИ ДОБАВЛЕНИЯ
        //-------------------------------------------------------------------------------------
        /// <summary>
        /// Функция добавления теста в БД
        /// </summary>
        /// <param name="service"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public void AddTest(Message mess)
        {
            // СДЕЛАТЬ
            Comments comments = readTextOfTest(mess.args[0], mess.args[1]);
            foreach (var el in mess.args) Console.WriteLine(el);
            string name = comments.comment[0];
            comments.comment.RemoveAt(0);
            string steps = comments.comment[0];
            comments.comment.RemoveAt(0);
            comments.step.RemoveAt(0);
            comments.step.RemoveAt(0);
            string commS = JsonConvert.SerializeObject(comments);
            query = "UPDATE tests SET `name` = @name,`service` = @service, `status` = @status, " +
                "`author` = @author, `comments` = @comments,`statistic` = @statistic ,`used` = @used " +
                "WHERE `id` = @id and `status` = @earlyStatus";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@id", mess.args[1]);
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            command.Parameters.AddWithValue("@status", "add");
            command.Parameters.AddWithValue("@author", mess.args[2]);
            command.Parameters.AddWithValue("@comments", commS);
            command.Parameters.AddWithValue("@statistic", mess.args[3]);
            command.Parameters.AddWithValue("@used", "no");
            command.Parameters.AddWithValue("@earlyStatus", "no_add");
            database.OpenConnection();
            var UpdateTest = command.ExecuteNonQuery();
            database.CloseConnection();


            query = "SELECT * FROM kp WHERE `service` = @service";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();
            Message tests = new Message();
            int step = 0;
            if (SelectResult.HasRows)
            {
                while (SelectResult.Read())
                {
                    if (SelectResult["id"].ToString().Equals(mess.args[4]))
                    {
                        tests = JsonConvert.DeserializeObject<Message>(SelectResult["test"].ToString());
                        step = Int32.Parse(SelectResult["steps"].ToString());
                    }
                }
            }
            SelectResult.Close();
            database.CloseConnection();

            if (tests.args[0].Equals("not")) tests.args.RemoveAt(0);
            tests.Add(mess.args[1]);

            query = "UPDATE kp SET `test` = @test, `steps` = @steps " +
                "WHERE `id` = @id and `service` = @service";

            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@test", JsonConvert.SerializeObject(tests));
            command.Parameters.AddWithValue("@id", mess.args[4]);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            command.Parameters.AddWithValue("@steps", (step + Int32.Parse(steps)).ToString());
            database.OpenConnection();
            UpdateTest = command.ExecuteNonQuery();
            database.CloseConnection();


            res.Add("OK");
        }
        /// <summary>
        /// Функция добавления документа
        /// </summary>
        /// <param name="service"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public void AddDoc(Message param)
        {
            query = "INSERT INTO doc (`id`,`pim`, `date`, `service`)"
                + "VALUES (@id, @pim, @date, @service)";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@id", param.args[1]);
            command.Parameters.AddWithValue("@pim", param.args[1]);
            command.Parameters.AddWithValue("@date", param.args[2]);
            command.Parameters.AddWithValue("@service", param.args[0]);
            database.OpenConnection();
            var InsertTesult = command.ExecuteNonQuery();
            database.CloseConnection();
            logger.WriteLog(InsertTesult.ToString() + " create doc");

            res.Add("OK");
        }
        public void AddStatisticDemon(Message mess)
        {
            query = "INSERT INTO statistic (`id`, `test`, `service`, `result`, `time_step`, `time_end`, `time_lose`, `steps`, `date`, `version`, `stend`)" +
                "VALUES (@id, @test, @service, @result, @time_step, @time_end, @time_lose, @steps, @date, @version, @stend)";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@id", mess.args[1]);
            command.Parameters.AddWithValue("@test", mess.args[2]);
            command.Parameters.AddWithValue("@service", mess.args[3]);
            command.Parameters.AddWithValue("@result", mess.args[4]);
            command.Parameters.AddWithValue("@time_step", JsonConvert.SerializeObject(mess.args[5]));
            command.Parameters.AddWithValue("@time_end", mess.args[6]);
            command.Parameters.AddWithValue("@time_lose", 0);
            command.Parameters.AddWithValue("@steps", JsonConvert.SerializeObject(mess.args[8]));
            command.Parameters.AddWithValue("@date", mess.args[9]);
            command.Parameters.AddWithValue("@version", mess.args[9]);
            command.Parameters.AddWithValue("@stend", mess.args[10]);
            database.OpenConnection();
            command.ExecuteNonQuery();
            database.CloseConnection();
        }
        /// <summary>
        /// Функция добавления набора в БД
        /// </summary>
        /// <param name="service"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public void AddPack(Message mess)
        {
            Message tests = JsonConvert.DeserializeObject<Message>(mess.args[2]);
            Tests te = new Tests();
            for (int i = 0; i < tests.args.Count; i++)
            {
                te.id.Add(tests.args[i]);
                te.restart.Add("default");
                te.start.Add(i == 0 ? "первый" : tests.args[i - 1]);
                te.time.Add("default");
                te.dependon.Add("{\"args\":[\"not\"]}");
                te.browser.Add("default");
                te.duplicate.Add("not");

            }
            string teS = JsonConvert.SerializeObject(te);
            query = "INSERT INTO packs (`id`, `name`, `tests`,`browser`, `time`, `count_restart`, `service`, " +
                "`ip`, `status`, `stend`) VALUES (@id, @name, @tests, @browser, @time, @count_restart, " +
                "@service, @ip, @status, @stend)";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@id", mess.args[1]);
            command.Parameters.AddWithValue("@name", mess.args[1]);
            command.Parameters.AddWithValue("@tests", teS);
            command.Parameters.AddWithValue("@time", mess.args[3]);
            command.Parameters.AddWithValue("@count_restart", mess.args[4]);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            command.Parameters.AddWithValue("@ip", mess.args[5]);
            command.Parameters.AddWithValue("@status", "no_start");
            command.Parameters.AddWithValue("@browser", mess.args[6]);
            command.Parameters.AddWithValue("@stend", mess.args[7]);
            database.OpenConnection();
            var InsertPack = command.ExecuteNonQuery();
            database.CloseConnection();
            logger.WriteLog("{0} create packs", InsertPack.ToString());

            query = "UPDATE tests SET `used` = 'yes' WHERE `id` = @id and `service` = @service";
            command = new SQLiteCommand(query, database.connect);
            for (int i = 0; i < tests.args.Count; i++)
            {
                command.Parameters.AddWithValue("@id", tests.args[i]);
                command.Parameters.AddWithValue("@service", mess.args[0]);
                database.OpenConnection();
                var UpdateTest = command.ExecuteNonQuery();
                database.CloseConnection();
                logger.WriteLog("{0} update test", UpdateTest.ToString());
            }
            res.Add("OK");
        }

        public void AddKP(Message mess)
        {
            query = "INSERT INTO kp (`id`, `name`, `steps`, `author`, `date`, `id_doc`" +
                ", `service`,`test`)"
                + "VALUES (@id, @name, @steps, @author, @date, @id_doc, @service, @test)";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@id", mess.args[1]);
            command.Parameters.AddWithValue("@name", mess.args[1]);
            command.Parameters.AddWithValue("@steps", mess.args[5]);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            command.Parameters.AddWithValue("@author", mess.args[3]);
            command.Parameters.AddWithValue("@test", "--");
            command.Parameters.AddWithValue("@id_doc", mess.args[4]);
            command.Parameters.AddWithValue("@date", mess.args[2]);
            database.OpenConnection();
            var InsertTesult = command.ExecuteNonQuery();
            database.CloseConnection();
            logger.WriteLog(InsertTesult.ToString() + " create kp");

            res.Add("OK");
        }
        
        public void AddAutostart(Message mess)
        {
            query = "INSERT INTO autostart (`id`, `name`, `days`, `service`, `time`, `packs`, `type`)"
                + "VALUES (@id_auto, @Name, @Days, @Service, @Time, @Packs, @Type)";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@id_auto", mess.args[1]);
            command.Parameters.AddWithValue("@Name", mess.args[2]);
            command.Parameters.AddWithValue("@Days", mess.args[4]);
            command.Parameters.AddWithValue("@Service", mess.args[0]);
            command.Parameters.AddWithValue("@Time", mess.args[6] + ":" + mess.args[7]);
            command.Parameters.AddWithValue("@Packs", mess.args[5]);
            command.Parameters.AddWithValue("@Type", mess.args[3]);
            database.OpenConnection();
            var InsertTesult = command.ExecuteNonQuery();
            database.CloseConnection();
            logger.WriteLog(InsertTesult.ToString() + " create auto");

            res.Add("OK");
        }
        public void AddBug(Message mess)
        {
            List<string> tests = new List<string>();
            query = "SELECT * FROM packs WHERE `service` = @service";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();
            if (SelectResult.HasRows)
            {
                while (SelectResult.Read())
                {
                    Tests test = JsonConvert.DeserializeObject<Tests>(SelectResult["tests"].ToString());
                    if (test.id.Contains(mess.args[1]))
                    {
                        for(int i =0; i < test.id.Count; i++)
                        {
                            Message message = JsonConvert.DeserializeObject<Message>(test.dependon[i]);
                            if (message.args.Contains(mess.args[1]))
                            {
                                if(!tests.Contains(test.id[i]))
                                    tests.Add(test.id[i]);
                            }
                            if (test.duplicate.Equals(mess.args[1]))
                            {
                                if (!tests.Contains(test.id[i]))
                                    tests.Add(test.id[i]);
                            }
                        }
                    }
                }
            }
            SelectResult.Close();
            database.CloseConnection();
            tests.Add(mess.args[1]);
            for (int i = 0; i < tests.Count; i++)
            {
                query = "INSERT INTO jira (`test`, `link`,`status`)"
                + "VALUES (@test, @link, 'В работе')";
                command = new SQLiteCommand(query, database.connect);
                command.Parameters.AddWithValue("@test", tests[i]);
                command.Parameters.AddWithValue("@link", mess.args[2]);
                database.OpenConnection();
                var InsertTesult = command.ExecuteNonQuery();
                database.CloseConnection();
                logger.WriteLog(InsertTesult.ToString() + " create bug");
            }
            res.Add("OK");
        }
        public void DeleteAutostart(Message mess)
        {
            query = "DELETE FROM autostart WHERE `service`= @service AND status = 'start' AND TYPE = 'one'";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            database.OpenConnection();
            command.ExecuteNonQuery();
            database.CloseConnection();
        }
        public void DeleteBug(Message mess)
        {
            List<string> tests = new List<string>();
            query = "SELECT * FROM packs WHERE `service` = @service";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();
            if (SelectResult.HasRows)
            {
                while (SelectResult.Read())
                {
                    Tests test = JsonConvert.DeserializeObject<Tests>(SelectResult["tests"].ToString());
                    if (test.id.Contains(mess.args[1]))
                    {
                        for (int i = 0; i < test.id.Count; i++)
                        {
                            Message message = JsonConvert.DeserializeObject<Message>(test.dependon[i]);
                            if (message.args.Contains(mess.args[1]))
                            {
                                if (!tests.Contains(test.id[i]))
                                    tests.Add(test.id[i]);
                            }
                            if (test.duplicate.Equals(mess.args[1]))
                            {
                                if (!tests.Contains(test.id[i]))
                                    tests.Add(test.id[i]);
                            }
                        }
                    }
                }
            }
            SelectResult.Close();
            database.CloseConnection();
            tests.Add(mess.args[1]);
            for (int i = 0; i < tests.Count; i++)
            {
                query = "DELETE FROM jira WHERE `test`= @test AND link = @link";
                command = new SQLiteCommand(query, database.connect);
                command.Parameters.AddWithValue("@test", tests[i]);
                command.Parameters.AddWithValue("@link", mess.args[2]);
                database.OpenConnection();
                command.ExecuteNonQuery();
                database.CloseConnection();
            }
        }
        //-------------------------------------------------------------------------------------       
        // ФУНКЦИИ ОБНОВЛЕНИЯ
        //-------------------------------------------------------------------------------------
        public void UpdateTest(Message mess)
        {
            Comments comments = readTextOfTest(mess.args[0], mess.args[1]);
            string name = comments.comment[0];
            comments.comment.RemoveAt(0);
            string steps = comments.comment[0];
            comments.comment.RemoveAt(0);
            comments.step.RemoveAt(0);
            comments.step.RemoveAt(0);
            string commS = JsonConvert.SerializeObject(comments);
            query = "UPDATE tests SET `name` = @name," +
                "`author` = @author, `comments` = @comments, `statistic` = @statistic WHERE `id` = @id";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@id", mess.args[1]);
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@author", mess.args[2]);
            command.Parameters.AddWithValue("@comments", commS);
            command.Parameters.AddWithValue("@statistic", mess.args[3]);

            database.OpenConnection();
            var UpdateTest = command.ExecuteNonQuery();
            database.CloseConnection();
            logger.WriteLog("{0} update test", UpdateTest.ToString());

            query = "SELECT * FROM kp WHERE `service` = @service";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();
            Message tests = new Message();
            Message addTests = new Message();
            int step = 0;
            int addStep  = 0;
            string id = "";
            int i = 0;
            if (SelectResult.HasRows)
            {
                while (SelectResult.Read())
                {
                    Message tmp = JsonConvert.DeserializeObject<Message>(SelectResult["test"].ToString());
                    i = tmp.args.IndexOf(mess.args[1]);
                    if (i != -1)
                    {
                        try
                        {
                            tests = tmp;
                            tests.args.RemoveAt(i);
                            step = Int32.Parse(SelectResult["steps"].ToString());
                            id = SelectResult["id"].ToString();
                        }
                        catch { }
                    }
                    if (SelectResult["id"].ToString().Equals(mess.args[4]))
                    {
                        addTests = JsonConvert.DeserializeObject<Message>(SelectResult["test"].ToString());
                        addStep = Int32.Parse(SelectResult["steps"].ToString());
                    }
                }
            }
            SelectResult.Close();
            database.CloseConnection();

            if (tests.args.Count == 0) tests.args.Add("not");
            if (addTests.args[0].Equals("not")) addTests.args.RemoveAt(0);
            addTests.Add(mess.args[1]);

            query = "UPDATE kp SET `test` = @newTest, `steps` = @steps " +
                "WHERE `id` = @id and `service` = @service";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@newTest", JsonConvert.SerializeObject(tests));
            command.Parameters.AddWithValue("@service", mess.args[0]);
            command.Parameters.AddWithValue("@steps", (step - Int32.Parse(steps)).ToString());
            database.OpenConnection();
            UpdateTest = command.ExecuteNonQuery();
            database.CloseConnection();

            query = "UPDATE kp SET `test` = @test, `steps` = @steps " +
                "WHERE `id` = @id and `service` = @service";

            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@test", JsonConvert.SerializeObject(addTests));
            command.Parameters.AddWithValue("@id", mess.args[4]);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            command.Parameters.AddWithValue("@steps", (addStep + Int32.Parse(steps)).ToString());
            database.OpenConnection();
            UpdateTest = command.ExecuteNonQuery();
            database.CloseConnection();
            res.Add("OK");
        }
        public void UpdateVersion(Message mess)
        {
            if (mess.args[1] != "no_version")
            {
                query = "SELECT * FROM stends where `service` = @service and `version` = @version";
                command = new SQLiteCommand(query, database.connect);
                command.Parameters.AddWithValue("@service", mess.args[0]);
                command.Parameters.AddWithValue("@version", mess.args[1]);
                database.OpenConnection();
                SQLiteDataReader SelectResult = command.ExecuteReader();
                Message messeage = new Message();
                if (SelectResult.HasRows)
                {
                    while (SelectResult.Read()) messeage.Add(
                        SelectResult["version"].ToString());
                }
                SelectResult.Close();
                database.CloseConnection();
                if (messeage.args.Count == 0)
                {
                    query = "UPDATE stends SET `version` = @version," +
                    "`data` = @data WHERE `service` = @service";
                    command = new SQLiteCommand(query, database.connect);
                    command.Parameters.AddWithValue("@version", mess.args[1]);
                    command.Parameters.AddWithValue("@data", mess.args[2]);
                    command.Parameters.AddWithValue("@service", mess.args[0]);

                    database.OpenConnection();
                    var UpdateTest = command.ExecuteNonQuery();
                    database.CloseConnection();
                    logger.WriteLog("{0} update version", UpdateTest.ToString());
                }
            }
        }
        public void UpdateTestChange(Message mess)
        {
            query = "SELECT * FROM tests WHERE `service` = @service AND `id` = @id";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            command.Parameters.AddWithValue("@id", mess.args[1]);
            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();
            if (SelectResult.HasRows)
            {
                while (SelectResult.Read())
                    res.Add(SelectResult["name"].ToString(), SelectResult["kp"].ToString(), SelectResult["statistic"].ToString());
            }
            else
            {
                res.Add("error");
            }
            SelectResult.Close();
            database.CloseConnection();
        }
        /// <summary>
        /// Функция получения данных по набору для его редактирования
        /// </summary>
        /// <param name="service"> сервис </param>
        /// <param name="ID"> ID набора </param>
        /// <returns></returns>
        public void UpdatePackChange(Message mess)
        {
            query = "UPDATE packs SET `name` = @newname,`time` = @time, `count_restart` = @restart, `ip` = @ip, " +
                "`tests` = @tests, `browser` = @browser ,`stend` = @stend WHERE `id` = @id_pack AND `service` = @service";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@id_pack", mess.args[1]);
            command.Parameters.AddWithValue("@newname", mess.args[2]);
            command.Parameters.AddWithValue("@time", mess.args[4]);
            command.Parameters.AddWithValue("@restart", mess.args[5]);
            command.Parameters.AddWithValue("@ip", mess.args[6]);
            command.Parameters.AddWithValue("@tests", mess.args[3]);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            command.Parameters.AddWithValue("@browser", mess.args[8]);
            command.Parameters.AddWithValue("@stend", mess.args[9]);

            database.OpenConnection();
            var UpdateTest = command.ExecuteNonQuery();
            database.CloseConnection();


            Tests te = JsonConvert.DeserializeObject<Tests>(mess.args[3]);
            for (int i = 0; i < te.id.Count; i++)
            {
                query = "UPDATE tests SET `used` = @used WHERE `id` = @id AND `service` = @service";
                command = new SQLiteCommand(query, database.connect);
                command.Parameters.AddWithValue("@id", te.id[i]);
                command.Parameters.AddWithValue("@used", "yes");
                command.Parameters.AddWithValue("@service", mess.args[0]);

                database.OpenConnection();
                UpdateTest = command.ExecuteNonQuery();
                database.CloseConnection();
            }

            Message removeTe = JsonConvert.DeserializeObject<Message>(mess.args[7]);
            for (int i = 0; i < removeTe.args.Count; i++)
            {
                query = "UPDATE tests SET `used` = @used WHERE `id` = @id AND `service` = @service";
                command = new SQLiteCommand(query, database.connect);
                command.Parameters.AddWithValue("@id", removeTe.args[i]);
                command.Parameters.AddWithValue("@used", "no");
                command.Parameters.AddWithValue("@service", mess.args[0]);

                database.OpenConnection();
                UpdateTest = command.ExecuteNonQuery();
                database.CloseConnection();
            }
            logger.WriteLog("{0} update pack", UpdateTest.ToString());
            res.Add("OK");
        }
        public void UpdateDoc(Message mess)
        {
            query = "UPDATE doc SET `pim` = @pim," +
                "`date` = @date WHERE `id` = @id_doc AND `service` = @service";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@id_doc", mess.args[3]);
            command.Parameters.AddWithValue("@pim", mess.args[1]);
            command.Parameters.AddWithValue("@date", mess.args[2]);
            command.Parameters.AddWithValue("@service", mess.args[0]);

            database.OpenConnection();
            var UpdateTest = command.ExecuteNonQuery();
            database.CloseConnection();
            logger.WriteLog("{0} update doc", UpdateTest.ToString());

            res.Add("OK");
        }
        public void UpdateKP(Message mess)
        {
            query = "UPDATE kp SET `name` = @name, " +
                "`date` = @date, `author` = @author WHERE `id` = @id_kp AND `service` = @service";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@id_kp", mess.args[1]);
            command.Parameters.AddWithValue("@name", mess.args[2]);
            command.Parameters.AddWithValue("@date", mess.args[3]);
            command.Parameters.AddWithValue("@author", mess.args[4]);
            command.Parameters.AddWithValue("@service", mess.args[0]);

            database.OpenConnection();
            var UpdateTest = command.ExecuteNonQuery();
            database.CloseConnection();
            logger.WriteLog("{0} update kp", UpdateTest.ToString());

            res.Add("OK");
        }
        public void StartTests(Message mess)
        {
            StartTests startTests = new StartTests();

            SQLiteDataReader SelectResult;
            SQLiteDataReader SelectResult1;
            Database database1 = new Database();

            Message request = new Message();
            Message dirs = new Message();
            Tests tests = new Tests();
            Tests tests1 = new Tests();
            List<string> packs = new List<string>();
            query = "SELECT * FROM packs WHERE `service` = @service AND `id` = @id_pack";
            command = new SQLiteCommand(query, database.connect);
            if(mess.args[1] == "no_pack")
            {
                mess.args.RemoveAt(1);
                for(int i = 2; i < mess.args.Count; i++)
                {
                    tests1.id.Add(mess.args[i]);
                    mess.args.RemoveAt(i);
                    i--;
                }
            }
            for (int i = 1; i < mess.args.Count; i++)
            {
                command.Parameters.AddWithValue("@service", mess.args[0]);
                command.Parameters.AddWithValue("@id_pack", mess.args[1]);
                database.OpenConnection();
                SelectResult = command.ExecuteReader();
                if (SelectResult.HasRows)
                {
                    while (SelectResult.Read())
                    {
                        if (SelectResult["status"].ToString() == "start")
                        {
                            SelectResult.Close();
                            database.CloseConnection();
                            res.Add("START");
                            return;
                        }
                    }
                }
                SelectResult.Close();
                database.CloseConnection();
            }
            if (mess.args.Count > 1)
            {
                for (int i = 1; i < mess.args.Count; i++)
                {
                    query = "SELECT * FROM packs WHERE `service` = @service AND `id` = @id_pack AND " +
                    "`status` = 'no_start'";
                    command = new SQLiteCommand(query, database.connect);
                    command.Parameters.AddWithValue("@service", mess.args[0]);
                    command.Parameters.AddWithValue("@id_pack", mess.args[i]);
                    database.OpenConnection();
                    SelectResult = command.ExecuteReader();
                   
                    if (SelectResult.HasRows)
                    {
                        while (SelectResult.Read())
                        {
                            packs.Add(SelectResult["id"].ToString());
                            tests = JsonConvert.DeserializeObject<Tests>(SelectResult["tests"].ToString());
                            for(int j = 0; j < tests1.id.Count; j++)
                            {
                                int q = tests.id.IndexOf(tests1.id[j]);
                                if (tests.id.Contains(tests1.id[j]))
                                {
                                    tests1.id[j] = tests.id[q];
                                    if(j==0)
                                        tests1.start.Add("Первый");
                                    else
                                        tests1.start.Add(tests1.id[j - 1]);
                                    tests1.restart.Add(tests.restart[q]);
                                    tests1.time.Add(tests.time[q]);
                                    tests1.dependon.Add("{\"args\":[\"not\"]}");
                                    tests1.duplicate.Add(tests.duplicate[q]);
                                    tests1.browser.Add(tests.browser[q]);
                                }
                            }
                            if (tests1.id.Count != 0)
                                tests = tests1;
                            for (int j = 0; j < tests.id.Count; j++)
                            {
                                query = "SELECT `path` FROM dirs WHERE `service` = @service AND `test` = @test";
                                command = new SQLiteCommand(query, database.connect);
                                command.Parameters.AddWithValue("@service", mess.args[0]);
                                command.Parameters.AddWithValue("@test", tests.id[j]);
                                database.OpenConnection();
                                SelectResult1 = command.ExecuteReader();
                                if (SelectResult1.HasRows)
                                {
                                    while (SelectResult1.Read())
                                    {
                                        dirs.Add(SelectResult1["path"].ToString());
                                    }
                                }
                                SelectResult1.Close();
                                //database1.CloseConnection();
                            }
                            request.Add(mess.args[0], mess.args[i], JsonConvert.SerializeObject(dirs), SelectResult["ip"].ToString(), SelectResult["time"].ToString(), JsonConvert.SerializeObject(tests),  SelectResult["browser"].ToString(), SelectResult["count_restart"].ToString(), SelectResult["stend"].ToString());
                           
                        }
                    }
                    SelectResult.Close();
                    database.CloseConnection();
                }

                packs.ForEach(id =>
                {
                    query = "UPDATE packs SET `status` = 'start' WHERE `id` = @id";
                    command = new SQLiteCommand(query, database.connect);
                    command.Parameters.AddWithValue("@id", id);
                    database.OpenConnection();
                    var UpdateTest = command.ExecuteNonQuery();
                    database.CloseConnection();
                    logger.WriteLog("Обновлены статусы наборов! Произведен запуск набора " + id);
                });
                Thread startPack = new Thread(new ParameterizedThreadStart(startTests.Init));
                startPack.Start(request);
                res.Add("OK");
               
            }
            else res.Add("ERROR");
        }
        public void UpdateStatusAutostart(Message mess)
        {
            query = "UPDATE autostart SET `status` = 'no_start' WHERE `status` = 'start' and `service`= @service";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            database.OpenConnection();
            command.ExecuteNonQuery();
            database.CloseConnection();
        }
        public void UpdateStatusPack(Message mess)
        {
            query = "UPDATE packs SET `status` = 'no_start' WHERE `id` = @id";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@id", mess.args[0]);
            database.OpenConnection();
            command.ExecuteNonQuery();
            database.CloseConnection();
        }
        public void UpdateTestOfPack(Message mess)
        {
            database.OpenConnection();
            Tests te = new Tests();
            query = "SELECT * FROM packs WHERE `id` = @id";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@id", mess.args[1]);
            SQLiteDataReader SelectResult = command.ExecuteReader();
            while (SelectResult.Read())
            {
                te = JsonConvert.DeserializeObject<Tests>(SelectResult["tests"].ToString());
                for (int j = 0; j < te.id.Count; j++)
                {
                    if (te.id[j].Equals(mess.args[2]))
                    {
                        te.start[j] = mess.args[3].Equals("last") ? te.start[j] : mess.args[3];
                        te.dependon[j] = mess.args[4].Equals("last") ? te.dependon[j] : mess.args[4];
                        te.time[j] = mess.args[5].Equals("last") ? te.time[j] : mess.args[5];
                        te.restart[j] = mess.args[6].Equals("last") ? te.restart[j] : mess.args[6];
                        te.browser[j] = mess.args[7].Equals("last") ? te.browser[j] : mess.args[7];
                    }
                }
            }
            SelectResult.Close();
            database.CloseConnection();

            string teS = JsonConvert.SerializeObject(te);
            query = "UPDATE packs SET `tests` = @tests WHERE `id` = @id AND `service` = @service";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@id", mess.args[1]);
            command.Parameters.AddWithValue("@tests", teS);
            command.Parameters.AddWithValue("@service", mess.args[0]);

            database.OpenConnection();
            var UpdateTest = command.ExecuteNonQuery();
            database.CloseConnection();
            logger.WriteLog("{0} update test", UpdateTest.ToString());

            res.Add("ok");
        }
        public void UpdateDuplicate(Message mess)
        {
            Tests tests = new Tests();
            if(mess.args[3] == "add")
            {
                query = "SELECT * FROM packs WHERE `service` = @service AND `id` = @id";
                command = new SQLiteCommand(query, database.connect);
                command.Parameters.AddWithValue("@service", mess.args[0]);
                command.Parameters.AddWithValue("@id", mess.args[1]);
                database.OpenConnection();
                SQLiteDataReader SelectResult = command.ExecuteReader();
                if (SelectResult.HasRows)
                {
                    SelectResult.Read();
                    tests = JsonConvert.DeserializeObject<Tests>(SelectResult["tests"].ToString());
                    int j = 0;
                    int q = 0;
                    for (int i = 0; i < tests.id.Count; i++)
                    {
                        if (tests.id[i].Contains(mess.args[2]))
                        {
                            j++;
                        }
                        if (tests.id[i].Equals(mess.args[2]))
                        {
                            q = i;
                        }
                    }
                    tests.start.Add(tests.id.Last());
                    tests.id.Add(mess.args[2] + "(Дубликат " + j + ")");
                    tests.restart.Add(tests.restart[q]);

                    tests.time.Add(tests.time[q]);
                    tests.browser.Add(tests.browser[q]);
                    tests.dependon.Add(tests.dependon[q]);
                    tests.duplicate.Add(tests.id[q]);

                }
                else
                {
                    res.Add("error");
                }
                SelectResult.Close();
            }
            else
            {
                query = "SELECT * FROM packs WHERE `service` = @service AND `id` = @id";
                command = new SQLiteCommand(query, database.connect);
                command.Parameters.AddWithValue("@service", mess.args[0]);
                command.Parameters.AddWithValue("@id", mess.args[1]);
                database.OpenConnection();
                SQLiteDataReader SelectResult = command.ExecuteReader();
                if (SelectResult.HasRows)
                {
                    SelectResult.Read();
                    tests = JsonConvert.DeserializeObject<Tests>(SelectResult["tests"].ToString());
                    int q = 0;
                    for (int i = 0; i < tests.id.Count; i++)
                    {
                        if (tests.id[i].Equals(mess.args[2]))
                        {
                            tests.id.RemoveAt(i);
                            tests.restart.RemoveAt(i);
                            tests.start.RemoveAt(i);
                            tests.time.RemoveAt(i);
                            tests.browser.RemoveAt(i);
                            tests.dependon.RemoveAt(i);
                            tests.duplicate.RemoveAt(i);
                            break;
                        }
                    }
                }
                else
                {
                    res.Add("error");
                }
                SelectResult.Close();
            }
            query = "UPDATE packs SET `tests` = @tests WHERE `id` = @id AND `service` = @service";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@id", mess.args[1]);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            command.Parameters.AddWithValue("@tests",JsonConvert.SerializeObject(tests));
            database.OpenConnection();
            var InsertTesult = command.ExecuteNonQuery();
            database.CloseConnection();
            logger.WriteLog(InsertTesult.ToString() + " update testsInPack");
            res.Add("OK");
            
        }
        public void UpdateAutostart(Message mess)
        {
            query = "UPDATE autostart SET `name` = @name, `days` = @days, `time` = @time, `packs` = @packs, `type` = @type WHERE `id` = @id AND `service` = @service";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@id", mess.args[1]);
            command.Parameters.AddWithValue("@service", mess.args[0]);
            command.Parameters.AddWithValue("@name", mess.args[2]);
            command.Parameters.AddWithValue("@days", mess.args[4]);
            command.Parameters.AddWithValue("@time", mess.args[6] + ":" + mess.args[7]);
            command.Parameters.AddWithValue("@packs", mess.args[5]);
            command.Parameters.AddWithValue("@type", mess.args[3]);
            database.OpenConnection();
            var InsertTesult = command.ExecuteNonQuery();
            database.CloseConnection();
            logger.WriteLog(InsertTesult.ToString() + " update auto");

            res.Add("OK");
        }
        public void ChangePositionList(Message mess)
        {
            Message ids = JsonConvert.DeserializeObject<Message>(mess.args[2]);
            database.OpenConnection();
            Tests te;
            Tests tmp = new Tests();
            query = "SELECT * FROM packs WHERE `id` = @id";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@id", mess.args[1]);
            SQLiteDataReader SelectResult = command.ExecuteReader();
            while (SelectResult.Read())
            {
                te = JsonConvert.DeserializeObject<Tests>(SelectResult["tests"].ToString());
                for (int i = 0; i < ids.args.Count; i++)
                {
                    int j = te.id.IndexOf(ids.args[i]);
                    if (i == 0)
                    {
                        tmp.start.Add("первый");
                    }
                    else
                    {
                        tmp.start.Add(ids.args[i - 1]);
                    }
                    tmp.id.Add(te.id[j]);
                    tmp.dependon.Add(te.dependon[j]);
                    tmp.time.Add(te.time[j]);
                    tmp.restart.Add(te.restart[j]);
                    tmp.browser.Add(te.browser[j]);
                    tmp.duplicate.Add(te.duplicate[j]);
                }
            }
            SelectResult.Close();
            database.CloseConnection();


            string teS = JsonConvert.SerializeObject(tmp);
            query = "UPDATE packs SET `tests` = @tests WHERE `id` = @id AND `service` = @service";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@id", mess.args[1]);
            command.Parameters.AddWithValue("@tests", teS);
            command.Parameters.AddWithValue("@service", mess.args[0]);

            database.OpenConnection();
            var UpdateTest = command.ExecuteNonQuery();
            database.CloseConnection();
            logger.WriteLog("{0} update test", UpdateTest.ToString());

            res.Add("ok");
        }



        /*возвращает список где 1 запись имя, 2 количество шагов а остальные комментарии*/
        public Comments readTextOfTest(string service, string testId)
        {
            Comments comments = new Comments();
            string path = "";
            query = "SELECT * FROM dirs WHERE `test` = @test";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@test", testId);

            database.OpenConnection();
            SQLiteDataReader SelectResult = command.ExecuteReader();
            while (SelectResult.Read()) path = SelectResult["Path"].ToString();
            SelectResult.Close();
            database.CloseConnection();

            DirectoryInfo path1 = new DirectoryInfo(path + "\\" + testId);

            foreach (var item in path1.GetDirectories())
            {
                if (item.ToString().Contains("Action") && !item.ToString().Contains("Action0"))
                {
                    path = path + "\\" + testId + "\\" + item.ToString() + "\\Script.mts";
                    break;
                }
            }

            string text = System.IO.File.ReadAllText(path);
            string[] lines = text.Split('\n');
            string nameTest = "";
            Boolean i = false;
            int index = 0;

            foreach (string elem in lines)
            {
                if (elem.Trim().StartsWith("NameTest"))
                {
                    nameTest = elem.Replace("NameTest", "");
                    nameTest = nameTest.Replace("\"", "");
                    nameTest = nameTest.Replace("'", "");
                    nameTest = nameTest.Trim();
                }
                if (elem.Contains("Services.StartTransaction"))
                {
                    if (i == true)
                    {
                        index++;
                        comments.step.Add(index.ToString());
                        comments.comment.Add("Отсутствуют комментарии к шагу");
                        
                    }
                    i = true;
                }
                if (elem.Trim().StartsWith("Comment"))
                {
                    string el;
                    el = elem.Replace("Comment", "");
                    el = el.Replace("\"", "");
                    el = el.Replace("'", "");
                    el = el.Trim();
                    if (i == true)
                    {
                        index++;
                        comments.step.Add(index.ToString());
                        comments.comment.Add(el);
                        
                    }
                    else
                    {
                        comments.step.Add("-");
                        comments.comment.Add(el);
                    }
                    i = false;
                }
            }
            if (i == true)
            {
                comments.step.Add(index.ToString());
                comments.comment.Add("Отсутствуют комментарии к шагу");
            }

            comments.step.Insert(0, "");
            comments.step.Insert(0, "");
            comments.comment.Insert(0, index.ToString());
            comments.comment.Insert(0, nameTest);
            return comments;
        }
    }
    public class Tests
    {
        public Tests()
        {
            id = new List<string>();
            start = new List<string>();
            time = new List<string>();
            dependon = new List<string>();
            restart = new List<string>();
            browser = new List<string>();
            duplicate = new List<string>();
        }
        public List<string> id { get; set; }
        public List<string> start { get; set; }
        public List<string> time { get; set; }
        public List<string> dependon { get; set; }
        public List<string> restart { get; set; }
        public List<string> browser { get; set; }
        public List<string> duplicate { get; set; }

        public void Remove(int i )
        {
            id.RemoveAt(i);
            start.RemoveAt(i);
            time.RemoveAt(i);
            dependon.RemoveAt(i);
            restart.RemoveAt(i);
            browser.RemoveAt(i);
            duplicate.RemoveAt(i);
        }
    }

    public class Message
    {
        public Message()
        {
            args = new List<string>();
        }

        public void Add(params string[] tmp)
        {
            for (int i = 0; i < tmp.Length; i++)
            {
                args.Add(tmp[i]);
            }
        }
        public List<string> args { get; set; }
    }

    public class Comments
    {
        public Comments()
        {
            step = new List<string>();
            comment = new List<string>();
        }
        public List<string> step { get; set; }
        public List<string> comment { get; set; }
    }
    public class Doc
    {
        public string date { get; set; }
        public string pim { get; set; }
        //public string id { get; set; }
    }
}