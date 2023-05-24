using System.IO.Pipelines;

namespace EMSuite.Mocked.Services
{
    public interface IAlarmTemplateService
    {
        Task<LoadResult> GetNotificationUsers(int id, DataSourceLoadOptions options);
        Task<LoadResult> GetNotificationNodes(int id, DataSourceLoadOptions options);

        Task<DatabaseCommandResult> HandleNotificationUser(int thresholdId, NotificationPackageUser notificationPackageUser);
        Task<DatabaseCommandResult> HandleNotificationZoneSpecific(int thresholdId, NotificationPackageZoneSpecific notificationPackageZoneSpecific);
    }

        public class AlarmTemplateService
    {
    }
}
