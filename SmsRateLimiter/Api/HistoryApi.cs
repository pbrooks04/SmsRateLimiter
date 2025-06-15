using SmsRateLimiter.Api.HistoryResources;

namespace SmsRateLimiter.Api;

public static class HistoryApi
{
    public static IEndpointRouteBuilder MapHistoryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("api/history", HistoryHandler.SendHistoryRecords)
            .RequireRateLimiting("RateLimitPolicy");

        return app;
    }
}

