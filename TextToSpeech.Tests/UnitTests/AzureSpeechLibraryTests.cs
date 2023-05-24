using AzureCognitivePrototype;
using FluentAssertions;
using Moq;

namespace TextToSpeech.Tests.UnitTests
{

    [TestFixture]
    public class AzureSpeechLibraryTests
    {
        private Mock<ISpeechSynthesizerWrapper> _speechSynthesizerWrapperMock;
        private Mock<IBlobServiceClientWrapper> _blobServiceClientWrapperMock;
        private Mock<IBlobContainerClientWrapper> _blobContainerClientWrapperMock;
        private Mock<IBlobClientWrapper> _blobClientWrapperMock;
        private Mock<ISpeechSynthesisResultWrapper> _synthesisResultWrapperMock;
        private Mock<IAudioDataStream> _audioDataStream;
        private Mock<MemoryStreamWrapper> _audioDataStreamProcessorMock;
        private AzureSpeechLibrary _azureSpeechLibrary;

        [SetUp]
        public void SetUp()
        {
            _speechSynthesizerWrapperMock = new Mock<ISpeechSynthesizerWrapper>();
            _blobServiceClientWrapperMock = new Mock<IBlobServiceClientWrapper>();
            _blobContainerClientWrapperMock = new Mock<IBlobContainerClientWrapper>();
            _blobClientWrapperMock = new Mock<IBlobClientWrapper>();
            _synthesisResultWrapperMock = new Mock<ISpeechSynthesisResultWrapper>();
            _audioDataStream = new Mock<IAudioDataStream>();
            _audioDataStreamProcessorMock = new Mock<MemoryStreamWrapper>();
            _azureSpeechLibrary = new AzureSpeechLibrary(_speechSynthesizerWrapperMock.Object, _blobServiceClientWrapperMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _azureSpeechLibrary.Dispose();
        }

        [Test]
        public async Task GenerateTextToSpeechAudio_ReturnsExpectedAudioDataStream()
        {
            // Arrange
            string language = "en-US";
            string voiceName = "Microsoft.ServerSpeech.TextToSpeech.Voice.en-US.Guy24KRUS";
            string text = "Test text";

            string actualSsml = null;
            _speechSynthesizerWrapperMock
                .Setup(x => x.SpeakSsml(It.IsAny<string>()))
                .Callback<string>(s => actualSsml = s)
                .ReturnsAsync(_synthesisResultWrapperMock.Object);

            _synthesisResultWrapperMock.Setup(x => x.AudioDataStream).Returns(_audioDataStream.Object);

            // Act
            var result = await _azureSpeechLibrary.GenerateTextToSpeechAudio(text, "+0st", "0.8", "loud", voiceName);

            // Assert
            result.Should().NotBeNull();
            actualSsml.Should().NotBeNullOrEmpty();
            actualSsml.Should().Contain(text);
            actualSsml.Should().Contain(voiceName);
            actualSsml.Should().Contain("+0st");
            actualSsml.Should().Contain("Test text");
            actualSsml.Should().Contain("Microsoft.ServerSpeech.TextToSpeech.Voice.en-US.Guy24KRUS");
            actualSsml.Should().Contain("0.8");
            actualSsml.Should().Contain("loud");
            _synthesisResultWrapperMock.Verify(x => x.AudioDataStream, Times.Once);
        }


        [Test]
        public async Task UploadAudioToAzureBlob_ReturnsExpectedUri()
        {
            // Arrange
            string blobName = "test.mp3";
            string expectedUri = "https://example.com/test.mp3";

            _blobServiceClientWrapperMock.Setup(x => x.GetBlobContainerClient(It.IsAny<string>())).Returns(_blobContainerClientWrapperMock.Object);
            _blobContainerClientWrapperMock.Setup(x => x.CreateIfNotExistsAsync()).Returns(Task.CompletedTask);
            _blobContainerClientWrapperMock.Setup(x => x.SetAccessPolicyAsync(It.IsAny<Azure.Storage.Blobs.Models.PublicAccessType>())).Returns(Task.CompletedTask);
            _blobContainerClientWrapperMock.Setup(x => x.GetBlobClient(blobName)).Returns(_blobClientWrapperMock.Object);
            _blobClientWrapperMock.Setup(x => x.UploadAsync(_audioDataStreamProcessorMock.Object, true)).Returns(Task.CompletedTask);
            _blobClientWrapperMock.Setup(x => x.Uri).Returns(new Uri(expectedUri));

            // Act
            var result = await _azureSpeechLibrary.UploadAudioToAzureBlob(_audioDataStreamProcessorMock.Object, blobName);

            // Assert
            result.Should().NotBeNull();
            _blobServiceClientWrapperMock.Verify(x => x.GetBlobContainerClient(It.IsAny<string>()), Times.Once);
            _blobContainerClientWrapperMock.Verify(x => x.CreateIfNotExistsAsync(), Times.Once);
            _blobContainerClientWrapperMock.Verify(x => x.SetAccessPolicyAsync(It.IsAny<Azure.Storage.Blobs.Models.PublicAccessType>()), Times.Once);
            _blobContainerClientWrapperMock.Verify(x => x.GetBlobClient(blobName), Times.Once);
            _blobClientWrapperMock.Verify(x => x.UploadAsync(_audioDataStreamProcessorMock.Object, true), Times.Once);
        }

        [Test]
        public async Task GetAvailableVoices_ReturnsExpectedVoiceList()
        {
            // Arrange
            var expectedVoices = new List<string>
            {
                "Voice1",
                "Voice2",
                "Voice3"
            };
            _speechSynthesizerWrapperMock.Setup(x => x.GetVoices()).ReturnsAsync(expectedVoices);

            // Act
            var result = await _azureSpeechLibrary.GetAvailableVoices();

            // Assert
            result.Should().NotBeNull()
                  .And.HaveCount(expectedVoices.Count)
                  .And.BeEquivalentTo(expectedVoices);
            _speechSynthesizerWrapperMock.Verify(x => x.GetVoices(), Times.Once);
        }

        [Test]
        public async Task GetAvailableLanguages_ReturnsExpectedLanguageList()
        {
            // Arrange
            var languages = new List<string>
                {
                "en-US",
                "fr-FR",
                "en-GB"
                };

            _speechSynthesizerWrapperMock.Setup(x => x.GetLanguages()).ReturnsAsync(languages);

            // Act
            var result = await _azureSpeechLibrary.GetAvailableLanguages();

            // Assert
            result.Should().HaveCount(languages.Count);
            result.Should().Contain(languages);
            _speechSynthesizerWrapperMock.Verify(x => x.GetLanguages(), Times.Once);
        }
    }
}
