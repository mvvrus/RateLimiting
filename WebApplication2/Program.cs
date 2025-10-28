using Microsoft.AspNetCore.RateLimiting;
using MVVRus.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using static Microsoft.AspNetCore.Http.StatusCodes;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
String policy_name = "SelectivePolicy";
builder.Services.AddRateLimiter(
    options => {
        PartitionedRateLimiter<HttpContext> limiter = PartitionedRateLimiter.CreateChained(
            PartitionedRateLimiter.Create(
                (HttpContext ctx) => RateLimitPartition.GetFixedWindowLimiter(
                    ctx.User.Identity?.Name??"", _ => 
                        new FixedWindowRateLimiterOptions() { PermitLimit=10, Window=TimeSpan.FromMinutes(1) }
                )

            ),
            PartitionedRateLimiter.Create(
                (HttpContext ctx) => RateLimitPartition.GetConcurrencyLimiter(
                    ctx.User.Identity?.Name??"", _ => new ConcurrencyLimiterOptions() { PermitLimit=3 }
                )

            )
        );
        SelectiveRateLimiterPolicy policy = new SelectiveRateLimiterPolicy(
            limiter,
            (context, _) => { 
                context.HttpContext.Response.StatusCode=Status429TooManyRequests; 
                return ValueTask.CompletedTask; 
            }
        );
        options.AddPolicy(policy_name, policy);
    }
);

var app = builder.Build();
app.UseBackLink();
app.UseRateLimiter();

app.MapGet("/", () => "Hello World!").RequireRateLimiting(policy_name);
app.Run();
