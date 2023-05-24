using EMSuite.PhoneNotification.Models;
using EMSuite.PhoneNotification.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhoneNotificationService.Tests.IntegrationTests
{
    [TestFixture]
    public class AzureSpeechIntegrationTests
    {

        IAzureSpeechService _azureSpeechService;

        [SetUp]
        public void Setup()
        {
            IConfiguration configuration = GetTestConfiguration();
            ISpeechSynthesizer speechSynthesizer = new SpeechSynthesizerHandler();
            IMemoryStreamHandler memoryStreamHandler = new MemoryStreamHandler();
            _azureSpeechService = new AzureSpeechService(configuration, speechSynthesizer, memoryStreamHandler);
        }

        [Test]
        public async Task GenerateTextToSpeechAudio_ReturnsAudio_Success()
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

            // Act
            var result = await _azureSpeechService.GenerateTextToSpeechAudio(generateSpeechConfiguration);

            // Assert
            result.Should().NotBeNull();
            result.Length.Should().BeGreaterThan(0);
        }

        [Test]
        public async Task GetAvailableVoices_ReturnsVoices_Success()
        {
            // Act
            var result = await _azureSpeechService.GetAvailableVoices();

            // Assert
            result.Should().NotBeNull();
            result.Count().Should().BeGreaterThan(0);
        }

        [Test]
        public async Task GetAvailableLanguages_ReturnsLanguages_Success()
        {
            // Act
            var result = await _azureSpeechService.GetAvailableLanguages();

            // Assert
            result.Should().NotBeNull();
            result.Count().Should().BeGreaterThan(0);
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
