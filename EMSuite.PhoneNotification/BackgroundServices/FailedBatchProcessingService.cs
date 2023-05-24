using EMSuite.PhoneNotification.Services;

namespace EMSuite.PhoneNotification.BackgroundServices
{
    public class FailedBatchProcessingService : IHostedService, IDisposable
    {
        private readonly IPhoneProcessor _phoneProcessor;
        private readonly INotificationLogService _notificationLogService;
        private Timer _timer;

        public FailedBatchProcessingService(IPhoneProcessor phoneProcessor, INotificationLogService notificationLogService)
        {
            _phoneProcessor = phoneProcessor;
            _notificationLogService = notificationLogService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(ProcessFailedBatches, null, TimeSpan.Zero, TimeSpan.FromMinutes(10));
            return Task.CompletedTask;
        }

        private async void ProcessFailedBatches(object state)
        {
            var failedBatchNotificationPackages = await _notificationLogService.GetFailedBatchNotificationPackages();

            foreach (var batchNotificationPackage in failedBatchNotificationPackages)
            {
                await _phoneProcessor.CallPhones(batchNotificationPackage, CancellationToken.None);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
