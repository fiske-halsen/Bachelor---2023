namespace EMSuite.PhoneNotification.Services
{
    public interface IDelayProvider
    {
        Task Delay(TimeSpan delay, CancellationToken cancellationToken);
    }

    public class DelayProvider : IDelayProvider
    {
        public Task Delay(TimeSpan delay, CancellationToken cancellationToken)
        {
            return Task.Delay(delay, cancellationToken);
        }
    }
}
