using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Azure;

namespace EMSuite.PhoneNotification.Services
{
    public interface IBlobContainerClientWrapper
    {
        Task CreateIfNotExistsAsync();
        Task SetAccessPolicyAsync(PublicAccessType accessType);
        Task DeleteBlob(string blobName);
        Task<List<BlobItem>> GetBlobItems();
        IBlobClientWrapper GetBlobClient(string blobName);
    }

    public class BlobContainerClientWrapper : IBlobContainerClientWrapper
    {
        private readonly BlobContainerClient _blobContainerClient;

        public BlobContainerClientWrapper(BlobContainerClient blobContainerClient)
        {
            _blobContainerClient = blobContainerClient;
        }

        public async Task CreateIfNotExistsAsync()
        {
            await _blobContainerClient.CreateIfNotExistsAsync();
        }

        public async Task SetAccessPolicyAsync(PublicAccessType accessType)
        {
            await _blobContainerClient.SetAccessPolicyAsync(accessType);
        }

        public IBlobClientWrapper GetBlobClient(string blobName)
        {
            return new BlobClientWrapper(_blobContainerClient.GetBlobClient(blobName));
        }

        public async Task<List<BlobItem>> GetBlobItems()
        {
            List<BlobItem> blobs = new List<BlobItem>();
            await foreach (BlobItem blob in _blobContainerClient.GetBlobsAsync())
            {
                blobs.Add(blob);
            }
            return blobs;
        }

        public async Task DeleteBlob(string blobName)
        {
          await _blobContainerClient.DeleteBlobAsync(blobName);
        }
    }
}
