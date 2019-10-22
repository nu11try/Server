using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data.SQLite;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;
using Newtonsoft.Json;

namespace DashBoardServer
{
    public class StartTests
    {
        private Database database = new Database();
        private SQLiteCommand command;
        private Logger logger = new Logger();
        private string query = "";
        private string pathStend = "";
        private FileSystem fs = new FileSystem();
        private int TimePack = 0;
        int TimeOut = 0;
        Process StartTest = new Process();
        TimerCallback tm;
        Timer timer;
        Dictionary<string, string> dependonTests = new Dictionary<string, string>();
        Dictionary<string, string> resultTests = new Dictionary<string, string>();
        bool restartTime = false;

        static public void ReplaceInFile(string filePath, string searchText, string replaceText)
        {
            StreamReader reader = new StreamReader(filePath);
            string content = reader.ReadToEnd();
            reader.Close();
            content = Regex.Replace(content, searchText, "\"" + replaceText + "\"");
            StreamWriter writer = new StreamWriter(filePath);
            writer.Write(content);
            writer.Close();
        }

        public void Start(object RESPONSE)
        {
            string IPPC = "";
            string NAMEPACK = "";
            string service = "";
            List<string> NAMETESTS = new List<string>();
            List<string> dirsRes = new List<string>();            
            List<string> dirs = new List<string>();
            List<string> testsPath = new List<string>();
            List<string> resultPath = new List<string>();

            Message response = new Message();
            Tests tests = new Tests();

            // очередь файлов
            Queue<string> files = new Queue<string>();
            Queue<string> packs = new Queue<string>();

            // в этом примере всего 1 пак и он состоит из
            /*
             	[0]	"ai"
		        [1]	"pack1±local - 127.0.0.1±time"
		        [2]	"DEG_AI_0503129±Первым±not±DEG_AI\\\\Tests"
		        [3]	"DEG_AI_0503737±DEG_AI_0503129±DEG_AI_0503387±±DEG_AI\\\\Tests"
		        [4]	"DEG_AI_0503387±DEG_AI_0503737±not±DEG_AI\\\\Tests"
            */
            response = (Message)RESPONSE;         
            if (response.args.Count > 0)
            {
                service = response.args[0];
                tests = JsonConvert.DeserializeObject<Tests>(response.args[4]);
                for (int i = 0; i < response.args.Count; i++)
                {
                    NAMEPACK = response.args[0];
                    IPPC = response.args[2].Split(' ')[2];
                    TimePack = Int32.Parse(response.args[3]);

                    for (int k = 0; k < tests.id.Count; k++)
                    {
                        NAMETESTS.Add(tests.id[0]);
                        // заполнение словаря зависимостей
                        // название теста - это ключ
                        // а его значение - это его зависимость
                        dirsRes.Add(response.args[1]);
                        dependonTests.Add(tests.id[k], tests.dependon[k]);
                        
                    }
                    for (int j = 0, k = 2; j < NAMETESTS.Count; j++, k++)
                    {                      
                        try
                        {
                            File.Copy(AppDomain.CurrentDomain.BaseDirectory + "/startTests.vbs",
                                AppDomain.CurrentDomain.BaseDirectory + "test/startTests_"
                                + service + "_$$_" + NAMEPACK + "_$$_" + NAMETESTS[j] + ".vbs", true);

                            dirs.Add(AppDomain.CurrentDomain.BaseDirectory + "test/startTests_"
                                + service + "_$$_" + NAMEPACK + "_$$_" + NAMETESTS[j] + ".vbs");

                            query = "SELECT * FROM address_stend WHERE `Service` = @service";
                            command = new SQLiteCommand(query, database.connect);
                            command.Parameters.AddWithValue("@service", service);
                            database.OpenConnection();
                            SQLiteDataReader SelectResult = command.ExecuteReader();
                            if (SelectResult.HasRows)
                            {
                                while (SelectResult.Read()) pathStend = SelectResult["Path"].ToString();
                            }
                            SelectResult.Close();
                            database.CloseConnection();

                            ReplaceInFile(AppDomain.CurrentDomain.BaseDirectory + "test/startTests_"
                                + service + "_$$_" + NAMEPACK + "_$$_" + NAMETESTS[j] + ".vbs",
                                "AddressHost", pathStend);

                            using (FileStream fstream = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "test/startTests_"
                                + service + "_$$_" + NAMEPACK + "_$$_" + NAMETESTS[j] + ".vbs", FileMode.Append))
                            {
                                testsPath.Add("\\" + "\\172.31.197.220\\ATST\\" + response.args[0]
                                        + "\\" + NAMETESTS[j] + "\\Res1\\" + "\"");
                                // преобразуем строку в байты
                                    
                                byte[] array = System.Text.Encoding.Default.GetBytes("Call test_start(\"" + "\\" + "\\172.31.197.220\\ATST\\" + response.args[0]
                                        + "\\" + NAMETESTS[j] + "\", \"" + "\\" + "\\172.31.197.220\\ATST\\" + response.args[0]
                                        + "\\" + NAMETESTS[j] + "\\Res1\\" + "\")");
                                // запись массива байтов в файл
                                fstream.Write(array, 0, array.Length);

                                // добавляем файл в очередь
                                files.Enqueue(AppDomain.CurrentDomain.BaseDirectory + "test/startTests_"
                                    + service + "_$$_" + NAMEPACK + "_$$_" + NAMETESTS[j] + ".vbs");
                                resultPath.Add("Z:\\DEG_AI\\Tests\\" + NAMETESTS[j] + "\\Res1\\Report\\Results.xml");
                            }
                            packs.Enqueue(NAMEPACK);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            logger.WriteLog("[ЗАПУСК ТЕСТОВ] " + ex.Message, "ERROR");
                        }
                    }
                }
                int countTests = files.Count;
                string data = DateTime.Now.ToString("dd MMMM yyyy | HH:mm:ss");
                for (int i = 0; i < countTests; i++)
                {
                    try
                    {
                        string bufName = files.Dequeue();
                        CloseProc();
                        
                        foreach (var el in dependonTests)
                        {
                            Console.WriteLine(el.Key + " = " + el.Value);
                        }
                        Console.WriteLine("Какая зависимость для теста " + NAMETESTS[i] + " = " + dependonTests[NAMETESTS[i]]);
                        if (dependonTests[NAMETESTS[i]] == "not")
                        {
                            StartTest.StartInfo.FileName = bufName;
                            StartTest.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                            StartTest.StartInfo.UseShellExecute = true;
                            StartTest.StartInfo.LoadUserProfile = true;
                            StartTest.Start();

                            Console.WriteLine("Ждем поток");
                            tm = new TimerCallback(CloseUFT);
                            timer = new Timer(tm, TimeOut, 1000, 1000);

                            StartTest.WaitForExit();
                            
                            resultTests.Add(NAMETESTS[i], fs.ResultTest(service, NAMETESTS[i], resultPath[i], data));
                        }
                        else
                        {
                            try
                            {                                
                                if (resultTests[dependonTests[NAMETESTS[i]]] == "Failed")
                                {
                                    resultTests.Add(NAMETESTS[i], fs.ResultTest(service, NAMETESTS[i], resultPath[i], data, "dependen_error"));                                                                                                                                                
                                    continue;
                                }
                                else
                                {
                                    StartTest.StartInfo.FileName = bufName;
                                    StartTest.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                    StartTest.StartInfo.UseShellExecute = true;
                                    StartTest.StartInfo.LoadUserProfile = true;
                                    StartTest.Start();

                                    Console.WriteLine("Ждем поток");
                                    tm = new TimerCallback(CloseUFT);
                                    timer = new Timer(tm, TimeOut, 1000, 1000);

                                    StartTest.WaitForExit();
                                    
                                    resultTests.Add(NAMETESTS[i], fs.ResultTest(service, NAMETESTS[i], resultPath[i], data));
                                }
                            }
                            catch (Exception ex)
                            { Console.WriteLine(ex.Message); }
                        }
                        DeleteResDirectories(NAMETESTS[i], dirsRes[i]);
                        Console.WriteLine("Тест " + bufName + " выполнен!");
                        logger.WriteLog("[ЗАПУСК ТЕСТОВ] " + bufName, "START");
                        try { TimeOut = 0; } catch { }
                        restartTime = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    if (files.Count == 0)
                    {
                        CloseProc();
                        TimePack = 0;

                        foreach (var count in response.args)
                        {
                            string bufName = packs.Dequeue();
                            query = "UPDATE packs SET `Status` = 'no_start' WHERE `id_pack` = @id";
                            command = new SQLiteCommand(query, database.connect);
                            command.Parameters.AddWithValue("@id", bufName);
                            database.OpenConnection();
                            var UpdateTest = command.ExecuteNonQuery();
                            database.CloseConnection();
                            logger.WriteLog("[СТАТУС НАБОРА ОБНОВЛЕН] " + bufName, "START");
                            Console.WriteLine("Статус набора " + bufName + " обновлен!");
                        }
                    }
                }
            }
        }
        public void CloseUFT(object timeout)
        {
            Console.WriteLine("Секунд прошло = " + TimeOut);
            if (TimeOut >= TimePack && !restartTime)
            {
                Console.WriteLine("Таймаут");
                CloseProc();
                try { timer.Dispose(); } catch { }
                try { StartTest.Kill(); } catch { }
                try { StartTest.Close(); } catch { }
                try { TimeOut = 0; } catch { }
            }
            else if (restartTime)
            {
                try { TimeOut = 0; } catch { }
                restartTime = false;
            }
            else TimeOut += 1;
        }
        public void CloseProc()
        {
            try { foreach (Process proc in Process.GetProcessesByName("iexplore")) proc.Kill(); }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            try { foreach (Process proc in Process.GetProcessesByName("Mediator64")) proc.Kill(); }
            catch (Exception ex) { Console.WriteLine(ex.Message); }            

            try { foreach (Process proc in Process.GetProcessesByName("UFT")) proc.Kill(); }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            try { foreach (Process proc in Process.GetProcessesByName("QtpAutomationAgent")) proc.Kill(); }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            try { foreach (Process proc in Process.GetProcessesByName("wscript")) proc.Kill(); }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
        public void DeleteResDirectories(String nameTest, String dir)
        {
            String[] tmp = dir.Split('\\');
            String dirs = "Z:\\" + tmp[0] + "\\" + tmp[1] + "\\" + nameTest;
            Console.WriteLine(dirs);
            string[] ress = Directory.GetDirectories(dirs);
            foreach(string res in ress)
            {
                tmp = res.Split('\\');
                if (tmp[tmp.Length - 1].StartsWith("Res"))
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(res);
                    dirInfo.Delete(true);
                    Console.WriteLine(res);
                }
            }
        }
    }
}
