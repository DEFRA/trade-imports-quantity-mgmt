using System.Text.Json;
using Infrastructure.Messaging.Extensions;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public static class JsonExtensions
{
    private static readonly Lazy<JsonSerializerOptions> jsonSerializerOptions = new(
        () =>
        {
            var options = new JsonSerializerOptions();
            options.ConfigureJsonSerializerOptions();
            return options;
        },
        false
    );

    public static string ToJson<T>(this T value)
    {
        return JsonSerializer.Serialize(value, jsonSerializerOptions.Value);
    }

    public static T? FromJson<T>(this string value)
        where T : class
    {
        return JsonSerializer.Deserialize<T>(value, jsonSerializerOptions.Value);
    }

    public static void ConfigureJsonSerializerOptions(this JsonSerializerOptions jsonSerializerOptions)
    {
        jsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    }
}
