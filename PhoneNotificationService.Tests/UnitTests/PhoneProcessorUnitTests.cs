using EMSuite.Common.PhoneNotification;
using EMSuite.PhoneNotification.Models;
using EMSuite.PhoneNotification.Services;
using FluentAssertions;
using Moq;
using System.Diagnostics;
using System.Text;

namespace PhoneNotificationService.Tests.UnitTests
{
    [TestFixture]
    public class PhoneProcessorUnitTests
    {
        private Mock<IAzureSpeechService> _speechServiceMock;
        private Mock<IAzureBlobStorageService> _blobStorageServiceMock;
        private Mock<ITwillioService> _twillioServiceMock;
        private Mock<ITranslatorService> _translatorServiceMock;
        private Mock<IDelayProvider> _delayProviderMock;
        private Mock<INotificationLogService> _notificationLogService;
        private Mock<IAzureCognitiveVoiceProvider> _cogntiveVoiceProviderMock;
        private PhoneProcessor _phoneProcessor;


        [SetUp]
        public void Setup()
        {
            _speechServiceMock = new Mock<IAzureSpeechService>();
            _blobStorageServiceMock = new Mock<IAzureBlobStorageService>();
            _twillioServiceMock = new Mock<ITwillioService>();
            _translatorServiceMock = new Mock<ITranslatorService>();
            _delayProviderMock = new Mock<IDelayProvider>();
            _notificationLogService = new Mock<INotificationLogService>();
            _cogntiveVoiceProviderMock = new Mock<IAzureCognitiveVoiceProvider>();

            _phoneProcessor = new PhoneProcessor(
               _speechServiceMock.Object,
               _blobStorageServiceMock.Object,
               _twillioServiceMock.Object,
               _translatorServiceMock.Object,
               _notificationLogService.Object,
               _delayProviderMock.Object,
               _cogntiveVoiceProviderMock.Object);
        }

