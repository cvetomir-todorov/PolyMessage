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

            ILogger logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(Server));
            ServerFactory factory = new ServerFactory(logger, options);

            PolyFormat format = factory.CreateFormat();
            PolyTransport transport = factory.CreateTransport(serviceProvider);

            PolyHost host = new PolyHost(transport, format, serviceProvider);
            host.AddContract<ILoadTestingContract>();
            host.Start();

            logger.LogInformation("Press ENTER to exit.");
            Console.ReadKey();
            logger.LogInformation("Bye!");
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
