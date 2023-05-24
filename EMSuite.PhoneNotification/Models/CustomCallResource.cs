using Twilio.Rest.Api.V2010.Account;

namespace EMSuite.PhoneNotification.Models
{
    public interface ICallResource
    {
        string Sid { get; }
        DateTime PhoneCallTimeStamp { get; }
    }

    public class CustomCallResource : ICallResource
    {
        public string Sid { get; private set; }
        public DateTime PhoneCallTimeStamp { get; private set; }

        public CustomCallResource() { }
        public CustomCallResource(CallResource callResource)
        {
            Sid = callResource.Sid;
            PhoneCallTimeStamp = DateTime.UtcNow;
        }
    }
}
