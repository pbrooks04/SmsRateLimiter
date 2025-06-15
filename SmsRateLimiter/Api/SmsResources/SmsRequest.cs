using System.Text.Json.Serialization;

namespace SmsRateLimiter.Api.SmsResources
{
    public class SmsRequest
    {
        public SmsRequest(string phoneNumber, string accountId, string message)
        {
            PhoneNumber = phoneNumber;
            AccountId = accountId;
            Message = message;
        }

        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("accountId")]
        public string AccountId { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}