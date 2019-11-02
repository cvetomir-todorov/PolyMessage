using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace PolyMessage.IntegrationTests
{
    public class TcpTests
    {
        private readonly ITestOutputHelper _output;

        public TcpTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TcpListener()
        {
            TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 10678);
            bool started = false;
            bool cancelled = false;
            bool completed = false;

            Task serverTask = Task.Run(async () =>
            {
                try
                {
                    listener.Start();
                    while (!cancelled)
                    {
                        started = true;
                        await listener.AcceptTcpClientAsync();
                    }
                }
                catch (Exception exception)
                {
                    _output.WriteLine(exception.Message);
                }
                finally
                {
                    completed = true;
                }
            });

            for (int i = 0; i < 7; ++i)
            {
                TcpClient client = new TcpClient();
                client.Connect("127.0.0.1", 10678);
                _output.WriteLine("Connected: {0}", client.Connected);
            }

            cancelled = true;
            listener.Stop();

            await Task.Delay(TimeSpan.FromSeconds(1));

            _output.WriteLine("Started: {0}. Completed {1}. Server faulted: {2}", started, completed, serverTask.IsFaulted);
        }
    }
}
