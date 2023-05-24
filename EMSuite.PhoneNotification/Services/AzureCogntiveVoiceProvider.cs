namespace EMSuite.PhoneNotification.Services
{
    public interface IAzureCognitiveVoiceProvider
    {
        string GetAzureCognitiveVoiceByGender(string userLanguage, int voiceGenderId);
        string GetAzureCognitiveVoiceRate(string userLanguage);
        string GetAzureCognitiveLanguage(string userLanguage);
    }

    public class AzureCogntiveVoiceProvider : IAzureCognitiveVoiceProvider
    {
        public string GetAzureCognitiveVoiceByGender(string userLanguage, int voiceGenderId)
        {
            return voiceGenderId switch
            {
                1 => GetAzureCognitiveMaleVoice(userLanguage),
                2 => GetAzureCognitiveFemaleVoice(userLanguage),
                _ => GetAzureCognitiveFemaleVoice(userLanguage),
            };
        }

        public string GetAzureCognitiveVoiceRate(string userLanguage)
        {
            return userLanguage switch
            {
                "en" => "0.7",
                "fr" => "0.8",
                "es" => "0.8",
                "pt" => "0.85",
                "de" => "0.85",
                "zh" => "0.7",
                "ko" => "0.75",
                "ja" => "0.7",
                "nl" => "0.8",
                "pl" => "0.85",
                "it" => "0.75",
                "ru" => "0.9",
                "tr" => "0.85",
                _ => "en-US",
            };
        }

        public string GetAzureCognitiveLanguage(string userLanguage)
        {
            return userLanguage switch
            {
                "en" => "en-US",
                "fr" => "fr-FR",
                "es" => "es-ES",
                "pt" => "pt-PT",
                "de" => "de-DE",
                "zh" => "zh-CN",
                "ko" => "ko-KR",
                "ja" => "ja-JP",
                "nl" => "nl-NL",
                "pl" => "pl-PL",
                "it" => "it-IT",
                "ru" => "ru-RU",
                "tr" => "tr-TR",
                _ => "en-US",
            };
        }

        public string GetAzureCognitiveFemaleVoice(string userLanguage)
        {
            return userLanguage switch
            {
                "en" => "en-US-AriaNeural",
                "fr" => "fr-FR-BrigitteNeural",
                "es" => "es-ES-ElviraNeural",
                "pt" => "pt-PT-RaquelNeural",
                "de" => "de-DE-AmalaNeural",
                "zh" => "zh-CN-XiaochenNeural",
                "ko" => "ko-KR-SunHiNeural",
                "ja" => "ja-JP-MayuNeural",
                "nl" => "nl-NL-ColetteNeural",
                "pl" => "pl-PL-ZofiaNeural",
                "it" => "it-IT-ElsaNeural",
                "ru" => "ru-RU-DariyaNeural",
                "tr" => "tr-TR-EmelNeural",
                _ => "en-US-AriaNeural",
            };
        }

        public string GetAzureCognitiveMaleVoice(string userLanguage)
        {
            return userLanguage switch
            {
                "en" => "en-US-GuyNeural",
                "fr" => "fr-FR-DenisNeural",
                "es" => "es-ES-AlvaroNeural",
                "pt" => "pt-PT-DuarteNeural",
                "de" => "de-DE-JohannesNeural",
                "zh" => "zh-CN-XiaoxuanNeural",
                "ko" => "ko-KR-InpyoNeural",
                "ja" => "ja-JP-NanjoNeural",
                "nl" => "nl-NL-MaartenNeural",
                "pl" => "pl-PL-MarekNeural",
                "it" => "it-IT-AlessioNeural",
                "ru" => "ru-RU-DmitriNeural",
                "tr" => "tr-TR-AhmetNeural",
                _ => "en-US-GuyNeural",
            };
        }
    }
}
