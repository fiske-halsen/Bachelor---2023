using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EMSuite.PhoneNotification.Exceptions;
using EMSuite.PhoneNotification.Models;

namespace EMSuite.PhoneNotification.Services
{
    public interface IAzureBlobStorageService
    {
        Task<string> UploadAudioToAzureBlob(MemoryStreamResult memoryStreamWrapper, string blobName);
    }

    public class AzureBlobStorageService : IAzureBlobStorageService
    {
        private readonly IBlobServiceClient _blobServiceClient;
        private readonly IConfiguration _configuration;

        public AzureBlobStorageService(IConfiguration configuration, IBlobServiceClient blobServiceClient)
        {
            _configuration = configuration;
            _blobServiceClient = blobServiceClient;
        }

        public async Task<string> UploadAudioToAzureBlob(MemoryStreamResult memoryStreamWrapper, string blobName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_configuration["BlobStorage:ContainerStorageName"]);

                await containerClient.CreateIfNotExistsAsync();
                await containerClient.SetAccessPolicyAsync(PublicAccessType.Blob);

                var blobClient = containerClient.GetBlobClient(blobName);

                BlobUploadOptions uploadOptions = new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = "audio/mpeg",
                    }
                };

                await blobClient.UploadAsync(memoryStreamWrapper, uploadOptions);

                return blobClient.Uri.AbsoluteUri;
            }
            catch (Exception ex)
            {
                throw new CustomException("Error uploading audio to Azure Blob.", ex);
            }
        }
    }
}
