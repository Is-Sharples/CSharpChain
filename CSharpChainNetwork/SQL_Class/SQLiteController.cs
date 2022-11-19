using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;

namespace CSharpChainNetwork.SQL_Class
{
    public class SQLiteController
    {
        private SQLiteConnection conn;

        public SQLiteController(string database)
        {
            conn = new SQLiteConnection($"Data Source={database}.db;Version=3;New=True;Compress=True;");
            try
            {
                conn.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Connection failed man!");
            }
        }

        public SQLiteConnection GetConnection()
        {
            return conn;
        }

        public bool CreateTable(string tableName, string spec)
        {
            //Example of paramters
            //CreateTable("Testie","(column1 VARCHAR(20), column2 INT)");
            SQLiteCommand sqlite_cmd;
            try
            {
                sqlite_cmd = conn.CreateCommand();
                sqlite_cmd.CommandText = $"CREATE TABLE {tableName} {spec}";
                sqlite_cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            { 
                if (e.Message.Contains("already exists"))
                {
                    Console.Clear();
                    Console.WriteLine("Error: Table Already Exists for Query:");
                    Console.WriteLine($"CREATE TABLE {tableName} {spec}");
                }
                
                return false;
            }
            

            return true;
        }

        public bool InsertData(string tableName, string query)
        {
            //example of paramenters
            //InsertData(tableName, "(wallet, location) VALUES('3000', '234,163
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = $"INSERT INTO {tableName}  {query}";
            try{
                sqlite_cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                return false;
            }
            

            return true;
        }

        public bool ReadData(string tableName, string query, bool getAll)
        {
            StreamWriter writer = new StreamWriter("C:/temp/SQLite/temp.txt");
            SQLiteDataReader sqlite_datareader;
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            if (!getAll)
            {
                sqlite_cmd.CommandText = $"SELECT {query} FROM {tableName}";
            }else
            {
                sqlite_cmd.CommandText = $"SELECT * FROM {tableName}";
            }
            sqlite_datareader = sqlite_cmd.ExecuteReader();
            int columns = sqlite_datareader.FieldCount;

            while (sqlite_datareader.Read())
            {
                for (int i = 0; i < columns; i++)
                {
                    writer.WriteLine(sqlite_datareader.GetValue(i).ToString());    
                }
            }

            writer.Close();
            conn.Close();
            return true;
        }

        public bool CheckForTable(string newTable)
        {
            List<string> tables = new List<string>();
            SQLiteCommand comm = conn.CreateCommand();
            comm.CommandText = "SELECT name FROM sqlite_schema WHERE type = 'table' AND name NOT LIKE 'sqlite_%'; ";
            comm.ExecuteNonQuery();
            SQLiteDataReader sqlite_datareader = comm.ExecuteReader();
            while (sqlite_datareader.Read())
            {
                tables.Add(sqlite_datareader.GetString(0));
            }
            foreach (string table in tables)
            {
                if(table.Trim() == newTable.Trim())
                {
                    return false;
                }
            }

            return true;
        }

    }
}
