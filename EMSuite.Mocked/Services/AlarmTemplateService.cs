using Microsoft.Extensions.Localization;
using System.IO.Pipelines;
using System.Resources;

namespace EMSuite.Mocked.Services
{
    public interface IAlarmTemplateService
    {
        Task<LoadResult> GetNotificationUsers(int id, DataSourceLoadOptions options);
        Task<LoadResult> GetNotificationNodes(int id, DataSourceLoadOptions options);

        Task<DatabaseCommandResult> HandleNotificationUser(int thresholdId, NotificationPackageUser notificationPackageUser);
        Task<DatabaseCommandResult> HandleNotificationZoneSpecific(int thresholdId, NotificationPackageZoneSpecific notificationPackageZoneSpecific);
        Task SetLocationSpecificMode(int thresholdId, bool mode);
    }

    public class AlarmTemplateService : IAlarmTemplateService
    {
        private readonly IStringLocalizer<AlarmTemplateService> _localizer;
        private readonly IStringLocalizer _measurementLocalizer;
        private readonly IDataAccess _context;
        private readonly IAuditService _auditService;
        private readonly ILogger<AlarmTemplateService> _logger;
        private readonly IHardwareNotifier _notifier;
        private readonly IHubNotifier _hubNotifier;
        private readonly IParticleService _particleService;

        // TODO: Make resources Uniform
        public AlarmTemplateService(
            IStringLocalizer<AlarmTemplateService> localizer,
            IStringLocalizerFactory localizefactory,
            ILogger<AlarmTemplateService> logger,
            IDataAccess context,
            IAuditService auditService,
            IHardwareNotifier notifier,
            IHubNotifier hubNotifier,
            IParticleService particleService)
        {
            _localizer = localizer;
            _logger = logger;
            _context = context;
            _auditService = auditService;
            _notifier = notifier;
            _hubNotifier = hubNotifier;
            _measurementLocalizer = localizefactory.Create("MeasurementType.MeasurementTypes", ResourceRef.ResourceAssembly);
            _particleService = particleService;
        }

        public async Task<LoadResult> GetNotificationUsers(int id, DataSourceLoadOptions options)
        {
            string sql = "SELECT " +
                         "  Id AS 'UserId', " +
                         "  UserName As 'Name', " +
                         "  FullName, " +
                         "  Email As 'EMail', " +
                         "  PhoneNumber As 'PhoneNumber', " +
                         "  CASE WHEN ta.ThresholdId IS NOT NULL THEN 1 ELSE 0 END AS 'Selected' " +
                         "FROM AspNetUsers u " +
                         "LEFT JOIN UserAlarmTemplateAllocation ta ON u.Id = ta.UserId AND ta.ThresholdId = @thresholdId AND ta.ZoneId = 0 " +
                         "WHERE (Email IS NOT NULL OR PhoneNumber IS NOT NULL) " +
                         "AND u.CreatedByAD = (SELECT UseActiveDirectory FROM PasswordSecuritySettings) " +
                         "AND u.CreatedByAzureAD = (SELECT UseAzureActiveDirectory FROM PasswordSecuritySettings) ";

            return await _context.LoadDataSource<NotificationUser>(sql, options, new Dictionary<string, object> { { "thresholdId", id } });
        }

