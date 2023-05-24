using EMSuite.Common.PhoneNotification;
using IdentityModel.Client;
using Microsoft.AspNetCore.SignalR.Client;

namespace EMSuite.PhoneNotification.Services
{
    public interface ISignalRClient : IDisposable
    {
        bool IsConnected { get; }
        bool IsDisconnected { get; }
     
        Task<bool> Connect(CancellationToken cancellationToken);
        Task<bool> Disconnect(CancellationToken cancellationToken);
    }

    public class SignalRClient : ISignalRClient
    {
        private readonly IPhoneProcessor _phoneProcessor;
        private readonly IConfiguration _config;
        private readonly HubConnection _connection;

        private DateTime _expiryTime;
        private TokenResponse _token;


        public SignalRClient(IConfiguration configuration, IPhoneProcessor phoneProcessor)
        {
            _config = configuration;
            _phoneProcessor = phoneProcessor;
            var hubUrl = configuration.GetValue<string>("SignalHub");

            _connection = new HubConnectionBuilder()
               .WithUrl(hubUrl, options =>
               {
                  options.AccessTokenProvider = () => GetToken();
               })
               .WithAutomaticReconnect()
               .Build();
        }

        private async Task<bool> RegisterPhoneNotificationService()
            => await Invoke("RegisterPhoneNotificationService", null, CancellationToken.None);

        public bool IsConnected
        {
            get { return _connection.State == HubConnectionState.Connected; }
        }

        public bool IsDisconnected
        {
            get { return _connection.State == HubConnectionState.Disconnected; }
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
            }

            return false;
        }

        public async Task<bool> Connect(CancellationToken cancellationToken)
        {
            try
            {
                _connection.On<PhoneNotificationPackage>("ReceivePhoneAlarmPackage", async (phonePackage) =>
                {
                    await _phoneProcessor.CallPhones(phonePackage, cancellationToken);
                });

                await _connection.StartAsync(cancellationToken);
                await RegisterPhoneNotificationService();
                return true;
            }
            catch (Exception ex)
            {
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

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async void Dispose()
        {
            GC.SuppressFinalize(this);

            if (_connection != null)
            {
                await _connection.DisposeAsync();
            }

        }
    }
}
