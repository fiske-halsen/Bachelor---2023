using EMSuite.PhoneNotification.Models;
using Microsoft.CognitiveServices.Speech;

namespace EMSuite.PhoneNotification.Services
{
    public interface ISpeechSynthesizer : IDisposable
    {
        Task<ISpeechSynthesisResult> SpeakSsmlAsync(string ssml, SpeechConfig speechConfig);
        Task<ISynthesizerVoiceResult> GetVoicesAsync(SpeechConfig speechConfig);
    }

    public class SpeechSynthesizerHandler : ISpeechSynthesizer
    {
        public async Task<ISpeechSynthesisResult> SpeakSsmlAsync(string ssml, SpeechConfig speechConfig)
        {
            using var synthesizer = new SpeechSynthesizer(speechConfig, null);
            var result = await synthesizer.SpeakSsmlAsync(ssml);
            return new CustomSpeechSynthesisResult(result);
        }

        public async Task<ISynthesizerVoiceResult> GetVoicesAsync(SpeechConfig speechConfig)
        {
            using var synthesizer = new SpeechSynthesizer(speechConfig);
            return new CustomSynthesizerVoiceResult(await synthesizer.GetVoicesAsync());
        }

        public void Dispose() { }
    }
}
