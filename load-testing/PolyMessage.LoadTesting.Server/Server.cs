using System;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PolyMessage.LoadTesting.Contracts;

namespace PolyMessage.LoadTesting.Server
{
    public static class Server
    {
        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<ServerOptions>(args)
                .WithParsed(options => Start(options));
        }

        private static void Start(ServerOptions options)
        {
            IServiceProvider serviceProvider = BuildServiceProvider(options.LogLevel);
            ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            ILogger logger = loggerFactory.CreateLogger(typeof(Server));

            ServerFactory factory = new ServerFactory(logger, options);
            PolyFormat format = factory.CreateFormat();
            PolyTransport transport = factory.CreateTransport(serviceProvider);

            PolyHost host = new PolyHost(transport, format, serviceProvider);
            host.AddContract<ILoadTestingContract>();
            host.Start();

            logger.LogInformation("Press ENTER to exit.");
            Console.ReadLine();
            host.Dispose();
            logger.LogInformation("Bye!");
            loggerFactory.Dispose();
        }

        private static IServiceProvider BuildServiceProvider(LogLevel logLevel)
        {
            IServiceCollection services = new ServiceCollection()
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.SetMinimumLevel(logLevel);
                    loggingBuilder.AddDebug();
                    loggingBuilder.AddConsole();
                });
            services.AddScoped<ILoadTestingContract, LoadTestingImplementor>();

            return services.BuildServiceProvider();
        }
    }
}
