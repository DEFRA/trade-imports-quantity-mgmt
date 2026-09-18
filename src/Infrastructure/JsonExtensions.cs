using System.Text.Json;
using Infrastructure.Messaging.Extensions;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public static class JsonExtensions
{
    // A plain static field, initialised once by the type initialiser (which the CLR already
    // guards with a lock), rather than a Lazy<T> - a Lazy<T> constructed with isThreadSafe:false
    // uses LazyThreadSafetyMode.None, which isn't safe against concurrent first access and can
    // throw "ValueFactory attempted to access the Value property of this instance" when two
    // threads race to initialise it (observed under parallel test execution in CI).
    private static readonly JsonSerializerOptions jsonSerializerOptions = CreateJsonSerializerOptions();

    public static string ToJson<T>(this T value)
    {
        return JsonSerializer.Serialize(value, jsonSerializerOptions);
    }

    public static T? FromJson<T>(this string value)
        where T : class
    {
        return JsonSerializer.Deserialize<T>(value, jsonSerializerOptions);
    }

    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions();
        options.ConfigureJsonSerializerOptions();
        return options;
    }

    public static void ConfigureJsonSerializerOptions(this JsonSerializerOptions jsonSerializerOptions)
    {
        jsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    }
}
