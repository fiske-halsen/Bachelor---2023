using Azure.Storage.Blobs;

namespace AzureCognitivePrototype
{

    public interface IBlobServiceClientWrapper
    {
        IBlobContainerClientWrapper GetBlobContainerClient(string containerName);
    }

    public class BlobServiceClientWrapper : IBlobServiceClientWrapper
    {
        private readonly BlobServiceClient _blobServiceClient;

        public BlobServiceClientWrapper(string connectionString)
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public IBlobContainerClientWrapper GetBlobContainerClient(string containerName)
        {
            return new BlobContainerClientWrapper(_blobServiceClient.GetBlobContainerClient(containerName));
        }
    }
}
