using System.ComponentModel.DataAnnotations;

namespace TradeImportsQuantityMgmt.Config
{
    public class ResourceEventConsumerOptions
    {
        public const string SectionName = "ResourceEventsConsumer";

        [Required]
        public required string ResourceEventsQueueUrl { get; init; }
    }
}
