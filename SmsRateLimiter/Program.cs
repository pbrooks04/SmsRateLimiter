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
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: "rateLimitPartitionKey",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromSeconds(1),
                QueueLimit = 0
            });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend",
        policy =>
        {
            // Just to allow the front end to access this service
            policy.WithOrigins("http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
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

app.UseCors("Frontend");


// Map endpoints
app.MapSmsEndpoints();
app.MapHistoryEndpoints();

app.Run();

// For testing
public partial class Program { }
