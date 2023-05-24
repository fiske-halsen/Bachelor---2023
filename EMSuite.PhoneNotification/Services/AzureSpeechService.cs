using EMSuite.PhoneNotification.Exceptions;
using EMSuite.PhoneNotification.Models;
using Microsoft.CognitiveServices.Speech;
using System.Text;

namespace EMSuite.PhoneNotification.Services
{
    public interface IAzureSpeechService
    {
        Task<MemoryStreamResult> GenerateTextToSpeechAudio(GenerateSpeechConfiguration generateSpeechConfiguration);
        Task<IEnumerable<string>> GetAvailableVoices();
        Task<IEnumerable<string>> GetAvailableLanguages();
    }

    public class AzureSpeechService : IAzureSpeechService
    {
        private readonly IConfiguration _configuration;
        private readonly ISpeechSynthesizer _speechSynthesizer;
        private readonly IMemoryStreamHandler _streamStreamHandler;

        public AzureSpeechService(
            IConfiguration configuration,
            ISpeechSynthesizer speechSynthesizer, 
            IMemoryStreamHandler streamStreamHandler)
        {
            _configuration = configuration;
            _speechSynthesizer = speechSynthesizer;
            _streamStreamHandler = streamStreamHandler;
        }

        public async Task<MemoryStreamResult> GenerateTextToSpeechAudio(GenerateSpeechConfiguration generateSpeechConfiguration)
        {
            try
            {
                var speechConfig = GetSpeechConfig();
                speechConfig.SpeechSynthesisLanguage = generateSpeechConfiguration.Language;
                speechConfig.SpeechSynthesisVoiceName = generateSpeechConfiguration.VoiceName; 
                speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz64KBitRateMonoMp3);

                var generatedSSML = GenerateSSML(generateSpeechConfiguration);
                    
                var result = await _speechSynthesizer.SpeakSsmlAsync(generatedSSML, speechConfig);

                using var memoryStreamWrapper = _streamStreamHandler.GetMemoryStream(result.AudioDataStream);

                return memoryStreamWrapper;
            }
            catch (Exception ex)
            {
                throw new CustomException("Failed to generate text-to-speech audio.", ex);
            }
        }

        public async Task<IEnumerable<string>> GetAvailableVoices()
        {
            try
            {
                var speechConfig = GetSpeechConfig();
                using var synthesizer = new SpeechSynthesizer(speechConfig);
                var voices = await _speechSynthesizer.GetVoicesAsync(speechConfig);
                return voices.Voices.Select(voice => voice.Name).ToList();
            }
            catch (Exception ex)
            {
                throw new CustomException("Failed to get available voices.", ex);
            }
        }

        public async Task<IEnumerable<string>> GetAvailableLanguages()
        {
            try
            {
                var speechConfig = GetSpeechConfig();
                using var synthesizer = new SpeechSynthesizer(speechConfig);
                var voices = await _speechSynthesizer.GetVoicesAsync(speechConfig);
                return voices.Voices.Select(voice => voice.Locale).Distinct().ToList();
            }
            catch (Exception ex)
            {
                throw new CustomException("Failed to get available languages.", ex);
            }
        }

        private SpeechConfig GetSpeechConfig()
        {
            return SpeechConfig.FromSubscription(
                _configuration["AzureCogniveService:ApiKey"],
                _configuration["AzureCogniveService:Region"]);
        }

        private string GenerateSSML(GenerateSpeechConfiguration generateSpeechConfiguration)
        {
            var sb = new StringBuilder();
            sb.Append($"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{generateSpeechConfiguration.Language}'>");
            sb.Append($"<voice name='{generateSpeechConfiguration.VoiceName}'>");
            sb.Append($"<prosody pitch='{generateSpeechConfiguration.Pitch}' rate='{generateSpeechConfiguration.Rate}' " +
                $"volume='{generateSpeechConfiguration.Volume}'>{generateSpeechConfiguration.Text}</prosody>");
            sb.Append("</voice></speak>");
            return sb.ToString();
        }
    }
}
