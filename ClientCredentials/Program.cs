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

// using HelseId.Samples.PVKBroker.Encryption;


namespace HelseId.Samples.ClientCredentials
// This file is used for bootstrapping the example. Nothing of interest here.
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            // The Main method uses the System.Commandline library to parse the command line parameters:
            var useMultiTenantPattern = new Option<bool>(
                aliases: new[] { "--use-multi-tenant", "-mt" },
                description: "If set, the application will use a client set up for multi-tenancy, i.e. it makes use of an organization number that is connected to the client.",
                getDefaultValue: () => false);

            var useChildOrgNumberOption = new Option<bool>(
                aliases: new[] { "--use-child-org-number", "-uc" },
                description: "If set, the application will request an child organization (underenhet) claim for the access token.",
                getDefaultValue: () => false);

            var rootCommand = new RootCommand("A client credentials usage sample")
            {
                useChildOrgNumberOption, useMultiTenantPattern
            };

            rootCommand.SetHandler(async (useChildOrgNumberOptionValue, useMultiTenantPatternOptionValue) =>
            {
                var clientConfigurator = new ClientConfigurator();
                var client = clientConfigurator.ConfigureClient(useChildOrgNumberOptionValue, useMultiTenantPatternOptionValue);
                var repeatCall = true;
                while (repeatCall)
                {
                    repeatCall = await CallApiWithToken(client);
                }
            }, useChildOrgNumberOption, useMultiTenantPattern);

            await rootCommand.InvokeAsync(args);
        }

        private static async Task<bool> CallApiWithToken(Machine2MachineClientNoDpop client)
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
            var pem = File.ReadAllText("keys/test_pvk_private_key.pem");
            var rsa = RSA.Create();
            rsa.ImportFromPem(pem.ToCharArray());

            Console.WriteLine("RSA: " + rsa.ExportPkcs8PrivateKeyPem());

            return ShouldCallAgain();

        }

        private static bool ShouldCallAgain()
        {
            Console.WriteLine("Type 'a' to call the API again, or any other key to exit:");
            var input = Console.ReadKey();
            Console.WriteLine();
            return input.Key == ConsoleKey.A;
        }
    }
}
