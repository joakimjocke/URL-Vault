using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace UrlVault.Services;

public class TitleFetcherService
{
    private static readonly HttpClient HttpClient;

    static TitleFetcherService()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        HttpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(8)
        };
        HttpClient.DefaultRequestHeaders.TryAddWithoutValidation(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        );
    }

    public async Task<string> FetchTitleAsync(string url)
    {
        try
        {
            using var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var buffer = new byte[256 * 1024];
            var bytesRead = await stream.ReadAsync(buffer).ConfigureAwait(false);
            var content = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);

            var match = Regex.Match(content, @"<title[^>]*>(.*?)</title>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (match.Success)
                return WebUtility.HtmlDecode(match.Groups[1].Value.Trim());

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
