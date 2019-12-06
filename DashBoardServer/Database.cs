using System.Data.SQLite;
using System.Diagnostics;
using System.IO;

namespace DashBoardServer
{
    class Database
    {
        public SQLiteConnection connect;

        public Database()
        {
            connect = new SQLiteConnection("Data Source=" + Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\DashBoardSQL.db");
        }

        public void OpenConnection()
        {
            if (connect.State != System.Data.ConnectionState.Open) connect.Open();
        }

        public void CloseConnection()
        {
            if (connect.State != System.Data.ConnectionState.Closed) connect.Close();
        }
    }
}
