using System.CommandLine;
using System.Collections.Generic;
using MySql.Data;
using MySql.Data.MySqlClient;
using Serilog;

using PvkBroker.Configuration;
using System;
using System.ComponentModel;

namespace PvkBroker.Kodeliste
{
    public class DBConnection
    {
        private DBConnection() { }

        public string? Server { get; set; }
        public string? DatabaseName { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }

        public MySqlConnection? Connection { get; set; }
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
                string connstring = string.Format(Server, DatabaseName, UserName, Password);
                try
                {
                    Connection = new MySqlConnection(connstring);
                    Connection.Open();
                }
                catch (MySqlException ex)
                {
                    Log.Error("MySQL Connection error: {@ex}", ex);
                    return false;
                }
            }

            return true;
        }

        public DBConnection SetupConnection()
        {
            var dbCon = DBConnection.Instance();
            dbCon.Server = ConfigurationValues.KodelisteServer;
            dbCon.DatabaseName = ConfigurationValues.KodelisteDbName;
            dbCon.UserName = ConfigurationValues.KodelisteUsername;
            dbCon.Password = ConfigurationValues.KodelistePassword;

            return dbCon
        }

        public void Close() { Connection.Close(); }

    }
}