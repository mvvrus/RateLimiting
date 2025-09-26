using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace MVVRus.AspNetCore.RateLimiting.Tests
{
    public class DelegatedRateLimiterPolicyTest
    {
        [Fact]
        public async Task Test1()
        {
            IHostBuilder host_builder = new HostBuilder()
                .ConfigureWebHost(webHost => {
                    webHost.Configure(_ => { });
                    webHost.UseTestServer(); 
                } );
            await host_builder.StartAsync();
            //IHost host = host_builder.Build();
            //await host.StartAsync();
        }
    }
}