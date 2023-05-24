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
    public class UploadToAzureBlobIntegrationTests
    {
        IBlobServiceClient _blobServiceClient;
        IAzureBlobStorageService _blobStorageService;

        [SetUp]
        public void Setup()
        {
            IConfiguration configuration = GetTestConfiguration();
            _blobServiceClient = new BlobServiceClientWrapper(configuration["BlobStorage:AzureBlobConnectionString"]);
            _blobStorageService = new AzureBlobStorageService(configuration, _blobServiceClient);
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
        public async Task UploadAudioToAzureBlob_UploadsAndReturnsUri_Success()
        {
            // Arrange
            byte[] testAudioContent = GenerateDummyAudioContent();
            MemoryStreamResult memoryStreamWrapper = new MemoryStreamResult(testAudioContent);
            string blobName = $"test-audio-{Guid.NewGuid()}.mp3";

            // Act
            string result = await _blobStorageService.UploadAudioToAzureBlob(memoryStreamWrapper, blobName);
            var uploadedBlobs = await _blobServiceClient.GetBlobContainerClient("audiofilecontainertest").GetBlobItems();
            var uploadedBlob = uploadedBlobs.FirstOrDefault(blob => blob.Name == blobName);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain(blobName);
            uploadedBlob.Should().NotBeNull();
            blobName.Should().Be(uploadedBlob.Name);
        }

        private byte[] GenerateDummyAudioContent()
        {
            var random = new Random();
            byte[] dummyContent = new byte[1024];
            random.NextBytes(dummyContent);
            return dummyContent;
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
