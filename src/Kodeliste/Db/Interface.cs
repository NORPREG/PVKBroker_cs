using System.CommandLine;
using System.Collections.Generic;
using MySql.Data;
using MySql.Data.MySqlClient;
using Serilog;

using PvkBroker.Configuration;
using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;

namespace PvkBroker.Kodeliste.Db
{
    public class Interface
    {
        DbConnection dbCon;


        private Interface()
        {
            dbCon = SetupConnection();
        }

        public List<string> GetPatients()
        {
            List<string> pasienter = new List<string>();

            if ( dbCon.UserName == null || dbCon.Password == null )
            {
                Log.Error("MySQL username or password not set");
                throw new Exception("MySQL username or password not set");
                return pasienter;
            }

            if (dbCon.IsConnected())
            {
                // Convert this to ORM model
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
                        string decryptedName = Encryption.DecryptWithHashKey(encryptedName, "DICOMBroker");
                        pasienter.Add(decryptedName);
                    }
                }
            }
            return pasienter;
        }

        public void AddPatient(string name)
        {
            name_encrypted = Encryption.EncryptWithHashKey(name, "DICOMBroker");

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
