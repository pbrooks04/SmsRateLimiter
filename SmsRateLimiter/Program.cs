using SmsRateLimiter.Api;
using SmsRateLimiter.History;
using SmsRateLimiter.RateLimiterMiddleware;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<HistoryLog>();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("RateLimitPolicy", httpContext =>
    {
        // ToDo: This will have to change. It's only being used by `History` and
        // this endpoint doesn't use `accountId`.
        var accountId = httpContext.Request.Query["accountId"].ToString();

        var partitionKey = !string.IsNullOrEmpty(accountId)
            ? accountId
            : httpContext.User.Identity?.Name
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitionKey,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(10),
                QueueLimit = 0
            });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseWhen(
    context => context.Request.Method == "POST" &&
               context.Request.Path.Equals("/api/sms/send", StringComparison.OrdinalIgnoreCase),
    appBuilder =>
    {
        appBuilder.UseMiddleware<RateLimiterMiddleware>();
    });


// Map endpoints
app.MapSmsEndpoints();
app.MapHistoryEndpoints();

app.Run();

// For testing
public partial class Program { }
