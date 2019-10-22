using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashBoardServer
{
    class Autostart
    {
        private Database database = new Database();
        private SQLiteCommand command;
        private SQLiteDataReader SelectResult;
        private Logger logger = new Logger();
        private string query = "";


        private List<string> pack = new List<string>();
        private List<string> time = new List<string>();
        private List<string> days = new List<string>();
        private List<string> service = new List<string>();
        private List<string> type = new List<string>();
        private List<string> status = new List<string>();

        private MethodsDB methodsDB = new MethodsDB();

        private int packNow = 0;
        private int countAutostartAll = 0;
        private int countAutostartNow = 0;

        public void Init(object Obj)
        {
            query = "SELECT Count(id_auto) FROM autostart";
            command = new SQLiteCommand(query, database.connect);
            database.OpenConnection();
            SelectResult = command.ExecuteReader();
            if (SelectResult.HasRows)
            {
                while (SelectResult.Read()) countAutostartNow = Int32.Parse(SelectResult[0].ToString());
            }
            SelectResult.Close();
            database.CloseConnection();

            Console.WriteLine("Автотестов сейчас " + countAutostartNow);
            Console.WriteLine("Автотестов всего " + countAutostartAll);

            if (countAutostartAll != countAutostartNow)
            {
                countAutostartAll = countAutostartNow;
                packNow = 0;                

                query = "SELECT * FROM autostart";
                command = new SQLiteCommand(query, database.connect);
                database.OpenConnection();
                SelectResult = command.ExecuteReader();
                if (SelectResult.HasRows)
                {
                    while (SelectResult.Read())
                    {
                        packNow++;
                        Console.WriteLine("Packs = " + SelectResult["Packs"].ToString());
                        pack.Add(SelectResult["Packs"].ToString());
                        time.Add(SelectResult["Time"].ToString());
                        days.Add(SelectResult["Days"].ToString());
                        service.Add(SelectResult["Service"].ToString());
                        type.Add(SelectResult["Type"].ToString());
                        status.Add(SelectResult["Service"].ToString());
                    }
                }
                SelectResult.Close();
                database.CloseConnection();
                
                for (int i = 0; i < packNow - 1; i++)
                {
                    pack[i] = pack[i].Substring(0, pack[i].Length - 1);
                    //Console.WriteLine(methodsDB.UpdateStatusPack(service[i], pack[i]));
                }
            }
        }
    }
}
