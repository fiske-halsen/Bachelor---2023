using EMSuite.PhoneNotification.Exceptions;
using EMSuite.PhoneNotification.Models;
using EMSuite.PhoneNotification.Services;
using FluentAssertions;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using Moq;

namespace PhoneNotificationService.Tests.UnitTests
{
    [TestFixture]
    public class AzureSpeechServiceUnitTests
    {
        private Mock<IConfiguration> _configurationMock;
        private Mock<ISpeechSynthesizer> _speechSynthesizerMock;
        private Mock<IMemoryStreamHandler> _memoryStreamHandlerMock;
        private AzureSpeechService _azureSpeechService;

        [SetUp]
        public void Setup()
        {
            _configurationMock = new Mock<IConfiguration>();
            _speechSynthesizerMock = new Mock<ISpeechSynthesizer>();
            _memoryStreamHandlerMock = new Mock<IMemoryStreamHandler>();

            _configurationMock.SetupGet(c => c["AzureCogniveService:ApiKey"]).Returns("api_key");
            _configurationMock.SetupGet(c => c["AzureCogniveService:Region"]).Returns("region");

            _azureSpeechService = new AzureSpeechService(
                _configurationMock.Object,
                _speechSynthesizerMock.Object,
                _memoryStreamHandlerMock.Object);
        }

        [TearDown]
        public void Teardown()
        {
            _azureSpeechService = null;
            _configurationMock = null;
            _speechSynthesizerMock = null;
            _memoryStreamHandlerMock = null;
        }

