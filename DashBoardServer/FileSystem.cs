using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace DashBoardServer
{
    public class Step
    {
        public string name { get; set; }
        public List<string> innerSteps { get; set; }
        public string time { get; set; }
    }
    public class Steps
    {
        public List<string> name { get; set; }
        public List<List<string>> innerSteps { get; set; }
    }
    public class FileSystem
    {
        private Database database = new Database();
        private SQLiteCommand command;
        private string query = "";
        private Logger logger = new Logger();

        Dictionary<string, string> elementXML = new Dictionary<string, string>();
        XmlNode attr;
        XmlDocument xDoc = new XmlDocument();

        public string TypeResultTest(string resultPath)
        {
            string result = "";
            xDoc.Load(resultPath);
            XmlElement xRoot = xDoc.DocumentElement;
            foreach (XmlNode xNode in xRoot)
            {
                foreach (XmlNode children in xNode.ChildNodes)
                {         
                    if (children.Name == "Data")
                    {
                        foreach (XmlNode dataChildren in children.ChildNodes)
                        {
                            if (dataChildren.Name == "Result")
                            {
                                result = dataChildren.InnerText;
                            }
                        }
                    }
                }
            }
            return result;
        }
        public string ResultTest(string service, string nameTest, string resultPath, string data, string version)
        {
            // в конце - статус теста
            // каждый элемент - результат выполнения теста
            // в каждом элементе содержатся шаги и время
            // они идут через один (ШАГ ВРЕМЯ ШАГ...)
            // бывают момент, когда времени 2
            // 2-ое время - это потерянное время
            List<Step> listSteps = new List<Step>();
            string duration = "";
            string result = "";
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(resultPath);
            XmlElement xRoot = xDoc.DocumentElement;
            int flag = 0;
            Step step = new Step();
            step.innerSteps = new List<string>();
            foreach (XmlNode xNode in xRoot)
            {
                foreach (XmlNode children in xNode.ChildNodes)
                {
                    if (children.Name == "ReportNode")
                    {
                        foreach (XmlNode reports in children.ChildNodes)
                        {
                            foreach (XmlNode steps in reports.ChildNodes)
                            {
                                if (steps.Name != "Data")
                                {
                                    foreach (XmlNode datas in steps.ChildNodes)
                                    {
                                        foreach (XmlNode dataCh in datas.ChildNodes)
                                        {
                                            if (dataCh.Name == "Name" && flag == 0 && dataCh.InnerText.StartsWith("Step"))
                                            {
                                                flag = 1;
                                                step = new Step();
                                                step.innerSteps = new List<string>();
                                                step.name = dataCh.InnerText;

                                            }
                                            else
                                            if (dataCh.Name == "Name" && flag == 1 && dataCh.InnerText.StartsWith("Step"))
                                            {
                                                listSteps.Add(step);
                                                flag = 0;

                                            }
                                            if (dataCh.Name == "Name" && !dataCh.InnerText.StartsWith("Step") && flag == 1)
                                            {
                                                step.innerSteps.Add(dataCh.InnerText);
                                                if (dataCh.InnerText.Contains("Stop Run"))
                                                {
                                                    flag = 0;
                                                    step.time = dataCh.InnerText;
                                                    listSteps.Add(step);
                                                }
                                            }
                                            if (dataCh.Name == "Description" && dataCh.InnerText.Contains("Total Duration:"))
                                            {
                                                string dur = dataCh.InnerText.Substring(dataCh.InnerText.LastIndexOf("Total Duration: "));
                                                step.time = dur.Split(' ')[2];
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (children.Name == "Data")
                    {
                        foreach (XmlNode dataChildren in children.ChildNodes)
                        {
                            if (dataChildren.Name == "Duration")
                            {
                                duration = dataChildren.InnerText;
                            }
                            if (dataChildren.Name == "Result")
                            {
                                result = dataChildren.InnerText;
                            }
                        }
                    }
                }
            }
            Steps steps1 = new Steps();
            steps1.innerSteps = new List<List<string>>();
            steps1.name = new List<string>();
            Message mess = new Message();
            for (int i = 0; i < listSteps.Count; i++)
            {
                steps1.name.Add(listSteps[i].name);
                steps1.innerSteps.Add(listSteps[i].innerSteps);
                mess.Add(listSteps[i].time);
            }
           
            query = "INSERT INTO statistic (`id`, `test`, `service`, `result`, `time_step`, `time_end`, `time_lose`, `steps`, `date`, `version`)" +
                "VALUES (@id, @test, @service, @result, @time_step, @time_end, @time_lose, @steps, @date, @version)";
            command = new SQLiteCommand(query, database.connect);        
            command.Parameters.AddWithValue("@id", nameTest);
            command.Parameters.AddWithValue("@test", nameTest);
            command.Parameters.AddWithValue("@service", service);
            command.Parameters.AddWithValue("@result", result);
            command.Parameters.AddWithValue("@time_step", JsonConvert.SerializeObject(mess));
            command.Parameters.AddWithValue("@time_end", duration);
            command.Parameters.AddWithValue("@time_lose", 0);
            command.Parameters.AddWithValue("@steps", JsonConvert.SerializeObject(steps1));
            command.Parameters.AddWithValue("@date", data);
            command.Parameters.AddWithValue("@version", version);
            database.OpenConnection();
            var InsertTesult = command.ExecuteNonQuery();
            database.CloseConnection();
            logger.WriteLog("Добавлена статистика для теста " + nameTest);

            return result;
        }
        public string ResultTest(string service, string nameTest, string resultPath, string data, string options, string version)
        {
            if (options == "dependen_error")
            {
                query = "INSERT INTO statistic (`id`, `test`, `service`, `result`, `time_step`, `time_end`, `time_lose`, `steps`, `date`, `version`)" +
                "VALUES (@id, @test, @service, @result, @time_step, @time_end, @time_lose, @steps, @date, @version)";
                command = new SQLiteCommand(query, database.connect);

                command.Parameters.AddWithValue("@id", nameTest);
                command.Parameters.AddWithValue("@test", nameTest);
                command.Parameters.AddWithValue("@service", service);
                command.Parameters.AddWithValue("@result", "Failed");
                command.Parameters.AddWithValue("@time_step", "DEPENDEN ERROR");
                command.Parameters.AddWithValue("@time_end", "DEPENDEN ERROR");
                command.Parameters.AddWithValue("@time_lose", "DEPENDEN ERROR");
                command.Parameters.AddWithValue("@steps", "DEPENDEN ERROR");
                command.Parameters.AddWithValue("@date", data);
                command.Parameters.AddWithValue("@version", version);
                database.OpenConnection();
                var InsertTesult = command.ExecuteNonQuery();
                database.CloseConnection();
                logger.WriteLog("Добавлена статистика для теста " + nameTest);
            }

           
            else if (options == "time_out")
            {
                query = "INSERT INTO statistic (`id`, `test`, `service`, `result`, `time_step`, `time_end`, `time_lose`, `steps`, `date`, `version`)" +
                "VALUES (@id, @test, @service, @result, @time_step, @time_end, @time_lose, @steps, @date, @version)";
                command = new SQLiteCommand(query, database.connect);
                
                command.Parameters.AddWithValue("@id", nameTest);
                command.Parameters.AddWithValue("@test", nameTest);
                command.Parameters.AddWithValue("@service", service);
                command.Parameters.AddWithValue("@result", "Failed");
                command.Parameters.AddWithValue("@time_step", "TIMEOUT");
                command.Parameters.AddWithValue("@time_end", "TIMEOUT");
                command.Parameters.AddWithValue("@time_lose", "TIMEOUT");
                command.Parameters.AddWithValue("@steps", "TIMEOUT");
                command.Parameters.AddWithValue("@date", data);
                command.Parameters.AddWithValue("@version", version);
                database.OpenConnection();
                var InsertTesult = command.ExecuteNonQuery();
                database.CloseConnection();
                logger.WriteLog("Добавлена статистика для теста " + nameTest);
            }
            return "Failed";
        }
    }
}
