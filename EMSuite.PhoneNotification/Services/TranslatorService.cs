using EMSuite.PhoneNotification.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace EMSuite.PhoneNotification.Services
{
    public interface ITranslatorService
    {
        Task<string> TranslateText(string inputText, string targetLanguage, string sourceLanguage = "en");
    }

    public class TranslatorService : ITranslatorService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public TranslatorService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> TranslateText(string inputText, string targetLanguage, string sourceLanguage = null)
        {
            string route = $"translate?api-version=3.0&from={(sourceLanguage == null ? "" : sourceLanguage)}&to={targetLanguage}";
            string apiUrl = _configuration["AzureCogniveService:TranslatorEndpoint"] + route;

            using (var client = _httpClientFactory.CreateClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(apiUrl);
                request.Content = new StringContent("[{\"Text\":\"" + inputText + "\"}]", Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", _configuration["AzureCogniveService:ApiKey"]);
                request.Headers.Add("Ocp-Apim-Subscription-Region", _configuration["AzureCogniveService:Region"]);

                try
                {
                    HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new CustomException($"Translation failed. Status code: {response.StatusCode}");
                    }

                    string result = await response.Content.ReadAsStringAsync();
                    JArray jsonResponse = JArray.Parse(result);

                    string translatedText = jsonResponse[0]["translations"][0]["text"].ToString();
                    return translatedText;
                }
                catch (HttpRequestException ex)
                {
                    throw new CustomException("Translation failed due to a request error.", ex);
                }
                catch (JsonReaderException ex)
                {
                    throw new CustomException("Translation failed due to an error parsing the response.", ex);
                }
            }
        }
    }
}

