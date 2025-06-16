using System.Text.Json.Serialization;

namespace SmsRateLimiter.Api.SmsResources
{
    public class SmsRequest
    {
        public SmsRequest(string phoneNumber, string accountId, string message)
        {
            AccountId = accountId;
            PhoneNumber = phoneNumber;
            Message = message;
        }

        [JsonPropertyName("accountId")]
        public string AccountId { get; set; }

        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}