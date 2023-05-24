using Microsoft.Extensions.Localization;
using System.Collections.Concurrent;

namespace EMSuite.Hardware.Api.Mocked.Services
{
    public class HardwareStatusService
    {
        public class HardwareStatusService
        {
            private readonly ISignalClient _signalClient;
            private readonly IServiceScope _scope;
            private readonly ILogger<HardwareStatusService> _logger;
            private readonly IStringLocalizer<HardwareStatusService> _localizer;
            private readonly IMaintenanceService _maintenance;

            private IHardwareService Context => _scope.ServiceProvider.GetRequiredService<IHardwareService>();
            private IMessageService MessageQueue => _scope.ServiceProvider.GetRequiredService<IMessageService>();

            public ConcurrentDictionary<uint, ServiceAccessPoint> AccessPoints { get; private set; }

            public ConcurrentDictionary<uint, LoggerRequest> LoggerRequests { get; private set; }

            public HardwareStatusService(
                ISignalClient signalClient,
                IServiceProvider services,
                IMaintenanceService maintenance,
                IStringLocalizer<HardwareStatusService> localizer,
                ILogger<HardwareStatusService> logger)
            {
                AccessPoints = new ConcurrentDictionary<uint, ServiceAccessPoint>();
                LoggerRequests = new ConcurrentDictionary<uint, LoggerRequest>();

                _signalClient = signalClient;
                _scope = services.CreateScope();
                _logger = logger;
                _localizer = localizer;
                _maintenance = maintenance;
            }
            public async Task<bool> StartAlarm(uint logger, LoggerPort port, Channel channel, Threshold threshold, Reading reading)
        {
            if (await Context.StartAlarm(
                channel.LoggerChannelId,
                channel.SensorChannelId,
                threshold,
                reading))
            {
                await AlarmStateChange(
                    channel,
                    reading,
                    AlarmStatus.Active);

                await SendAlarmMessages(logger, port, channel, threshold);

                return true;
            }
            return false;
        }

        private async Task SendAlarmMessages(uint logger, LoggerPort port, Channel channel, Threshold threshold)
        {
            List<AlarmContact> contacts = await Context.GetNotificationUsers(threshold.Id, channel.LoggerChannelId);
            string source = $"{logger}-{channel.Number + 1 + (port.PortNumber * 4)}";
            ChannelLocation channelLocation = await Context.GetChannelSiteZoneInfo(channel.LoggerChannelId);

            var tz = await Context.GetTimeZone(logger);
            var time = tz.GetDisplayDate(DateTime.UtcNow);

            await QueueEmailMessages(logger, source, time, contacts, channelLocation, channel, threshold, tz);
            await InitatePhoneCall(time, channel, threshold);

            var smsDevices = await MessageQueue.GetThresholdHardwareSmsDevices(threshold.Id);

            if (smsDevices.Any())
            {
                await QueueSmsMessages(logger, source, time, contacts, channelLocation, channel, threshold, smsDevices);
            }
        }

        private async Task InitatePhoneCall(string sentAt, Channel channel, Threshold threshold)
        {
            List<AlarmContact> contacts = await Context.GetNotificationUsers(threshold.Id, channel.LoggerChannelId);

            if (contacts == null || !contacts.Any()) return;

            ChannelLocation channelLocation = await Context.GetChannelSiteZoneInfo(channel.LoggerChannelId);

            var baseText = $"{_localizer["PhoneAlarmInitializer"].Value}";

            baseText += threshold.Type switch
            {
                AlarmType.ElapsedTime => $"{string.Format(_localizer["PhoneAlarmDescription"].Value, "Elapsed time", channelLocation.ChannelName)}",
                AlarmType.RateOfChange => $"{string.Format(_localizer["PhoneAlarmDescription"].Value, "Rate of change", channelLocation.ChannelName)}",
                AlarmType.FallingLimit => $"{string.Format(_localizer["PhoneAlarmDescription"].Value, "Falling limit", channelLocation.ChannelName)}",
                AlarmType.RisingLimit => $"{string.Format(_localizer["PhoneAlarmDescription"].Value, "Rising limit", channelLocation.ChannelName)}",
                _ => null,
            };

            if (channelLocation != null && channelLocation.LoggerChannelID > 0)
            {
                baseText += $" {string.Format(_localizer["PhoneAlarmLocation"].Value, channelLocation.SiteName, channelLocation.ZoneName)}";
            }

            baseText += string.Format(_localizer["PhoneAlarmTime"].Value, sentAt);



            var phoneNotificationPackage = new PhoneNotificationPackage
            {
                BaseText = baseText,
                AlarmId = contacts.First().AlarmId,
                RoundRobinInterval = contacts.First().RoundRobinInterval,
                GenderId = contacts.First().GenderID,
                SystemBaseLanguage = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                PhoneContacts = contacts.Select(a => new PhoneContact
                {
                    UserId = a.UserId,
                    PhoneNumber = a.PhoneNumber,
                    UserName = a.UserName,
                    Language = a.LanguageIsoCode
                }).ToList()
            };

            await _signalClient.SendPhoneCallNotification(phoneNotificationPackage, CancellationToken.None);
        }


    }
}
