using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace EMSuite.Hardware.Api.Mocked.Services
{
    public interface ISignalClient : IDisposable
    {
        event EventHandler<LoggerRefreshEventArgs> OnLoggerChanged;
        event EventHandler OnRequestRefresh;
        event EventHandler OnRequestDiscovery;

        bool IsConnected { get; }

        bool IsDisconnected { get; }
        Task<bool> LoggerConfigurationConfirmed(uint LoggerSerial, CancellationToken cancellationToken);
        Task<bool> AlarmStateChange(Alarm data, CancellationToken cancellationToken);
        Task<bool> Connect(CancellationToken cancellationToken);
        Task<bool> Disconnect(CancellationToken cancellationToken);
        Task<bool> DiscoveryList(IEnumerable<DiscoveryAccessPoint> data, CancellationToken cancellationToken);
        Task<bool> NewData(AccessPointData data, CancellationToken cancellationToken);
        Task<bool> RegisterAsApi(CancellationToken cancellationToken);
        Task<bool> MaintenanceStateChange(MaintenanceNotification data, CancellationToken cancellationToken);
        Task<bool> SendHardwareSms(SmsMessage data, CancellationToken cancellationToken);
        Task<bool> SendPhoneCallNotification(PhoneNotificationPackage phoneNotificationPackage, CancellationToken cancellationToken);
    }

    public class SignalClient : ISignalClient
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SignalClient> _logger;
        private readonly HubConnection _connection;

        private DateTime _expiryTime;
        private TokenResponse _token;

        public event EventHandler<LoggerRefreshEventArgs> OnLoggerChanged;
        public event EventHandler OnRequestRefresh;
        public event EventHandler OnRequestDiscovery;

        public bool IsConnected
        {
            get { return _connection.State == HubConnectionState.Connected; }
        }

        public bool IsDisconnected
        {
            get { return _connection.State == HubConnectionState.Disconnected; }
        }

        public SignalClient(IConfiguration configuration, ILogger<SignalClient> logger)
        {
            var hubUrl = configuration.GetValue<string>("SignalHub");

            _config = configuration;
            _logger = logger;

            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    //options.AccessTokenProvider = () => GetToken();
                })
                .WithAutomaticReconnect()
                .Build();

            _connection.Closed += ex =>
            {
                if (ex != null)
                {
                    _logger.LogError(ex, "SignalR Connection Error");
                }

                return Task.CompletedTask;
            };

            _connection.Reconnecting += ex =>
            {
                if (ex != null)
                {
                    _logger.LogError(ex, "SignalR ReConnection Error");
                }

                return Task.CompletedTask;
            };

            _connection.Reconnected += async a => await RegisterAsApi(CancellationToken.None);

            _connection.On<LoggerRefreshEventArgs>("LoggerChange", s => OnLoggerChanged(this, s));
            _connection.On("RequestRefresh", () => OnRequestRefresh(this, new EventArgs()));
            _connection.On("RequestDiscovery", () => OnRequestDiscovery(this, new EventArgs()));

            _logger.LogInformation("SignalR Client Started");
        }

        private async Task<string> GetToken()
        {
            if (_token == null || _expiryTime < DateTime.UtcNow)
            {
                using var client = new HttpClient();

                var disco = await client.GetDiscoveryDocumentAsync(_config["HardwareApi:ApiPath"]);

                if (disco != null)
                {
                    if (!string.IsNullOrWhiteSpace(disco.Error))
                    {
                        _logger.LogError(disco.Exception, "Discovery Error: {Error}", disco.Error);
                    }
                    else
                    {
                        _token = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
                        {
                            Address = disco.TokenEndpoint,
                            ClientId = "hardware-api",
                            ClientSecret = _config["HardwareApi:ApiKey"],
                            Scope = "hardware-api"
                        });

                        if (_token != null)
                        {
                            _expiryTime = DateTime.UtcNow.AddSeconds(_token.ExpiresIn - 10);
                        }
                    }
                }
            }

            if (_token == null) throw new InvalidOperationException("No access token available");

            return _token.AccessToken;
        }

        public async Task<bool> Connect(CancellationToken cancellationToken)
        {
            try
            {
                await _connection.StartAsync(cancellationToken);
                _logger.LogInformation("SignalR Client Connected");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR Client Connection to {Path} Failed", _config.GetValue<string>("SignalHub"));
                return false;
            }
        }

        public async Task<bool> Disconnect(CancellationToken cancellationToken)
        {
            try
            {
                if (_connection.State != HubConnectionState.Disconnected)
                {
                    await _connection.StopAsync(cancellationToken);
                }

                OnRequestDiscovery = null;

                _logger.LogInformation("SignalR Client Disconnected");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR Client Disconnect Failed");
                return false;
            }
        }

        private async Task<bool> Invoke(string target, CancellationToken cancellationToken)
            => await Invoke(target, null, cancellationToken);

        public async Task<bool> SendPhoneCallNotification(PhoneNotificationPackage phoneNotificationPackage, CancellationToken cancellationToken)
            => await Invoke("SendPhoneAlarmCall", phoneNotificationPackage, cancellationToken);


        private async Task<bool> Invoke(string target, object content, CancellationToken cancellationToken)
        {
            try
            {
                if (_connection == null || _connection.State == HubConnectionState.Disconnected)
                {
                    await Connect(cancellationToken);
                }

                if (_connection.State == HubConnectionState.Connected)
                {
                    if (content != null)
                        await _connection.InvokeAsync(target, content, cancellationToken);
                    else
                        await _connection.InvokeAsync(target, cancellationToken);

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR Client failed to invoke '{Target}'", target);
            }

            return false;
        }

        private async Task<bool> Invoke<T>(string target, object content, CancellationToken cancellationToken)
        {
            try
            {
                if (_connection == null || _connection.State == HubConnectionState.Disconnected)
                {
                    await Connect(cancellationToken);
                }

                if (_connection.State == HubConnectionState.Connected)
                {
                    if (content != null)
                        await _connection.InvokeAsync<T>(target, content, cancellationToken);
                    else
                        await _connection.InvokeAsync<T>(target, cancellationToken);

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR Client failed to invoke '{Target}'", target);
            }

            return false;
        }




        public async void Dispose()
        {
            GC.SuppressFinalize(this);

            if (_connection != null)
            {
                await _connection.DisposeAsync();
            }

            OnRequestDiscovery = null;
        }
    }
}
