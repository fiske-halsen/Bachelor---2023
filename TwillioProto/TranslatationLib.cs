using Newtonsoft.Json.Linq;
using System.Text;

namespace TwillioProto
{
    public static class TranslatationLib
    {

        private static readonly string subscriptionKey = "7de2c1fecaf441b0a340ebdf24a36b36";
        private static readonly string endpoint = "https://cs-texttospeech.cognitiveservices.azure.com/translator/text/v3.0/";

        public static async Task<string> TranslateText(string inputText, string targetLanguage, string sourceLanguage = null)
        {
            string route = $"translate?api-version=3.0&from={(sourceLanguage == null ? "" : sourceLanguage)}&to={targetLanguage}";

            string apiUrl = endpoint + route;

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(apiUrl);
                request.Content = new StringContent("[{\"Text\":\"" + inputText + "\"}]", Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                request.Headers.Add("Ocp-Apim-Subscription-Region", "westeurope");

                HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                string result = await response.Content.ReadAsStringAsync();
                JArray jsonResponse = JArray.Parse(result);

                string translatedText =
                    jsonResponse[0]["translations"][0]["text"].ToString();
                return translatedText;
            }
        }
    }
}