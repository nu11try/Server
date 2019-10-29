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
        FreeRAM freeRAM = new FreeRAM();
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
            List<string> resultPath = new List<string>();

            Message response = new Message();
            Message testsDirs = new Message();
            Tests tests = new Tests();

            // очередь файлов
            Queue<string> files = new Queue<string>();
            Queue<string> packs = new Queue<string>();

            response = (Message)RESPONSE;
            if (response.args.Count > 0)
            {
                service = response.args[0];
                tests = JsonConvert.DeserializeObject<Tests>(response.args[5]);
                testsDirs = JsonConvert.DeserializeObject<Message>(response.args[2]);
                for (int i = 0; i < response.args.Count; i += 7)
                {
                    NAMEPACK = response.args[i + 1];
                    IPPC = response.args[i + 3].Split(' ')[2];
                    TimePack = Int32.Parse(response.args[i + 4]);
                }
                for (int i = 0; i < tests.id.Count; i++)
                {
                    try
                    {
                        File.Copy(AppDomain.CurrentDomain.BaseDirectory + "/startTests.vbs",
                            AppDomain.CurrentDomain.BaseDirectory + "test/" + tests.id[i] + ".vbs", true);

                        query = "SELECT * FROM stends WHERE `service` = @service";
                        command = new SQLiteCommand(query, database.connect);
                        command.Parameters.AddWithValue("@service", service);
                        database.OpenConnection();
                        SQLiteDataReader SelectResult = command.ExecuteReader();
                        if (SelectResult.HasRows)
                        {
                            while (SelectResult.Read()) pathStend = SelectResult["url"].ToString();
                        }
                        SelectResult.Close();
                        database.CloseConnection();

                        ReplaceInFile(AppDomain.CurrentDomain.BaseDirectory + "test/" + tests.id[i] + ".vbs",
                            "AddressHost", pathStend);

                        using (FileStream fstream = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "test/" + tests.id[i] + ".vbs", FileMode.Append))
                        {
                            byte[] array = System.Text.Encoding.Default.GetBytes("Call test_start(\"" + "\\" + "\\172.31.197.220\\ATST\\" + testsDirs.args[i].Replace("Z:\\" + "\\", "\\").Replace("\\" + "\\", "\\")
                                    + "\\" + tests.id[i] + "\", \"" + "\\" + "\\172.31.197.220\\ATST\\" + testsDirs.args[i].Replace("Z:\\" + "\\", "\\").Replace("\\" + "\\", "\\")
                                    + "\\" + tests.id[i] + "\\Res1\\" + "\")");
                            fstream.Write(array, 0, array.Length);

                            // добавляем файл в очередь
                            files.Enqueue(AppDomain.CurrentDomain.BaseDirectory + "test/" + tests.id[i] + ".vbs");
                            resultPath.Add("Z:\\" + testsDirs.args[i].Replace("Z:\\" + "\\", "\\").Replace("\\" + "\\", "\\") + "\\" + tests.id[i] + "\\Res1\\Report\\Results.xml");
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
            string data = DateTime.Now.ToString("dd MMMM yyyy | HH:mm:ss");
            Message dependons = new Message();
            bool flagDep = true;
            string bufName = "";
            foreach (var el in files) Console.WriteLine(el);
            for (int i = 0; i < files.Count; i++)
            {
                try
                {
                    bufName = files.Dequeue();
                    CloseProc();
                    dependons = JsonConvert.DeserializeObject<Message>(tests.dependon[i]);
                    if (dependons.args[0] == "not")
                    {
                        StartScript(bufName);
                        resultTests.Add(tests.id[i], fs.ResultTest(service, tests.id[i], resultPath[i], data));
                    }
                    else
                    {
                        try
                        {
                            foreach (var resDep in dependons.args)
                            {
                                if (resultTests[resDep].Equals("Failed"))
                                {
                                    flagDep = false;
                                    break;
                                }
                            }
                            if (!flagDep)
                            {
                                resultTests.Add(tests.id[i], fs.ResultTest(service, tests.id[i], resultPath[i], data, "dependen_error"));
                                continue;
                            }
                            else
                            {
                                StartScript(bufName);
                                resultTests.Add(tests.id[i], fs.ResultTest(service, tests.id[i], resultPath[i], data));
                            }
                        }
                        catch (Exception ex)
                        { Console.WriteLine(ex.Message); }
                    }                  
                    Console.WriteLine("Тест " + bufName + " выполнен!");
                    logger.WriteLog("[ЗАПУСК ТЕСТОВ] " + bufName, "START");
                    try { TimeOut = 0; } catch { }
                    restartTime = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            Finish(response, packs, tests, resultPath);            
        }

        public void Finish(Message response, Queue<string> packs, Tests tests, List<string> resultPath)
        {
            CloseProc();
            TimePack = 0;
            string bufName = "";

            for (int i = 0; i < packs.Count; i++)
            {
                bufName = packs.Dequeue();
                query = "UPDATE packs SET `status` = 'no_start' WHERE `id` = @id";
                command = new SQLiteCommand(query, database.connect);
                command.Parameters.AddWithValue("@id", bufName);
                database.OpenConnection();
                var UpdateTest = command.ExecuteNonQuery();
                database.CloseConnection();
                logger.WriteLog("[СТАТУС НАБОРА ОБНОВЛЕН] " + bufName, "START");
                Console.WriteLine("Статус набора " + bufName + " обновлен!");
            }
            for (int i = 0; i < tests.id.Count; i++) DeleteResDirectories(tests.id[i], resultPath[i]);
            freeRAM.Free();
        }
        public void StartScript(string bufName)
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

            try { foreach (Process proc in Process.GetProcessesByName("phantomjs")) proc.Kill(); }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            try { foreach (Process proc in Process.GetProcessesByName("chrome")) proc.Kill(); }
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
            String dirs = tmp[0] + "\\" + tmp[2] + "\\" + tmp[3] + "\\" + nameTest;
            Console.WriteLine(dirs);
            string[] ress = Directory.GetDirectories(dirs);
            foreach (string res in ress)
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
