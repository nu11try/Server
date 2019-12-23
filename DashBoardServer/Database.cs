using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using MySql.Data.MySqlClient;

namespace DashBoardServer
{
    class Database
    {
        public MySqlConnection connect;
        public Database()
        {
            string server = "localhost";
            string user = "root";
            string dbName = "dashboard";
            string port = "3306";
            string password = "root";
            string connStr = "server=" + server + ";user=" + user +
                ";database=" + dbName +
                ";port=" + port +
                ";password=" + password + ";";

            connect = new MySqlConnection(connStr);            
        }

        public void OpenConnection()
        {
            if (!connect.State.Equals("Open"))
            {
                try
                {
                    connect.Open();
                }
                catch { }
            }
        }

        public void CloseConnection()
        {
            if (!connect.State.Equals("Open"))
            {
                try
                {
                    connect.Close();
                }
                catch { }
            }
        }
    }
}
