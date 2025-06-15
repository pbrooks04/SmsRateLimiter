using SmsRateLimiter.Api;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<SmsResourceManager>();
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("SmsSendPolicy", httpContext =>
    {
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
                PermitLimit = 5,                  // 5 requests
                Window = TimeSpan.FromSeconds(1), // per second
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

var app = builder.Build();

app.UseRateLimiter();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapSmsEndpoints();

app.Run();

// For testing
public partial class Program { }
