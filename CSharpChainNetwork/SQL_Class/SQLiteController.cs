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
            //InsertData(tableName, "(wallet, location) VALUES('3000', '234,163))"
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = $"INSERT INTO {tableName} {query}";
            try{
                sqlite_cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                return false;
            }
            

            return true;
        }

        public bool InsertBlobData(string tableName, string query, byte [] compressed)
        {
            
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = $"INSERT INTO {tableName} {query}";
            Console.WriteLine(GetString(compressed));
            sqlite_cmd.Parameters.Add("@location", System.Data.DbType.Binary).Value = compressed;
            try
            {
                sqlite_cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception e)
            {
                return false;
            }


            return true;
        }

        public bool UpdateBlobData(string user , byte[] compressed)
        {
            //$"UPDATE users SET location = '{temp}' WHERE wallet='{user.name}'"
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = $"UPDATE brotliUsers SET location = @location WHERE wallet = '{user}'";
            //Console.WriteLine(GetString(compressed));
            sqlite_cmd.Parameters.Add("@location", System.Data.DbType.Binary).Value = compressed;
            try
            {
                sqlite_cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                return false;
            }


            return true;
            return false;
        }
        public string ReadData(string tableName, string columns, bool getAll, string where)
        {
            string path = "C:/temp/SQLite/temp.txt";
            StreamWriter writer = new StreamWriter(File.Open(path,FileMode.Create),Encoding.ASCII);
            SQLiteDataReader sqlite_datareader;
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            try
            {
                if (!getAll)
                {
                    sqlite_cmd.CommandText = $"SELECT {columns} FROM {tableName} {where}";
                }
                else
                {
                    sqlite_cmd.CommandText = $"SELECT * FROM {tableName}";
                }
                sqlite_datareader = sqlite_cmd.ExecuteReader();
                int cols = sqlite_datareader.FieldCount;

                while (sqlite_datareader.Read())
                {
                    for (int i = 0; i < cols; i++)
                    {
                        dynamic temp = sqlite_datareader.GetValue(i);
                        if (temp is Array)
                        {
                            Console.WriteLine(GetString(temp));
                            writer.WriteLine(GetString(temp));
                        }else
                        {
                            writer.WriteLine(temp.ToString());
                        }
                        
                    }
                }
            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
            

            writer.Close();
            conn.Close();
            return path;
        }

        public Tuple<string,byte[]> ReadBlobData(string tableName, string columns, bool getAll, string where)
        {
            string path = "C:/temp/SQLite/temp.txt";
            StreamWriter writer = new StreamWriter(File.OpenWrite(path), Encoding.ASCII);
            SQLiteDataReader sqlite_datareader;
            SQLiteCommand sqlite_cmd;
            byte[] arr = new byte[0];
            string wallet = "";
            sqlite_cmd = conn.CreateCommand();
            try
            {
                if (!getAll)
                {
                    sqlite_cmd.CommandText = $"SELECT {columns} FROM {tableName} {where}";
                }
                else
                {
                    sqlite_cmd.CommandText = $"SELECT * FROM {tableName}";
                }
                sqlite_datareader = sqlite_cmd.ExecuteReader();
                int cols = sqlite_datareader.FieldCount;
                
                while (sqlite_datareader.Read())
                {
                    for (int i = 0; i < cols; i++)
                    {
                        dynamic temp = sqlite_datareader.GetValue(i);
                        if (temp is Array)
                        {
                            //Console.WriteLine(GetString(temp));
                            arr = temp;
                        }
                        else
                        {
                            wallet = temp;
                            //writer.WriteLine(temp.ToString());
                        }

                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }


            writer.Close();
            conn.Close();
            return new Tuple<string, byte[]>(wallet,arr);
        }

        public string ReadDataForAppending(string columns, string tableName, string where,bool close)
        {
            SQLiteDataReader sqlite_datareader;
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            //List<string> result = new List<string>();
            string result = "";
            try
            {       
                sqlite_cmd.CommandText = $"SELECT {columns} FROM {tableName} {where}";
                
                sqlite_datareader = sqlite_cmd.ExecuteReader();
                int cols = sqlite_datareader.FieldCount;

                while (sqlite_datareader.Read())
                {
                    for (int i = 0; i < cols; i++)
                    {
                        result = (sqlite_datareader.GetValue(i).ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
            if (close)
            {
                conn.Close();
            }
            
            return result;
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

        public void CloseConnection()
        {
            this.conn.Close();
        }

        public int CustomCommand(string query)
        {
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = query;
            try
            {
                sqlite_cmd.ExecuteNonQuery();
                return 200;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 400;
            }

            return 400;
        }

        static byte[] GetBytes(string input)
        {
            return Encoding.ASCII.GetBytes(input);
        }

        static string GetString(byte[] input)
        {
            return Encoding.ASCII.GetString(input);
        }
    }
}
