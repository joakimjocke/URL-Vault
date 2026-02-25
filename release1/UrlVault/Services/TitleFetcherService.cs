using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace UrlVault.Services;

public class TitleFetcherService
{
    private static readonly HttpClient HttpClient;
    private static readonly Regex TitleRegex = new(
        @"<title[^>]*>(.*?)</title>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

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
        const int maxCharsToScan = 1024 * 1024; // 1 MB of text is enough for title discovery.
        try
        {
            using var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var encoding = GetResponseEncoding(response) ?? Encoding.UTF8;
            using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true);
            var contentBuilder = new StringBuilder(capacity: 16 * 1024);
            var buffer = new char[4096];

            while (contentBuilder.Length < maxCharsToScan)
            {
                var readCount = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                if (readCount <= 0)
                {
                    break;
                }

                var charsToAppend = Math.Min(readCount, maxCharsToScan - contentBuilder.Length);
                contentBuilder.Append(buffer, 0, charsToAppend);

                var content = contentBuilder.ToString();
                var match = TitleRegex.Match(content);
                if (match.Success)
                {
                    return WebUtility.HtmlDecode(match.Groups[1].Value.Trim());
                }
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static Encoding? GetResponseEncoding(HttpResponseMessage response)
    {
        try
        {
            var charset = response.Content.Headers.ContentType?.CharSet;
            if (string.IsNullOrWhiteSpace(charset))
            {
                return null;
            }

            return Encoding.GetEncoding(charset.Trim('\"', '\''));
        }
        catch
        {
            return null;
        }
    }
}
