using Microsoft.CognitiveServices.Speech;

namespace AzureCognitivePrototype
{
    public interface ISpeechSynthesisResultWrapper
    {
        ResultReason Reason { get; }
        IAudioDataStream AudioDataStream { get; }
    }

    public class SpeechSynthesisResultWrapper : ISpeechSynthesisResultWrapper
    {
        private readonly SpeechSynthesisResult _speechSynthesisResult;

        public SpeechSynthesisResultWrapper(SpeechSynthesisResult speechSynthesisResult)
        {
            _speechSynthesisResult = speechSynthesisResult;
        }

        public ResultReason Reason => _speechSynthesisResult.Reason;

        public IAudioDataStream AudioDataStream => new AudioDataStreamWrapper(_speechSynthesisResult);
    }
}
