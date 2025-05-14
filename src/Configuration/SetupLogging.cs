using System.Runtime.CompilerServices;
using Serilog;

namespace PvkBroker.Configuration;
public class SetupLogging
{
    public static void Initialize()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(Path.Combine("../../LogFiles", $"{DateTime.Now.Year}-{DateTime.Now.Month}", "Log.txt"),
                rollingInterval: RollingInterval.Infinite,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}")
            .CreateLogger();
    }
}