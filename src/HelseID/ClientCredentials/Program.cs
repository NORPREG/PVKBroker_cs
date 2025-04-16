using System.CommandLine;
using HelseId.ClientCredentials.Client;
using HelseId.ClientCredentials.Configuration;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;

// using HelseId.Samples.PVKBroker.Encryption;

namespace HelseId.ClientCredentials
// This file is used for bootstrapping the example. Nothing of interest here.
{
    static class Program
    {
        static async Task Main(string[] args)
        {
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


            Console.WriteLine($"Access token: {accessToken}");
        }
    }
}
