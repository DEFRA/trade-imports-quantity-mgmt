using System.Diagnostics.CodeAnalysis;
using Amazon.SQS.Model;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Infrastructure.Messaging.Consuming;

namespace Infrastructure.Messaging.Extensions;

[ExcludeFromCodeCoverage]
public static class MessageExtensions
{
    public static bool IsFinalisation(this MessageContext messageContext)
    {
        return messageContext.GetHeader(nameof(ResourceEvent<>.SubResourceType)) == nameof(Finalisation);
    }
}
