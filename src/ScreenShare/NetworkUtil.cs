using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ScreenShare
{
    public static class NetworkUtil
    {
        public static async Task<string> GetAsync(string url)
        {
            using var wc = new WebClient();
            return await wc.DownloadStringTaskAsync(url);
        }

        public static Task<string> PostJsonAsync(string url, string body) => RequestAsync(url, body, "application/json");
        public static async Task<string> RequestAsync(string requestUri, string body, string contentType)
        {
            using var httpMessageHandler = new HttpClientHandler();
            using var httpClient = new HttpClient(httpMessageHandler);
            //httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));

            using var httpContent = new StringContent(body, Encoding.UTF8, contentType);
            using var httpResponseMessage = await httpClient.PostAsync(requestUri, httpContent);

            httpResponseMessage.EnsureSuccessStatusCode();
            return await httpResponseMessage.Content.ReadAsStringAsync();
        }
    }
}
