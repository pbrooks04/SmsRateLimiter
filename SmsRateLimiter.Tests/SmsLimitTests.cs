using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using SmsRateLimiter.Api.SmsResources;
using System.Net;
using System.Net.Http.Json;

namespace SmsRateLimiter.Tests
{
    public class Tests
    {
        private HttpClient client;

        [SetUp]
        public void Setup()
        {
            var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => { });
            client = factory.CreateClient();
        }
        [TearDown]
        public void TearDown()
        {
            client?.Dispose();
        }

        [Test]
        public async Task CanSendSms()
        {
            var response = await client.PostAsJsonAsync(
                "/api/sms/send",
                new SmsRequest("1234567890", "abc", "Hello there")
            );

            response.EnsureSuccessStatusCode();
            var smsResponse = await response.Content.ReadFromJsonAsync<SmsResponse>();
        }

        [Test]
        public async Task SendLimitPerPhoneNumberIsCapped()
        {
            const int maxRequests = 5;
            const string phoneNumber = "1234567890";
            var requestsSent = 0;

            // Reach the maximum number of requests
            do
            {
                var response = await client.PostAsJsonAsync(
                    "/api/sms/send",
                    // Change the provider each time to ensure that the phone number is
                    // blocked from sending too many requests.
                    new SmsRequest(phoneNumber, $"{requestsSent}abc", "Hello there")
                );

                response.EnsureSuccessStatusCode();
                requestsSent++;
                var smsResponse = await response.Content.ReadFromJsonAsync<SmsResponse>();

                // Ensure that the message was sent
                Assert.IsTrue(smsResponse?.RequestWasSent);
            } while (requestsSent < maxRequests);

            // Send another request and ensure that it is rejected
            var failingResponse = await client.PostAsJsonAsync(
                "/api/sms/send",
                new SmsRequest(phoneNumber, "abc", "Hello there")
            );

            Assert.That(failingResponse.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
        }

        [Test]
        public async Task SendLimitPerAccountIsCapped()
        {
            const int maxRequests = 5;
            const string accountId = "abc";
            var requestsSent = 0;

            // Reach the maximum number of requests
            do
            {
                var response = await client.PostAsJsonAsync(
                    "/api/sms/send",
                    new SmsRequest($"123456789{requestsSent}", accountId, "Hello there")
                );

                response.EnsureSuccessStatusCode();
                requestsSent++;
                var smsResponse = await response.Content.ReadFromJsonAsync<SmsResponse>();

                // Ensure that the message was sent
                Assert.IsTrue(smsResponse?.RequestWasSent);
            } while (requestsSent < maxRequests);

            // Send another request and ensure that it is rejected
            var failingResponse = await client.PostAsJsonAsync(
                "/api/sms/send",
                new SmsRequest($"123456789{requestsSent}", accountId, "Hello there")
            );

            Assert.That(failingResponse.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
        }

        [Test]
        public async Task ShouldRejectMalformedRequests()
        {
            var response = await client.PostAsJsonAsync(
                "/api/sms/send",
                new { message = "Hello there" }
            );
            Assert.IsFalse(response.IsSuccessStatusCode);
        }
    }
}