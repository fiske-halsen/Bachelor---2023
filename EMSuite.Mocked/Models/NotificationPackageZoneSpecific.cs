namespace EMSuite.Mocked.Models
{
    public class NotificationPackageZoneSpecific
    {
        public SerializableTimeSpan RoundRobinInterval { get; set; }
        public int Gender { get; set; }
        public IEnumerable<UserZone> SelectedUsers { get; set; }
    }
}
