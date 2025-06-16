using System.Collections.Concurrent;

// Thread safe singleton to manage requests by phone number
public class SmsResourceManager
{
    // Phone numbers are the keys
    private readonly ConcurrentDictionary<string, SmsResource> _resources = new();

    // Set entries to expire after 5 minutes
    private readonly TimeSpan _expiration = TimeSpan.FromMinutes(5);

    public SmsResourceManager()
    {
        // Start background cleanup
        Task.Run(CleanupLoop);
    }

    public SmsResource GetOrCreate(string phoneNumber)
    {
        var resource = _resources.GetOrAdd(phoneNumber, _ => new SmsResource());
        resource.Touch();
        return resource;
    }

    private async Task CleanupLoop()
    {
        while (true)
        {
            foreach (var kvp in _resources)
            {
                // Remove entries that have not been accessed within the expiration time
                if (DateTime.UtcNow - kvp.Value.LastAccessed > _expiration)
                {
                    _resources.TryRemove(kvp.Key, out _);
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(1)); // Check every minute
        }
    }
}

public class SmsResource
{
    public SemaphoreSlim Lock { get; } = new(1, 1);

    // A default value of 5 requests per phone number
    public int RequestsRemaining { get; set; } = 5;

    public DateTime LastAccessed { get; private set; } = DateTime.UtcNow;

    public void Touch()
    {
        LastAccessed = DateTime.UtcNow;
    }
}