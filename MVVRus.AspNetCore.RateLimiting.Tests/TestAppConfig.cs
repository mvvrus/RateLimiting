using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace MVVRus.AspNetCore.RateLimiting.Tests
{
    internal class TestAppConfig
    {
        public TestAppConfig() { }

        public void ConfigureTestApp(IWebHostBuilder webHost)
        {
            webHost.ConfigureServices(ConfigureAppServices);
            webHost.Configure(ConfigureAppPipeline);
            webHost.UseTestServer();
        }

        private void ConfigureAppServices(IServiceCollection services)
        {
            services.AddRouting();
            ConfigureServices(services);
        }

        private void ConfigureAppPipeline(IApplicationBuilder app)
        {
            app.UseRouting();
            Configure(app);
            app.UseEndpoints(ConfigureRouting);
            ConfigureRest(app);
        }

        protected virtual void ConfigureServices(IServiceCollection services) { }
        protected virtual void ConfigureRouting(IEndpointRouteBuilder builder) { }
        protected virtual void Configure(IApplicationBuilder app) { }
        protected virtual void ConfigureRest(IApplicationBuilder app) { }
    }

}
