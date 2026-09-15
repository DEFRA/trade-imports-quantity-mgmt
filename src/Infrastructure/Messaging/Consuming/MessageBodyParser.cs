using System.IO.Compression;
using System.Text;
using Infrastructure.Messaging.Exceptions;

namespace Infrastructure.Messaging.Consuming;

public static class MessageBodyParser
{
    private const string CompressedHeader = "Content-Encoding";

    public static T? ParseMessage<T>(this MessageContext context)
        where T : class
    {
        var body = context.Body ?? string.Empty;

        var header = context.GetHeader(CompressedHeader);
        if (!string.IsNullOrWhiteSpace(header))
        {
            var normalized = header.Trim().ToLowerInvariant();
            // Publisher sets: "gzip, base64"
            if (normalized.Contains("gzip") && normalized.Contains("base64"))
            {
                try
                {
                    var bytes = Convert.FromBase64String(body);
                    using var ms = new MemoryStream(bytes);
                    using var gzip = new GZipStream(ms, CompressionMode.Decompress);
                    using var sr = new StreamReader(gzip, Encoding.UTF8);
                    body = sr.ReadToEnd();
                }
                catch (Exception ex)
                {
                    // Invalid compressed payload - surface a clear exception for callers to handle (e.g. DLQ)
                    throw new InvalidMessageCompressionException(
                        $"Failed to decompress message body as gzip/base64 for message Id {context.MessageId}",
                        ex
                    );
                }
            }
        }

        return body.FromJson<T>();
    }
}
