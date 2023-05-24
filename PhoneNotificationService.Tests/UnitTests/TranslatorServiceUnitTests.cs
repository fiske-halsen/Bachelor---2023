using EMSuite.PhoneNotification.Exceptions;
using EMSuite.PhoneNotification.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Newtonsoft.Json.Linq;
using System.Net;

namespace PhoneNotificationService.Tests.UnitTests
{
    [TestFixture]
    public class TranslatorServiceUnitTests
    {
        private Mock<IConfiguration> _configurationMock;
        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;

        [SetUp]
        public void Setup()
        {
            _configurationMock = new Mock<IConfiguration>();
            _configurationMock.SetupGet(c => c["AzureCogniveService:TranslatorEndpoint"]).Returns("https://api.example.com/");
            _configurationMock.SetupGet(c => c["AzureCogniveService:ApiKey"]).Returns("api-key");
            _configurationMock.SetupGet(c => c["AzureCogniveService:Region"]).Returns("region");

            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        }

        [Test]
        public async Task TranslateText_ReturnsTranslatedText()
        {
            // Arrange
            var inputText = "Hello";
            var targetLanguage = "es";
            var expectedTranslation = "Hola";
            var jsonResponse = JArray.Parse($"[{{\"translations\":[{{\"text\":\"{expectedTranslation}\"}}]}}]");

            _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>())
                  .ReturnsAsync(new HttpResponseMessage()
                  {
                      StatusCode = HttpStatusCode.OK,
                      Content = new StringContent(jsonResponse.ToString()),
                  });
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_httpMessageHandlerMock.Object));
            var translatorService = new TranslatorService(_configurationMock.Object, _httpClientFactoryMock.Object);

            // Act
            string translatedText = await translatorService.TranslateText(inputText, targetLanguage);

            // Assert
            translatedText.Should().Be(expectedTranslation);
            _httpMessageHandlerMock.Protected().Verify("SendAsync", Times.Exactly(1), ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post && req.RequestUri.ToString().Contains("translate") &&
                    req.Headers.Contains("Ocp-Apim-Subscription-Key") &&
                    req.Headers.Contains("Ocp-Apim-Subscription-Region")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Test]
        public async Task TranslateText_ApiRequestFails_ThrowsCustomException()
        {
            // Arrange
            var inputText = "Hello";
            var targetLanguage = "es";
            var expectedErrorMessage = "Translation failed. Status code: BadRequest";

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.BadRequest
                });

            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_httpMessageHandlerMock.Object));
            var translatorService = new TranslatorService(_configurationMock.Object, _httpClientFactoryMock.Object);

            // Act & Assert
            CustomException ex = Assert.ThrowsAsync<CustomException>(async () => await translatorService.TranslateText(inputText, targetLanguage));
            ex.Message.Should().Be(expectedErrorMessage);
        }

        [Test]
        public async Task TranslateText_JsonParsingFails_ThrowsCustomException()
        {
            // Arrange
            var inputText = "Hello";
            var targetLanguage = "es";
            var expectedErrorMessage = "Translation failed due to an error parsing the response.";

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("Invalid JSON"),
                });

            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_httpMessageHandlerMock.Object));
            var translatorService = new TranslatorService(_configurationMock.Object, _httpClientFactoryMock.Object);

            // Act & Assert
            CustomException ex = Assert.ThrowsAsync<CustomException>(async () => await translatorService.TranslateText(inputText, targetLanguage));
            ex.Message.Should().Be(expectedErrorMessage);
        }
    }
}
