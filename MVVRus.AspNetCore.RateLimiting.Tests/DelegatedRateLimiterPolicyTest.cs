using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.RateLimiting;
using MVVRus.Threading.AsyncSynchroPrimitives;

namespace MVVRus.AspNetCore.RateLimiting.Tests
{
    public class DelegatedRateLimiterPolicyTest
    {
        [Fact(Timeout = 0)]
        public async Task Test1()
        {
            int i;
            Int32 num_requests = 6, limit = 5;
            ThisTestAppConfig tac = new ThisTestAppConfig(num_requests);
            IHostBuilder host_builder = new HostBuilder().ConfigureWebHost(tac.ConfigureTestApp);
            IHost host = await host_builder.StartAsync();
            TestServer server = host.GetTestServer();
            server.BaseAddress = new Uri("http://localhost/");
            HttpContext context;
            Task<HttpContext>[] sendResults = new Task<HttpContext>[num_requests];
            for(i = 0; i < num_requests; i++) {
                sendResults[i] = server.SendAsync(ctx=>ctx.Request.Path="/ping", default);
            }
            await tac.QuorumEvent.WaitAsync();
            tac.Continue();

            for(i=0; i<num_requests; i++) {
                context = await sendResults[i];
                if (i<limit) Assert.InRange(context.Response.StatusCode, 200, 299);
                else Assert.NotInRange(context.Response.StatusCode, 200, 299);
            }

        }



        class ThisTestAppConfig : TestAppConfig
        {
            AsyncManualResetEvent _mre;
            public AsyncCountdownEvent QuorumEvent { get; }

            public ThisTestAppConfig(Int32 quorumCount)
            {
                _mre=new AsyncManualResetEvent();
                QuorumEvent = new AsyncCountdownEvent(quorumCount);
            }

            public void Continue()
            {
                _mre.Set();
            }

            protected override void Configure(IApplicationBuilder app)
            {
                app.Use(next => async context=>{ QuorumEvent.Signal(); await next(context); });
                app.UseRateLimiter();
                base.Configure(app);
            }

            protected override void ConfigureRest(IApplicationBuilder app)
            {
                app.Run(async ctx => { await _mre.WaitAsync(); ctx.Response.StatusCode=StatusCodes.Status204NoContent; });
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
                        (HttpContext ctx)=>RateLimitPartition.GetConcurrencyLimiter(
                            0, _ => new ConcurrencyLimiterOptions() { PermitLimit=5}
                        )
                    );
                });
                base.ConfigureServices(services);
            }
        }
    }
}