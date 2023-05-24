using Azure.Storage.Blobs.Models;
using EMSuite.PhoneNotification.Exceptions;
using EMSuite.PhoneNotification.Models;
using EMSuite.PhoneNotification.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace PhoneNotificationService.Tests.UnitTests
{
    [TestFixture]
    public class AzureBlobStorageServiceUnitTests
    {

        private Mock<IBlobServiceClient> _blobServiceClientMock;
        private IConfiguration _configuration;
        private Mock<IBlobContainerClientWrapper> _blobContainerClientMock;
        private Mock<IBlobClientWrapper> _blobClientMock;
        private IAzureBlobStorageService _azureBlobStorageService;

        [SetUp]
        public void SetUp()
        {
            _blobServiceClientMock = new Mock<IBlobServiceClient>();
            _configuration = Mock.Of<IConfiguration>(cfg =>
                cfg["BlobStorage:ContainerStorageName"] == "test-container" &&
                cfg["BlobStorage:AzureBlobConnectionString"] == "test-connection-string");

            _blobContainerClientMock = new Mock<IBlobContainerClientWrapper>();
            _blobClientMock = new Mock<IBlobClientWrapper>();

            _azureBlobStorageService = new AzureBlobStorageService(_configuration, _blobServiceClientMock.Object);
        }

        [Test]
        public async Task UploadAudioToAzureBlob_UploadsMemoryStreamToBlobStorage()
        {
            // Arrange
            var memoryStreamWrapper = new MemoryStreamResult();
            string blobName = "test-blob";

            _blobServiceClientMock.Setup(x => x.GetBlobContainerClient("test-container")).Returns(_blobContainerClientMock.Object);
            _blobContainerClientMock.Setup(x => x.CreateIfNotExistsAsync()).Returns(Task.FromResult(true));
            _blobContainerClientMock.Setup(x => x.SetAccessPolicyAsync(PublicAccessType.Blob)).Returns(Task.FromResult(true));
            _blobContainerClientMock.Setup(x => x.GetBlobClient(blobName)).Returns(_blobClientMock.Object);
            _blobClientMock.Setup(x => x.UploadAsync(memoryStreamWrapper, It.IsAny<BlobUploadOptions>())).Returns(Task.FromResult(true));
            _blobClientMock.SetupGet(x => x.Uri).Returns(new Uri("https://example.com/test-container/test-blob"));

            // Act
            var result = await _azureBlobStorageService.UploadAudioToAzureBlob(memoryStreamWrapper, blobName);
            var expectedResult = "https://example.com/test-container/test-blob";

            // Assert

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedResult);
            _blobServiceClientMock.Verify(x => x.GetBlobContainerClient("test-container"), Times.Once);
            _blobContainerClientMock.Verify(x => x.CreateIfNotExistsAsync(), Times.Once);
            _blobContainerClientMock.Verify(x => x.SetAccessPolicyAsync(PublicAccessType.Blob), Times.Once);
            _blobContainerClientMock.Verify(x => x.GetBlobClient(blobName), Times.Once);
            _blobClientMock.Verify(x => x.UploadAsync(memoryStreamWrapper, It.IsAny<BlobUploadOptions>()), Times.Once);
        }


        [Test]
        public void UploadAudioToAzureBlob_ThrowsCustomException_WhenUploadFails()
        {
            // Arrange
            var memoryStreamWrapper = new MemoryStreamResult();
            string blobName = "test-blob";

            _blobServiceClientMock.Setup(x => x.GetBlobContainerClient("test-container")).Returns(_blobContainerClientMock.Object);
            _blobContainerClientMock.Setup(x => x.CreateIfNotExistsAsync()).Returns(Task.FromResult(true));
            _blobContainerClientMock.Setup(x => x.SetAccessPolicyAsync(PublicAccessType.Blob)).Returns(Task.FromResult(true));
            _blobContainerClientMock.Setup(x => x.GetBlobClient(blobName)).Returns(_blobClientMock.Object);
            _blobClientMock.Setup(x => x.UploadAsync(memoryStreamWrapper, It.IsAny<BlobUploadOptions>())).ThrowsAsync(new Exception("Upload failed"));

            var expectedErrorMessage = "Error uploading audio to Azure Blob.";

            // Act and Assert
            var ex = Assert.ThrowsAsync<CustomException>(async () => await _azureBlobStorageService.UploadAudioToAzureBlob(memoryStreamWrapper, blobName));
            ex.Message.Should().Be(expectedErrorMessage);
        }
    }
}
