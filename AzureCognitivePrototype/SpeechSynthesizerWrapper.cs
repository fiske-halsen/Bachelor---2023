using Microsoft.CognitiveServices.Speech;

namespace AzureCognitivePrototype
{
    public interface ISpeechSynthesizerWrapper : IDisposable
    {
        Task<ISpeechSynthesisResultWrapper> SpeakSsml(string ssml);
        Task<List<string>> GetVoices();
        Task<List<string>> GetLanguages();
    }

    public class SpeechSynthesizerWrapper : ISpeechSynthesizerWrapper
    {
        private readonly SpeechSynthesizer _synthesizer;

        public SpeechSynthesizerWrapper(SpeechConfig speechConfig)
        {
            _synthesizer = new SpeechSynthesizer(speechConfig, null);
        }

        public async Task<List<string>> GetVoices()
        {
            var resultVoices = await _synthesizer.GetVoicesAsync();
            return resultVoices.Voices.Select(voice => voice.Name).ToList();
        }

        public async Task<List<string>> GetLanguages()
        {
            var resultVoices = await _synthesizer.GetVoicesAsync();
            return resultVoices.Voices.Select(voice => voice.Locale).Distinct().ToList();
        }

        public async Task<ISpeechSynthesisResultWrapper> SpeakSsml(string ssml)
        {
            var result = await _synthesizer.SpeakSsmlAsync(ssml);
            return new SpeechSynthesisResultWrapper(result);
        }

        public void Dispose()
        {
            _synthesizer.Dispose();
        }
    }
}
