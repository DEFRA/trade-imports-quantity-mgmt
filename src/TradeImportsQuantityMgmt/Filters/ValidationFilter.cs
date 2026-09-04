using FluentValidation;

namespace TradeImportsQuantityMgmt.Filters;

public sealed class ValidationFilter<T> : IEndpointFilter
    where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<T>().FirstOrDefault();

        if (request is null)
            return await next(context);

        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();

        if (validator is null)
            return await next(context);

        var validation = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);

        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        return await next(context);
    }
}
