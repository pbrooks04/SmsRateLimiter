using SmsRateLimiter.Api.SmsResources;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace SmsRateLimiter.RateLimiterMiddleware
{
    public class RateLimiterMiddleware
    {
        // Reference to the incoming request
        private readonly RequestDelegate _next;

        // Thread safe dictionaries to manage rate limit leases
        private readonly ConcurrentDictionary<string, FixedWindowRateLimiter> _accountLimiters = new();
        private readonly ConcurrentDictionary<string, FixedWindowRateLimiter> _phoneLimiters = new();

        // This is used as a constant for all limiters. It could be more adaptable
        // if it were to read from a variable if Providers/Accounts have different
        // rate limit requirements.
        private readonly FixedWindowRateLimiterOptions _limiterOptions = new()
        {
            PermitLimit = 5,
            Window = TimeSpan.FromSeconds(10),
            QueueLimit = 0
        };

        public RateLimiterMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only intercept POSTs with bodies
            if (context.Request.Method != HttpMethods.Post ||
                !context.Request.ContentType?.Contains("application/json") == true)
            {
                await _next(context);
                return;
            }
            
            // Allow the request to be read more than once so that the values can be extracted
            // from the body for limiting and still be read to process the request.
            context.Request.EnableBuffering();

            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();

            // Reset the body so it can be read again.
            context.Request.Body.Position = 0;

            SmsRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<SmsRequest>(body);
            }
            catch
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Invalid request body");
                return;
            }

            var accountId = request?.AccountId ?? "unknown_account";
            var phoneNumber = request?.PhoneNumber ?? "unknown_phone";
            
            // As mentioned above, the limiter options are fixed. This could be more adaptable to read from a source
            // of truth and have different values depending on the Provider/Account.
            var accountLimiter = _accountLimiters.GetOrAdd(accountId, _ => new FixedWindowRateLimiter(_limiterOptions));
            var phoneLimiter = _phoneLimiters.GetOrAdd(phoneNumber, _ => new FixedWindowRateLimiter(_limiterOptions));

            var accountLease = await accountLimiter.AcquireAsync(1);
            if (!accountLease.IsAcquired)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("Rate limit exceeded (account)");
                return;
            }

            var phoneLease = await phoneLimiter.AcquireAsync(1);
            if (!phoneLease.IsAcquired)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("Rate limit exceeded (phone)");
                return;
            }

            await _next(context);
        }
    }
}