        public async Task<LoadResult> GetNotificationNodes(int id, DataSourceLoadOptions options)
        {
            string sql =
                // Site Nodes
                "SELECT" +
                "   CONCAT('S', s.ID) AS 'NodeID', " +
                "	s.[Name] AS 'Name', " +
                "	NULL AS 'Parent', " +
                "	NULL AS 'Selected', " +
                "	1 AS 'Expanded' " +
                "FROM dbo.Site s " +
                "WHERE IsLogical = 0 " +

                "UNION ALL " +

                // Zone Nodes
                "SELECT " +
                "   CONCAT('Z', z.ID) as 'NodeID', z.[Name] as 'Name', " +
                "	CASE WHEN sub.SubZoneID IS NULL THEN CONCAT('S', z.SiteId) ELSE CONCAT('Z', sub.MainZoneID) END AS 'Parent', " +
                "	NULL AS 'Selected', " +
                "	1 AS 'Expanded' " +
                "FROM dbo.[Zone] z " +
                "JOIN dbo.[Site] s ON s.ID = z.SiteID " +
                "LEFT JOIN dbo.SubZoneAllocation sub ON z.ID = sub.SubZoneID " +
                "WHERE s.IsLogical = 0 " +

                "UNION ALL " +

                // User Nodes (Grouped to avoid duplicates)
                "SELECT NodeID, [Name], Parent, MAX(Selected), MAX(Expanded) " +
                "FROM (  " +

                // Include all mapped users
                "   SELECT " +
                "       CONCAT('U_', u.Id, '_', ISNULL(rz.ZoneID, 0)) AS 'NodeID', " +
                "       IIF(u.PhoneNumber IS NULL, u.Username, CONCAT(u.Username, ' (', u.PhoneNumber, ')')) AS 'Name', " +
                "       CASE WHEN rz.ZoneID IS NOT NULL THEN CONCAT('Z', rz.ZoneID) ELSE NULL END AS 'Parent',  " +
                "       CASE WHEN(ta.ThresholdId IS NOT NULL) AND(rz.ZoneID = ta.ZoneID)  " +
                "           THEN 1  " +
                "           ELSE 0  " +
                "       END AS 'Selected', " +
                "       0 AS 'Expanded'  " +
                "   FROM AspNetUsers u  " +
                "   JOIN UserSiteAllocation rs ON u.Id = rs.UserId  " +
                "   JOIN UserZoneAllocation rz ON u.Id = rz.UserId  " +
                "   LEFT JOIN UserAlarmTemplateAllocation ta ON u.Id = ta.UserId AND ta.ThresholdId = @thresholdId " +
                "   WHERE (Email IS NOT NULL OR PhoneNumber IS NOT NULL)  " +
                "   AND u.CreatedByAD = (SELECT UseActiveDirectory FROM PasswordSecuritySettings) " +
                "   AND u.CreatedByAzureAD = (SELECT UseAzureActiveDirectory FROM PasswordSecuritySettings) " +
                ") U " +
                "GROUP BY NodeID, [Name], Parent ";

            return await _context.LoadDataSource<TreeNodeModel>(sql, options, new Dictionary<string, object> { { "thresholdId", id } });
        }


        public async Task SetLocationSpecificMode(int thresholdId, bool mode)
        {
            await _context.ExecuteAsync(
                "UPDATE dbo.[ChannelAlarmDefinition] SET [NotificationMode] = @mode WHERE ID = @id",
                new { mode = mode ? 1 : 0, id = thresholdId });
        }



        private async Task UpdateChannelNotificationSetup(int thresholdId, SerializableTimeSpan roundRobinTimeSpan, int genderId)
        {
            await _context.ExecuteAsync(
              "UPDATE dbo.[ChannelAlarmDefinition] SET [GenderID] = @genderId, [RoundRobinInterval] = @roundRobinInterval WHERE ID = @alarmDefinitionId",
              new
              {
                  genderId = genderId,
                  roundRobinInterval = roundRobinTimeSpan.TotalSeconds * 10000000,
                  alarmDefinitionId = thresholdId
              });
        }

        public async Task<DatabaseCommandResult> HandleNotificationUser(int thresholdId, NotificationPackageUser notificationPackageUser)
        {
            await UpdateChannelNotificationSetup(thresholdId, notificationPackageUser.RoundRobinInterval, notificationPackageUser.Gender);
            return await AssignUsers(thresholdId, notificationPackageUser.SelectedUsers);
        }

        public async Task<DatabaseCommandResult> HandleNotificationZoneSpecific(int thresholdId, NotificationPackageZoneSpecific notificationPackageZoneSpecific)
        {
            await UpdateChannelNotificationSetup(thresholdId, notificationPackageZoneSpecific.RoundRobinInterval, notificationPackageZoneSpecific.Gender);
            return await AssignUsers(thresholdId, notificationPackageZoneSpecific.SelectedUsers);
        }

