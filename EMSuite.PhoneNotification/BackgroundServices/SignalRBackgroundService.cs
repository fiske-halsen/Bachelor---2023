using EMSuite.PhoneNotification.Services;

namespace EMSuite.PhoneNotification.BackgroundServices
{
    public class SignalRBackroundService : IHostedService
    {
        private readonly ISignalRClient _client;

        public SignalRBackroundService(ISignalRClient client)
        {
            _client = client;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _client.Connect(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _client.Dispose();
            return Task.CompletedTask;
        }
    }
}
