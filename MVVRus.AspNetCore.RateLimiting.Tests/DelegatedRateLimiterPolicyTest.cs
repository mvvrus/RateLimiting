using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.RateLimiting;

namespace MVVRus.AspNetCore.RateLimiting.Tests
{
    public class DelegatedRateLimiterPolicyTest
    {
        [Fact]
        public async Task Test1()
        {
            ThisTestAppConfig tac = new ThisTestAppConfig();
            IHostBuilder host_builder = new HostBuilder().ConfigureWebHost(tac.ConfigureTestApp);
            IHost host = await host_builder.StartAsync();
            TestServer server = host.GetTestServer();
            server.BaseAddress = new Uri("http://localhost/");
            HttpContext context;
            for(int i = 0; i < 6; i++) {
                context = await server.SendAsync(ctx=>ctx.Request.Path="/ping", default);
                Assert.InRange(context.Response.StatusCode, 200, 299);
            }
        }



        class ThisTestAppConfig : TestAppConfig
        {
            protected override void Configure(IApplicationBuilder app)
            {
                app.UseRateLimiter();
                base.Configure(app);
            }

            protected override void ConfigureRest(IApplicationBuilder app)
            {
                app.Run(async ctx => { await Task.Yield(); ctx.Response.StatusCode=StatusCodes.Status204NoContent; });
                base.ConfigureRest(app);
            }

            protected override void ConfigureRouting(IEndpointRouteBuilder builder)
            {
                base.ConfigureRouting(builder);
            }

            protected override void ConfigureServices(IServiceCollection services)
            {
                services.AddRateLimiter(options => {
                    options.GlobalLimiter = PartitionedRateLimiter.Create(
                        (HttpContext ctx)=>RateLimitPartition.GetFixedWindowLimiter(
                            0, _ => new FixedWindowRateLimiterOptions() { PermitLimit=5, Window=TimeSpan.FromSeconds(10)}
                        )
                    );
                });
                base.ConfigureServices(services);
            }
        }
    }
}