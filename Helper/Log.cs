using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace PoolControl
{
    public class Log
    {
        private static ILogger? _logger;
        private static readonly object padlock = new object();

        public static ILogger Logger
        {
            get
            {
                lock (padlock)
                {
                    if (_logger == null)
                    {
                        try
                        {
                            var configuration = new ConfigurationBuilder()
                                .AddJsonFile("appsettings.json")
                                .Build();

                            _logger = new LoggerConfiguration()
                                .ReadFrom.Configuration(configuration)
                                .CreateLogger();

                            _logger.ForContext<Log>().Information("Logger erstellt");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }

                    return _logger;
                }
            }
        }
    }
}
