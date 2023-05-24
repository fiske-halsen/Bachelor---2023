using Microsoft.CognitiveServices.Speech;

namespace EMSuite.PhoneNotification.Models
{
    public interface ISpeechSynthesisResult
    {
        string Reason { get; }
        AudioDataStream AudioDataStream { get; }
    }

    public class CustomSpeechSynthesisResult : ISpeechSynthesisResult
    {
        public string Reason { get; }
        public AudioDataStream AudioDataStream { get; }

        public CustomSpeechSynthesisResult() { }

        public CustomSpeechSynthesisResult(SpeechSynthesisResult result)
        {
            Reason = result.Reason.ToString();
            AudioDataStream = AudioDataStream.FromResult(result);
        }
    }
}
