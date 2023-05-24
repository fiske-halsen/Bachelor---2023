using Azure.Storage.Blobs.Models;
using System.Text;

namespace AzureCognitivePrototype
{
    public interface IAzureSpeechLibrary : IDisposable
    {
        Task<IAudioDataStream> GenerateTextToSpeechAudio(string text, string pitch, string rate, string volume, string voiceName);
        Task<string> UploadAudioToAzureBlob(MemoryStreamWrapper memoryStreamWrapper, string blobName);
        Task<List<string>> GetAvailableVoices();
        Task<List<string>> GetAvailableLanguages();
    }

    public class AzureSpeechLibrary : IAzureSpeechLibrary
    {
        private readonly ISpeechSynthesizerWrapper _speechSynthesizerWrapper;
        private readonly IBlobServiceClientWrapper _blobServiceClientWrapper;

        private readonly static string azure_container_storage_name = "audiofilecontainer";

        public AzureSpeechLibrary(
            ISpeechSynthesizerWrapper speechSynthesizerWrapper,
            IBlobServiceClientWrapper blobServiceClientWrapper)
        {
            _speechSynthesizerWrapper = speechSynthesizerWrapper;
            _blobServiceClientWrapper = blobServiceClientWrapper;
        }

        public async Task<IAudioDataStream> GenerateTextToSpeechAudio(string text, string pitch, string rate, string volume, string voiceName)
        {
            var ssml = GenerateSSML(text, pitch, rate, volume, voiceName);
            var result = await _speechSynthesizerWrapper.SpeakSsml(ssml);
            return result.AudioDataStream;
        }

        public async Task<string> UploadAudioToAzureBlob(MemoryStreamWrapper memoryStreamWrapper, string blobName)
        {
            var containerClient = _blobServiceClientWrapper.GetBlobContainerClient(azure_container_storage_name);

            await containerClient.CreateIfNotExistsAsync();
            await containerClient.SetAccessPolicyAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob);

            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.UploadAsync(memoryStreamWrapper, overwrite: true);

            return blobClient.Uri.AbsoluteUri;
        }

        private string GenerateSSML(string text, string pitch, string rate, string volume, string voiceName)
        {
            var sb = new StringBuilder();
            sb.Append("<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>");
            sb.Append($"<voice name='{voiceName}'>");
            sb.Append($"<prosody pitch='{pitch}' rate='{rate}' volume='{volume}'>{text}</prosody>");
            sb.Append("</voice></speak>");
            return sb.ToString();
        }

        public async Task<List<string>> GetAvailableVoices()
        {
            return await _speechSynthesizerWrapper.GetVoices();
        }

        public async Task<List<string>> GetAvailableLanguages()
        {
            return await _speechSynthesizerWrapper.GetLanguages();
        }

        public void Dispose()
        {
            _speechSynthesizerWrapper.Dispose();
        }
    }

    //public class AzureSpeechLibrary : IAzureSpeechLibrary
    //{
    //    private readonly static string azure_api_key = "xxx";
    //    private readonly static string azure_region = "northeurope";
    //    private readonly static string azure_container_storage_name = "audiofilecontainer";
    //    private readonly static string azure_container_storage_connection_string = "xxx";

    //    public async Task<AudioDataStream> GenerateTextToSpeechAudio(string ssml, string language, string voiceName)
    //    {
    //        var speechConfig = SpeechConfig.FromSubscription(azure_api_key, azure_region);
    //        speechConfig.SpeechSynthesisLanguage = language;
    //        speechConfig.SpeechSynthesisVoiceName = voiceName;
    //        speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz32KBitRateMonoMp3);

    //        using var synthesizer = new SpeechSynthesizer(speechConfig, null);
    //        var result = await synthesizer.SpeakSsmlAsync(ssml);

    //        var stream = AudioDataStream.FromResult(result);

    //        return stream;
    //    }

    //    public async Task<string> UploadAudioToAzureBlob(AudioDataStreamWrapper audioDataStreamWrapper, string blobName)
    //    {
    //        var blobServiceClient = new BlobServiceClient(azure_container_storage_connection_string);
    //        var containerClient = blobServiceClient.GetBlobContainerClient(azure_container_storage_name);

    //        // Create the container if it doesn't exist and set the access level to public
    //        await containerClient.CreateIfNotExistsAsync();
    //        await containerClient.SetAccessPolicyAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob);

    //        var blobClient = containerClient.GetBlobClient(blobName);
    //        await blobClient.UploadAsync(audioDataStreamWrapper, overwrite: true);

    //        return blobClient.Uri.AbsoluteUri;
    //    }

    //    public string GenerateSSML(string text, string pitch, string rate, string volume, string voiceName)
    //    {
    //        var sb = new StringBuilder();
    //        sb.Append("<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>");
    //        sb.Append($"<voice name='{voiceName}'>");
    //        sb.Append($"<prosody pitch='{pitch}' rate='{rate}' volume='{volume}'>{text}</prosody>");
    //        sb.Append("</voice></speak>");
    //        return sb.ToString();
    //    }

    //    public async Task<List<string>> GetAvailableVoices()
    //    {
    //        var speechConfig = SpeechConfig.FromSubscription(azure_api_key, azure_region);
    //        using var synthesizer = new SpeechSynthesizer(speechConfig);
    //        var resultVoices = await synthesizer.GetVoicesAsync();

    //        return resultVoices.Voices.Select(voice => voice.Name).ToList();
    //    }

    //    public async Task<List<string>> GetAvailableLanguages()
    //    {
    //        var speechConfig = SpeechConfig.FromSubscription(azure_api_key, azure_region);
    //        using var synthesizer = new SpeechSynthesizer(speechConfig);
    //        var resultVoices = await synthesizer.GetVoicesAsync();

    //        return resultVoices.Voices.Select(voice => voice.Locale).Distinct().ToList();
    //    }
    //}
}
