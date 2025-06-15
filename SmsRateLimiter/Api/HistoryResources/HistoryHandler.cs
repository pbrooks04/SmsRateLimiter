using SmsRateLimiter.History;

namespace SmsRateLimiter.Api.HistoryResources;

public class HistoryHandler
{
    public static async Task<IResult> SendHistoryRecords(HistoryLog historyLog)
    {
        // Add implementation here
        var historyEntries = historyLog.GetEntries();
        return Results.Ok(historyEntries);
    }
}
