using EMSuite.DataAccess;
using EMSuite.PhoneNotification.Models;
using System.Runtime.CompilerServices;

namespace EMSuite.PhoneNotification.Services
{
    public interface INotificationLogService
    {
        Task<bool> InsertNoticationLog(NotificationLog notificationLog);
        Task<bool> UpdateNotificationlog(string callId, string status);
        Task<IEnumerable<BatchNotificationPackage>> GetFailedBatchNotificationPackages();
        Task<IEnumerable<BatchNotificationPackage>> GetAllBatchNotificationPackages();
        Task<IEnumerable<NotificationLog>> GetNotificationLogsByAlarmId(int alarmId);
    }

    public class NotificationLogService : INotificationLogService
    {
        private readonly IDataAccess _dataAccess;

        public NotificationLogService(IDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        public async Task<IEnumerable<BatchNotificationPackage>> GetFailedBatchNotificationPackages()
        {
            var notificationEntries = await GetFailedBatchRuns();

            var batchNotificationPackages = notificationEntries
                .GroupBy(ne => new { ne.BatchAlarmId, ne.GenderId, ne.RoundRobinInterval })
                .Select(group => new BatchNotificationPackage
                {
                    BatchAlarmId = group.Key.BatchAlarmId,
                    GenderId = group.Key.GenderId,
                    RoundRobinInterval = group.Key.RoundRobinInterval,
                    PhoneContacts = group.Select(ne => new LogPhoneContact
                    {

                        UserId = ne.UserId,
                        PhoneNumber = ne.PhoneNumber,
                        AzureBlobUrl = ne.AzureBlobUrl,
                        AlarmMessage = ne.AlarmMessage,

                    }).ToList()
                });

            return batchNotificationPackages;
        }

        private async Task<IEnumerable<NotificationEntry>> GetFailedBatchRuns()
        {
            string query = @"
                    WITH BatchCounts AS (
                        SELECT pbc.BatchAlarmId, COUNT(*) as TotalCount, SUM(CASE WHEN pbc.StatusIndicator = 'failed' THEN 1 ELSE 0 END) as FailedCount
                        FROM PhoneCallLog pbc
                        GROUP BY pbc.BatchAlarmId
                        HAVING COUNT(DISTINCT pbc.BatchAlarmId) < 3
                    )

                    SELECT pbc.UserId, 
                    pbc.BatchAlarmId, pbc.AzureBlobUrl, anu.PhoneNumber, 
                    pbc.StatusIndicator, pbc.CallId, pbc.AlarmMessage, pbc.GenderId, 
                    pbc.RoundRobinInterval, pbc.NotificationDate
                    FROM PhoneCallLog pbc
                    INNER JOIN AspNetUsers anu ON pbc.UserId = anu.Id
                    INNER JOIN BatchCounts bc ON pbc.BatchAlarmId = bc.BatchAlarmId 
                    WHERE pbc.StatusIndicator = 'failed' AND bc.FailedCount = bc.TotalCount;";


            return await _dataAccess.Query<NotificationEntry>(query);
        }

        public async Task<IEnumerable<BatchNotificationPackage>> GetAllBatchNotificationPackages()
        {
            var notificationEntries = await GetAllBatchRuns();

            var batchNotificationPackages = notificationEntries
                .GroupBy(ne => new { ne.BatchAlarmId, ne.GenderId, ne.RoundRobinInterval })
                .Select(group => new BatchNotificationPackage
                {
                    BatchAlarmId = group.Key.BatchAlarmId,
                    GenderId = group.Key.GenderId,
                    RoundRobinInterval = group.Key.RoundRobinInterval,
                    PhoneContacts = group.Select(ne => new LogPhoneContact
                    {
                        UserId = ne.UserId,
                        PhoneNumber = ne.PhoneNumber,
                        AzureBlobUrl = ne.AzureBlobUrl,
                        AlarmMessage = ne.AlarmMessage,
                    }).ToList()
                });

            return batchNotificationPackages;
        }


        private async Task<IEnumerable<NotificationEntry>> GetAllBatchRuns()
        {
            string query = @"
            SELECT pbc.UserId, 
            pbc.BatchAlarmId, pbc.AzureBlobUrl, anu.PhoneNumber, 
            pbc.StatusIndicator, pbc.CallId, pbc.AlarmMessage, pbc.GenderId, 
            pbc.RoundRobinInterval, pbc.NotificationDate
            FROM PhoneCallLog pbc
            INNER JOIN AspNetUsers anu ON pbc.UserId = anu.Id;";

            return await _dataAccess.Query<NotificationEntry>(query);
        }

        public async Task<IEnumerable<NotificationLog>> GetNotificationLogsByAlarmId(int alarmId)
        {
            string query = @"
            SELECT * 
            FROM PhoneCallLog";

            return await _dataAccess.Query<NotificationLog>(query, new { AlarmId = alarmId });
        }

        public async Task<bool> InsertNoticationLog(NotificationLog notificationLog)
        {
            using var transaction = await _dataAccess.StartTransaction();
            try
            {
                await _dataAccess.Insert("PhoneCallLog", transaction,
                    ColumnParameter.Create("UserId", notificationLog.UserId),
                    ColumnParameter.Create("CallId", notificationLog.CallId),
                    ColumnParameter.Create("AlarmMessage", notificationLog.AlarmMessage),
                    ColumnParameter.Create("BatchAlarmId", notificationLog.BatchAlarmId),
                    ColumnParameter.Create("GenderId", notificationLog.GenderId),
                    ColumnParameter.Create("AzureBlobUrl", notificationLog.AzureBlobUrl),
                    ColumnParameter.Create("RoundRobinInterval", notificationLog.RoundRobinInterval),
                    ColumnParameter.Create("NotificationDate", notificationLog.PhoneCallTimeStamp));

                await transaction.Commit();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollBack();
                return false;
            }
        }

        public async Task<bool> UpdateNotificationlog(string callId, string status)
        {

            if (!string.IsNullOrEmpty(status) || !string.IsNullOrEmpty(callId))
            {
                return false;
            }

                using var transaction = await _dataAccess.StartTransaction();

            try
            {
                var result = await _dataAccess.Update("PhoneCallLog", "CallId = @callId", transaction,
                QueryParameter.Create("@callid", callId),
                ColumnParameter.Create("StatusIndicator", status));

                await transaction.Commit();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollBack();
                return false;
            }
        }
    }
}
