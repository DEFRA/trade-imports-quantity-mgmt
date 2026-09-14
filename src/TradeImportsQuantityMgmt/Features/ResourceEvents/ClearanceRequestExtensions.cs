using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using MongoDB.Bson.IO;

namespace TradeImportsQuantityMgmt.Features.ResourceEvents
{
    public static partial class ClearanceRequestExtensions
    {
        public static IEnumerable<string> GetTracesCheds(this ClearanceRequest? clearanceRequest)
        {
            if (clearanceRequest?.Commodities != null)
            {
                var returnedCheds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var commodity in clearanceRequest.Commodities)
                {
                    var ched = commodity.GetChedNumber();
                    if (ched != null && !returnedCheds.Contains(ched))
                    {
                        returnedCheds.Add(ched);
                        yield return ched;
                    }
                }
            }
        }
    }
}
