using System.CommandLine;
using System.Collections.Generic;
using MySql.Data;
using MySql.Data.MySqlClient;
using PVKBroker;

namespace PVKBroker.Kodeliste
{
    public class DBConnection
    {
        private DBConnection() { }

        public string Server { get; set; }
        public string DatabaseName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public MySqlConnection Connection { get; set; }
        public static DBConnection _instance = null;
        public static DBConnection Instance()

        {
            if (_instance == null) { _instance = new DBConnection(); }
            return _instance;
        }

        public bool IsConnected()
        {
            if (Connection == null)
            {
                if (string.IsNullOrEmpty(DatabaseName)) { return false; }
                string connstring = string.Format(
                    "Server={0}; database={1}; UID={2}; password={3}",
                    Server, DatabaseName, UserName, Password);
                Connection = new MySqlConnection(connstring);
                Connection.Open();
            }

            return true;
        }

        public void Close() { Connection.Close(); }

    }

    public class Kodeliste
    {
        private Kodeliste() { }

        static public List<string> GetPatients()
        {
            var dbCon = DBConnection.Instance();

            // Temporary MySQL server, dummy values & contents
            // Move to configuration toml file
            dbCon.Server = "localhost";
            dbCon.DatabaseName = "kodeliste";
            dbCon.UserName = "cs";
            dbCon.Password = "InitializeComponent547";

            List<string> pasienter = new List<string>();

            if (dbCon.IsConnected())
            {
                // Convert this to ORM model ... somehow
                // maybe https://github.com/jonwagner/Insight.Database?
                string query = "SELECT name FROM Patient";
                var cmd = new MySqlCommand(query, dbCon.Connection);
                var reader = cmd.ExecuteReader();
                int count = reader.FieldCount;
                while (reader.Read())
                {
                    for (int i = 0; i < count; i++)
                    {
                        string encryptedName = reader.GetString(i);
                        string decryptedName = Encryption.Encryption.DecryptWithHashKey(encryptedName, "DICOMBroker");
                        pasienter.Add(decryptedName);
                    }
                }
            }
            return pasienter;
        }

        static public void AddPatient(string name)
        {
            var dbCon = DBConnection.Instance();
            dbCon.Server = "localhost";
            dbCon.DatabaseName = "kodeliste";
            dbCon.UserName = "cs";
            dbCon.Password = "InitializeComponent547";

            if (dbCon.IsConnected())
            {
                string query = "INSERT INTO Patient (name) VALUES (@name)";
                MySqlCommand cmd = new MySqlCommand(query, dbCon.Connection);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
