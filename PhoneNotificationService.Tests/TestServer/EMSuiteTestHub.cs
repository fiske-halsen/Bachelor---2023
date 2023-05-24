using EMSuite.Common.PhoneNotification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PhoneNotificationService.Tests.TestServer
{
    [AllowAnonymous]
    public class EMSuiteTestHub : Hub
    {
        #region Phone calls
        public async Task SendPhoneAlarmCall(PhoneNotificationPackage phoneNotificationPackage)
        {
            await Clients.All.SendAsync("ReceivePhoneAlarmPackage", phoneNotificationPackage);
        }

        #endregion
    }
}
