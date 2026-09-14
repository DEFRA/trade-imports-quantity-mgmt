using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using MongoDB.Bson.IO;

namespace TradeImportsQuantityMgmt.Features.ResourceEvents
{
    public static partial class CommodityExtensions
    {
        public static string? GetChedNumber(this Commodity commodity)
        {
            return commodity?.Documents?.SingleOrDefault(document => document.IsTracesChed())?.DocumentReference?.Value;
        }

        public static bool IsTracesChed(this ImportDocument? importDocument)
        {
            return (
                    importDocument?.DocumentCode
                    is "9115"
                        or "C085"
                        or "C640"
                        or "C633"
                        or "C678"
                        or "N002"
                        or "N851"
                        or "N852"
                        or "N853"
                ) && ValidTracesChedReference().IsMatch(importDocument?.DocumentReference?.Value ?? string.Empty);
        }

        [GeneratedRegex("^(?:CHEDA|CHED|CHEDP|CHEDPP)\\.[A-Z]{2}\\.\\d{4}\\.\\d{7,8}$")]
        private static partial Regex ValidTracesChedReference();
    }
}
