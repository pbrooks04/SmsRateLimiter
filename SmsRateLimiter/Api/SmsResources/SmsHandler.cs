using SmsRateLimiter.Api.SmsResources;
using SmsRateLimiter.History;

public static class SmsHandler
{
    private static readonly HttpClient _client = new HttpClient();

    public static async Task<IResult> SendSms(
        HttpContext context, 
        SmsRequest request, 
        SmsResourceManager resourceManager,
        HistoryLog historyLog
    )
    {
        if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return Results.BadRequest(new { error = "Phone number is required" });
        }

        var accountId = context.Request.Query["accountId"].ToString();

        // Store a record of the event
        historyLog.AddEntry(request, accountId);

        var resource = resourceManager.GetOrCreate(request.PhoneNumber);

        return await WithResourceLock(resource, async () =>
        {
            if (resource.RequestsRemaining <= 0)
            {
                return Results.Json(new SmsResponse
                {
                    RemainingRequests = 0,
                    RequestWasSent = false,
                    Message = "No more credits remaining",
                }, statusCode: StatusCodes.Status429TooManyRequests);
            }

            var response = await MockSendSms(request.Message);

            resource.RequestsRemaining--;

            return Results.Ok(new SmsResponse
            {
                RemainingRequests = resource.RequestsRemaining,
                RequestWasSent = true,
            });
        });
    }

    private static async Task<IResult> MockSendSms(string message)
    {
        var values = new Dictionary<string, string>
        {
            { "message", message },
            { "success", "true" }
        };

        var content = new FormUrlEncodedContent(values);

        // Here's what the actual implementation may look like. Since there isn't
        // an endpoint to send the SMS request, it will just wait for half a second
        // to mock a request.
        //
        // var response = await _client.PostAsync("https://www.notrealdomain.com/send/sms", content);
        // if (response.IsSuccessStatusCode)
        // {
        //     var responseBody = await response.Content.ReadAsStringAsync();
        //     return Results.Ok(responseBody);
        // }
        // throw new Exception("Failed to send SMS to provider");
        await Task.Delay(500);

        return Results.Ok(content);
    }

    private static async Task<IResult> WithResourceLock(SmsResource resource, Func<Task<IResult>> action)
    {
        await resource.Lock.WaitAsync();
        try
        {
            return await action();
        }
        finally
        {
            resource.Lock.Release();
        }
    }
}