using EMSuite.Common.PhoneNotification;

namespace EMSuite.PhoneNotification.Models
{
    public class BatchNotificationPackage
    {
        public List<LogPhoneContact> PhoneContacts { get; set; }
        public int GenderId { get; set; }
        public int RoundRobinInterval { get; set; }
        public int BatchAlarmId { get; set; }
    }

    public class LogPhoneContact
    {
        public string UserId { get; set; }
        public string PhoneNumber { get; set; }
        public string AlarmMessage { get; set; } 
        public string AzureBlobUrl { get; set; }
    }
}
