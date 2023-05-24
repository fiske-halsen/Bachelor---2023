using EMSuite.Common.PhoneNotification;
using EMSuite.PhoneNotification.Exceptions;
using EMSuite.PhoneNotification.Models;
using System.Diagnostics;

namespace EMSuite.PhoneNotification.Services
{
    public interface IPhoneProcessor
    {
        Task<(int callCount, List<DateTime> callTimestamps)> CallPhones(PhoneNotificationPackage phoneNotificationPackage, CancellationToken cancellationToken);
        Task<(int callCount, List<DateTime> callTimestamps)> CallPhones(BatchNotificationPackage batchNotificationPackage, CancellationToken cancellationToken);
    }

    public class PhoneProcessor : IPhoneProcessor
    {
        private readonly IAzureSpeechService _speechService;
        private readonly IAzureBlobStorageService _blobStorageService;
        private readonly ITwillioService _twillioService;
        private readonly ITranslatorService _translatorService;
        private readonly INotificationLogService _notificationLogService;
        private readonly IDelayProvider _delayProvider;
        private readonly IAzureCognitiveVoiceProvider _azureCognitiveVoiceProvider;

        public PhoneProcessor(
            IAzureSpeechService speechService,
            IAzureBlobStorageService blobStorageService,
            ITwillioService twillioService,
            ITranslatorService translatorService,
            INotificationLogService notificationRepository,
            IDelayProvider delayProvider,
            IAzureCognitiveVoiceProvider azureCogntiveVoiceProvider
            )
        {
            _speechService = speechService;
            _blobStorageService = blobStorageService;
            _twillioService = twillioService;
            _translatorService = translatorService;
            _notificationLogService = notificationRepository;
            _delayProvider = delayProvider;
            _azureCognitiveVoiceProvider = azureCogntiveVoiceProvider;
        }

        public async Task<(int callCount, List<DateTime> callTimestamps)> CallPhones(PhoneNotificationPackage phoneNotificationPackage, CancellationToken cancellationToken)
        {
            int callCount = 0;
            var baseText = phoneNotificationPackage.BaseText;
            var baseSystemLanguage = phoneNotificationPackage.SystemBaseLanguage;

            var callTimestamps = new List<DateTime>();

            foreach (var user in phoneNotificationPackage.PhoneContacts)
            {
                try
                {
                    string translatedText = baseText;

                    if (!user.Language.Equals(baseSystemLanguage))
                    {
                        translatedText = await _translatorService.TranslateText(baseText, user.Language, phoneNotificationPackage.SystemBaseLanguage);
                    }

                    var azureCognitiveLanguage = _azureCognitiveVoiceProvider.GetAzureCognitiveLanguage(user.Language);
                    var azureCognitiveVoice = _azureCognitiveVoiceProvider.GetAzureCognitiveVoiceByGender(user.Language, phoneNotificationPackage.GenderId); 
                    var azureCognitiveVoiceRate = _azureCognitiveVoiceProvider.GetAzureCognitiveVoiceRate(user.Language);

                    var textToSpeechConfiguration = new GenerateSpeechConfiguration
                    {
                        Language = azureCognitiveLanguage,
                        VoiceName = azureCognitiveVoice,
                        Text = translatedText,
                        Pitch = "-0st",
                        Rate = azureCognitiveVoiceRate,
                        Volume = "medium"
                    };

                    var memoryStream = await _speechService.GenerateTextToSpeechAudio(textToSpeechConfiguration);
                    var mp3Url = await _blobStorageService.UploadAudioToAzureBlob(memoryStream, $"{user.UserName}-{phoneNotificationPackage.AlarmId}-{DateTime.Now}.mp3");
                    var callResource = _twillioService.MakeCall(mp3Url, user.PhoneNumber);
               
                    if (callResource != null)
                    {
                        var phoneCallTimeStamp = callResource.PhoneCallTimeStamp;

                        await _notificationLogService.InsertNoticationLog(
                            new NotificationLog
                            {
                                UserId = user.UserId,
                                BatchAlarmId = phoneNotificationPackage.AlarmId,
                                CallId = callResource.Sid,
                                AlarmMessage = translatedText,
                                GenderId = phoneNotificationPackage.GenderId,
                                RoundRobinInterval = phoneNotificationPackage.RoundRobinInterval,
                                AzureBlobUrl = mp3Url,
                                PhoneCallTimeStamp = phoneCallTimeStamp
                            });

                        callTimestamps.Add(phoneCallTimeStamp);
                    }

                    await _delayProvider.Delay(TimeSpan.FromSeconds(phoneNotificationPackage.RoundRobinInterval / 10000000), cancellationToken);

                    callCount++;
                }
                catch (TaskCanceledException ex)
                {
                    // Log or output the exception details to help identify the cause of the cancellation
                    // _logger.LogError(ex, "A task was canceled in CallPhones method: {Message}", ex.Message);
                    var test = ex;
                }
               
                catch (CustomException ex)
                {
                    // _logger.LogError(ex, "An error occurred in CallPhones method: {Message}", ex.Message);
                }
            }

            return (callCount, callTimestamps);
        }

        public async Task<(int callCount, List<DateTime> callTimestamps)> CallPhones(BatchNotificationPackage batchNotificationPackage, CancellationToken cancellationToken)
        {
            int callCount = 0;
            var callTimestamps = new List<DateTime>();

            foreach (var failedPhoneContact in batchNotificationPackage.PhoneContacts)
            {
                try
                {
                    string alarmMessage = failedPhoneContact.AlarmMessage;

                    var callResource = _twillioService.MakeCall(failedPhoneContact.AzureBlobUrl, failedPhoneContact.PhoneNumber);

                    if (callResource != null)
                    {
                        var phoneCallTimeStamp = callResource.PhoneCallTimeStamp;

                        await _notificationLogService.InsertNoticationLog(
                            new NotificationLog
                            {
                                UserId = failedPhoneContact.UserId,
                                BatchAlarmId = batchNotificationPackage.BatchAlarmId,
                                CallId = callResource.Sid,
                                AlarmMessage = alarmMessage,
                                GenderId = batchNotificationPackage.GenderId,
                                RoundRobinInterval = batchNotificationPackage.RoundRobinInterval,
                                PhoneCallTimeStamp = phoneCallTimeStamp
                            });

                        callTimestamps.Add(phoneCallTimeStamp);
                    }

                    await _delayProvider.Delay(TimeSpan.FromSeconds(batchNotificationPackage.RoundRobinInterval / 10000000), CancellationToken.None);

                    callCount++;
                }
                catch (CustomException ex)
                {
                    // _logger.LogError(ex, "An error occurred in ReCallFailedBatches method: {Message}", ex.Message);
                }
            }

            return (callCount, callTimestamps);
        }
    }
}
