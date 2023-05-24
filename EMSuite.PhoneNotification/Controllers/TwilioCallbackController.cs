using EMSuite.PhoneNotification.Services;
using Microsoft.AspNetCore.Mvc;
using Twilio.AspNet.Core;

namespace EMSuite.PhoneNotification.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TwilioCallbackController : TwilioController
    {
        private readonly INotificationLogService _notificationLogService;
        public TwilioCallbackController(INotificationLogService notificationLogService)
        {
            _notificationLogService = notificationLogService;
        }

        [HttpPost("StatusCallback")]
        public async Task<IActionResult> StatusCallback()
        {
            var callStatus = Request.Form["CallStatus"];
            var callSid = Request.Form["CallSid"];

            var isSucces = await _notificationLogService.UpdateNotificationlog(callStatus, callSid);

            return isSucces ? Ok() : BadRequest();
        }
    }
}
