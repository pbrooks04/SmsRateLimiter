using SmsRateLimiter.Api.SmsResources;
using System.Collections.ObjectModel;

namespace SmsRateLimiter.History;
public class HistoryLog
{
    private List<HistoryEntry> _entries = new List<HistoryEntry>();
    private int _maxNumberOfRecords = 500;

    public HistoryLog() { }
    public void AddEntry(SmsRequest smsRequest, string status)
    {
        // Keep new entries at the start
        _entries.Insert(0, new HistoryEntry(smsRequest, status));
        if (_entries.Count > _maxNumberOfRecords)
        {
            // Keep the history to a predefined number of entries
            _entries.RemoveRange(_maxNumberOfRecords, _entries.Count - _maxNumberOfRecords);
        }
    }

    public ReadOnlyCollection<HistoryEntry> GetEntries() => _entries.AsReadOnly();

}

public class HistoryEntry
{
    public SmsRequest SmsRequest { get; }
    public DateTime Date { get; }
    public string Status { get; }

    public HistoryEntry(SmsRequest smsRequest, string status)
    {
        this.SmsRequest = smsRequest;
        this.Date = DateTime.UtcNow;
        this.Status = status;
    }
}
