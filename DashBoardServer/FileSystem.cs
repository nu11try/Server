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
    public class FileSystem
    {
        private Database database = new Database();
        private SQLiteCommand command;
        private string query = "";
        private Logger logger = new Logger();

        Dictionary<string, string> elementXML = new Dictionary<string, string>();
        XmlNode attr;
        XmlDocument xDoc = new XmlDocument();

        public string ResultTest(string service, string nameTest, string resultPath, string data)
        {
            // в конце - статус теста
            // каждый элемент - результат выполнения теста
            // в каждом элементе содержатся шаги и время
            // они идут через один (ШАГ ВРЕМЯ ШАГ...)
            // бывают момент, когда времени 2
            // 2-ое время - это потерянное время
            List<List<string>> resultTest = new List<List<string>>();
            List<string> bufResult = new List<string>();
            List<string> steps = new List<string>();
            List<string> eTime = new List<string>();
            List<string> lTime = new List<string>();

            xDoc.Load(resultPath);
            XmlElement xRoot = xDoc.DocumentElement;
            foreach (XmlNode xNode in xRoot)
            {
                foreach (XmlNode children in xNode.ChildNodes)
                {
                    if (children.Name == "NodeArgs")
                    {
                        bufResult.Add(children.Attributes.GetNamedItem("status").Value);
                        resultTest.Add(bufResult);
                        bufResult = new List<string>();
                    }
                    if (children.Name == "DIter")
                    {
                        foreach (XmlNode childrenCh in children.ChildNodes)
                        {
                            if (childrenCh.Name == "Action")
                            {
                                foreach (XmlNode buf in childrenCh.ChildNodes)
                                {
                                    if (buf.Name == "Step")
                                    {
                                        foreach (XmlNode buf1 in buf.ChildNodes)
                                        {
                                            if (buf1.Name == "Obj")
                                            {
                                                if (buf1.InnerText.IndexOf("Step") != -1)
                                                {
                                                    if (bufResult.IndexOf(buf1.InnerText) == -1) bufResult.Add(buf1.InnerText);
                                                }
                                            }
                                            if (buf1.Name == "Details")
                                            {
                                                if (buf1.InnerText.IndexOf("завершена") != -1)
                                                {
                                                    MatchCollection matches = new Regex(@"(\d,)+[0-9]+\S").Matches(buf1.InnerText);
                                                    foreach (Match mat in matches) bufResult.Add(mat.Value);
                                                }
                                            }
                                        }
                                    }
                                }
                                resultTest.Add(bufResult);
                                bufResult = new List<string>();
                            }
                        }
                    }
                }
            }

            int timeLoseNow = 1;
            for (int i = 0; i < resultTest.Count - 1; i++)
            {
                for (int j = 0; j < resultTest[i].Count; j++)
                {
                    if (resultTest[i][j].IndexOf("Step") != -1 && timeLoseNow == 1)
                    {
                        steps.Add(resultTest[i][j]);
                        timeLoseNow++;
                        Console.WriteLine(resultTest[i][j] + " ");
                    }
                    if (resultTest[i][j].IndexOf("Step") != -1 && timeLoseNow == 3)
                    {
                        steps.Add(resultTest[i][j]);
                        lTime.Add("0");
                        timeLoseNow = 2;
                        Console.WriteLine(0 + "\n");
                        Console.WriteLine(resultTest[i][j] + " ");
                    }
                    if (resultTest[i][j].IndexOf("Step") == -1 && timeLoseNow == 2)
                    {
                        eTime.Add(resultTest[i][j]);
                        timeLoseNow++;
                        Console.WriteLine(resultTest[i][j] + " ");
                    }
                    else if (resultTest[i][j].IndexOf("Step") == -1 && timeLoseNow == 3)
                    {
                        lTime.Add(resultTest[i][j]);
                        timeLoseNow = 1;
                        Console.WriteLine(resultTest[i][j] + "\n");
                    }
                }
            }
            if (steps.Count > eTime.Count)
            {
                eTime.Add("0");
                lTime.Add("0");
            }
            else if (eTime.Count > lTime.Count) lTime.Add("0");

            string bufStr = "";
            string bufStr1 = "";
            string bufStr2 = "";
            double bufInt = 0;
            query = "INSERT INTO statistic (`id`, `test`, `service`, `result`, `time_step`, `time_end`, `time_lose`, `steps`, `date`, `version`)" +
                "VALUES (@id, @test, @service, @result, @time_step, @time_end, @time_lose, @steps, @date, @version)";
            command = new SQLiteCommand(query, database.connect);
            for (int i = 0; i < steps.Count; i++) bufStr += steps[i] + "\n";
            for (int i = 0; i < eTime.Count; i++) bufStr1 += eTime[i] + "\n";
            for (int i = 0; i < eTime.Count; i++) bufInt += Double.Parse(eTime[i]);
            for (int i = 0; i < lTime.Count; i++) bufStr2 += lTime[i] + "\n";
            command.Parameters.AddWithValue("@id", nameTest);
            command.Parameters.AddWithValue("@test", nameTest);
            command.Parameters.AddWithValue("@service", service);
            command.Parameters.AddWithValue("@result", resultTest[resultTest.Count - 1][0]);
            command.Parameters.AddWithValue("@time_step", bufStr1);
            command.Parameters.AddWithValue("@time_end", bufInt.ToString());
            command.Parameters.AddWithValue("@time_lose", bufStr2);
            command.Parameters.AddWithValue("@steps", bufStr);
            command.Parameters.AddWithValue("@date", data);
            command.Parameters.AddWithValue("@version", "TEST");
            database.OpenConnection();
            var InsertTesult = command.ExecuteNonQuery();
            database.CloseConnection();
            logger.WriteLog("Добавлена статистика для теста " + nameTest);

            return resultTest[resultTest.Count - 1][0];
        }
        public string ResultTest(string service, string nameTest, string resultPath, string data, string options)
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
                command.Parameters.AddWithValue("@version", "TEST");
                database.OpenConnection();
                var InsertTesult = command.ExecuteNonQuery();
                database.CloseConnection();
                logger.WriteLog("Добавлена статистика для теста " + nameTest);
            }
            return "Failed";
        }
    }
}
