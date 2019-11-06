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
    public class PackStart
    {

        public PackStart()
        {
            TestsInPack = new Tests();
            ResultTest = new Dictionary<string, string>();
            ResultFolders = new List<string>();
            FilesToStart = new List<string>();
        }

        public string Name = "";
        public string Service = "";
        public string IP = "";
        public string Restart = "";
        public string Browser = "";
        public string Time = "";
        public string Stend = "";
        public string PathToTests = "";

        public Tests TestsInPack;
        public Dictionary<string, string> ResultTest;
        public List<string> ResultFolders;
        public List<string> FilesToStart;
    }

    public class StartTests
    {
        private Database database = new Database();
        private FreeRAM freeRAM = new FreeRAM();
        private SQLiteCommand command;
        private string query = "";

        private Logger logger = new Logger();
        private FileSystem fs = new FileSystem();

        Process StartTest = new Process();
        TimerCallback tm;
        Timer timer;

        private Message Response = new Message();
        private PackStart pack = new PackStart();

        private bool FlagStarted = true;
        private int SeconsdEnd;

        public void Init(object RESPONSE)
        {
            string data = DateTime.Now.ToString("dd MMMM yyyy | HH:mm:ss");

            Response = (Message)RESPONSE;

            if (Response.args.Count > 0)
            {
                for (int i = 0; i < Response.args.Count; i += 9)
                {
                    pack.Name = Response.args[i + 1];
                    pack.Service = Response.args[i];
                    pack.Browser = Response.args[i + 6];
                    pack.Restart = Response.args[i + 7];
                    pack.Stend = Response.args[i + 8];
                    pack.Time = Response.args[i + 4];
                    pack.IP = Response.args[i + 3].Split(' ')[2];
                    pack.PathToTests = JsonConvert.DeserializeObject<Message>(Response.args[i + 2]).args[0];
                    pack.TestsInPack = JsonConvert.DeserializeObject<Tests>(Response.args[i + 5]);

                    ConfigStartTest();
                }
                for (int i = 0; i < pack.TestsInPack.id.Count; i++)
                {
                    if (pack.TestsInPack.restart[i].Equals("default"))
                        pack.TestsInPack.restart[i] = pack.Restart;
                    if (pack.TestsInPack.time[i].Equals("default"))
                        pack.TestsInPack.time[i] = pack.Time;
                }
            }
            else return;

            for (int i = 0; i < pack.ResultFolders.Count(); i++)
            {
                try
                {
                    string bufDependons = JsonConvert.DeserializeObject<Message>(pack.TestsInPack.dependon[i]).args[0];
                    if (bufDependons.Equals("not"))
                    {
                        StartScript(pack.FilesToStart[i]);
                        if (fs.TypeResultTest(pack.ResultFolders[i]).Equals("Passed"))
                            pack.ResultTest.Add(pack.TestsInPack.id[i], fs.ResultTest(pack.Service, pack.TestsInPack.id[i], pack.ResultFolders[i], data));
                        else if (fs.TypeResultTest(pack.ResultFolders[i]).Equals("Failed"))
                        {
                            while (Int32.Parse(pack.TestsInPack.restart[i]) > 0)
                            {
                                StartScript(pack.FilesToStart[i]);
                                pack.TestsInPack.restart[i] = (Int32.Parse(pack.TestsInPack.restart[i]) - 1).ToString();
                                Console.WriteLine("1");
                                Console.WriteLine(pack.ResultFolders[i]);
                                Console.WriteLine(fs.TypeResultTest(pack.ResultFolders[i]));
                                if (!fs.TypeResultTest(pack.ResultFolders[i]).Equals("Failed"))
                                {
                                    Console.WriteLine(fs.TypeResultTest(pack.ResultFolders[i]));
                                    break;
                                }

                            }
                            pack.ResultTest.Add(pack.TestsInPack.id[i], fs.ResultTest(pack.Service, pack.TestsInPack.id[i], pack.ResultFolders[i], data));
                        }
                    }
                    else
                    {
                        try
                        {
                            if (pack.ResultTest[bufDependons].Equals("Failed"))
                                pack.ResultTest.Add(pack.TestsInPack.id[i], fs.ResultTest(pack.Service, pack.TestsInPack.id[i], pack.ResultFolders[i], data, "dependon_error"));
                            else
                            {
                                StartScript(pack.FilesToStart[i]);
                                if (fs.TypeResultTest(pack.ResultFolders[i]).Equals("Passed"))
                                    pack.ResultTest.Add(pack.TestsInPack.id[i], fs.ResultTest(pack.Service, pack.TestsInPack.id[i], pack.ResultFolders[i], data));
                                else if (fs.TypeResultTest(pack.ResultFolders[i]).Equals("Failed"))
                                {
                                    while (Int32.Parse(pack.TestsInPack.restart[i]) > 0)
                                    {
                                        StartScript(pack.FilesToStart[i]);
                                        pack.TestsInPack.restart[i] = (Int32.Parse(pack.TestsInPack.restart[i]) - 1).ToString();

                                        if (!fs.TypeResultTest(pack.ResultFolders[i]).Equals("Failed")) break;

                                    }
                                    pack.ResultTest.Add(pack.TestsInPack.id[i], fs.ResultTest(pack.Service, pack.TestsInPack.id[i], pack.ResultFolders[i], data));
                                }
                            }
                        }
                        catch (Exception ex)
                        { Console.WriteLine(ex.Message); }
                    }
                    Console.WriteLine("Тест " + pack.FilesToStart[i] + " выполнен!");
                    logger.WriteLog("[ЗАПУСК ТЕСТОВ] " + pack.FilesToStart[i], "START");
                    FlagStarted = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            Finish();
        }
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
        public void ConfigStartTest()
        {
            for (int i = 0; i < pack.TestsInPack.id.Count; i++)
            {
                try
                {
                    File.Copy(AppDomain.CurrentDomain.BaseDirectory + "/startTests.vbs",
                        AppDomain.CurrentDomain.BaseDirectory + "test/" + pack.TestsInPack.id[i] + ".vbs", true);

                    ReplaceInFile(AppDomain.CurrentDomain.BaseDirectory + "test/" + pack.TestsInPack.id[i] + ".vbs",
                        "AddressHost", pack.Stend);

                    if (pack.TestsInPack.browser[i].Equals("default"))
                        ReplaceInFile(AppDomain.CurrentDomain.BaseDirectory + "test/" + pack.TestsInPack.id[i] + ".vbs",
                            "BrowserName", pack.Browser);
                    else ReplaceInFile(AppDomain.CurrentDomain.BaseDirectory + "test/" + pack.TestsInPack.id[i] + ".vbs",
                            "BrowserName", pack.TestsInPack.browser[i]);

                    using (FileStream fstream = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "test/" + pack.TestsInPack.id[i] + ".vbs", FileMode.Append))
                    {
                        byte[] array = System.Text.Encoding.Default.GetBytes("Call test_start(\"" + "\\" + "\\172.31.197.220\\ATST\\" + pack.PathToTests.Replace("Z:\\" + "\\", "\\").Replace("\\" + "\\", "\\")
                                + "\\" + pack.TestsInPack.id[i] + "\", \"" + "\\" + "\\172.31.197.220\\ATST\\" + pack.PathToTests.Replace("Z:\\" + "\\", "\\").Replace("\\" + "\\", "\\")
                                + "\\" + pack.TestsInPack.id[i] + "\\Res1\\" + "\")");
                        fstream.Write(array, 0, array.Length);

                        pack.FilesToStart.Add(AppDomain.CurrentDomain.BaseDirectory + "test/" + pack.TestsInPack.id[i] + ".vbs");
                        pack.ResultFolders.Add("Z:\\" + pack.PathToTests.Replace("Z:\\" + "\\", "\\").Replace("\\" + "\\", "\\") + "\\" + pack.TestsInPack.id[i] + "\\Res1\\Report\\Results.xml");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    logger.WriteLog("[ЗАПУСК ТЕСТОВ] " + ex.Message, "ERROR");

                }
            }
        }
        public void Finish()
        {
            CloseProc();
            CloseUFT();

            query = "UPDATE packs SET `status` = 'no_start' WHERE `id` = @id";
            command = new SQLiteCommand(query, database.connect);
            command.Parameters.AddWithValue("@id", pack.Name);
            database.OpenConnection();
            command.ExecuteNonQuery();
            database.CloseConnection();
            logger.WriteLog("[СТАТУС НАБОРА ОБНОВЛЕН] " + pack.Name, "START");
            Console.WriteLine("Статус набора " + pack.Name + " обновлен!");

            for (int i = 0; i < pack.TestsInPack.id.Count(); i++)
            {
                try
                {
                    DeleteResDirectories(pack.TestsInPack.id[i], pack.ResultFolders[i]);
                }
                catch
                {
                    Console.WriteLine("Не найдена папка для удаления результата");
                }
            }
            freeRAM.Free();
        }
        public void StartScript(string file)
        {
            CloseProc();
            CloseUFT();

            SeconsdEnd = 0;

            StartTest.StartInfo.FileName = file;
            StartTest.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            StartTest.StartInfo.UseShellExecute = true;
            StartTest.StartInfo.LoadUserProfile = true;
            StartTest.Start();

            Console.WriteLine("Ждем поток");
            tm = new TimerCallback(TimeOut);
            timer = new Timer(tm, file, 1000, 1000);

            StartTest.WaitForExit();
        }
        public void TimeOut(object fileStarted)
        {
            Console.WriteLine("Секунд прошло = " + SeconsdEnd);
            if (SeconsdEnd >= Int32.Parse(pack.TestsInPack.time[pack.FilesToStart.IndexOf(fileStarted.ToString())]) && FlagStarted)
            {
                CloseProc();
                try { timer.Dispose(); } catch { }
                try { StartTest.Kill(); } catch { }
                try { StartTest.Close(); } catch { }
                try { SeconsdEnd = 0; } catch { }
                FlagStarted = false;
            }
            else if (!FlagStarted)
            {
                try { SeconsdEnd = 0; } catch { }
                FlagStarted = false;
            }
            else SeconsdEnd++;
        }
        public void CloseUFT()
        {
            CloseProc();
            try { timer.Dispose(); } catch { }
            try { StartTest.Kill(); } catch { }
            try { StartTest.Close(); } catch { }
            try { SeconsdEnd = 0; } catch { }
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
