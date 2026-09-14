using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Amazon.SQS.Model;
using Infrastructure.Messaging.Consuming;
using Infrastructure.Messaging.Exceptions;
using Xunit;

namespace TradeImportsQuantityMgmt.Tests;

public class MessageBodyParserTests
{
    private sealed class TestDto
    {
        public string? Foo { get; set; }
    }

    [Fact]
    public void ParseMessage_ReturnsObject_WhenNotCompressed()
    {
        var dto = new TestDto { Foo = "bar" };
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var message = new Message { Body = json, MessageId = "1" };

        var context = new MessageContext
        {
            Message = message,
            QueueUrl = "q",
            ConsumerType = typeof(object),
        };

        var parsed = context.ParseMessage<TestDto>();

        Assert.NotNull(parsed);
        Assert.Equal("bar", parsed!.Foo);
    }

    [Fact]
    public void ParseMessage_ReturnsObject_WhenCompressed()
    {
        var dto = new TestDto { Foo = "baz" };
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var bytes = Encoding.UTF8.GetBytes(json);

        using var ms = new MemoryStream();
        using (var gzip = new GZipStream(ms, CompressionLevel.Optimal, leaveOpen: true))
        {
            gzip.Write(bytes, 0, bytes.Length);
        }

        var base64 = Convert.ToBase64String(ms.ToArray());

        var message = new Message
        {
            Body = base64,
            MessageId = "1",
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                {
                    "Content-Encoding",
                    new MessageAttributeValue { StringValue = "gzip, base64", DataType = "String" }
                },
            },
        };

        var context = new MessageContext
        {
            Message = message,
            QueueUrl = "q",
            ConsumerType = typeof(object),
        };

        var parsed = context.ParseMessage<TestDto>();

        Assert.NotNull(parsed);
        Assert.Equal("baz", parsed!.Foo);
    }

    [Fact]
    public void ParseMessage_ThrowsInvalidMessageCompressionException_WhenCompressedButInvalid()
    {
        var message = new Message
        {
            Body = "not-base64",
            MessageId = "1",
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                {
                    "Content-Encoding",
                    new MessageAttributeValue { StringValue = "gzip, base64", DataType = "String" }
                },
            },
        };

        var context = new MessageContext
        {
            Message = message,
            QueueUrl = "q",
            ConsumerType = typeof(object),
        };

        Assert.Throws<InvalidMessageCompressionException>(() => context.ParseMessage<TestDto>());
    }
}
