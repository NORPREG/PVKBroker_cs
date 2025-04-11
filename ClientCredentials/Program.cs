using System.CommandLine;
using HelseId.Samples.ClientCredentials.Client;
using HelseId.Samples.ClientCredentials.Configuration;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.IO;
using System.Linq;
using System.Text;
using HelseId.Samples.PVKBroker.Kodeliste;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;

// using HelseId.Samples.PVKBroker.Encryption;

namespace HelseId.Samples.ClientCredentials
// This file is used for bootstrapping the example. Nothing of interest here.
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            // Change this to be invoked as method? not rootCommand here
            // but rather run a parent program as a service 

            var rootCommand = new RootCommand("A client credentials usage sample");

            rootCommand.SetHandler(async () =>
            {
                var clientConfigurator = new ClientConfigurator();
                var client = clientConfigurator.ConfigureClient();

                await CallApiWithToken(client);
            });

            await rootCommand.InvokeAsync(args);
        }

        private static async Task CallApiWithToken(Machine2MachineClient client)
        {
            await client.CallApiWithToken();

            // Store access token in DB
            string accessToken = client.GetAccessToken().ToString();

            List<string> patients = Kodeliste.GetPatients();
            foreach (string patient in patients)
            {
                Console.WriteLine(patient);
            }

            // Test load PEM file
            var pem_2 = File.ReadAllText("keys/test_pvk_private_key_encrypted.pem");
            var rsa_2 = RSA.Create();
            rsa_2.ImportFromEncryptedPem(pem_2.ToCharArray(), "test_password");
            Console.WriteLine("RSA (encrypted): " + rsa_2.ExportPkcs8PrivateKeyPem());

            var rsaKey = new RsaSecurityKey(rsa_2);
            JsonWebKey jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(rsaKey);

            string alg = "RS256"; // OK with openssl inspection
            string use = "sig"; // check this

            string GeneralPrivateRsaKey =
                $$"""
                { 
                    "p": {{jwk.P}},
                    "kty": {{jwk.Kty}},
                    "q": {{jwk.Q}},
                    "d": {{jwk.D}},
                    "e": {{jwk.E}},
                    "use": {{use}},
                    "qi": {{jwk.QI}},
                    "dp": {{jwk.DP}},
                    "alg": {{alg}},
                    "dq": {{jwk.DQ}},
                    "n": {{jwk.N}}
                }
                """;

            Console.WriteLine(GeneralPrivateRsaKey);
        }
    }
}
