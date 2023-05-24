using Azure.Storage.Blobs;

namespace EMSuite.PhoneNotification.Services
{
    public interface IBlobServiceClient
    {
        IBlobContainerClientWrapper GetBlobContainerClient(string containerName);
    }

    public class BlobServiceClientWrapper : IBlobServiceClient
    {
        private readonly BlobServiceClient _blobServiceClient;

        public BlobServiceClientWrapper(string connectionString)
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public IBlobContainerClientWrapper GetBlobContainerClient(string containerName)
        {
            var blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            return new BlobContainerClientWrapper(blobContainerClient);
        }
    }
}
