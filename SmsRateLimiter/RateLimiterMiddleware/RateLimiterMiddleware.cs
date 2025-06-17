using SmsRateLimiter.Api.SmsResources;
using SmsRateLimiter.History;
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
        private readonly ConcurrentDictionary<string, TimedRateLimiter> _accountLimiters = new();
        private readonly ConcurrentDictionary<string, TimedRateLimiter> _phoneLimiters = new();

        // This is used as a constant for all limiters. It could be more adaptable
        // if it were to read from a variable if Providers/Accounts have different
        // rate limit requirements.
        private readonly FixedWindowRateLimiterOptions _limiterOptions = new()
        {
            PermitLimit = 5,
            Window = TimeSpan.FromSeconds(10),
            QueueLimit = 0
        };

        // Set a timespan for idle limiters to determine when they should be removed.
        private readonly TimeSpan _idleThreshold = TimeSpan.FromMinutes(5);
        // The frequency that the clean up task will run
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(1);

        public RateLimiterMiddleware(RequestDelegate next)
        {
            _next = next;
            StartCleanupLoop();
        }

        public async Task InvokeAsync(HttpContext context, HistoryLog historyLog)
        {
            // Only intercept JSON POST requests.
            if (context.Request.Method != HttpMethods.Post ||
                !context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
            {
                await _next(context);
                return;
            }

            // Enable buffering so the request body can be read here for rate limiting,
            // and again later by the actual request handler.
            context.Request.EnableBuffering();

            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();

            // Reset the body stream position to allow downstream code to re-read it.
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
            var accountLimiter = _accountLimiters.GetOrAdd(accountId,
                _ => new TimedRateLimiter(new FixedWindowRateLimiter(_limiterOptions)));

            var phoneLimiter = _phoneLimiters.GetOrAdd(phoneNumber,
                _ => new TimedRateLimiter(new FixedWindowRateLimiter(_limiterOptions)));

            var accountLease = await accountLimiter.AcquireAsync();
            if (!accountLease.IsAcquired)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("Rate limit exceeded (account)");
                historyLog.AddEntry(request, "rejected");
                return;
            }

            var phoneLease = await phoneLimiter.AcquireAsync();
            if (!phoneLease.IsAcquired)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("Rate limit exceeded (phone)");
                historyLog.AddEntry(request, "rejected");
                return;
            }

            await _next(context);
        }

        private void StartCleanupLoop()
        {
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(_cleanupInterval);
                    
                    CleanupDictionary(_accountLimiters);
                    CleanupDictionary(_phoneLimiters);
                }
            });
        }

        private void CleanupDictionary(ConcurrentDictionary<string, TimedRateLimiter> dict)
        {
            var now = DateTime.UtcNow;

            foreach (var kvp in dict)
            {
                if (now - kvp.Value.LastAccess > _idleThreshold)
                {
                    if (dict.TryRemove(kvp.Key, out var removed))
                    {
                        removed.Limiter.Dispose();
                    }
                }
            }
        }
    }

    class TimedRateLimiter
    {
        public FixedWindowRateLimiter Limiter { get; }
        public DateTime LastAccess { get; private set; }

        public TimedRateLimiter(FixedWindowRateLimiter limiter)
        {
            Limiter = limiter;
            Touch();
        }

        public async Task<RateLimitLease> AcquireAsync()
        {
            Touch();
            return await Limiter.AcquireAsync(1);
        }

        private void Touch()
        {
            LastAccess = DateTime.UtcNow;
        }
    }
}
