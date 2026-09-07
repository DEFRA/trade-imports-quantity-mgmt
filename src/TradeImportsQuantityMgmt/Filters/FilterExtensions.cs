namespace TradeImportsQuantityMgmt.Filters;

public static class FilterExtensions
{
    public static RouteHandlerBuilder Validates<T>(this RouteHandlerBuilder builder)
        where T : class
    {
        builder.AddEndpointFilter((context, next) => new ValidationFilter<T>().InvokeAsync(context, next));

        return builder;
    }
}