        [Test]
        public async Task CallPhones_TranslatesText_WhenUserLanguageIsDifferentFromSystemBaseLanguage()
        {
            // Arrange
            var phoneNotificationPackage = new PhoneNotificationPackage
            {
                SystemBaseLanguage = "en",
                BaseText = "Hello, this is a test notification.",
                PhoneContacts = new List<PhoneContact>
                    {
                      new PhoneContact { UserName = "Phillip", PhoneNumber = "+4534567890", Language = "es" }
                    }
            };

            _translatorServiceMock
                .Setup(x => x.TranslateText(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("Hola, esta es una notificación de prueba.");


            _speechServiceMock
             .Setup(x => x.GenerateTextToSpeechAudio(It.IsAny<GenerateSpeechConfiguration>()))
             .ReturnsAsync(() => new MemoryStreamResult(Encoding.UTF8.GetBytes("Dummy audio content")));


            _blobStorageServiceMock
                .Setup(x => x.UploadAudioToAzureBlob(It.IsAny<MemoryStreamResult>(), It.IsAny<string>()))
                .ReturnsAsync("https://test.blob.core.windows.net/audio/Phillip.mp3");

            _delayProviderMock
                .Setup(x => x.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _twillioServiceMock
                .Setup(x => x.MakeCall(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => new CustomCallResource { });

            _notificationLogService
                .Setup(x => x.InsertNoticationLog(It.IsAny<NotificationLog>()))
                .Returns(Task.FromResult(true));

            _cogntiveVoiceProviderMock
                .Setup(x => x.GetAzureCognitiveVoiceByGender(It.IsAny<string>(), It.IsAny<int>()))
                .Returns("dummy-female-voice");

            _cogntiveVoiceProviderMock
                .Setup(x => x.GetAzureCognitiveVoiceRate(It.IsAny<string>()))
                .Returns("0.8");

            _cogntiveVoiceProviderMock
                .Setup(x => x.GetAzureCognitiveLanguage(It.IsAny<string>()))
                .Returns("es-ES");

            // Act
            await _phoneProcessor.CallPhones(phoneNotificationPackage, CancellationToken.None);

            // Assert
            _translatorServiceMock.Verify(x => x.TranslateText(It.IsAny<string>(), "es", "en"), Times.Once);
            _twillioServiceMock.Verify(x => x.MakeCall(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(1));
            _notificationLogService.Verify(x => x.InsertNoticationLog(It.IsAny<NotificationLog>()), Times.Exactly(1));
            _delayProviderMock.Verify(x => x.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
        }
   

        [Test]
        public async Task CallPhones_DoesNotTranslateText_WhenUserLanguageIsSameAsSystemBaseLanguage()
        {
            // Arrange
            var phoneNotificationPackage = new PhoneNotificationPackage
            {
                SystemBaseLanguage = "en",
                BaseText = "Hello, this is a test notification.",
                PhoneContacts = new List<PhoneContact>
                      {
                        new PhoneContact { UserName = "Phillip", PhoneNumber = "+4534567890", Language = "en" }
                      }
            };

            _translatorServiceMock
                .Setup(x => x.TranslateText(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("Hola, esta es una notificación de prueba.");


            _speechServiceMock
             .Setup(x => x.GenerateTextToSpeechAudio(It.IsAny<GenerateSpeechConfiguration>()))
             .ReturnsAsync(() => new MemoryStreamResult(Encoding.UTF8.GetBytes("Dummy audio content")));


            _blobStorageServiceMock
                .Setup(x => x.UploadAudioToAzureBlob(It.IsAny<MemoryStreamResult>(), It.IsAny<string>()))
                .ReturnsAsync("https://test.blob.core.windows.net/audio/Phillip.mp3");

            _delayProviderMock
                .Setup(x => x.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _twillioServiceMock
              .Setup(x => x.MakeCall(It.IsAny<string>(), It.IsAny<string>()))
              .Returns(() => new CustomCallResource { });

            _notificationLogService
                .Setup(x => x.InsertNoticationLog(It.IsAny<NotificationLog>()))
                .Returns(Task.FromResult(true));

            _cogntiveVoiceProviderMock
               .Setup(x => x.GetAzureCognitiveVoiceByGender(It.IsAny<string>(), It.IsAny<int>()))
               .Returns("dummy-female-voice");

            _cogntiveVoiceProviderMock
                .Setup(x => x.GetAzureCognitiveVoiceRate(It.IsAny<string>()))
                .Returns("0.8");

            _cogntiveVoiceProviderMock
                .Setup(x => x.GetAzureCognitiveLanguage(It.IsAny<string>()))
                .Returns("es-ES");

            // Act
            await _phoneProcessor.CallPhones(phoneNotificationPackage, CancellationToken.None);

            // Assert
            _translatorServiceMock.Verify(x => x.TranslateText(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _twillioServiceMock.Verify(x => x.MakeCall(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(1));
            _notificationLogService.Verify(x => x.InsertNoticationLog(It.IsAny<NotificationLog>()), Times.Exactly(1));
            _delayProviderMock.Verify(x => x.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [Test]
        public async Task ReCallFailedBatches_CallsFailedPhoneContacts_AndReturnsCorrectResult()
        {
            // Arrange
            var failedPhoneContacts = new List<LogPhoneContact>
                    {
                        new LogPhoneContact
                        {
                            UserId = "1",
                            PhoneNumber = "+1234567890",
                            AzureBlobUrl = "https://test.blob.core.windows.net/audio/1.mp3",
                            AlarmMessage = "Failed alarm message"
                        },
                        new LogPhoneContact
                        {
                            UserId = "2",
                            PhoneNumber = "+2345678901",
                            AzureBlobUrl = "https://test.blob.core.windows.net/audio/2.mp3",
                            AlarmMessage = "Failed alarm message"
                        }
                    };

            var batchNotificationPackage = new BatchNotificationPackage
            {
                BatchAlarmId = 1,
                GenderId = 1,
                RoundRobinInterval = 10000000,
                PhoneContacts = failedPhoneContacts
            };

            _twillioServiceMock
                .Setup(x => x.MakeCall(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => new CustomCallResource { });

            _notificationLogService
                .Setup(x => x.InsertNoticationLog(It.IsAny<NotificationLog>()))
                .Returns(Task.FromResult(true));

            _delayProviderMock
                .Setup(x => x.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _cogntiveVoiceProviderMock
               .Setup(x => x.GetAzureCognitiveVoiceByGender(It.IsAny<string>(), It.IsAny<int>()))
               .Returns("dummy-female-voice");

            _cogntiveVoiceProviderMock
                .Setup(x => x.GetAzureCognitiveVoiceRate(It.IsAny<string>()))
                .Returns("0.8");

            _cogntiveVoiceProviderMock
                .Setup(x => x.GetAzureCognitiveLanguage(It.IsAny<string>()))
                .Returns("es-ES");

            // Act
            var (callCount, callTimestamps) = await _phoneProcessor.CallPhones(batchNotificationPackage, CancellationToken.None);
            var expected = 2;

            // Assert
            callCount.Should().Be(expected);
            callTimestamps.Count.Should().Be(expected);
            _twillioServiceMock.Verify(x => x.MakeCall(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
            _notificationLogService.Verify(x => x.InsertNoticationLog(It.IsAny<NotificationLog>()), Times.Exactly(2));
            _delayProviderMock.Verify(x => x.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }
    }
}
