using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AzureCognitivePrototype
{
    public interface IBlobContainerClientWrapper
    {
        Task CreateIfNotExistsAsync();
        Task SetAccessPolicyAsync(PublicAccessType accessType);
        IBlobClientWrapper GetBlobClient(string blobName);
    }

    public class BlobContainerClientWrapper : IBlobContainerClientWrapper
    {
        private readonly BlobContainerClient _containerClient;

        public BlobContainerClientWrapper(BlobContainerClient containerClient)
        {
            _containerClient = containerClient;
        }

        public async Task CreateIfNotExistsAsync()
        {
            await _containerClient.CreateIfNotExistsAsync();
        }

        public async Task SetAccessPolicyAsync(PublicAccessType accessType)
        {
            await _containerClient.SetAccessPolicyAsync(accessType);
        }

        public IBlobClientWrapper GetBlobClient(string blobName)
        {
            return new BlobClientWrapper(_containerClient.GetBlobClient(blobName));
        }
    }
}
