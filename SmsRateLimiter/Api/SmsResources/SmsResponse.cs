using System.Text.Json.Serialization;

namespace SmsRateLimiter.Api.SmsResources
{
    public class SmsResponse
    {
        [JsonPropertyName("remainingRequests")]
        public int RemainingRequests { get; set; }

        [JsonPropertyName("requestWasSent")]
        public bool RequestWasSent { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
