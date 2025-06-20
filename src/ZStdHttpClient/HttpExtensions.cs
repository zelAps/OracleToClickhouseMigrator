
using System.Net.Http.Headers;
namespace ZstdHttpClient;

/// <summary>
/// Методы расширения для работы с сжатым HTTP ответом.
/// </summary>
public static class HttpExtensions
{
    private static readonly StringWithQualityHeaderValue ZStdHeader = new StringWithQualityHeaderValue("zstd", 1);

    /// <summary>
    /// Выполняет GET запрос с поддержкой zstd-сжатия.
    /// </summary>
    public static async Task<HttpResponseMessage> GetAsync(
        this HttpClient client,
        string url,
        CompressionType cp
        )
    {
        if (cp == CompressionType.Zstd)
        {
            client.DefaultRequestHeaders.AcceptEncoding.Add(ZStdHeader);
        }

        var response = await client.GetAsync(url).ConfigureAwait(false);

        if (cp == CompressionType.Zstd)
        {
            if (response.Content.Headers.ContentEncoding.LastOrDefault() == "zstd")
            {
                response.Content = new ZStdHttpContent(response.Content);
            }
        }

        return response;
    }
}
