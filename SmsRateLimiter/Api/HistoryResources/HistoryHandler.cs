using SmsRateLimiter.History;

namespace SmsRateLimiter.Api.HistoryResources;

public class HistoryHandler
{
    public static async Task<IResult> SendHistoryRecords(HistoryLog historyLog)
    {
        // Just a note; in a real world scenario, the structure returned in an endpoint
        // should not reflect the structure of the underlying data i.e. the DB schema
        var historyEntries = historyLog.GetEntries();
        return Results.Ok(historyEntries);
    }
}
