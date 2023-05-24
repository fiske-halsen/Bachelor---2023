using AzureCognitivePrototype;
using FluentAssertions;
using Microsoft.CognitiveServices.Speech;

namespace TextToSpeech.Tests.IntegrationTests
{

    [TestFixture]
    public class AzureSpeechLibraryIntegrationTests
    {
        private IAzureSpeechLibrary _azureSpeechLibrary;

        private static readonly string azure_api_key = "7de2c1fecaf441b0a340ebdf24a36b36";
        private static readonly string azure_region = "westeurope";
        private static readonly string azure_container_storage_connection_string = "DefaultEndpointsProtocol=https;AccountName=satexttospeech;AccountKey=GW/U21VRyWyOGaZJGrSQpj2sL/215Vpp+uKsKCZNR0TqlWZwh0v2KQnEVvb8z1c6a2y9bncn7SI9+AStP/w9HQ==;EndpointSuffix=core.windows.net";

        [SetUp]
        public void SetUp() { }

        [TearDown]
        public void TearDown()
        {
            _azureSpeechLibrary.Dispose();
        }

        [Test]
        public async Task TestGenerateTextToSpeechAudio()
        {
            // Arrange
            var speechConfig = SpeechConfig.FromSubscription(azure_api_key, azure_region);
            var blobServiceClientWrapper = new BlobServiceClientWrapper(azure_container_storage_connection_string);
            var speechSynthesizerWrapper = new SpeechSynthesizerWrapper(speechConfig);
            _azureSpeechLibrary = new AzureSpeechLibrary(speechSynthesizerWrapper, blobServiceClientWrapper);

            var text = "Hello my name is phillip";
            var voiceName = "en-US-GuyNeural";

            //Act
            var audioDataStream = await _azureSpeechLibrary.GenerateTextToSpeechAudio(text, "+0st", "0.8", "loud", voiceName);

            //Assert
            Assert.IsNotNull(audioDataStream);
        }

        [Test]
        public async Task TestUploadAudioToAzureBlob()
        {
            //Arrange
            var text = "Bu dünyadaki her şeyi seviyorum. Bir alarm tetiklendi. Lütfen farkında olun ve harekete geçin.";
            var blobName = "TilTester.mp3";
            var language = "tr-TR";
            var voiceName = "tr-TR-EmelNeural"; 

            var speechConfig = SpeechConfig.FromSubscription(azure_api_key, azure_region);
            speechConfig.SpeechSynthesisLanguage = language;
            speechConfig.SpeechSynthesisVoiceName = voiceName;
            speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz64KBitRateMonoMp3);

            var blobServiceClientWrapper = new BlobServiceClientWrapper(azure_container_storage_connection_string);
            var speechSynthesizerWrapper = new SpeechSynthesizerWrapper(speechConfig);

            _azureSpeechLibrary = new AzureSpeechLibrary(speechSynthesizerWrapper, blobServiceClientWrapper);

            var audioDataStream = await _azureSpeechLibrary.GenerateTextToSpeechAudio(text, "-0st", "0.85", "medium", voiceName);

            using var audioDataStreamWrapper = new MemoryStreamWrapper(audioDataStream.AudioDataStream);

            //Act
            var uploadedUrl = await _azureSpeechLibrary.UploadAudioToAzureBlob(audioDataStreamWrapper, blobName);

            //Assert
            uploadedUrl.Should().NotBeNullOrEmpty();
            uploadedUrl.Should().Contain(blobName);
        }


        [Test]
        public async Task TestGetAvailableVoices()
        {
            // Arrange
            var speechConfig = SpeechConfig.FromSubscription(azure_api_key, azure_region);

            var blobServiceClientWrapper = new BlobServiceClientWrapper(azure_container_storage_connection_string);
            var speechSynthesizerWrapper = new SpeechSynthesizerWrapper(speechConfig);

            _azureSpeechLibrary = new AzureSpeechLibrary(speechSynthesizerWrapper, blobServiceClientWrapper);

            // Act
            var voices = await _azureSpeechLibrary.GetAvailableVoices();

            // Assert
            voices.Should().NotBeNull().And.HaveCountGreaterThan(0);
        }

        [Test]
        public async Task TestGetAvailableLanguages()
        {
            // Arrange
            var speechConfig = SpeechConfig.FromSubscription(azure_api_key, azure_region);

            var blobServiceClientWrapper = new BlobServiceClientWrapper(azure_container_storage_connection_string);
            var speechSynthesizerWrapper = new SpeechSynthesizerWrapper(speechConfig);

            _azureSpeechLibrary = new AzureSpeechLibrary(speechSynthesizerWrapper, blobServiceClientWrapper);

            // Act
            var languages = await _azureSpeechLibrary.GetAvailableLanguages();

            // Assert
            languages.Should().NotBeNull().And.HaveCountGreaterThan(0);
        }
    }
}
