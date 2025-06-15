using System.Text.Json.Serialization;

namespace SmsRateLimiter.Api.SmsResources
{
    public class SmsRequest
    {
        public SmsRequest(string phoneNumber, string message)
        {
            PhoneNumber = phoneNumber;
            Message = message;
        }

        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}