        private async Task<DatabaseCommandResult> AssignUsers(int thresholdId, IEnumerable<string> users)
        {
            return await AssignUserZones(
                thresholdId,
                users.Select(u => new UserZone { UserId = u, ZoneId = 0 }),
                new List<int> { 0 });
        }

        private async Task<DatabaseCommandResult> AssignUsers(int templateId, IEnumerable<UserZone> data)
        {
            return await AssignUserZones(
                templateId,
                data,
                (await _context.Query<int>("SELECT ID FROM dbo.[Zone]")).ToList());
        }

        private async Task<DatabaseCommandResult> AssignUserZones(int thresholdId, IEnumerable<UserZone> data, IEnumerable<int> zoneIds)
        {
            List<string> deletedUserIds = new();
            List<string> addedUserIds = new();

            foreach (int zoneId in zoneIds)
            {
                var zoneUsers = data.Where(d => d.ZoneId == zoneId).Select(d => d.UserId);

                if (zoneUsers.Any())
                {
                    deletedUserIds.AddRange(
                        await _context.Query<string>(
                            "DELETE FROM UserAlarmTemplateAllocation " +
                            "OUTPUT DELETED.UserId " +
                            "FROM dbo.UserAlarmTemplateAllocation " +
                            "WHERE ThresholdId = @thresholdId " +
                            " AND ZoneId = @zoneId " +
                            " AND UserId NOT IN @zoneUsers; ",
                            new { thresholdId, zoneId, zoneUsers }));

                    addedUserIds.AddRange(
                        await _context.Query<string>(
                           "INSERT INTO UserAlarmTemplateAllocation (AlarmTemplateID, ThresholdId, UserId, ZoneId, AssignedAt) " +
                           "OUTPUT INSERTED.UserId " +
                           "SELECT " +
                           "    (SELECT TOP 1 AlarmTemplateID FROM ChannelAlarmDefinition WHERE ID = @thresholdId) AS 'AlarmTemplateID', " +
                           "    @thresholdId AS 'ThresholdId', " +
                           "    Id AS 'UserId', " +
                           "    @zoneId AS 'ZoneId', " +
                           "    GETUTCDATE() " +
                           "FROM AspNetUsers u " +
                           "LEFT JOIN UserAlarmTemplateAllocation ta ON u.Id = ta.UserId AND ta.ThresholdId = @thresholdId AND ta.ZoneId = @zoneId " +
                           "WHERE u.Id IN @zoneUsers AND ta.ThresholdId IS NULL; ",
                           new { thresholdId, zoneId, zoneUsers }));
                }
                else
                {
                    deletedUserIds.AddRange(
                       await _context.Query<string>(
                            "DELETE FROM UserAlarmTemplateAllocation " +
                            "OUTPUT DELETED.UserId " +
                            "WHERE ThresholdId = @thresholdId AND ZoneId = @zoneId;",
                            new { thresholdId, zoneId }));
                }
            }

            string templateName = await _context.QueryScaler<string>(
                    "SELECT [Name] " +
                    "FROM dbo.AlarmTemplate t " +
                    "JOIN dbo.ChannelAlarmDefinition th ON t.ID = th.AlarmTemplateID " +
                    "WHERE th.ID = @thresholdId; ",
                    new { thresholdId });

            if (deletedUserIds.Any())
            {
                var removed = await GetNamesFromIds(deletedUserIds);
                await _auditService.AuditEntry(new AuditEntryObject() { LocalisationKey = "RemoveUsersFromAlarmThreshold" }, templateName, thresholdId, string.Join(',', removed));
            }

            if (addedUserIds.Any())
            {
                var added = await GetNamesFromIds(addedUserIds);
                await _auditService.AuditEntry(new AuditEntryObject() { LocalisationKey = "AssignUsersToAlarmThreshold" }, templateName, thresholdId, string.Join(',', added));
            }

            return new DatabaseCommandResult
            {
                Success = true,
                Message = _localizer["SuccessfullyUpdatedUsers"].Value
            };
        }

    }
}
