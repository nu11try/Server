using System.Data.SQLite;

namespace DashBoardServer
{
    class Database
    {
        public SQLiteConnection connect;

        public Database()
        {
            connect = new SQLiteConnection("Data Source=DashBoardSQL.db");
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
