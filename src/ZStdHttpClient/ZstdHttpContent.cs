using ZstdSharp;

namespace ZstdHttpClient;
/// <summary>
/// HttpContent, который распаковывает данные в формате zstd.
/// </summary>
public sealed class ZStdHttpContent : DecompressedContent
{
    public ZStdHttpContent(HttpContent originalContent) : base(originalContent) { }

    protected override Stream GetDecompressedStream(Stream originalStream)
        => new DecompressionStream(originalStream);
}
