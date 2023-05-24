using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using EMSuite.PhoneNotification.Models;

namespace EMSuite.PhoneNotification.Services
{
    public interface IBlobClientWrapper
    {
        Task UploadAsync(MemoryStreamResult memoryStreamResult, BlobUploadOptions uploadOptions);
        Uri Uri { get; }
    }

    public class BlobClientWrapper : IBlobClientWrapper
    {
        private readonly BlobClient _blobClient;

        public BlobClientWrapper(BlobClient blobClient)
        {
            _blobClient = blobClient;
        }

        public async Task UploadAsync(MemoryStreamResult memoryStreamWrapper, BlobUploadOptions uploadOptions)
        {
            await _blobClient.UploadAsync(memoryStreamWrapper, uploadOptions);
        }

        public Uri Uri => _blobClient.Uri;
    }
}
