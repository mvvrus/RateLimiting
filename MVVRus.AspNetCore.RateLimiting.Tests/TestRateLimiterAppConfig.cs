using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;

namespace MVVRus.AspNetCore.RateLimiting.Tests
{
    internal class TestRateLimiterAppConfig : TestAppConfig
    {
        Action<RateLimiterOptions> _configureLimiter;
        Action<HttpContext>? _preLimiterHandler;
        RequestDelegate? _terminalHandler;
        EndpointData[] _endpoints;

        public TestRateLimiterAppConfig(Action<RateLimiterOptions> configureLimiter,
            Action<HttpContext>? preLimiterHandler,
            RequestDelegate? terminalHandler,
            EndpointData[]? endpoints=null)
        {
            _configureLimiter=configureLimiter;
            _preLimiterHandler = preLimiterHandler;
            _terminalHandler=terminalHandler;
            _endpoints = endpoints ?? new EndpointData[0];
        }

        protected override void Configure(IApplicationBuilder app)
        {
            if(_preLimiterHandler!=null)
                app.Use(next => async context => { _preLimiterHandler(context); await next(context); });
            app.UseRateLimiter();
            base.Configure(app);
        }

        protected override void ConfigureRest(IApplicationBuilder app)
        {
            if(_terminalHandler!=null) app.Run(_terminalHandler);
            base.ConfigureRest(app);
        }

        protected override void ConfigureRouting(IEndpointRouteBuilder builder)
        {
            foreach(EndpointData ep in _endpoints)
                ep.Configurator(builder.Map(ep.Endpoint, ep.Handler));
            base.ConfigureRouting(builder);
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddRateLimiter(_configureLimiter);
            base.ConfigureServices(services);
        }

        internal class EndpointData
        {
            public RoutePattern Endpoint { get; }
            public RequestDelegate Handler { get; }
            public Action<IEndpointConventionBuilder> Configurator { get; }

            static public Action<IEndpointConventionBuilder> MakeConfigurator<TPartitionKey>(IRateLimiterPolicy<TPartitionKey>? policy)
            {
                return policy is null ? _ => { }: ecb => ecb.RequireRateLimiting(policy);
            }

            static public Action<IEndpointConventionBuilder> MakeConfigurator(String policyName)
            {
                return ecb => ecb.RequireRateLimiting(policyName);
            }

            public EndpointData(String endpoint, RequestDelegate handler, Action<IEndpointConventionBuilder> configurator) 
            {
                Endpoint = RoutePatternFactory.Parse(endpoint);
                Handler = handler;
                Configurator = configurator;
            }
        }
    }

}
