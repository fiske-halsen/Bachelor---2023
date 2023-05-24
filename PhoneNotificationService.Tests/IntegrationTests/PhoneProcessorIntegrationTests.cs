using EMSuite.Common.PhoneNotification;
using EMSuite.DataAccess;
using EMSuite.PhoneNotification.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PhoneNotificationService.Tests.Integration;

[TestFixture]
public class PhoneProcessorIntegrationTests
{
    private ITranslatorService _translatorService;
    private IAzureSpeechService _speechService;
    private IAzureBlobStorageService _blobStorageService;
    private ITwillioService _twillioService;
    private IDelayProvider _delayProvider;
    private IMemoryStreamHandler _streamHandler;
    private ISpeechSynthesizer _speechSynthesizer;
    private IPhoneProcessor _phoneProcessor;
    private IBlobServiceClient _blobServiceClient;
    private ITwillioCallHandler _callHandler;
    private INotificationLogService _notificationLogService;
    private IDataAccess _dataAccess;
    private IAzureCognitiveVoiceProvider _azureCognitiveVoiceProvider;

    [SetUp]
    public void Setup()
    {
        IConfiguration configuration = GetTestConfiguration();

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
    public async Task Cleanup()
    {
        // Clean up the uploaded audio files from Azure Blob Storage
        var blobContainer = _blobServiceClient.GetBlobContainerClient("audiofilecontainertest");
        var blobItems = await blobContainer.GetBlobItems();

        foreach (var blobItem in blobItems)
        {
            await blobContainer.DeleteBlob(blobItem.Name);
        }
    }

    [Test]
    public async Task CallPhones_IntegrationTest()
    {
        // Arrange
        var phoneNotificationPackage = new PhoneNotificationPackage
        {
            SystemBaseLanguage = "en",
            BaseText = "HEllo i want to watch prison break right now...",
            GenderId = 1,
            RoundRobinInterval = 200000000, // 20 seconds round robin interval
            AlarmId = 1,
            PhoneContacts = new List<PhoneContact>
            {
                new PhoneContact { UserId = "1", UserName = "Phillip", PhoneNumber = "+4542913009", Language = "zh" },
                new PhoneContact { UserId = "2", UserName = "Sumit", PhoneNumber = "+4542913009", Language = "es" },
            }
        };

        // Act
        var (callCount, callTimestamps) = await _phoneProcessor.CallPhones(phoneNotificationPackage, CancellationToken.None);
        int expectedCallCount = 2;

        // Assert
        callCount.Should().Be(expectedCallCount);

        for (int i = 1; i < callTimestamps.Count; i++)
        {
            TimeSpan timeDifference = callTimestamps[i] - callTimestamps[i - 1];
            Assert.GreaterOrEqual(timeDifference, TimeSpan.FromSeconds(phoneNotificationPackage.RoundRobinInterval / 10000000));
        }

        // Check if records have been inserted in the database
        var logs = await _notificationLogService.GetNotificationLogsByAlarmId(phoneNotificationPackage.AlarmId);
        logs.Count().Should().Be(2);

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
