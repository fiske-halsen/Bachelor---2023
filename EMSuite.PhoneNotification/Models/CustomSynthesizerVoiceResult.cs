using Microsoft.CognitiveServices.Speech;

namespace EMSuite.PhoneNotification.Models
{
    public interface ISynthesizerVoiceResult
    {
        IEnumerable<VoiceInfoResult> Voices { get; set; }
    }

    public class CustomSynthesizerVoiceResult : ISynthesizerVoiceResult
    {
        public IEnumerable<VoiceInfoResult> Voices { get; set; }

        public CustomSynthesizerVoiceResult()
        {
            Voices = Enumerable.Empty<VoiceInfoResult>();
        }

        public CustomSynthesizerVoiceResult(IEnumerable<VoiceInfoResult> testVoices)
        {
            Voices = testVoices;
        }

        public CustomSynthesizerVoiceResult(SynthesisVoicesResult speechSynthesisResult)
        {
            Voices = speechSynthesisResult.Voices.Select(v => new VoiceInfoResult { Name = v.Name, Locale = v.Locale });
        }
    }

    public class VoiceInfoResult
    {
        public string Name { get; set; }
        public string Locale { get; set; }
    }
}
