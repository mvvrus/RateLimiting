using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.RateLimiting;
using MVVRus.Threading.AsyncSynchroPrimitives;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.RateLimiting;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace MVVRus.AspNetCore.RateLimiting.Tests
{
    public class RateLimiterPolicyFunctionalTests
    {
        [Fact(Timeout = 0)]
        public async Task SelectiveRateLimiterPolicyFunctionalTest()
        {
            int i;
            Int32 num_requests, limit = 5;
            Int32 num_passes = 2;
            num_requests = (limit+1)*num_passes+1;

            String path1 = "/ping", path2 = "/other";
            String policy_name = "ping_policy";

            ClaimsPrincipal user1 = new ClaimsPrincipal(new GenericIdentity("USER1"));
            ClaimsPrincipal user2 = new ClaimsPrincipal(new GenericIdentity("USER2"));

            ClaimsPrincipal GetUser(int n)
            {
                return n<limit+1? user1: user2;
            }

            String GetPath(int n)
            {
                return n<(limit+1)*num_passes ? path1 : path2;
            }

            AsyncManualResetEvent can_proceed = new AsyncManualResetEvent();
            AsyncCountdownEvent quorum_reached = new AsyncCountdownEvent(num_requests);

            TestRateLimiterAppConfig.EndpointData[] endpoints = //new TestRateLimiterAppConfig.EndpointData[]
            {
                new TestRateLimiterAppConfig.EndpointData(path1, TerminalHandler, TestRateLimiterAppConfig.EndpointData.MakeConfigurator(policy_name)),
                // Uncomment to fail:
                // new TestRateLimiterAppConfig.EndpointData(path2, TerminalHandler, TestRateLimiterAppConfig.EndpointData.MakeConfigurator(policy_name)),
            };
            HttpContext context;
            TestRateLimiterAppConfig tac = new TestRateLimiterAppConfig(
                SetLimiterOptions, PreLimiterHandler, TerminalHandler,endpoints, PreConfigure);
            IHostBuilder host_builder = new HostBuilder().ConfigureWebHost(tac.ConfigureTestApp);
            IHost host = await host_builder.StartAsync();
            TestServer server = host.GetTestServer();
            server.BaseAddress = new Uri("http://localhost/");

            Task<HttpContext>[] sendResults = new Task<HttpContext>[num_requests];
            for(i = 0; i < num_requests; i++) {
                ClaimsPrincipal user = GetUser(i);
                String path = GetPath(i);
                sendResults[i] = server.SendAsync(ctx => { ctx.Request.Path=path; ctx.User = user; }, default);
                await Task.Yield();
            }

            await quorum_reached.WaitAsync();
            await Task.Yield();
            can_proceed.Set();

            for(i=0; i<num_requests; i++) {
                context = await sendResults[i];
                if(i%(limit+1) < limit) Assert.Equal(Status204NoContent, context.Response.StatusCode);
                else Assert.Equal(Status429TooManyRequests ,context.Response.StatusCode);
            }

            void SetLimiterOptions(RateLimiterOptions options)
            {
                PartitionedRateLimiter<HttpContext> limiter = PartitionedRateLimiter.Create(
                        (HttpContext ctx) => RateLimitPartition.GetConcurrencyLimiter(
                        ctx.User.Identity?.Name??"", _ => new ConcurrencyLimiterOptions() { PermitLimit=limit })

                );
                SelectiveRateLimiterPolicy policy = new SelectiveRateLimiterPolicy(
                    limiter,
                    SetStatusCode429);
                options.AddPolicy(policy_name, policy);
            }

            async Task TerminalHandler(HttpContext ctx)
            {
                ctx.Response.StatusCode=Status204NoContent;
                await can_proceed.WaitAsync(); 
            }

            void PreLimiterHandler(HttpContext context)
            {
                quorum_reached.Signal();
            }

            ValueTask SetStatusCode429(OnRejectedContext context, CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                context.HttpContext.Response.StatusCode=Status429TooManyRequests;
                return ValueTask.CompletedTask;
            }

            IApplicationBuilder PreConfigure(IApplicationBuilder app)
            {
                app.UseBackLink();
                return app;
            }

        }

        [Fact(Timeout=0)]
        public async Task DelegatedRateLimiterPolicyFunctionalTest()
        {
            int i;
            Int32 num_requests, limit = 5;
            Int32 num_passes = 2;
            num_requests = (limit+1)*num_passes+1;

            String path1 = "/ping", path2 = "/other";
            String policy_name = "ping_policy";

            ClaimsPrincipal user1 = new ClaimsPrincipal(new GenericIdentity("USER1"));
            ClaimsPrincipal user2 = new ClaimsPrincipal(new GenericIdentity("USER2"));

            ClaimsPrincipal GetUser(int n)
            {
                return n<limit+1 ? user1 : user2;
            }

            String GetPath(int n)
            {
                return n<(limit+1)*num_passes ? path1 : path2;
            }

            AsyncManualResetEvent can_proceed = new AsyncManualResetEvent();
            AsyncCountdownEvent quorum_reached = new AsyncCountdownEvent(num_requests);

            TestRateLimiterAppConfig.EndpointData[] endpoints = //new TestRateLimiterAppConfig.EndpointData[]
            {
                new TestRateLimiterAppConfig.EndpointData(path1, TerminalHandler, TestRateLimiterAppConfig.EndpointData.MakeConfigurator(policy_name)),
                // Uncomment to fail:
                // new TestRateLimiterAppConfig.EndpointData(path2, TerminalHandler, TestRateLimiterAppConfig.EndpointData.MakeConfigurator(policy_name)),
            };
            HttpContext context;
            TestRateLimiterAppConfig tac = new TestRateLimiterAppConfig(
                SetLimiterOptions, PreLimiterHandler, TerminalHandler, endpoints);
            IHostBuilder host_builder = new HostBuilder().ConfigureWebHost(tac.ConfigureTestApp);
            IHost host = await host_builder.StartAsync();
            TestServer server = host.GetTestServer();
            server.BaseAddress = new Uri("http://localhost/");

            Task<HttpContext>[] sendResults = new Task<HttpContext>[num_requests];
            for(i = 0; i < num_requests; i++) {
                ClaimsPrincipal user = GetUser(i);
                String path = GetPath(i);
                sendResults[i] = server.SendAsync(ctx => { ctx.Request.Path=path; ctx.User = user; }, default);
                await Task.Yield();
            }

            await quorum_reached.WaitAsync();
            await Task.Yield();
            can_proceed.Set();

            for(i=0; i<num_requests; i++) {
                context = await sendResults[i];
                if(i%(limit+1) < limit) Assert.Equal(Status204NoContent, context.Response.StatusCode);
                else Assert.Equal(Status429TooManyRequests, context.Response.StatusCode);
            }

            void SetLimiterOptions(RateLimiterOptions options)
            {
                //options.GlobalLimiter = PartitionedRateLimiter.Create(
                DelegatedRateLimiterPolicy<String> policy = new DelegatedRateLimiterPolicy<String>(
                    (HttpContext ctx) => RateLimitPartition.GetConcurrencyLimiter(
                        ctx.User.Identity?.Name??"", _ => new ConcurrencyLimiterOptions() { PermitLimit=limit}
                    ),
                    SetStatusCode429
                );
                options.AddPolicy(policy_name, policy);
            }

            async Task TerminalHandler(HttpContext ctx)
            {
                ctx.Response.StatusCode=Status204NoContent;
                await can_proceed.WaitAsync();
            }

            void PreLimiterHandler(HttpContext context)
            {
                quorum_reached.Signal();
            }

            ValueTask SetStatusCode429(OnRejectedContext context,CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                context.HttpContext.Response.StatusCode=Status429TooManyRequests;
                return ValueTask.CompletedTask;
            }
        }



    }
}