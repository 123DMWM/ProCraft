//Add MySql Library
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MySql.Data.MySqlClient;

namespace fCraft.Network
{
    class DbConnect
    {
        /*public static MySqlConnection Connection;
        public static string _server;
        public static string _database;
        public static string _uid;
        public static string _password;

        //Constructor
        public DbConnect()
        {
            Initialize();
        }

        //Initialize values
        public static void Initialize()
        {
            _server = "localhost";
            _database = "test";
            _uid = "root";
            _password = "password";
            string connectionString = "SERVER=" + _server + ";" + "DATABASE=" + _database + ";" + "UID=" + _uid + ";" + "PASSWORD=" + _password + ";";

            Connection = new MySqlConnection(connectionString);
        }


        //open connection to database
        public static bool OpenConnection()
        {
            try
            {
                Connection.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                //When handling errors, you can your application's response based on the error number.
                //The two most common error numbers when connecting are as follows:
                //0: Cannot connect to server.
                //1045: Invalid user name and/or password.
                switch (ex.Number)
                {
                    case 0:
                        Logger.Log(LogType.Error, "Cannot connect to server.  Contact administrator");
                        break;

                    case 1045:
                        Logger.Log(LogType.Error, "Invalid username/password, please try again");
                        break;
                }
                return false;
            }
        }

        //Close connection
        public static void CloseConnection()
        {
            try {
                Connection.Close();
            } catch (MySqlException ex) {
                Logger.Log(LogType.Error, ex.Message);
            }
        }

        //Insert statement
        public static void Insert() {
            const string query =
                "CREATE TABLE IF NOT EXISTS tableinfo(Id INT PRIMARY KEY AUTO_INCREMENT, name VARCHAR(25)) ENGINE=INNODB;";

            //open connection
            if (OpenConnection())
            {
                //create command and assign the query and connection from the constructor
                MySqlCommand cmd = new MySqlCommand(query, Connection);
                cmd.ExecuteNonQuery();
                foreach (PlayerInfo p in PlayerDB.PlayerInfoList) {
                    string player = String.Format("INSERT INTO tableinfo (name) VALUES('{0}')", p.Name);
                    cmd = new MySqlCommand(player, Connection);

                    //Execute command
                    cmd.ExecuteNonQuery();
                }

                //close connection
                CloseConnection();
            }
        }

        //Update statement
        public void Update() {
            const string query = "UPDATE tableinfo SET name='Joe', age='22' WHERE name='John Smith'";

            //Open connection
            if (OpenConnection())
            {
                //create mysql command
                MySqlCommand cmd = new MySqlCommand {CommandText = query, Connection = Connection};
                //Assign the query using CommandText
                //Assign the connection using Connection

                //Execute query
                cmd.ExecuteNonQuery();

                //close connection
                CloseConnection();
            }
        }

        //Delete statement
        public void Delete() {
            const string query = "DELETE FROM tableinfo WHERE name='John Smith'";

            if (OpenConnection())
            {
                MySqlCommand cmd = new MySqlCommand(query, Connection);
                cmd.ExecuteNonQuery();
                CloseConnection();
            }
        }

        //Select statement
        public List<string>[] Select()
        {
            const string query = "SELECT * FROM tableinfo";

            //Create a list to store the result
            List<string>[] list = new List<string>[3];
            list[0] = new List<string>();
            list[1] = new List<string>();
            list[2] = new List<string>();

            //Open connection
            if (OpenConnection())
            {
                //Create Command
                MySqlCommand cmd = new MySqlCommand(query, Connection);
                //Create a data reader and Execute the command
                MySqlDataReader dataReader = cmd.ExecuteReader();
                
                //Read the data and store them in the list
                while (dataReader.Read())
                {
                    list[0].Add(dataReader["id"] + "");
                    list[1].Add(dataReader["name"] + "");
                    list[2].Add(dataReader["age"] + "");
                }

                //close Data Reader
                dataReader.Close();

                //close Connection
                CloseConnection();

                //return list to be displayed
                return list;
            }
            return list;
        }

        //Count statement
        public int Count()
        {
            const string query = "SELECT Count(*) FROM tableinfo";
            int count = -1;

            //Open Connection
            if (OpenConnection())
            {
                //Create Mysql Command
                MySqlCommand cmd = new MySqlCommand(query, Connection);

                //ExecuteScalar will return one value
                count = int.Parse(cmd.ExecuteScalar()+"");
                
                //close Connection
                CloseConnection();

                return count;
            }
            return count;
        }

        //Backup
        public void Backup()
        {
            try
            {
                DateTime time = DateTime.Now;
                int year = time.Year;
                int month = time.Month;
                int day = time.Day;
                int hour = time.Hour;
                int minute = time.Minute;
                int second = time.Second;
                int millisecond = time.Millisecond;

                //Save file to C:\ with the current date as a filename
                string path = "C:\\" + year + "-" + month + "-" + day + "-" + hour + "-" + minute + "-" + second + "-" + millisecond + ".sql";
                StreamWriter file = new StreamWriter(path);

                
                ProcessStartInfo psi = new ProcessStartInfo {
                    FileName = "mysqldump",
                    RedirectStandardInput = false,
                    RedirectStandardOutput = true,
                    Arguments = string.Format(@"-u{0} -p{1} -h{2} {3}", _uid, _password, _server, _database),
                    UseShellExecute = false
                };

                Process process = Process.Start(psi);

                string output = process.StandardOutput.ReadToEnd();
                file.WriteLine(output);
                process.WaitForExit();
                file.Close();
                process.Close();
            }
            catch (IOException ex)
            {
                Logger.Log(LogType.Error, "Error , unable to backup!");
            }
        }

        //Restore
        public void Restore()
        {
            try
            {
                //Read file from C:\
                const string path = "C:\\MySqlBackup.sql";
                StreamReader file = new StreamReader(path);
                string input = file.ReadToEnd();
                file.Close();


                ProcessStartInfo psi = new ProcessStartInfo {
                    FileName = "mysql",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = false,
                    Arguments = string.Format(@"-u{0} -p{1} -h{2} {3}", _uid, _password, _server, _database),
                    UseShellExecute = false
                };

                Process process = Process.Start(psi);
                process.StandardInput.WriteLine(input);
                process.StandardInput.Close();
                process.WaitForExit();
                process.Close();
            }
            catch (IOException ex)
            {
                Logger.Log(LogType.Error, "Error , unable to Restore!");
            }
        }*/
    }
}
