using Xunit;

namespace PolyMessage.IntegrationTests
{
    public sealed class StartStopTests
    {
        [Fact]
        public void ShouldStartAndStop()
        {
            PolyHost target = new PolyHost();
            target.Start();
            target.Stop();
        }
    }
}
