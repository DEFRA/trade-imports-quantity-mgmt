namespace TradeImportsQuantityMgmt.Contract;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MediaTypeAttribute(string mediaType) : Attribute
{
    public string MediaType { get; } = mediaType;

    public static string For<T>() =>
        typeof(T).GetCustomAttributes(typeof(MediaTypeAttribute), false) is [MediaTypeAttribute attr, ..]
            ? attr.MediaType
            : "application/json";
}
