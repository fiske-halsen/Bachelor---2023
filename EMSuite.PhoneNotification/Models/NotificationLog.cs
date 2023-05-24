namespace EMSuite.PhoneNotification.Models
{
    public class NotificationLog
    {
        public string UserId { get; set; }
        public int BatchAlarmId { get; set; }
        public string CallId { get; set; }
        public string AlarmMessage { get; set; }
        public int GenderId { get; set; }
        public int RoundRobinInterval { get; set; }
        public string AzureBlobUrl { get; set; }
        public DateTime PhoneCallTimeStamp { get; set; }
    }
}
