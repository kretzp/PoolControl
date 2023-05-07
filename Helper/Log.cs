using System;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace PoolControl.Helper;

public class Log
{
    private static ILogger? _logger;
    private static readonly object Padlock = new object();

    public static ILogger? Logger
    {
        get
        {
            lock (Padlock)
            {
                if (_logger != null) return _logger;
                try
                {
                    var configuration = new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json")
                        .Build();

                    _logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(configuration)
                        .CreateLogger();

                    _logger.ForContext<Log>().Information("Logger created!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                return _logger;
            }
        }
    }
}