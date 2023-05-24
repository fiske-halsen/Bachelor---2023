using EMSuite.PhoneNotification.Services;
using NUnit.Framework;

namespace PhoneNotificationService.Tests.UnitTests
{
    [TestFixture]
    public class AzureCognitiveVoiceProviderUnitTests
    {

        private IAzureCognitiveVoiceProvider _voiceProvider;

        [SetUp]
        public void Setup()
        {
            _voiceProvider = new AzureCogntiveVoiceProvider();
        }

        [TestCase("en", 1, ExpectedResult = "en-US-GuyNeural")]
        [TestCase("en", 2, ExpectedResult = "en-US-AriaNeural")]
        [TestCase("fr", 1, ExpectedResult = "fr-FR-DenisNeural")]
        [TestCase("fr", 2, ExpectedResult = "fr-FR-BrigitteNeural")]
        [TestCase("es", 1, ExpectedResult = "es-ES-AlvaroNeural")]
        [TestCase("es", 2, ExpectedResult = "es-ES-ElviraNeural")]
        [TestCase("pt", 1, ExpectedResult = "pt-PT-DuarteNeural")]
        [TestCase("pt", 2, ExpectedResult = "pt-PT-RaquelNeural")]
        [TestCase("de", 1, ExpectedResult = "de-DE-JohannesNeural")]
        [TestCase("de", 2, ExpectedResult = "de-DE-AmalaNeural")]
        [TestCase("zh", 1, ExpectedResult = "zh-CN-XiaoxuanNeural")]
        [TestCase("zh", 2, ExpectedResult = "zh-CN-XiaochenNeural")]
        [TestCase("ko", 1, ExpectedResult = "ko-KR-InpyoNeural")]
        [TestCase("ko", 2, ExpectedResult = "ko-KR-SunHiNeural")]
        [TestCase("ja", 1, ExpectedResult = "ja-JP-NanjoNeural")]
        [TestCase("ja", 2, ExpectedResult = "ja-JP-MayuNeural")]
        [TestCase("nl", 1, ExpectedResult = "nl-NL-MaartenNeural")]
        [TestCase("nl", 2, ExpectedResult = "nl-NL-ColetteNeural")]
        [TestCase("pl", 1, ExpectedResult = "pl-PL-MarekNeural")]
        [TestCase("pl", 2, ExpectedResult = "pl-PL-ZofiaNeural")]
        [TestCase("it", 1, ExpectedResult = "it-IT-AlessioNeural")]
        [TestCase("it", 2, ExpectedResult = "it-IT-ElsaNeural")]
        [TestCase("ru", 1, ExpectedResult = "ru-RU-DmitriNeural")]
        [TestCase("ru", 2, ExpectedResult = "ru-RU-DariyaNeural")]
        [TestCase("tr", 1, ExpectedResult = "tr-TR-AhmetNeural")]
        [TestCase("tr", 2, ExpectedResult = "tr-TR-EmelNeural")]
        [TestCase("unsupported", 2, ExpectedResult = "en-US-AriaNeural")]
        public string GetAzureCognitiveVoiceByGender_GivenLanguageAndGenderId_ReturnsCorrectVoice(string userLanguage, int voiceGenderId)
        {
            return _voiceProvider.GetAzureCognitiveVoiceByGender(userLanguage, voiceGenderId);
        }

        [TestCase("en", ExpectedResult = "0.7")]
        [TestCase("es", ExpectedResult = "0.8")]
        [TestCase("pt", ExpectedResult = "0.85")]
        [TestCase("de", ExpectedResult = "0.85")]
        [TestCase("zh", ExpectedResult = "0.7")]
        [TestCase("ko", ExpectedResult = "0.75")]
        [TestCase("ja", ExpectedResult = "0.7")]
        [TestCase("nl", ExpectedResult = "0.8")]
        [TestCase("pl", ExpectedResult = "0.85")]
        [TestCase("it", ExpectedResult = "0.75")]
        [TestCase("ru", ExpectedResult = "0.9")]
        [TestCase("tr", ExpectedResult = "0.85")]
        [TestCase("unsupported", ExpectedResult = "en-US")]
        public string GetAzureCognitiveRate_GivenLanguage_ReturnsCorrectRate(string userLanguage)
        {
            return _voiceProvider.GetAzureCognitiveVoiceRate(userLanguage);
        }


        [TestCase("en", ExpectedResult = "en-US")]
        [TestCase("fr", ExpectedResult = "fr-FR")]
        [TestCase("es", ExpectedResult = "es-ES")]
        [TestCase("pt", ExpectedResult = "pt-PT")]
        [TestCase("de", ExpectedResult = "de-DE")]
        [TestCase("zh", ExpectedResult = "zh-CN")]
        [TestCase("ko", ExpectedResult = "ko-KR")]
        [TestCase("ja", ExpectedResult = "ja-JP")]
        [TestCase("nl", ExpectedResult = "nl-NL")]
        [TestCase("pl", ExpectedResult = "pl-PL")]
        [TestCase("it", ExpectedResult = "it-IT")]
        [TestCase("ru", ExpectedResult = "ru-RU")]
        [TestCase("tr", ExpectedResult = "tr-TR")]
        [TestCase("unsupported", ExpectedResult = "en-US")]
        public string GetAzureCognitiveLanguage_GivenLanguage_ReturnsCorrectLanguage(string userLanguage)
        {
            return _voiceProvider.GetAzureCognitiveLanguage(userLanguage);
        }
    }
}
