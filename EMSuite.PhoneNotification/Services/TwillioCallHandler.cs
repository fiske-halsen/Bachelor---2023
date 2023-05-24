using EMSuite.PhoneNotification.Models;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace EMSuite.PhoneNotification.Services
{
    public interface ITwillioCallHandler
    {
        ICallResource Create(PhoneNumber toPhoneNumber, PhoneNumber fromPhoneNumber, string twimlUrl, string statusCallBackUrl);
    }

    public class TwillioCallHandler : ITwillioCallHandler
    {
        public ICallResource Create(PhoneNumber toPhoneNumber, PhoneNumber fromPhoneNumber, string twimlUrl, string statusCallBackUrl)
        {

            var callResource = CallResource.Create(
                to: toPhoneNumber,
                from: fromPhoneNumber,
                url: new Uri(twimlUrl),
             statusCallback: new Uri(statusCallBackUrl),
             statusCallbackEvent: new List<string> { "completed", "busy", "failed", "no-answer", "canceled" }
            );

            return new CustomCallResource(callResource);
        }
    }
}
