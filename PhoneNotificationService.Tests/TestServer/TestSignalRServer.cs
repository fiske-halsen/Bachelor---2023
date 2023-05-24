using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PhoneNotificationService.Tests.TestServer
{
    public class TestSignalRServer : IDisposable
    {
        private readonly IWebHost _webHost;

        public TestSignalRServer(string url)
        {
            _webHost = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.ListenAnyIP(5000, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                    });
                })
                .UseUrls(url)
                .ConfigureServices(services =>
                {
                    services.AddSignalR();
                    services.AddCors(pol => pol.AddPolicy("signalRPolicy", builder =>
                    {
                        builder.AllowAnyOrigin()
                               .AllowAnyHeader()
                               .AllowAnyMethod();
                    }));
                    services.AddAuthorization(options =>
                    {
                        options.AddPolicy("SignalR", policy =>
                        {
                            policy.RequireAssertion(context => true);
                        });

                        options.DefaultPolicy = new AuthorizationPolicyBuilder()
                            .RequireAssertion(context => true)
                            .Build();
                    });
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseCors("signalRPolicy");
                    app.UseAuthorization(); // add this line
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapHub<EMSuiteTestHub>("/emsuite")
                            .RequireAuthorization("SignalR");
                    });
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .Build();

            _webHost.Start();
        }

        public string Url => _webHost.ServerFeatures.Get<IServerAddressesFeature>().Addresses.FirstOrDefault();

        public void Dispose()
        {
            _webHost?.Dispose();
        }
    }
}
