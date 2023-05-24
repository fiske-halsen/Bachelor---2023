using EMSuite.Common.PhoneNotification;
using EMSuite.DataAccess;
using EMSuite.Hardware.Api.Service;
using EMSuite.PhoneNotification.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhoneNotificationService.Tests.TestServer;

namespace PhoneNotificationService.Tests.IntegrationTests
{
    [TestFixture]
    public class EMSuiteIntegrationTests
    {
        private TestSignalRServer _signalRServer;
        private ISignalClient _hardwareApiSignalRClient;
        private ISignalRClient _phoneProcessorSignalRClient;
        private IPhoneProcessor _phoneProcessor;
        private ITranslatorService _translatorService;
        private IAzureSpeechService _speechService;
        private IAzureBlobStorageService _blobStorageService;
        private ITwillioService _twillioService;
        private IDelayProvider _delayProvider;
        private IMemoryStreamHandler _streamHandler;
        private ISpeechSynthesizer _speechSynthesizer;
        private IBlobServiceClient _blobServiceClient;
        private ITwillioCallHandler _callHandler;
        private INotificationLogService _notificationLogService;
        private IDataAccess _dataAccess;
        private IAzureCognitiveVoiceProvider _azureCognitiveVoiceProvider;

        [SetUp]
        public void Setup()
        {

            _signalRServer = new TestSignalRServer("http://localhost:5000/");

            // Set up the configuration
            var configuration = GetTestConfiguration();

            //  hardware SignalClient
            _hardwareApiSignalRClient = new SignalClient(configuration, new LoggerFactory().CreateLogger<SignalClient>());


            // translator service
            IHttpClientFactory httpClientFactory = CreateHttpClientFactory();
            _translatorService = new TranslatorService(configuration, httpClientFactory);

            // Azure Speech Service
            _speechSynthesizer = new SpeechSynthesizerHandler();
            _streamHandler = new MemoryStreamHandler();
            _speechService = new AzureSpeechService(configuration, _speechSynthesizer, _streamHandler);

            // Azure blob Service
            _blobServiceClient = new BlobServiceClientWrapper(configuration["BlobStorage:AzureBlobConnectionString"]);
            _blobStorageService = new AzureBlobStorageService(configuration, _blobServiceClient);

            // Delay Provider (Round - Robin)
            _delayProvider = new DelayProvider();

            // Twillio Service
            _callHandler = new TwillioCallHandler();
            _twillioService = new TwillioService(configuration, _callHandler);

            // Data access
            _dataAccess = new SqlDataAccess(configuration["DatabaseConnection:DefaultConnection"]);

            //Repository service
            _notificationLogService = new NotificationLogService(_dataAccess);

            //Azure cognitive voice provider
            _azureCognitiveVoiceProvider = new AzureCogntiveVoiceProvider();

            //Phone processor
            _phoneProcessor = new PhoneProcessor(
                _speechService,
                _blobStorageService,
                _twillioService,
                _translatorService,
                _notificationLogService,
                _delayProvider,
                _azureCognitiveVoiceProvider);

            // Phoneprocessor signalclient
            _phoneProcessorSignalRClient = new SignalRClient(configuration, _phoneProcessor);

            ClearDatabase();

        }

        private async void ClearDatabase()
        {
            using var transaction = await _dataAccess.StartTransaction();

            try
            {
                await _dataAccess.ExecuteAsync("DELETE FROM PhoneCallBatch", transaction);
                await transaction.Commit();
            }
            catch
            {
                await transaction.RollBack();
            }
        }

        [TearDown]
        public void TearDown()
        {
            _signalRServer.Dispose();
            _hardwareApiSignalRClient.Dispose();
            _phoneProcessorSignalRClient.Dispose();
        }

        [Test]
        public async Task SendPhoneCallNotification_ProcessesPhoneAlarmPackage_Success()
        {
            // Arrange
            var phoneNotificationPackage = new PhoneNotificationPackage
            {
                SystemBaseLanguage = "en",
                BaseText = "Hello this is a test notification..",
                GenderId = 1,
                RoundRobinInterval = 200000000, // 20 seconds round robin interval
                AlarmId = 1,
                PhoneContacts = new List<PhoneContact>
            {
                new PhoneContact { UserId = "1", UserName = "Phillip", PhoneNumber = "+4542913009", Language = "en" },
                new PhoneContact { UserId = "2", UserName = "Sumit", PhoneNumber = "+4542913009", Language = "es" },
            }
            };

            var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            var isHardwareApiConnected = await _hardwareApiSignalRClient.Connect(cancellationToken);
            var IsPhoneNotificationServiceConnected = await _phoneProcessorSignalRClient.Connect(cancellationToken);

            // Act
            bool sentSuccessfully = await _hardwareApiSignalRClient.SendPhoneCallNotification(phoneNotificationPackage, cancellationToken);
            var logs = await _notificationLogService.GetNotificationLogsByAlarmId(phoneNotificationPackage.AlarmId);
            logs.Count().Should().Be(2);

            // Assert
            sentSuccessfully.Should().BeTrue();

            foreach (var phoneContact in phoneNotificationPackage.PhoneContacts)
            {
                logs.Any(log => log.UserId == phoneContact.UserId).Should().BeTrue();
            }
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
