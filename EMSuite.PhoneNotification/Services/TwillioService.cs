using EMSuite.PhoneNotification.Models;
using Twilio;
using Twilio.Types;

namespace EMSuite.PhoneNotification.Services
{
    public interface ITwillioService
    {
        ICallResource MakeCall(string azureBlobMp3Url, string toPhoneNumber);
    }

    public class TwillioService : ITwillioService
    {
        private IConfiguration _configuration;
        private ITwillioCallHandler _callHandler;

        public TwillioService(
            IConfiguration configuration, 
            ITwillioCallHandler callHandler)
        {
            _configuration = configuration;
            _callHandler = callHandler;
        }

        public ICallResource MakeCall(string azureBlobMp3Url, string toPhoneNumber)
        {
            string accountSid = _configuration["Twilio:AccountSid"];
            string authToken = _configuration["Twilio:AuthToken"];
            string phoneNumber = _configuration["Twilio:PhoneNumber"];
            string azureFunction = _configuration["AzureFunction:TriggerUrl"];
            string statusCallBack = _configuration["StatusCallBack:CallBackEndPoint"];

            try
            {
                TwilioClient.Init(accountSid, authToken);

                string encodedMp3Url = Uri.EscapeDataString(azureBlobMp3Url);

                var twimlUrl = $"{azureFunction}{encodedMp3Url}";

                return _callHandler.Create(
                         new PhoneNumber(toPhoneNumber),
                          new PhoneNumber(phoneNumber),
                          twimlUrl,
                          statusCallBack);
              
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