        [Test]
        public async Task GenerateTextToSpeechAudio_CallsSpeakSsmlAsyncWithGeneratedSSML()
        {
            // Arrange
            var generateSpeechConfiguration = new GenerateSpeechConfiguration
            {
                Language = "en-US",
                VoiceName = "en-US-GuyNeural",
                Text = "Hello, this is a test.",
                Pitch = "default",
                Rate = "default",
                Volume = "default"
            };

            _memoryStreamHandlerMock.Setup(x => x.GetMemoryStream(It.IsAny<AudioDataStream>())).Returns(new MemoryStreamResult());

            var expectedResult = new CustomSpeechSynthesisResult { };

            _speechSynthesizerMock
                .Setup(x => x.SpeakSsmlAsync(It.IsAny<string>(), It.IsAny<SpeechConfig>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _azureSpeechService.GenerateTextToSpeechAudio(generateSpeechConfiguration);

            // Assert
            _speechSynthesizerMock.Verify(x => x.SpeakSsmlAsync(It.IsAny<string>(), It.IsAny<SpeechConfig>()), Times.Once);
            _memoryStreamHandlerMock.Verify(x => x.GetMemoryStream(It.IsAny<AudioDataStream>()), Times.Once);
            result.Should().NotBeNull();
        }

        [Test]
        public void GenerateTextToSpeechAudio_ThrowsCustomException_WhenSpeakSsmlAsyncFails()
        {
            // Arrange
            var generateSpeechConfiguration = new GenerateSpeechConfiguration
            {
                Language = "en-US",
                VoiceName = "en-US-GuyNeural",
                Text = "Hello, this is a test.",
                Pitch = "default",
                Rate = "default",
                Volume = "default"
            };

            _speechSynthesizerMock
                .Setup(x => x.SpeakSsmlAsync(It.IsAny<string>(), It.IsAny<SpeechConfig>()))
                .ThrowsAsync(new Exception("Synthesis failed"));


            var expectedErrorMessage = "Failed to generate text-to-speech audio.";
            // Act and Assert
            var ex = Assert.ThrowsAsync<CustomException>(async () => await _azureSpeechService.GenerateTextToSpeechAudio(generateSpeechConfiguration));
            ex.Message.Should().Be(expectedErrorMessage);
        }


        [Test]
        public void GetAvailableLanguages_ThrowsCustomException_WhenGetLanguagesFails()
        {
            // Arrange
            _speechSynthesizerMock
                .Setup(x => x.GetVoicesAsync(It.IsAny<SpeechConfig>()))
                .ThrowsAsync(new Exception("Failed to get available languages."));

            var expectedErrorMessage = "Failed to get available languages.";

            // Act and Assert
            var ex = Assert.ThrowsAsync<CustomException>(async () => await _azureSpeechService.GetAvailableLanguages());
            ex.Message.Should().Be(expectedErrorMessage);
        }

        [Test]
        public void GetAvailableVoices_ThrowsCustomException_WhenGetVoicesFails()
        {
            // Arrange
            _speechSynthesizerMock
                .Setup(x => x.GetVoicesAsync(It.IsAny<SpeechConfig>()))
                .ThrowsAsync(new Exception("Failed to get available voices."));

            var expectedErrorMessage = "Failed to get available voices.";

            // Act and Assert
            var ex = Assert.ThrowsAsync<CustomException>(async () => await _azureSpeechService.GetAvailableVoices());
            ex.Message.Should().Be(expectedErrorMessage);
        }

        [Test]
        public async Task GenerateTextToSpeechAudio_GeneratesExpectedSSML()
        {
            // Arrange
            var generateSpeechConfiguration = new GenerateSpeechConfiguration
            {
                Language = "en-US",
                VoiceName = "en-US-GuyNeural",
                Text = "Hello, this is a test.",
                Pitch = "default",
                Rate = "default",
                Volume = "default"
            };

            string expectedSSML = $"" +
                $"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' " +
                $"xml:lang='{generateSpeechConfiguration.Language}'><voice name='{generateSpeechConfiguration.VoiceName}'><prosody pitch='{generateSpeechConfiguration.Pitch}' rate='{generateSpeechConfiguration.Rate}' volume='{generateSpeechConfiguration.Volume}'>{generateSpeechConfiguration.Text}" +
                $"</prosody></voice></speak>";

            _memoryStreamHandlerMock.Setup(x => x.GetMemoryStream(It.IsAny<AudioDataStream>())).Returns(new MemoryStreamResult());

            var expectedResult = new CustomSpeechSynthesisResult { };

            _speechSynthesizerMock
                .Setup(x => x.SpeakSsmlAsync(It.IsAny<string>(), It.IsAny<SpeechConfig>()))
                .Callback<string, SpeechConfig>((ssml, config) =>
                {
                    ssml.Should().Be(expectedSSML);
                })
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _azureSpeechService.GenerateTextToSpeechAudio(generateSpeechConfiguration);

            // Assert
            _speechSynthesizerMock.Verify(x => x.SpeakSsmlAsync(It.IsAny<string>(), It.IsAny<SpeechConfig>()), Times.Once);
            _memoryStreamHandlerMock.Verify(x => x.GetMemoryStream(It.IsAny<AudioDataStream>()), Times.Once);
            result.Should().NotBeNull();
        }

        [Test]
        public async Task GetAvailableVoices_ReturnsExpectedVoices()
        {
            // Arrange
            var voices = new List<VoiceInfoResult>
            {
                new VoiceInfoResult { Name = "Voice1", Locale = "en-US" },
                new VoiceInfoResult { Name = "Voice2", Locale = "en-GB" }
            };

            var customSynthesizerVoiceResult = new CustomSynthesizerVoiceResult
            {
                Voices = voices
            };

            _speechSynthesizerMock.Setup(x => x.GetVoicesAsync(It.IsAny<SpeechConfig>())).ReturnsAsync(customSynthesizerVoiceResult);

            // Act
            var result = await _azureSpeechService.GetAvailableVoices();

            // Assert
            result.Should().BeEquivalentTo(voices.Select(voice => voice.Name));
        }

        [Test]
        public async Task GetAvailableLanguages_ReturnsExpectedLanguages()
        {
            // Arrange
            var voices = new List<VoiceInfoResult>
            {
                new VoiceInfoResult { Name = "Voice1", Locale = "en-US" },
                new VoiceInfoResult { Name = "Voice2", Locale = "en-GB" },
                new VoiceInfoResult { Name = "Voice3", Locale = "en-US" }
            };

            var customSynthesizerVoiceResult = new CustomSynthesizerVoiceResult
            {
                Voices = voices
            };

            _speechSynthesizerMock.Setup(x => x.GetVoicesAsync(It.IsAny<SpeechConfig>())).ReturnsAsync(customSynthesizerVoiceResult);

            // Act
            var result = await _azureSpeechService.GetAvailableLanguages();

            // Assert
            result.Should().BeEquivalentTo(voices.Select(voice => voice.Locale).Distinct());
        }
    }
}
