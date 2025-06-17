using SmsRateLimiter.Api.SmsResources;
using SmsRateLimiter.History;

public static class SmsHandler
{
    private static readonly HttpClient _client = new HttpClient();

    public static async Task<IResult> SendSms(
        HttpContext context,
        SmsRequest request,
        HistoryLog historyLog
    )
    {
        if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return Results.BadRequest(new { error = "Phone number is required" });
        }

        // Store a record of the event
        historyLog.AddEntry(request, "success");

        var response = await MockSendSms(request.Message);

        return Results.Ok(new SmsResponse
        {
            RequestWasSent = true,
            Message = $"Sent message from {request.PhoneNumber}"
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
}