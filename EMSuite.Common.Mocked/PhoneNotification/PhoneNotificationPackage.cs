using System.Collections.Generic;

namespace EMSuite.Common.PhoneNotification
{
    public class PhoneNotificationPackage
    {
        public string SystemBaseLanguage { get; set; }
        public string BaseText { get; set; }
        public List<PhoneContact> PhoneContacts { get; set; }
        public int GenderId { get; set; }
        public int RoundRobinInterval { get; set; }
        public int AlarmId { get; set; }
    }

    public class PhoneContact
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }
        public string Language { get; set; } = "en-US"; //Default
    }
}
