namespace SmsRateLimiter.Api;

public static class SmsApi
{
    public static IEndpointRouteBuilder MapSmsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/sms/send", SmsHandler.SendSms);
        return app;
    }
}