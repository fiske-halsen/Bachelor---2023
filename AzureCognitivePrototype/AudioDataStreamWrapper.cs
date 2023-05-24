using Microsoft.CognitiveServices.Speech;

namespace AzureCognitivePrototype
{
    public interface IAudioDataStream
    {
        AudioDataStream AudioDataStream { get; }
    }

    public class AudioDataStreamWrapper : IAudioDataStream
    {
        private readonly SpeechSynthesisResult _speechSynthesisResult;

        public AudioDataStreamWrapper(SpeechSynthesisResult speechSynthesisResult)
        {
            _speechSynthesisResult = speechSynthesisResult;
        }

        public AudioDataStream AudioDataStream => AudioDataStream.FromResult(_speechSynthesisResult);
    }
}
