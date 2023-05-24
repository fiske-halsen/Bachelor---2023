using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace PhoneNotificationService.Tests.IntegrationTests
{
    [TestFixture]
    public class AzureFunctionIntegrationTests
    {
        private string AzureFunctionBaseUrl = string.Empty;

        [SetUp]
        public void Setup()
        {
            IConfiguration configuration = GetTestConfiguration();
            AzureFunctionBaseUrl = configuration["AzureFunction:TriggerUrl"];
        }
        [Test]
        public async Task TestAzureFunction_ReturnsCorrectUrl()
        {
            // Arrange
            using var httpClient = new HttpClient { BaseAddress = new Uri(AzureFunctionBaseUrl) };

            string sampleMp3Url = "http://example.com/sample.mp3";
            string encodedMp3Url = Uri.EscapeDataString(sampleMp3Url);

            string functionEndpoint = $"{AzureFunctionBaseUrl}{encodedMp3Url}";

            // Act
            HttpResponseMessage response = await httpClient.GetAsync(functionEndpoint);

            response.IsSuccessStatusCode.Should().BeTrue("Expected successful response from Azure Function");

            string responseContent = await response.Content.ReadAsStringAsync();

            string expectedResponse = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Response>
<Play>http://example.com/sample.mp3</Play>
</Response>";

            // Assert
            responseContent.Should().Be(expectedResponse, "Expected the Azure Function to return the correct URL");
        }

        private IConfiguration GetTestConfiguration()
        {
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.test.json", optional: false, reloadOnChange: true);

            return configBuilder.Build();
        }

    }
}
