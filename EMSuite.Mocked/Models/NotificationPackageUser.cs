namespace EMSuite.Mocked.Models
{
    public class NotificationPackageUser
    {
        public SerializableTimeSpan RoundRobinInterval { get; set; }
        public int Gender { get; set; }
        public IEnumerable<string> SelectedUsers { get; set; }
    }
}
