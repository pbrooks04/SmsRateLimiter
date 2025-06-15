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
                new SmsRequest("1234567890", "123", "Hello there")
            );

            response.EnsureSuccessStatusCode();
            var smsResponse = await response.Content.ReadFromJsonAsync<SmsResponse>();
            Assert.That(smsResponse?.RemainingRequests, Is.EqualTo(4));
        }

        [Test]
        public async Task SendLimitPerPhoneNumberIsCapped()
        {
            var maxRequests = 5;
            var requestsSent = 0;

            // Reach the maximum number of requests
            do
            {
                var response = await client.PostAsJsonAsync(
                    "/api/sms/send",
                    new SmsRequest("1234567890", "123", "Hello there")
                );

                response.EnsureSuccessStatusCode();
                requestsSent++;
                var smsResponse = await response.Content.ReadFromJsonAsync<SmsResponse>();

                // Check the number of remaining credits
                Assert.That(smsResponse?.RemainingRequests, Is.EqualTo(maxRequests - requestsSent));
                // Ensure that the message was sent
                Assert.IsTrue(smsResponse.RequestWasSent);
            } while (requestsSent < maxRequests);

            // Send another request and ensure that it is rejected
            var failingResponse = await client.PostAsJsonAsync(
                "/api/sms/send",
                new SmsRequest("1234567890", "123", "Hello there")
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