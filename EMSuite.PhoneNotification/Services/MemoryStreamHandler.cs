using EMSuite.PhoneNotification.Models;
using Microsoft.CognitiveServices.Speech;

namespace EMSuite.PhoneNotification.Services
{
    public interface IMemoryStreamHandler
    {
        MemoryStreamResult GetMemoryStream(AudioDataStream stream);
    }

    public class MemoryStreamHandler : IMemoryStreamHandler
    {
        public MemoryStreamResult GetMemoryStream(AudioDataStream stream)
        {
            return new MemoryStreamResult(stream);
        }
    }
}
