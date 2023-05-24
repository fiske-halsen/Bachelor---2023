using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AzureCognitivePrototype
{
    public interface IBlobClientWrapper
    {
        Task UploadAsync(Stream stream, bool overwrite);
        Uri Uri { get; }
    }

    public class BlobClientWrapper : IBlobClientWrapper
    {
        private readonly BlobClient _blobClient;

        public BlobClientWrapper(BlobClient blobClient)
        {
            _blobClient = blobClient;
        }

        public async Task UploadAsync(Stream stream, bool overwrite)
        {
            BlobUploadOptions uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "audio/mpeg",
                }
            };

            await _blobClient.UploadAsync(stream, uploadOptions);
        }

        public Uri Uri => _blobClient.Uri;
    }
}
