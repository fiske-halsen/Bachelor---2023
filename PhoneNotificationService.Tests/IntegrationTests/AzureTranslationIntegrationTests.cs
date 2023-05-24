using EMSuite.PhoneNotification.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PhoneNotificationService.Tests.IntegrationTests
{
    [TestFixture]
    public class AzureTranslationIntegrationTests
    {
        ITranslatorService _translatorService;

        [SetUp]
        public void Setup()
        {
            IConfiguration configuration = GetTestConfiguration();
            IHttpClientFactory httpClientFactory = CreateHttpClientFactory();

            _translatorService = new TranslatorService(configuration, httpClientFactory);
        }

        [TestCase("en", "Hello, this is a notification")]
        [TestCase("fr", "Bonjour, ceci est une notification")]
        [TestCase("es", "Hola, esta es una notificación")]
        [TestCase("pt", "Olá, esta é uma notificação")]
        [TestCase("de", "Hallo, dies ist eine Benachrichtigung")]
        [TestCase("zh", "您好，这是一个通知")]
        [TestCase("ko", "안녕하세요, 알림입니다")]
        [TestCase("ja", "こんにちは、これは通知です")]
        [TestCase("nl", "Hallo, dit is een melding")]
        [TestCase("pl", "Witaj, to jest powiadomienie")]
        [TestCase("it", "Ciao, questa è una notifica")]
        [TestCase("ru", "Здравствуйте, это уведомление")]
        [TestCase("tr", "Merhaba, bu bir bildirimdir")]

        public async Task TranslateText_TranslatesText_Success(string targetLanguage, string expectedTranslation)
        {
            // Arrange
            string inputText = "Hello, this is a notification";
            string sourceLanguage = "en";

            // Act
            string result = await _translatorService.TranslateText(inputText, targetLanguage, sourceLanguage);

            // Assert
            result.Should().NotBeNull();
            result.Should().Be(expectedTranslation);
        }

        private IConfiguration GetTestConfiguration()
        {
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.test.json", optional: false, reloadOnChange: true);

            return configBuilder.Build();
        }

        private IHttpClientFactory CreateHttpClientFactory()
        {
            var services = new ServiceCollection();
            services.AddHttpClient();
            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider.GetService<IHttpClientFactory>();
        }

    }
}
